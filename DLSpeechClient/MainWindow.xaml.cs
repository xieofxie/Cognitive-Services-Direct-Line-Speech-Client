// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace DLSpeechClient
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Threading;
    using AdaptiveCards;
    using AdaptiveCards.Rendering;
    using AdaptiveCards.Rendering.Wpf;
    using DLSpeechClient.Settings;
    using Microsoft.Bot.Schema;
    using Microsoft.CognitiveServices.Speech;
    using Microsoft.CognitiveServices.Speech.Audio;
    using Microsoft.CognitiveServices.Speech.Dialog;
    using Microsoft.Win32;
    using NAudio.Wave;
    using Newtonsoft.Json;

    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Objects are disposed OnClosed()")]
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const int UrlHistoryMaxLength = 10;

        private AppSettings settings = new AppSettings();
        private SpeechBotConnector connector = null;
        private string lastBotEndpoint = null;
        private WaveOutEvent player = new WaveOutEvent();
        private Queue<WavQueueEntry> playbackStreams = new Queue<WavQueueEntry>();
        private WakeWordConfiguration activeWakeWordConfig = null;
        private ListenState listening = ListenState.NotListening;
        private AdaptiveCardRenderer renderer;

        public MainWindow()
        {
            this.InitializeComponent();
            this.Dispatcher.UnhandledException += this.Dispatcher_UnhandledException;
            CommandBinding cb = new CommandBinding(ApplicationCommands.Copy, this.CopyCmdExecuted, this.CopyCmdCanExecute);
            this.ConversationView.ConversationHistory.CommandBindings.Add(cb);
            this.ActivitiesPane.CommandBindings.Add(cb);
            this.ActivityPayloadPane.CommandBindings.Add(cb);
            this.DataContext = this;
            this.player.PlaybackStopped += this.Player_PlaybackStopped;
            Services.Tracker.Configure(this.settings).Apply();

            this.renderer = new AdaptiveCardRenderer();
            this.renderer.UseXceedElementRenderers();
            var configFile = Path.Combine(System.Reflection.Assembly.GetExecutingAssembly().Location, "AdaptiveCardsHostConfig.json");
            if (File.Exists(configFile))
            {
                this.renderer.HostConfig = AdaptiveHostConfig.FromJson(File.ReadAllText(configFile));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<MessageDisplay> Messages { get; private set; } = new ObservableCollection<MessageDisplay>();

        public ObservableCollection<ActivityDisplay> Activities { get; private set; } = new ObservableCollection<ActivityDisplay>();

        public ListenState ListeningState
        {
            get
            {
                return this.listening;
            }

            private set
            {
                this.listening = value;
                this.OnPropertyChanged(nameof(this.ListeningState));
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (this.connector != null)
            {
                this.connector.Dispose();
            }

            if (this.player != null)
            {
                this.player.Dispose();
            }

            base.OnClosed(e);
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            if (string.IsNullOrWhiteSpace(this.settings.Settings.SubscriptionKey)
                ||
                string.IsNullOrWhiteSpace(this.settings.Settings.SubscriptionKeyRegion))
            {
                var settingsDialog = new SettingsDialog(this.settings.Settings);
                bool succeeded;
                succeeded = settingsDialog.ShowDialog() ?? false;

                if (!succeeded)
                {
                    this.Close();
                }
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            // Set this here as opposed to XAML since we do not do a full binding
            this.botEndpoint.ItemsSource = this.settings.DisplaySettings.UrlHistory;
            this.CustomActivityCollectionCombo.ItemsSource = this.settings.DisplaySettings.CustomPayloadData;
            this.CustomActivityCollectionCombo.DisplayMemberPath = "Name";
            this.CustomActivityCollectionCombo.SelectedValuePath = "Name";

            base.OnActivated(e);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
        }

        private void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            this.ShowException(e.Exception);
            e.Handled = true;
        }

        private void ShowException(Exception e)
        {
            this.RunOnUiThread(() =>
            {
                Debug.WriteLine(e);
                this.Messages.Add(new MessageDisplay($"App Error (see log for details): {Environment.NewLine} {e.Source} : {e.Message}", Sender.Channel));
                var trace = new Activity
                {
                    Type = "Exception",
                    Value = e,
                };
                this.Activities.Add(new ActivityDisplay(JsonConvert.SerializeObject(trace), trace, Sender.Channel));
            });
        }

        private void InitSpeechConnector()
        {
            var endpointValue = this.lastBotEndpoint = this.botEndpoint.Text;
            BotConnectorConfig config = null;

            // Allow for tagging bot ids:
            var namedIdPrefix = new Regex(@"^\[(?<tag>[a-zA-Z0-9][ a-zA-Z_0-9]*)\]\s*(?<botId>.+)$", RegexOptions.Compiled);
            var match = namedIdPrefix.Match(endpointValue);
            if (match.Success)
            {
                endpointValue = match.Groups["botId"].Value;
            }

            if (!string.IsNullOrWhiteSpace(this.settings.Settings.SubscriptionKey))
            {
                config = BotConnectorConfig.FromSecretKey(endpointValue, this.settings.Settings.SubscriptionKey, this.settings.Settings.SubscriptionKeyRegion);
            }

            if (!string.IsNullOrWhiteSpace(this.settings.Settings.UrlOverride))
            {
                config.SetProperty("SPEECH-Endpoint", this.settings.Settings.UrlOverride);
            }

            if (!string.IsNullOrWhiteSpace(this.settings.Settings.LogFilePath))
            {
                config.SetProperty("SPEECH-LogFilename", this.settings.Settings.LogFilePath);
            }

            if (!string.IsNullOrWhiteSpace(this.settings.Settings.Language))
            {
                config.SetProperty("SPEECH-RecoLanguage", this.settings.Settings.Language);
            }
            else
            {
                // (for now) required by the client
                config.SetProperty("SPEECH-RecoLanguage", "en-US");
            }

            if (!string.IsNullOrEmpty(this.settings.Settings.FromId))
            {
                config.SetProperty("BOT-FromId", this.settings.Settings.FromId);
            }

            if (this.connector != null)
            {
                this.connector.ActivityReceived -= this.Connector_ActivityReceived;
                this.connector.Recognizing -= this.Connector_Recognizing;
                this.connector.Recognized -= this.Connector_Recognized;
                this.connector.Canceled -= this.Connector_Canceled;
                this.connector.SessionStarted -= this.Connector_SessionStarted;
                this.connector.SessionStopped -= this.Connector_SessionStopped;
                this.connector.Dispose();
                this.connector = null;
            }

            if (!string.IsNullOrWhiteSpace(this.settings.Settings.ProxyHostName)
                && !string.IsNullOrWhiteSpace(this.settings.Settings.ProxyPortNumber)
                && int.TryParse(this.settings.Settings.ProxyPortNumber, out var proxyPortNumber))
            {
                config.SetProxy(this.settings.Settings.ProxyHostName, proxyPortNumber, string.Empty, string.Empty);
            }

            this.connector = new SpeechBotConnector(config, AudioConfig.FromDefaultMicrophoneInput());
            this.connector.ActivityReceived += this.Connector_ActivityReceived;
            this.connector.Recognizing += this.Connector_Recognizing;
            this.connector.Recognized += this.Connector_Recognized;
            this.connector.Canceled += this.Connector_Canceled;
            this.connector.SessionStarted += this.Connector_SessionStarted;
            this.connector.SessionStopped += this.Connector_SessionStopped;
            this.connector.ConnectAsync();

            this.AddBotIdEntryIntoHistory(this.lastBotEndpoint);

            if (this.settings.Settings.WakeWordEnabled)
            {
                this.activeWakeWordConfig = new WakeWordConfiguration(this.settings.Settings.WakeWordPath);
                this.connector.StartKeywordRecognitionAsync(this.activeWakeWordConfig.WakeWordModel);
            }
        }

        private void Connector_SessionStopped(object sender, SessionEventArgs e)
        {
            var message = "Stopped listening";

            if (this.settings.Settings.WakeWordEnabled)
            {
                message = $"Stopped actively listening - waiting for '{this.activeWakeWordConfig.Name}'";
            }

            this.UpdateStatus(message);
            this.RunOnUiThread(() => this.ListeningState = ListenState.NotListening);
        }

        private void Connector_SessionStarted(object sender, SessionEventArgs e)
        {
            this.UpdateStatus("Listening ...");
            this.RunOnUiThread(() => this.ListeningState = ListenState.Listening);
        }

        private void Connector_Canceled(object sender, SpeechRecognitionCanceledEventArgs e)
        {
            var err = $"Error {e.ErrorCode} : {e.ErrorDetails}";
            this.UpdateStatus(err);
            this.RunOnUiThread(() =>
            {
                this.ListeningState = ListenState.NotListening;
                this.Messages.Add(new MessageDisplay(err, Sender.Channel));
            });
        }

        private void Connector_Recognized(object sender, SpeechRecognitionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Connector_Recognized ({e.Result.Reason}): {e.Result.Text}");
            this.RunOnUiThread(() =>
            {
                this.UpdateStatus(string.Empty);
                if (!string.IsNullOrWhiteSpace(e.Result.Text) && e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    this.Messages.Add(new MessageDisplay(e.Result.Text, Sender.User));
                }
            });
        }

        private void Connector_Recognizing(object sender, SpeechRecognitionEventArgs e)
        {
            this.UpdateStatus(e.Result.Text, tentative: true);
        }

        private void Connector_ActivityReceived(object sender, ActivityReceivedEventArgs e)
        {
            var json = e.Activity;
            var activity = JsonConvert.DeserializeObject<Activity>(json);

            if (e.HasAudio)
            {
                var audio = e.Audio;
                var stream = new ProducerConsumerStream();

                Task.Run(() =>
                {
                    var buffer = new byte[800];
                    uint bytesRead = 0;
                    while ((bytesRead = audio.Read(buffer)) > 0)
                    {
                        stream.Write(buffer, 0, (int)bytesRead);
                    }
                });

                var channelData = activity.GetChannelData<SpeechChannelData>();
                var id = channelData?.ConversationalAiData?.RequestInfo?.InteractionId;
                if (!string.IsNullOrEmpty(id))
                {
                    System.Diagnostics.Debug.WriteLine($"Expecting TTS stream {id}");
                }

                var wavStream = new RawSourceWaveStream(stream, new WaveFormat(16000, 16, 1));
                this.playbackStreams.Enqueue(new WavQueueEntry(id, false, stream, wavStream));

                if (this.player.PlaybackState != PlaybackState.Playing)
                {
                    Task.Run(() => this.PlayFromAudioQueue());
                }
            }

            List<AdaptiveCard> cardsToBeRendered = new List<AdaptiveCard>();
            if (activity.Attachments?.Any() is true)
            {
                cardsToBeRendered = activity.Attachments
                    .Where(x => x.ContentType == AdaptiveCard.ContentType)
                    .Select(x =>
                       {
                           try
                           {
                               var parseResult = AdaptiveCard.FromJson(x.Content.ToString());
                               return parseResult.Card;
                           }
#pragma warning disable CA1031 // Do not catch general exception types
                           catch (Exception ex)
                           {
                               this.ShowException(ex);
                               return null;
                           }
#pragma warning restore CA1031 // Do not catch general exception types
                       })
                    .Where(x => x != null)
                    .ToList();
            }

            this.RunOnUiThread(() =>
            {
                this.Activities.Add(new ActivityDisplay(json, activity, Sender.Bot));
                if (activity.Type == ActivityTypes.Message || cardsToBeRendered?.Any() == true)
                {
                    var renderedCards = cardsToBeRendered.Select(x =>
                        {
                            var rendered = this.renderer.RenderCard(x);
                            rendered.OnAction += this.RenderedCard_OnAction;
                            rendered.OnMediaClicked += this.RenderedCard_OnMediaClicked;
                            return rendered?.FrameworkElement;
                        });
                    this.Messages.Add(new MessageDisplay(activity.Text, Sender.Bot, renderedCards));
                }
            });
        }

        private void RenderedCard_OnMediaClicked(RenderedAdaptiveCard sender, AdaptiveMediaEventArgs e)
        {
            MessageBox.Show(this, JsonConvert.SerializeObject(e.Media), "Host received Media");
        }

        private void RenderedCard_OnAction(RenderedAdaptiveCard sender, AdaptiveActionEventArgs e)
        {
            // TODO: update to Adaptive cards 1.2 nuget (ETA 5/24/2019). It contains a bugfix
            // for inline cards not showing. Don't need to handle the ShowCard action here.

            if (e.Action is AdaptiveOpenUrlAction openUrlAction)
            {
                Process.Start(openUrlAction.Url.AbsoluteUri);
            }
            else if (e.Action is AdaptiveSubmitAction submitAction)
            {
                var inputs = sender.UserInputs.AsJson();

                // Merge the Action.Submit Data property with the inputs
                inputs.Merge(submitAction.Data);

                MessageBox.Show(this, JsonConvert.SerializeObject(inputs, Formatting.Indented), "SubmitAction");
            }
        }

        private void SwitchToNewBotEndpoint()
        {
            this.Reset();
            this.Messages.Add(new MessageDisplay("Switched to updated Bot Endpoint", Sender.Channel));
        }

        private void Reset()
        {
            this.Messages.Clear();
            this.Activities.Clear();
            this.ListeningState = ListenState.NotListening;
            this.UpdateStatus("New conversation started");
            this.StopAnyTTSPlayback();
            this.InitSpeechConnector();

            var message = "New conversation started - type or press the microphone button";
            if (this.settings.Settings.WakeWordEnabled)
            {
                message = $"New conversation started - type, press the microphone button, or say '{this.activeWakeWordConfig.Name}'";
            }

            this.UpdateStatus(message);
        }

        private void Reconnect_Click(object sender, RoutedEventArgs e)
        {
            this.Reset();
        }

        private void StartListening()
        {
            if (this.ListeningState == ListenState.NotListening)
            {
                if (this.connector == null)
                {
                    this.InitSpeechConnector();
                }

                try
                {
                    // Bugbug: ListenOnceAsync() doesn't generate Session Started during KWS; if KWS is enabled, fake it here
                    if (this.settings.Settings.WakeWordEnabled)
                    {
                        this.Connector_SessionStarted(null, null);
                    }
                    else
                    {
                        this.ListeningState = ListenState.Initiated;
                    }

                    this.connector.ListenOnceAsync();
                    System.Diagnostics.Debug.WriteLine("Started ListenOnceAsync");
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
                {
                    this.ShowException(ex);
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }
        }

        private void Mic_Click(object sender, RoutedEventArgs e)
        {
            this.StopAnyTTSPlayback();

            if (this.lastBotEndpoint != null && this.lastBotEndpoint != this.botEndpoint.Text)
            {
                this.SwitchToNewBotEndpoint();
            }

            if (this.ListeningState == ListenState.NotListening)
            {
                this.StartListening();
            }
            else
            {
                // Todo: canceling listening not supported
            }
        }

        private void Player_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            lock (this.playbackStreams)
            {
                if (this.playbackStreams.Count == 0)
                {
                    return;
                }

                var entry = this.playbackStreams.Dequeue();
                entry.Stream.Close();
            }

            if (!this.PlayFromAudioQueue())
            {
                if (this.Activities.LastOrDefault(x => x.Activity.Type == ActivityTypes.Message)
                    ?.Activity?.AsMessageActivity()?.InputHint == InputHints.ExpectingInput)
                {
                    this.StartListening();
                }
            }
        }

        private void StopAnyTTSPlayback()
        {
            lock (this.playbackStreams)
            {
                this.playbackStreams.Clear();
            }

            if (this.player.PlaybackState == PlaybackState.Playing)
            {
                this.player.Stop();
            }
        }

        private void StatusBox_KeyUp(object sender, KeyEventArgs e)
        {
            this.StopAnyTTSPlayback();
            if (e.Key != Key.Enter)
            {
                return;
            }

            e.Handled = true;

            if (this.lastBotEndpoint != null && this.lastBotEndpoint != this.botEndpoint.Text)
            {
                this.SwitchToNewBotEndpoint();
            }

            if (this.connector == null)
            {
                this.InitSpeechConnector();
            }

            var bfActivity = Activity.CreateMessageActivity();
            bfActivity.Text = this.statusBox.Text;
            if (!string.IsNullOrEmpty(this.settings.Settings.FromId))
            {
                bfActivity.From = new ChannelAccount(this.settings.Settings.FromId);
            }

            this.statusBox.Clear();
            var jsonConnectorActivity = JsonConvert.SerializeObject(bfActivity);
            this.Messages.Add(new MessageDisplay(bfActivity.Text, Sender.User));
            this.Activities.Add(new ActivityDisplay(jsonConnectorActivity, bfActivity, Sender.User));
            this.connector.SendActivityAsync(jsonConnectorActivity);
        }

        private void ExportActivityLog_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog()
            {
                Filter = "Text Files(*.txt)|*.txt|All(*.*)|*",
            };

            if (dialog.ShowDialog() == true)
            {
                File.WriteAllLines(
                    dialog.FileName,
                    this.Messages.Select(x => x.ToString()).Concat(
                        this.Activities.Select(x => x.ToString()).ToList()).ToArray());
            }
        }

        private void CopyCmdExecuted(object target, ExecutedRoutedEventArgs e)
        {
            string copyContent = string.Empty;
            if (e.OriginalSource is ListBox lb)
            {
                foreach (var item in lb.SelectedItems)
                {
                    copyContent += item.ToString();
                    copyContent += Environment.NewLine;
                }
            }
            else if (e.OriginalSource is JsonViewerControl.JsonViewer jv)
            {
                copyContent = jv.SelectedItem?.ToString();
            }

            if (copyContent != null)
            {
                Clipboard.SetText(copyContent);
            }
        }

        private void CopyCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.OriginalSource is ListBox lb)
            {
                if (lb.SelectedItems.Count > 0)
                {
                    e.CanExecute = true;
                }
                else
                {
                    e.CanExecute = false;
                }
            }
            else if (e.OriginalSource is JsonViewerControl.JsonViewer)
            {
                e.CanExecute = true;
            }
        }

        private void RunOnUiThread(Action action)
        {
            this.statusBox.Dispatcher.InvokeAsync(action);
        }

        private void UpdateStatus(string msg, bool tentative = true)
        {
            if (Thread.CurrentThread != this.statusOverlay.Dispatcher.Thread)
            {
                this.RunOnUiThread(() => this.UpdateStatus(msg, tentative));
                return;
            }

            const string pad = "   ";

            if (tentative)
            {
                this.statusOverlay.Text = pad + msg;
            }
            else
            {
                this.statusBox.Clear();
                this.statusBox.Text = pad + msg;
            }
        }

        private bool PlayFromAudioQueue()
        {
            WavQueueEntry entry = null;
            lock (this.playbackStreams)
            {
                if (this.playbackStreams.Count > 0)
                {
                    entry = this.playbackStreams.Peek();
                }
            }

            if (entry != null)
            {
                System.Diagnostics.Debug.WriteLine($"START playing {entry.Id}");
                this.player.Init(entry.Reader);
                this.player.Play();
                return true;
            }

            return false;
        }

        private void SendActivity_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = this.CustomActivityCollectionCombo.SelectedIndex;
            if (selectedIndex != -1)
            {
                var selectedCustomActivity = this.settings.DisplaySettings.CustomPayloadData[selectedIndex];
                var connectorActivity = selectedCustomActivity.JsonData;
                var bfActivity = JsonConvert.DeserializeObject<Activity>(connectorActivity);
                this.Activities.Add(new ActivityDisplay(connectorActivity, bfActivity, Sender.User));
                if (this.connector == null)
                {
                    this.InitSpeechConnector();
                }

                this.connector.SendActivityAsync(connectorActivity);
            }
        }

        private void ActivitiesPane_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = (sender as ListView).SelectedItem;
            if (item != null && item is ActivityDisplay entry)
            {
                this.ActivityPayloadPane.Load(entry.Json);
                this.ActivityPayloadPane.ExpandAll();
            }
        }

        private void BotEndpoint_SelectionChanged(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Trace.TraceInformation($"BotEndpoint_SelectionChanged: {this.botEndpoint.Text}");
        }

        private void BotEndpoint_TextChanged(object sender, TextChangedEventArgs e)
        {
            System.Diagnostics.Trace.TraceInformation($"BotEndpoint_TextChanged: {this.botEndpoint.Text}");
        }

        private void BotEndpoint_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.Reset();
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            var endpointValue = this.botEndpoint.Text;
            this.RemoveBotIdEntryFromHistory(endpointValue);
        }

        private void AddBotIdEntryIntoHistory(string lastBotEndpoint)
        {
            var urlhistory = this.settings.DisplaySettings.UrlHistory;

            var existingItem = urlhistory.FirstOrDefault(item => string.Compare(item, lastBotEndpoint, StringComparison.OrdinalIgnoreCase) == 0);
            if (existingItem == null)
            {
                urlhistory.Insert(0, lastBotEndpoint);
                if (urlhistory.Count == UrlHistoryMaxLength)
                {
                    urlhistory.RemoveAt(UrlHistoryMaxLength - 1);
                }
            }
        }

        private void RemoveBotIdEntryFromHistory(string lastBotEndpoint)
        {
            var urlhistory = this.settings.DisplaySettings.UrlHistory;

            var existingItem = urlhistory.FirstOrDefault(item => string.Compare(item, lastBotEndpoint, StringComparison.OrdinalIgnoreCase) == 0);
            if (existingItem != null)
            {
                urlhistory.Remove(existingItem);
            }
        }

        private void NewCustomActivityButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new CustomActivityWindow(this.settings.DisplaySettings.CustomPayloadData, -1);
            bool? succeeded = window.ShowDialog();
            this.SelectOrDefault(succeeded, window);
        }

        private void EditCustomActivityButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new CustomActivityWindow(this.settings.DisplaySettings.CustomPayloadData, this.CustomActivityCollectionCombo.SelectedIndex);
            bool? succeeded = window.ShowDialog();
            this.SelectOrDefault(succeeded, window);
        }

        private void SelectOrDefault(bool? dialogResult, CustomActivityWindow dialog)
        {
            if (dialogResult.Value)
            {
                var activityTag = dialog.CustomActivityTag;
                if (!string.IsNullOrWhiteSpace(activityTag))
                {
                    var activityInfoEntry = this.settings.DisplaySettings.CustomPayloadData.FirstOrDefault(
                                                   (item) => item.Name.Equals(activityTag, StringComparison.OrdinalIgnoreCase));

                    this.CustomActivityCollectionCombo.SelectedItem = activityInfoEntry;
                }
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsDialog = new SettingsDialog(this.settings.Settings);
            var succeeded = settingsDialog.ShowDialog();

            // BUGBUG: Do not call reset, leave it for later as this is usually the first action.
        }
    }
}
