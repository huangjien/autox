﻿using System.Linq;

#region

// Hapa Project, CC
// Created @2012 08 24 09:25
// Last Updated  by Huang, Jien @2012 08 24 09:25
using System.Threading.Tasks;
using AutoX.Basic;
#endregion

#region

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using AutoX.Basic.Model;
using AutoX.Comm;
using AutoX.DB;
using IDataObject = AutoX.Basic.Model.IDataObject;



#endregion

namespace AutoX
{
    public partial class MainWindow
    {
        private readonly TableItem _clientSource = new TableItem();
        private readonly TableItem _instanceSource = new TableItem();
        private readonly TableItem _testCaseResultSource = new TableItem();
        private readonly TableItem _testStepSource = new TableItem();
        private readonly TableItem _translationSource = new TableItem();

        private void RefreshClientTable(object sender, RoutedEventArgs e)
        {
            try
            {
                var ret = Communication.GetInstance().GetComputersInfo();
                _clientSource.Clear();
                foreach (XElement computer in XElement.Parse(ret).Descendants())
                {
                    _clientSource.Add(Computer.FromXElement(computer));
                }
                ClientTable.ItemsSource = _clientSource.Get();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ExceptionHelper.FormatStackTrace(ex), "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClientTableEnterFilter(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            var filter = FilterClient.Text;
            ClientTable.ItemsSource = _clientSource.GetMatched(filter);
            e.Handled = true;
        }

        private void InstanceTableEnterFilter(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            var filter = FilterInstance.Text;
            InstanceTable.ItemsSource = _instanceSource.GetMatched(filter);
            e.Handled = true;
        }

        private void TestCaseTableEnterFilter(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            var filter = FilterTestCaseResult.Text;
            TestCaseResultTable.ItemsSource = _testCaseResultSource.GetMatched(filter);
            e.Handled = true;
        }

        private void TestStepTableEnterFilter(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            var filter = FilterTestCaseResult.Text;
            TestCaseResultTable.ItemsSource = _testCaseResultSource.GetMatched(filter);
            e.Handled = true;
        }

        private void TranslationEnterFilter(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            var filter = FilterTranslation.Text;
            TranslationTable.ItemsSource = _translationSource.GetMatched(filter);
            e.Handled = true;
        }

        private void HyperlinkClick(object sender, RoutedEventArgs e)
        {
            var r = sender as TextBlock;
            if (r == null) return;
            dynamic data = r.DataContext;
            string content = data.Link;
            if (!content.StartsWith("http"))
            {
                var fileName = Path.GetTempPath() + Guid.NewGuid() + ".htm";
                File.WriteAllText(fileName,
                    @"<html><body><img src='data:image/jpg;base64," + content + @"' /></body></html>");
                Process.Start(fileName);
            }
            else
                Process.Start(content);
        }

        private void DoubleClickOnTable(object sender, MouseButtonEventArgs e)
        {
            var table = sender as DataGrid;
            if (table == null) return;
            var selected = table.SelectedItem as IDataObject;
            if (selected == null) return;
            var element = selected.GetXElementFromObject();
            var source = table.ItemsSource as Collection<object>;
            if (source == null) return;
            var index = source.IndexOf(selected);
            var dialog = new XElementDialog(element, false);
            dialog.ShowDialog();
            if (dialog.DialogResult != true) return;
            var updated = dialog.GetElement().GetObjectFromXElement();
            source[index] = updated;
            table.ItemsSource = source;
            DBFactory.GetData().Save(dialog.GetElement());
        }

        private async void TestCaseResultTableSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TestCaseResultTable == null) return;
            var selected = TestCaseResultTable.SelectedItem as Result;
            if (selected == null) return;
            await UpdateResultTable(selected._id);
            
        }

        private async Task UpdateResultTable(string id)
        {
            _testStepSource.Clear();
            var xRoot = DBFactory.GetData().GetChildren(id);
            if (xRoot.HasElements)
            {
                foreach (var testcaseresult in xRoot.Descendants().Select(kid => kid.GetDataObjectFromXElement()).OfType<StepResult>())
                {
                    _testStepSource.Add(testcaseresult);
                }
            }
            TestStepsResultTable.ItemsSource = _testStepSource.Get();
        }
    }
}