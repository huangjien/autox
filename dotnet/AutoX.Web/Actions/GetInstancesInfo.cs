﻿// Hapa Project, CC
// Created @2012 08 24 09:25
// Last Updated  by Huang, Jien @2012 08 24 09:25

#region

using System;
using System.Activities;
using AutoX.Basic;

#endregion

namespace AutoX.Web.Actions
{
    public class GetInstancesInfo : NativeActivity
    {
        protected override void Execute(NativeActivityContext context)
        {
            string commandStr = Utils.GetContextValue(context, "command");

            try
            {
                string instances = InstanceManager.GetInstance().GetInstances();
                Utils.SetReturnMessage(context, instances);
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Log().Debug("we receive invalid command string:\n" + commandStr + "\n" + ex.Message);
                Utils.SetFailedReturnMessage(context, "Exception:\n" + ex.Message);
            }
        }
    }
}