// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace DLSpeechClient.Settings
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading.Tasks;

    [Serializable]
    public class RuntimeSettings : INotifyPropertyChanged
    {
        private string subscriptionKey;
        private string subscriptionKeyRegion;
        private string language;
        private string logFilePath;
        private string wakeWordPath;
        private bool wakeWordEnabled;
        private string urlOverride;
        private string proxyHostName;
        private string proxyPortNumber;
        private string fromId;

        public RuntimeSettings()
        {
            this.language = string.Empty;
        }

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public string SubscriptionKey
        {
            get => this.subscriptionKey;
            set => this.SetProperty(ref this.subscriptionKey, value);
        }

        public string SubscriptionKeyRegion
        {
            get => this.subscriptionKeyRegion;
            set => this.SetProperty(ref this.subscriptionKeyRegion, value);
        }

        public string Language
        {
            get => this.language;
            set => this.SetProperty(ref this.language, value);
        }

        public string LogFilePath
        {
            get => this.logFilePath;
            set => this.SetProperty(ref this.logFilePath, value);
        }

        public string WakeWordPath
        {
            get => this.wakeWordPath;
            set => this.SetProperty(ref this.wakeWordPath, value);
        }

        public bool WakeWordEnabled
        {
            get => this.wakeWordEnabled;
            set => this.SetProperty(ref this.wakeWordEnabled, value);
        }

        public string UrlOverride
        {
            get => this.urlOverride;
            set => this.SetProperty(ref this.urlOverride, value);
        }

        public string ProxyHostName
        {
            get => this.proxyHostName;
            set => this.SetProperty(ref this.proxyHostName, value);
        }

        public string ProxyPortNumber
        {
            get => this.proxyPortNumber;
            set => this.SetProperty(ref this.proxyPortNumber, value);
        }

        public string FromId
        {
            get => this.fromId;
            set => this.SetProperty(ref this.fromId, value);
        }

        internal (string subscriptionKey, string subscriptionKeyRegion, string language, string logFilePath, bool wakeWordEnabled, string urlOverride, string proxyHostName, string proxyPortNumber, string fromId) Get()
        {
            return (
                this.subscriptionKey,
                this.subscriptionKeyRegion,
                this.language,
                this.logFilePath,
                this.wakeWordEnabled,
                this.urlOverride,
                this.proxyHostName,
                this.proxyPortNumber,
                this.fromId);
        }

        internal void Set(
            string subscriptionKey,
            string subscriptionKeyRegion,
            string language,
            string logFilePath,
            string wakeWordPath,
            bool wakeWordEnabled,
            string urlOverride,
            string proxyHostName,
            string proxyPortNumber,
            string fromId)
        {
            (this.subscriptionKey,
                this.subscriptionKeyRegion,
                this.language,
                this.logFilePath,
                this.wakeWordPath,
                this.wakeWordEnabled,
                this.urlOverride,
                this.proxyHostName,
                this.proxyPortNumber,
                this.fromId)
                =
            (subscriptionKey,
                subscriptionKeyRegion,
                language,
                logFilePath,
                wakeWordPath,
                wakeWordEnabled,
                urlOverride,
                proxyHostName,
                proxyPortNumber,
                fromId);
        }

        protected void SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (!object.Equals(storage, value))
            {
                storage = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
