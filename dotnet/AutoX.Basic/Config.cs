﻿#region

// Hapa Project, CC
// Created @2012 08 24 09:25
// Last Updated  by Huang, Jien @2012 08 24 09:25

#region

using System;
using System.Collections.Generic;

#endregion

#endregion

namespace AutoX.Basic
{
    public class Config
    {
        private readonly Dictionary<string, string> _variables = new Dictionary<string, string>();

        public Config()
        {
            Id = Guid.NewGuid().ToString();
            _variables.Add(Constants._ID, Id);
        }

        private string Id { get; set; }

        public void Set(string key, string value)
        {
            if (_variables.ContainsKey(key))
                _variables[key] = value;
            else
                _variables.Add(key, value);
        }

        public string Get(string key)
        {
            if (_variables.ContainsKey(key))
                return _variables[key];
            return null;
        }

        public Dictionary<string, string> GetList()
        {
            return _variables;
        }

        public string Get(string p1, string p2)
        {
            return Get(p1) ?? p2;
        }
    }
}