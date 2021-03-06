﻿#region

// Hapa Project, CC
// Created @2012 08 24 09:25
// Last Updated  by Huang, Jien @2012 08 24 09:25

using System.CodeDom.Compiler;
using System.Text;

#region

using System;
using System.Activities;
using System.Activities.Presentation.Model;
using System.Activities.XamlIntegration;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using AutoX.Activities.AutoActivities;
using AutoX.Basic;

#endregion

#endregion

namespace AutoX.Activities
{
    public enum OnError
    {
        [Description("Always Return Success, Ignore All Errors")] AlwaysReturnTrue,

        [Description("Only Show Warning in Result, Even Error")] JustShowWarning,

        [Description("Mark Error in Result, but Continue Next Step")] Continue,

        [Description("Stop Current Script, Mark it Error")] StopCurrentScript,

        [Description("Terminate this Test Instance")] Terminate
    }

    public static class Utilities
    {
        public const string Filter = "Name;Type;Description;Type;_id;";
        public const string ReservedList = "_id|_type|_parentId|Created|Updated|";

        public static string GetEnumDescription(Enum value)
        {
            var fi = value.GetType().GetField(value.ToString());

            var attributes =
                (DescriptionAttribute[]) fi.GetCustomAttributes(
                    typeof (DescriptionAttribute),
                    false);

            return attributes.Length > 0 ? attributes[0].Description : value.ToString();
        }

        public static void DropXElementToDesigner(XElement data, string dropData, ModelItem navtiveModelItem)
        {
            var guid = data.GetAttributeValue(Constants._ID);
            var tag = data.Name.ToString();
            var modelProperty = navtiveModelItem.Properties[dropData];
            if (modelProperty == null) return;
            if (modelProperty.Value == null) return;
            var userData = modelProperty.Value.GetCurrentValue() as string ?? "";

            if (tag.Equals(Constants.DATUM))
            {
                if (userData.Contains(guid))
                {
                    userData = userData.Replace(guid, "");
                }
                userData += guid + ";";
            }
            if (tag.Equals(Constants.UI_OBJECT))
            {
                var xSteps = XElement.Parse(userData);

                var xStep = new XElement(Constants.STEP);
                var name = data.GetAttributeValue(Constants.NAME);
                xStep.SetAttributeValue(Constants._ID, Guid.NewGuid().ToString());
                xStep.SetAttributeValue(Constants.UI_ID, data.GetAttributeValue(Constants._ID));
                xStep.SetAttributeValue(Constants.UI_OBJECT, name);
                xStep.SetAttributeValue(Constants.ENABLE, "False");
                xStep.SetAttributeValue(Constants.DATA, "");
                xStep.SetAttributeValue(Constants.DEFAULT_DATA, "");
                xStep.SetAttributeValue(Constants.ACTION, "");
                xStep.SetAttributeValue(Constants.XPATH, data.GetAttributeValue(Constants.XPATH));
                
                xSteps.Add(xStep);
                userData = xSteps.ToString();
                AddVariable(navtiveModelItem, name);
            }
            modelProperty.SetValue(userData);
        }

        public static void AddVariable(ModelItem navtiveModelItem, string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            var variablesProperty = navtiveModelItem.Properties["Variables"];
            if (variablesProperty == null) return;
            var existed = variablesProperty.Collection != null && variablesProperty.Collection.Any(v => v.Properties[Constants.NAME].Value.ToString().Equals(name));
            if (existed) return;
            var variable = new Variable<string>(name);
            if (variablesProperty.Collection != null) variablesProperty.Collection.Add(variable);
        }

        public static bool CheckValidDrop(XElement data, params string[] types)
        {
            if (data == null)
                return false;
            var type = data.Name.ToString();
            return !String.IsNullOrEmpty(type) && types.Any(s => s.Equals(type));
        }

