#region

// Hapa Project, CC
// Created @2012 09 18 14:34
// Last Updated  by Huang, Jien @2012 09 18 14:34

using System.Linq;

#region

using System.Activities;
using System.Activities.Presentation.PropertyEditing;
using System.ComponentModel;
using System.Drawing;
using System.Xml.Linq;
using AutoX.Basic;
using System;
using AutoX.DB;

#endregion

#endregion

namespace AutoX.Activities.AutoActivities
{
    [ToolboxBitmap(typeof (CallTestScreenDesigner), "TestScreen.png")]
    [Designer(typeof (CallTestScreenDesigner))]
    public sealed class CallTestScreenActivity : AutomationActivity, IPassData
    {
        // Define an activity input argument of type string
        private CompletionCallback _onChildComplete;
        private string _steps = "<Steps />";
        private string _testScreenName;
        private string _userData = "";


        [DisplayName(@"Test Screen Name")]
        public string TestSreenName
        {
            get { return _testScreenName; }
            set
            {
                _testScreenName = value;
                DisplayName = "Call Test Screen: " + _testScreenName;
                NotifyPropertyChanged("TestScreenName");
                NotifyPropertyChanged("DisplayName");
            }
        }

        [DisplayName(@"On Error")]
        public OnError ErrorLevel { get; set; }

        [Browsable(false)]
        public string GUID { get; set; }

        [Browsable(false)]
        public string TestSreenId { get; set; }

        [DisplayName(@"Test Steps")]
        [Editor(typeof (StepsEditor), typeof (DialogPropertyValueEditor))]
        public string Steps
        {
            get { return _steps; }
            set { _steps = value; }
        }

        [DisplayName(@"User Data")]
        [Editor(typeof (UserDataEditor), typeof (DialogPropertyValueEditor))]
        public string UserData
        {
            get { return _userData; }
            set
            {
                _userData = value;
                //NotifyPropertyChanged("UserData");
            }
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            //call base.CacheMetadata to add the Activities and Variables to this activity's metadata
            base.CacheMetadata(metadata);

            string errorMessage = AutomationActivityValidation();
            if (!string.IsNullOrEmpty(errorMessage))
                metadata.AddValidationError(errorMessage);

        }

        public override string AutomationActivityValidation()
        {
            //add validation to this activity:every enabled steps must have action
            var stepsX = XElement.Parse(_steps);
            return (from step in stepsX.Descendants("Step") let enabled = step.GetAttributeValue("Enable") where !string.IsNullOrEmpty(enabled) select step.GetAttributeValue("Action")).Any(action => string.IsNullOrEmpty(action)) ? "Enabled step must has an action" : null;
        }

        #region IPassData Members

        public void PassData(string instanceId, string outerData)
        {
            InstanceId = instanceId;
            UserData = Utilities.PassData(outerData, UserData, OwnDataFirst);
        }

        //        protected override void CacheMetadata(NativeActivityMetadata metadata)
        //        {
        //            base.CacheMetadata(metadata);
        //            metadata.AddImplementationVariable(result);
        //        }

        #endregion

        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        protected override void Execute(NativeActivityContext context)
        {
            Result = new XElement(Constants.RESULT);
            ResultId = ResultId ?? Guid.NewGuid().ToString();
            
            SetResult();
            SetVariablesBeforeRunning(context);
            InternalExecute(context, null);
            
        }

