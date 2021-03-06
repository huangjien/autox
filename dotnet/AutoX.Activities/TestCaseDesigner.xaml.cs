﻿#region

// Hapa Project, CC
// Created @2012 08 24 09:25
// Last Updated  by Huang, Jien @2012 08 24 09:25

#region

using System;
using System.Activities;
using System.Activities.Presentation;
using System.Windows;
using System.Xml.Linq;
using AutoX.Basic;

#endregion

#endregion

namespace AutoX.Activities
{
    // Interaction logic for TestCaseDesigner.xaml
    public partial class TestCaseDesigner
    {
        public TestCaseDesigner()
        {
            InitializeComponent();
        }

        public void Connect(int connectionId, object target)
        {
            throw new NotImplementedException();
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            DragEnterTestCase(e);

            base.OnDragEnter(e);
        }

        private void DragEnterTestCase(DragEventArgs e)
        {
            if (DragDropHelper.AllowDrop(
                e.Data,
                Context,
                typeof (Activity)))
            {
                e.Effects = (DragDropEffects.Move & e.AllowedEffects);
                e.Handled = true;
            }
            else
            {
                var data = e.Data.GetData(Constants.DATA_FORMAT) as XElement;
                if (Utilities.CheckValidDrop(data, Constants.SCRIPT, Constants.DATUM))
                {
                    //var activity = Utilities.GetActivityFromXElement(data);
                    //if (activity != null)
                    //{
                    //    var mi = Context.Services.GetService<ModelTreeManager>().CreateModelItem(ModelItem,
                    //        activity);
                    //    Utilities.AddVariable(mi, data.GetAttributeValue(Constants.NAME).Replace(" ", "_"));
                    //    var dO = new DataObject(DragDropHelper.ModelItemDataFormat, mi);
                    //    try
                    //    {
                    //        DragDrop.DoDragDrop(this, dO, DragDropEffects.Move);
                    //    }
                    //    catch (Exception)
                    //    {
                    //    }
                    //}
                    e.Effects = (DragDropEffects.Move & e.AllowedEffects);
                    e.Handled = true;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                    e.Handled = true;
                }
            }
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            DragOverTestCase(e);
            base.OnDragOver(e);
        }

        private void DropOverTestCase(DragEventArgs e)
        {
            e.Handled = true;
            var data = e.Data.GetData(Constants.DATA_FORMAT) as XElement;
            if (data != null)
            {
                if (!data.Name.ToString().Equals("Script"))
                {
                    Utilities.DropXElementToDesigner(data, "UserData", ModelItem);
                }
                else
                {
                    var activity = Utilities.GetActivityFromXElement(data);
                    if (activity == null) return;
                    var canvasActivity = ModelItem;
                    var kids = canvasActivity.Properties["children"];
                    if (kids == null) return;
                    if (kids.Collection != null) kids.Collection.Add(activity);
                }
            }
        }

        protected override void OnDrop(DragEventArgs e)
        {
            DropOverTestCase(e);
            base.OnDrop(e);
        }

        private void DragOverTestCase(DragEventArgs e)
        {
            if (DragDropHelper.AllowDrop(
                e.Data,
                Context,
                typeof (Activity)))
            {
                e.Effects = (DragDropEffects.Move & e.AllowedEffects);
                e.Handled = true;
            }
            else
            {
                var data = e.Data.GetData(Constants.DATA_FORMAT) as XElement;
                if (!Utilities.CheckValidDrop(data, Constants.SCRIPT, Constants.DATUM)) return;
                e.Effects = (DragDropEffects.Move & e.AllowedEffects);
                e.Handled = true;
            }
        }
    }
}