        public static Activity GetActivityFromContentString(string content)
        {
            if (string.IsNullOrEmpty(content))
                return null;
            return ActivityXamlServices.Load(new StringReader(content));
        }

        public static Activity GetActivityFromXElement(XElement data)
        {
            var scriptType = data.GetAttributeValue(Constants.SCRIPT_TYPE);
            if (!String.IsNullOrEmpty(scriptType))
            {
                var host = HostManager.GetInstance().GetHost();
                if (scriptType.Equals("TestCase"))
                {
                    var activity = new CallTestCaseActivity
                    {
                        Authors = Environment.UserName,
                        TestCaseId = data.GetAttributeValue(Constants._ID),
                        TestCaseName = data.GetAttributeValue(Constants.NAME),
                        Name = data.GetAttributeValue(Constants.NAME),
                        DisplayName = "Call Test Case: " + data.GetAttributeValue(Constants.NAME)
                    };
                    var tsContent = data.GetAttributeValue("Content");
                    var a = GetActivityFromContentString(tsContent) as TestCaseActivity;
                    if (a != null)
                        foreach (var v in a.Variables)
                            activity.Variables.Add(v);
                    activity.SetHost(host);
                    return activity;
                }
                if (scriptType.Equals("TestScreen"))
                {
                    var ss = XElement.Parse(data.GetAttributeValue(Constants.CONTENT)).GetAttributeValue("Steps");
                    var sx = XElement.Parse(ss);
                    sx.SetAttributeValue(Constants._ID,data.GetAttributeValue(Constants._ID));
                    var activity = new CallTestScreenActivity
                    {
                        Authors = Environment.UserName,
                        TestSreenId = data.GetAttributeValue(Constants._ID),
                        TestSreenName = data.GetAttributeValue(Constants.NAME),
                        ErrorLevel = OnError.Continue,
                        GUID = Guid.NewGuid().ToString(),
                        UserData = "",
                        Name = data.GetAttributeValue(Constants.NAME),
                        DisplayName = "Call Test Screen: " + data.GetAttributeValue(Constants.NAME),
                        Steps = sx.ToString()
                    };
                    var tsContent = data.GetAttributeValue("Content");
                    var a = GetActivityFromContentString(tsContent) as TestScreenActivity;
                
                    if (a != null)
                        foreach (var v in a.Variables)
                            activity.Variables.Add(v);
                    activity.SetHost(host);
                    return activity;
                }
                if (scriptType.Equals("TestSuite"))
                {
                    var activity = new CallTestSuiteActivity
                    {
                        Authors = Environment.UserName,
                        TestSuiteId = data.GetAttributeValue(Constants._ID),
                        TestSuiteName = data.GetAttributeValue(Constants.NAME),
                        Name = data.GetAttributeValue(Constants.NAME),
                        Description = data.GetAttributeValue("Description"),
                        DisplayName = "Call Test Suite: " + data.GetAttributeValue(Constants.NAME)
                    };
                    var tsContent = data.GetAttributeValue("Content");
                    var a = GetActivityFromContentString(tsContent) as TestSuiteActivity;
                    if (a != null)
                        foreach (var v in a.Variables)
                            activity.Variables.Add(v);
                    activity.SetHost(host);
                    return activity;
                }
            }
            return null;
        }

        public static List<UserData> GetUserData(string rawData, IHost host)
        {
            var dic = GetRawUserData(rawData, host);
            return dic.Values.ToList();
        }

