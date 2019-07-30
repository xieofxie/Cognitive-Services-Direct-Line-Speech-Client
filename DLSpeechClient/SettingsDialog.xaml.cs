// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace DLSpeechClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;
    using DLSpeechClient.Settings;
    using Microsoft.Win32;

    /// <summary>
    /// Interaction logic for SettingsDialog.xaml.
    /// </summary>
    public partial class SettingsDialog : Window
    {
        private RuntimeSettings settings;

        private bool renderComplete;

        public SettingsDialog(RuntimeSettings settings)
        {
            this.renderComplete = false;

            this.settings = settings;
            (
                this.SubscriptionKey,
                this.SubscriptionKeyRegion,
                this.ConnectionLanguage,
                this.LogFilePath,
                this.WakeWordEnabled,
                this.UrlOverride,
                this.ProxyHostName,
                this.ProxyPortNumber,
                this.FromId) = settings.Get();

            this.WakeWordConfig = new WakeWordConfiguration(settings.WakeWordPath);

            this.InitializeComponent();
            this.DataContext = this;
            this.Owner = App.Current.MainWindow;
        }

        public string SubscriptionKey { get; set; }

        public string SubscriptionKeyRegion { get; set; }

        public string ConnectionLanguage { get; set; }

        public string LogFilePath { get; set; }

        public string UrlOverride { get; set; }

        public string ProxyHostName { get; set; }

        public string ProxyPortNumber { get; set; }

        public string FromId { get; set; }

        public WakeWordConfiguration WakeWordConfig { get; set; }

        public bool WakeWordEnabled { get; set; }

        protected override void OnContentRendered(EventArgs e)
        {
            this.WakeWordPathTextBox.Text = this.settings.WakeWordPath ?? string.Empty;
            this.UpdateOkButtonState();
            this.UpdateWakeWordStatus();
            base.OnContentRendered(e);
            this.renderComplete = true;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.settings.Set(
                this.SubscriptionKey,
                this.SubscriptionKeyRegion,
                this.ConnectionLanguage,
                this.LogFilePath,
                this.WakeWordConfig.Path,
                this.WakeWordEnabled,
                this.UrlOverride,
                this.ProxyHostName,
                this.ProxyPortNumber,
                this.FromId);
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void WakeWordBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog();

            // Filter files by the file extension .table, to find the file downloaded
            // from the Azure web portal for "Speech Customization - Custom Wake Word"
            openDialog.Filter = "Wake word files (*.table)|*.table|All files (*.*)|*.*";

            try
            {
                var fileInfo = new FileInfo(this.WakeWordPathTextBox.Text);
                openDialog.InitialDirectory = fileInfo.DirectoryName;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                Debug.WriteLine($"Bad path for initial directory: {ex.Message}");
            }
#pragma warning restore CA1031 // Do not catch general exception types

            bool? result = openDialog.ShowDialog();

            if (result == true)
            {
                this.WakeWordPathTextBox.Text = openDialog.FileName;
            }
        }

        private void WakeWordEnabledBox_Checked(object sender, RoutedEventArgs e)
        {
            if (this.renderComplete)
            {
                this.UpdateWakeWordStatus();
            }
        }

        private void UpdateOkButtonState()
        {
            // BUGBUG: The transfer into variables does not seem to be done consistently with these events so we read straight from the controls
            bool enableOkButton = !string.IsNullOrWhiteSpace(this.SubscriptionKeyTextBox.Text) &&
                            (!string.IsNullOrWhiteSpace(this.SubscriptionKeyRegionTextBox.Text) || !string.IsNullOrWhiteSpace(this.UrlOverrideTextBox.Text));
            this.OkButton.IsEnabled = enableOkButton;
        }

        private void UpdateWakeWordStatus()
        {
            this.WakeWordConfig = new WakeWordConfiguration(this.WakeWordPathTextBox.Text);

            if (!this.WakeWordConfig.IsValid)
            {
                this.WakeWordStatusLabel.Content = "Invalid wake word model file or location";
                this.WakeWordEnabled = false;
                this.WakeWordEnabledBox.IsChecked = false;
            }
            else if (this.WakeWordEnabled)
            {
                this.WakeWordStatusLabel.Content = $"Will listen for the wake word upon next connection";
            }
            else
            {
                this.WakeWordStatusLabel.Content = "Click to enable";
            }
        }

        private void SubscriptionKeyRegionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.UpdateOkButtonState();
        }

        private void SubscriptionKeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.UpdateOkButtonState();
        }

        private void WakeWordPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.renderComplete)
            {
                this.WakeWordEnabled = false;
                this.WakeWordEnabledBox.IsChecked = false;
            }

            this.WakeWordStatusLabel.Content = "Check to enable";
        }
    }
}