        private void InternalExecute(NativeActivityContext context, ActivityInstance instance)
        {
            if (_onChildComplete == null)
            {
                _onChildComplete = InternalExecute;
            }
            Log.Info("in CallTestScreenActivity internalexecute");
            var steps = GetSteps(context);
            Host.SetCommand(steps);
            var rElement = Host.GetResult();
            
            Log.Info("CallTestScreen Receive result from Host:\n"+rElement);
            foreach (var stepElement in rElement.Descendants())
            {
                stepElement.SetAttributeValue(Constants.PARENT_ID, ResultId);
                var ret = stepElement.GetAttributeValue(Constants.RESULT);
                if (!string.IsNullOrEmpty(ret))
                {
                    stepElement.SetAttributeValue("Original", ret);
                    stepElement.SetAttributeValue("Final", ret);
                    RunningResult = ret.Equals(Constants.SUCCESS) && RunningResult;
                }
                else
                    RunningResult = false;
                if (!RunningResult)
                {
                    if (ErrorLevel == OnError.AlwaysReturnTrue)
                        RunningResult = true;
                    if (ErrorLevel == OnError.Terminate)
                    {
                        Log.Fatal("Workflow terminated according OnError.Terminate");
                        context.Abort();
                    }
                    if (ErrorLevel == OnError.Continue)
                    {
                        //do nothing, just continue
                    }
                    if (ErrorLevel == OnError.JustShowWarning)
                    {
                        Log.Warn("Warning:\n" + DisplayName + " Error happened, but we ignore it");
                        RunningResult = true;
                    }
                    if (ErrorLevel == OnError.StopCurrentScript)
                    {
                        //we cannot stop it here, just pass the result to higher level, until it reach the testscript level, then testscript will stop itself
                        Log.Error("Error:\n" + DisplayName + " Error happened, stop current script.");
                        
                    }
                }
                //handle the get value here
                var action = stepElement.GetAttributeValue("Action");
                if (action.Equals("GetValue"))
                {
                    var data = stepElement.GetAttributeValue("Data");
                    if (!string.IsNullOrEmpty(data))
                    {
                        var pos = data.IndexOf("=>", StringComparison.Ordinal);
                        try
                        {
                            //var attr = data.Substring(0, pos);
                            var variable = data.Substring(pos + 2);
                            if (!string.IsNullOrEmpty(variable))
                            {
                                var value = stepElement.GetAttributeValue(variable);
                                if (!string.IsNullOrEmpty(value))
                                {

                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ExceptionHelper.FormatStackTrace("GetValue Failed:", ex));
                        }
                    }
                }
                //result.SetAttributeValue(Constants.UI_OBJECT, UIObject);
                stepElement.SetAttributeValue(Constants._TYPE, "Result");
                DBFactory.GetData().Save(stepElement);
            }
            SetFinalResult();
        }

        

        private XElement GetSteps(NativeActivityContext context)
        {
            //TODO: use screen id to get the latest steps, compare to the current one, 
            //force enable some steps(if original one enabled), 
            //delete some steps (if original one gone), 
            //add some steps, update some steps (new and mark enabled, also add the un-enabled items, they would not work anyway)
            //set command to instance, then get the result
            var data = Utilities.GetActualUserData(UserData, Host);
            
            //Utilities.PrintDictionary(data);
            //update the Steps into the format we want
            var steps = CreateStepsHeader();
            foreach (var descendant in XElement.Parse(_steps).Descendants(Constants.STEP))
            {
                var enable = descendant.GetAttributeValue(Constants.ENABLE);
                if (string.IsNullOrEmpty(enable))
                {
                    enable = "True";
                }
                if (!enable.ToLower().Equals("true"))
                    continue;
                var action = descendant.GetAttributeValue(Constants.ACTION);

                if (string.IsNullOrEmpty(action))
                {
                    Log.Error("Action is empty, please check!");
                    continue;
                }

                var step = XElement.Parse("<Step />");

                step.SetAttributeValue(Constants.ACTION, action);
                var defaultData = descendant.GetAttributeValue(Constants.DEFAULT_DATA);
                
                var dataref = descendant.GetAttributeValue(Constants.DATA);
                
                if (string.IsNullOrEmpty(dataref))
                {
                    if (!string.IsNullOrEmpty(defaultData))
                        step.SetAttributeValue(Constants.DATA, Utilities.Evaluate(Pretreat(context,defaultData)));
                }
                else
                {
                    if (data.ContainsKey(dataref))
                    {
                        step.SetAttributeValue(Constants.DATA, Utilities.Evaluate(Pretreat(context,data[dataref])));
                    }
                    else
                    {
                        var found = false;
                        foreach (PropertyDescriptor propertyDescriptor in context.DataContext.GetProperties())
                        {
                            if (!propertyDescriptor.Name.Equals(dataref)) continue;
                            step.SetAttributeValue(Constants.DATA, propertyDescriptor.GetValue(context.DataContext));
                            found = true;
                            break;
                        }
                        if (!found)
                        {
                            
                                var foundAgain = false;
                                var screenObj = Host.GetDataObject(TestSreenId);
                                var screen = XElement.Parse(screenObj.GetAttributeValue("Content"));
                                XNamespace p = "http://schemas.microsoft.com/netfx/2009/xaml/activities";
                                foreach (var v in screen.Descendants(p + "Variable"))
                                {
                                    if (!v.GetAttributeValue("Name").Equals(dataref)) continue;
                                    if (string.IsNullOrEmpty(v.GetAttributeValue("Default"))) continue;
                                    foundAgain = true;
                                    step.SetAttributeValue(Constants.DATA, v.GetAttributeValue("Default"));
                                    break;
                                }
                            
                            if (!foundAgain)
                                step.SetAttributeValue(Constants.DATA, !string.IsNullOrEmpty(defaultData) ? defaultData : "");
                        }
                    }
                }
                var stepId = descendant.GetAttributeValue(Constants._ID);
                if (string.IsNullOrEmpty(stepId))
                {
                    Log.Error("Step id is empty.");
                }
                else
                {
                    step.SetAttributeValue("StepId", stepId);
                }
                var uiid = descendant.GetAttributeValue(Constants.UI_ID);
                var uiObject = descendant.GetAttributeValue(Constants.UI_OBJECT);
                if (!string.IsNullOrEmpty(uiObject))
                {
                    step.SetAttributeValue(Constants.UI_OBJECT, uiObject);
                }
                //TODO we have NOT handle the parent here, add it later; for now, it can work.
                if (string.IsNullOrEmpty(uiid)) continue;
                step.SetAttributeValue(Constants.UI_ID, uiid);
                var uio = Host.GetDataObject(uiid);
                if (uio == null) continue;
                var xO = XElement.Parse("<UIObject />");
                var xpath = uio.GetAttributeValue("XPath");
                //TODO add name, id, css later!!!
                if (!string.IsNullOrEmpty(xpath))
                    xO.SetAttributeValue("XPath", xpath);
                step.Add(xO);
                steps.Add(step);
            }
            return steps;
        }

        private string Pretreat(NativeActivityContext context, string data)
        {
            try
            {
                if (!data.Contains("$("))
                    return data;
                int start = data.IndexOf("$(", StringComparison.Ordinal);
                int end = data.IndexOf(")", start + 2, StringComparison.Ordinal);
                string variable = data.Substring(start + 2, end - start - 2).Trim();
                string value = GetVariableValueByContext(context, variable);
                string ret = data.Substring(0, start) + value + data.Substring(end+1);
                return Pretreat(context, ret);
            }
            catch (Exception ex)
            {
                Log.Error(ExceptionHelper.FormatStackTrace("Statement error ["+data+"] ",ex));
            }
            return "";
        }

        private XElement CreateStepsHeader()
        {
            var steps = XElement.Parse("<AutoX.Steps />");
            steps.SetAttributeValue(Constants.ON_ERROR, ErrorLevel.ToString());
            steps.SetAttributeValue(Constants.INSTANCE_ID, InstanceId);
            steps.SetAttributeValue(Constants._ID, GUID);
            return steps;
        }
    }
}