        public static Dictionary<string, UserData> GetRawUserData(string rawData, IHost host)
        {
            var dic = new Dictionary<string, UserData>();
            if (!String.IsNullOrEmpty(rawData))
            {
                var dataStrings = rawData.Split(';');
                foreach (string dataString in dataStrings)
                {
                    if (String.IsNullOrEmpty(dataString))
                        continue;
                    var sData = host.GetDataObject(dataString);

                    //sometimes, some data has been deleted.
                    if (sData == null)
                        continue;
                    var dataSetName = sData.GetAttributeValue(Constants.NAME);

                    foreach (XAttribute xAttribute in sData.Attributes())
                    {
                        var name = xAttribute.Name.ToString();
                        if (Filter.Contains(name)) continue;
                        var dataValue = xAttribute.Value;
                        var data = new UserData
                        {
                            DataSet = dataSetName,
                            Name = name,
                            Value = dataValue,
                            DataSetId = dataString
                        };

                        //remove the duplicate value
                        if (dic.ContainsKey(name))
                            dic[name] = data;
                        else
                        {
                            dic.Add(name, data);
                        }
                    }
                }
            }
            return dic;
        }

        public static Dictionary<string, string> GetActualUserData(string rawData, IHost host)
        {
            var dic = new Dictionary<string, string>();
            if (!String.IsNullOrEmpty(rawData))
            {
                var dataStrings = rawData.Split(';');
                foreach (string dataString in dataStrings)
                {
                    if (String.IsNullOrEmpty(dataString))
                        continue;
                    var sData = host.GetDataObject(dataString).ToString();
                    if (String.IsNullOrEmpty(sData)) continue;
                    var xData = XElement.Parse(sData);

                    foreach (XAttribute xAttribute in xData.Attributes())
                    {
                        var name = xAttribute.Name.ToString();
                        if (Filter.Contains(name)) continue;
                        var dataValue = xAttribute.Value;
                        var data = dataValue;

                        //remove the duplicate value
                        if (dic.ContainsKey(name))
                            dic[name] = data;
                        else
                        {
                            dic.Add(name, data);
                        }
                    }
                }
            }
            return dic;
        }

        public static void PrintDictionary(Dictionary<string, string> dict)
        {
            var pS = dict.Aggregate("\n",
                (current, variable) => current + (variable.Key + "=" + variable.Value + "\n"));
            Log.Debug(pS);
        }

        public static ArrayList GetStepsList(string textValue, ArrayList possibleAction, IHost host)
        {
            var ret = new ArrayList();
            if (textValue != null)
            {
                var xSteps = XElement.Parse(textValue);
                foreach (XElement element in xSteps.Descendants(Constants.STEP))
                {
                    var uiId = element.GetAttributeValue(Constants.UI_ID);
                    if (String.IsNullOrEmpty(uiId)) continue;

                    var sData = host.GetDataObject(uiId);
                    if (sData == null) continue;

                    //var xData = sData.GetXElementFromDataObject();
                    var uiObject = element.GetAttributeValue(Constants.UI_OBJECT);
                    if (String.IsNullOrEmpty(uiObject)) continue;
                    var enable = Boolean.Parse(element.GetAttributeValue(Constants.ENABLE));
                    var defaultDataValue = element.GetAttributeValue(Constants.DEFAULT_DATA);
                    var dataName = element.GetAttributeValue(Constants.DATA);
                    var stepId = element.GetAttributeValue(Constants._ID);
                    var action = element.GetAttributeValue(Constants.ACTION) ?? "";
                    string xpath;
                    xpath = getXPath(element);
                    var step = new Step
                    {
                        _id = stepId,
                        Action = action,
                        UIId = uiId,
                        UIObject = uiObject,
                        Enable = enable,
                        DefaultData = defaultDataValue,
                        Data = dataName,
                        XPath = xpath,
                        PossibleAction = possibleAction
                    };
                    ret.Add(step);
                }
            }
            return ret;
        }

        private static string getXPath(XElement element)
        {
            if (element.HasElements)
            {
                var uiObject = element.Element(Constants.UI_OBJECT);
                if (uiObject != null)
                {
                    return uiObject.GetAttributeValue(Constants.XPATH);

                }
            }
            var xpath = element.GetAttributeValue(Constants.XPATH);
            return xpath;
        }

