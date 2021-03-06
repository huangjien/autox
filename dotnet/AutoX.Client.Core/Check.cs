#region

using System;
using System.Xml.Linq;

#endregion

namespace AutoX.Client.Core
{
    public class Check : AbstractAction
    {
        public override XElement Act()
        {
            var sr = new StepResult(this);
            if (UIObject.Count == 0)
            {
                sr.Error("Expected UI Object is not found!");
            }
            else
            {
                if (string.IsNullOrEmpty(Data))
                    UIObject[0].Click();
                else
                {
                    var toCheck = Convert.ToBoolean(Data);
                    var checkStatus = UIObject[0].Selected;
                    if (toCheck && !checkStatus || !toCheck && checkStatus)
                        UIObject[0].Click();
                }
            }
            return sr.GetResult();
        }
    }
}