// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace DLSpeechClient.LocalBotConnector
{
    using System;
    using System.Net.Http;
    using System.Runtime.Remoting.Channels;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Schema;
    using Microsoft.CognitiveServices.Speech;
    using Microsoft.CognitiveServices.Speech.Audio;
    using Microsoft.CognitiveServices.Speech.Dialog;
    using Microsoft.CognitiveServices.Speech.Transcription;
    using Microsoft.Owin.Hosting;
    using Newtonsoft.Json;

    public class Connector : IDisposable
    {
        // TODO
        public static Connector instance;

        private const int Port = 51234;

        private string serviceUrl = $"http://localhost:{Port}";

        private readonly HttpClient client = new HttpClient();

        private IDisposable webapp;

        private string botUrl;

        private string fromId;

        private string conversationId;

        private string locale;

        private bool disposed = false;

        public Connector(DialogServiceConfig config, AudioConfig audioConfig)
        {
            instance = this;

            botUrl = "http://localhost:53062/api/messages";// config.GetProperty(PropertyId.SpeechServiceConnection_Key);
            fromId = config.GetProperty(PropertyId.Conversation_From_Id);
            if (string.IsNullOrEmpty(fromId))
            {
                fromId = Guid.NewGuid().ToString();
            }

            conversationId = config.GetProperty(PropertyId.Conversation_Conversation_Id);
            if (string.IsNullOrEmpty(conversationId))
            {
                conversationId = Guid.NewGuid().ToString();
            }

            locale = config.Language;
            if (string.IsNullOrEmpty(locale))
            {
                locale = "en-us";
            }

            webapp = WebApp.Start<Startup>(url: serviceUrl);
        }

        ~Connector()
        {
            Dispose(false);
        }

        public event EventHandler<SpeechRecognitionEventArgs> Recognizing;

        public event EventHandler<SpeechRecognitionEventArgs> Recognized;

        public event EventHandler<SessionEventArgs> SessionStopped;

        public event EventHandler<SessionEventArgs> SessionStarted;

        public event EventHandler<ActivityReceivedEventArgsLocal> ActivityReceived;

        public event EventHandler<SpeechRecognitionCanceledEventArgs> Canceled;

        public void InvokeActivityReceived(ActivityReceivedEventArgsLocal e)
        {
            ActivityReceived.Invoke(this, e);
        }

        public Task ConnectAsync()
        {
            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        public Task<SpeechRecognitionResult> ListenOnceAsync()
        {
            return null;
        }

        public async Task<string> SendActivityAsync(string activityJSON)
        {
            var activity = JsonConvert.DeserializeObject<Activity>(activityJSON);
            activity.From = new ChannelAccount(fromId);
            activity.Conversation = new ConversationAccount(id: conversationId);
            activity.ChannelId = Channels.Emulator;
            activity.ServiceUrl = serviceUrl;
            activity.Locale = locale;

            activityJSON = JsonConvert.SerializeObject(activity);
            var postContent = new StringContent(activityJSON, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(botUrl, postContent).ConfigureAwait(false);
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        public Task StartKeywordRecognitionAsync(KeywordRecognitionModel model)
        {
            return Task.CompletedTask;
        }

        public Task StopKeywordRecognitionAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    webapp.Dispose();
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.

                // Note disposing has been done.
                disposed = true;

            }
        }
    }
}
