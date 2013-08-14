#region

// Hapa Project, CC
// Created @2012 09 18 14:34
// Last Updated  by Huang, Jien @2012 09 18 14:34

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
    [ToolboxBitmap(typeof (CallTestScreenDesigner), "TestScreen.bmp")]
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
            
            Log.Info(rElement.ToString());
            foreach (var stepElement in rElement.Descendants())
            {
                stepElement.SetAttributeValue(Constants.PARENT_ID, ResultId);
                var ret = stepElement.GetAttributeValue(Constants.RESULT);
                if (!string.IsNullOrEmpty(ret))
                {
                    stepElement.SetAttributeValue("Original", ret);
                    stepElement.SetAttributeValue("Final", ret);
                    _runningResult = ret.Equals(Constants.SUCCESS) && _runningResult;
                }
                else
                    _runningResult = false;
                if (!_runningResult)
                {
                    if (ErrorLevel == OnError.AlwaysReturnTrue)
                        _runningResult = true;
                    //if (ErrorLevel == OnError.Terminate)
                    //{
                    //    //TODO terminate the instance (send a status to instance)
                    //    Host.Stop();
                    //}
                    if (ErrorLevel == OnError.Continue)
                    {
                        //do nothing, just continue
                    }
                    if (ErrorLevel == OnError.JustShowWarning)
                    {
                        Log.Warn("Warning:\n" + DisplayName + " Error happened, but we ignore it");
                        _runningResult = true;
                    }
                    if (ErrorLevel == OnError.StopCurrentScript)
                    {
                        //we cannot stop it here, just pass the result to higher level, until it reach the testscript level, then testscript will stop itself
                        Log.Error("Error:\n" + DisplayName + " Error happened, stop current script.");
                        
                    }
                }
                //result.SetAttributeValue(Constants.UI_OBJECT, UIObject);
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
            var screenObj = Host.GetDataObject(TestSreenId);
            var screen = XElement.Parse(screenObj.GetAttributeValue("Content"));
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
                        step.SetAttributeValue(Constants.DATA, defaultData);
                }
                else
                {
                    if (data.ContainsKey(dataref))
                        step.SetAttributeValue(Constants.DATA, data[dataref]);
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
                            {
                                XNamespace p = "http://schemas.microsoft.com/netfx/2009/xaml/activities";
                                foreach (var v in screen.Descendants(p + "Variable"))
                                {
                                    if (!v.GetAttributeValue("Name").Equals(dataref)) continue;
                                    if (string.IsNullOrEmpty(v.GetAttributeValue("Default"))) continue;
                                    found = true;
                                    step.SetAttributeValue(Constants.DATA, v.GetAttributeValue("Default"));
                                    break;
                                }
                            }
                            if (!found)
                                step.SetAttributeValue(Constants.DATA,
                                    !string.IsNullOrEmpty(defaultData) ? defaultData : "");
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