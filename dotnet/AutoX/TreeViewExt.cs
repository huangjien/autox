﻿#region

// Hapa Project, CC
// Created @2012 08 24 09:25
// Last Updated  by Huang, Jien @2012 08 24 09:25

#region

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;
using AutoX.Basic;

#endregion

#endregion

namespace AutoX
{
    public static class TreeViewExt
    {
        public static TreeViewItem GetTreeViewItemFromXElement(this XElement xElement)
        {
            var treeViewItem = new TreeViewItem {DataContext = xElement};
            treeViewItem.UpdateTreeViewItem(xElement);
            return treeViewItem;
        }

        private static string GetIconType(this XElement xElement)
        {
            const string typeOrder = "ScriptType;Type;type;_type";

            var types = typeOrder.Split(';');
            return types.Select(xElement.GetAttributeValue).FirstOrDefault(t => !string.IsNullOrEmpty(t));
        }

        public static void UpdateTreeViewItem(this TreeViewItem treeViewItem, XElement xElement)
        {
            treeViewItem.DataContext = xElement;
            var head = new StackPanel {Orientation = Orientation.Horizontal};
            
            var text = new TextBlock();
            // ScriptType->Type->type->_Type
            var icon = xElement.GetIconType();
            var name = xElement.GetAttributeValue(Constants.NAME);
            var description = xElement.GetAttributeValue("Description");
            var height = text.FontSize;
            if (!string.IsNullOrWhiteSpace(icon))
            {
                AddIcon(head, icon, description, height);
            }
            var maturity = xElement.GetAttributeValue("Maturity");
            if (!string.IsNullOrEmpty(maturity))
            {
                AddIcon(head, maturity,"",height);
            }

            text.Text = name;
            text.Margin = new Thickness(1, 0, 1, 0);
            text.ToolTip = new ToolTip {Content = xElement.GetSimpleDescriptionFromXElement()};


            head.Children.Add(text);

            treeViewItem.Header = head;
        }

        private static void AddIcon(StackPanel head, string icon, string description, double height)
        {
            var bitmap = ImageList.GetInstance().Get(icon);
            if (bitmap != null)
            {
                // this is important, remove it, the tree will be 20 times slower
                Dispatcher.CurrentDispatcher.BeginInvoke((DispatcherPriority.Normal), (Action)(() =>
                {
                    var image = new Image
                    {
                        Source = bitmap,
                        Stretch = Stretch.Uniform,
                        MaxHeight = height,
                        MaxWidth = height,
                        Width = height,
                        Height = height,
                        MinHeight = 16,
                        MinWidth = 16,
                        Margin = new Thickness(1, 0, 1, 0),
                        ToolTip = description
                    };

                    
                    head.Children.Insert(0, image);
                }
                    ));
            }
        }

        public static bool FilterTreeItem(this TreeViewItem tree, string value)
        {
            if (tree == null) return false;
            var visible = false;

            foreach (object kid in tree.Items)
            {
                var kidItem = kid as TreeViewItem;
                if (kidItem == null) continue;
                if (FilterTreeItem(kidItem, value)) visible = true;
            }
            if (!visible)
            {
                var data = tree.DataContext as XElement;
                if (string.IsNullOrEmpty(value))
                    visible = true;
                else
                {
                    if (data != null && data.ToString().Contains(value)) visible = true;
                }
            }

            tree.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

            return visible;
        }
    }
}