        public static string PassData(string outerData, string userData, bool ownDataFirst)
        {
            string finalRet;
            if (!ownDataFirst)
                finalRet = userData + ";" + outerData;
            else
            {
                finalRet = outerData + ";" + userData;
            }
            return finalRet;
        }



        public static string Evaluate(string variable)
        {
            if (variable.StartsWith("${") && variable.EndsWith("}"))
            {
                try
                {
                    variable = variable.Substring(2, variable.Length - 3);
                    return Eval(variable).ToString();
                }
                catch (Exception exception)
                {
                    Log.Error(ExceptionHelper.FormatStackTrace("try to evaluate string "+variable+" failed.",exception));
                }
            }
            return variable;
        }

        public static object Eval(string sCSCode)
        {

            var c = CodeDomProvider.CreateProvider("CSharp");
            var cp = new CompilerParameters();

            cp.ReferencedAssemblies.Add("system.dll");
            cp.ReferencedAssemblies.Add("system.xml.dll");
            cp.ReferencedAssemblies.Add("system.data.dll");
            cp.ReferencedAssemblies.Add("system.windows.forms.dll");
            cp.ReferencedAssemblies.Add("system.drawing.dll");

            cp.CompilerOptions = "/t:library";
            cp.GenerateInMemory = true;

            var sb = new StringBuilder("");
            sb.Append("using System;\n");
            sb.Append("using System.Xml;\n");
            sb.Append("using System.Data;\n");
            sb.Append("using System.Data.SqlClient;\n");
            sb.Append("using System.Windows.Forms;\n");
            sb.Append("using System.Drawing;\n");

            sb.Append("namespace AutoX.Activities{ \n");
            sb.Append("public class CSCodeEvaler{ \n");
            sb.Append("public object EvalCode(){\n");
            sb.Append("return " + sCSCode + "; \n");
            sb.Append("} \n");
            sb.Append("} \n");
            sb.Append("}\n");

            var cr = c.CompileAssemblyFromSource(cp, sb.ToString());
            if (cr.Errors.Count > 0)
            {
                Log.Error("ERROR: " + cr.Errors[0].ErrorText);
                return null;
            }
            
            var a = cr.CompiledAssembly;
            var o = a.CreateInstance("AutoX.Activities.CSCodeEvaler");

            if (o == null) return null;
            var t = o.GetType();
            var mi = t.GetMethod("EvalCode");

            var s = mi.Invoke(o, null);
            return s;
        }

        public static WorkflowApplication GetWorkflowApplication(AutomationActivity activity)
        {
            var workflowApplication = new WorkflowApplication(activity)
            {
                Completed = delegate(WorkflowApplicationCompletedEventArgs e)
                {
                    switch (e.CompletionState)
                    {
                        case ActivityInstanceState.Faulted:

                            //Logger.GetInstance().Log().Error("workflow " +
                            //                                 scriptGuid +
                            //                                 " stopped! Error Message:\n"
                            //                                 +
                            //                                 e.TerminationException.
                            //                                     GetType().FullName +
                            //                                 "\n"
                            //                                 +
                            //                                 e.TerminationException.
                            //                                     Message);
                            //Status = "Terminated";
                            break;

                        case ActivityInstanceState.Canceled:

                            //Logger.GetInstance().Log().Warn("workflow " + scriptGuid +
                            //                                " Cancel.");
                            //Status = "Canceled";
                            break;

                            //Logger.GetInstance().Log().Info("workflow " + scriptGuid +
                            //                                " Completed.");
                            //Status = "Completed";
                    }
                },
                Aborted = delegate
                {
                    //Logger.GetInstance().Log().Error("workflow " +
                    //                                 scriptGuid
                    //                                 + " aborted! Error Message:\n"
                    //                                 + e.Reason.GetType().FullName + "\n" +
                    //                                 e.Reason.Message);
                    //Status = "Aborted";
                }
            };
            return workflowApplication;
        }
    }
}