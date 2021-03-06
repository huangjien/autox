﻿#region

// Hapa Project, CC
// Created @2012 08 24 09:25
// Last Updated  by Huang, Jien @2012 08 24 09:25

#region

using System;
using System.Xml.Linq;

#endregion

#endregion

namespace AutoX.Basic.Model
{
    public class Instance : IDataObject
    {
        public string TestName { get; set; }

        public string SuiteName { get; set; }

        public string Status { get; set; }

        public string ClientName { get; set; }

        public string ClientId { get; set; }

        public string DefaultURL { get; set; }

        public string Language { get; set; }

        public string ScriptGUID { get; set; }

        public string _id { get; set; }

        public string _parentId { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }

        public XElement ToXElement()
        {
            return this.GetXElementFromObject();
        }

        public static Instance FromXElement(XElement element)
        {
            return element.GetObjectFromXElement() as Instance;
        }
    }
}