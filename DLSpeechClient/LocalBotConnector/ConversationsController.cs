// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace DLSpeechClient.LocalBotConnector
{
    using Microsoft.CognitiveServices.Speech.Dialog;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using System.Web.Http;

    public class ConversationsController : ApiController
    {
        public JObject Post(string conversationId, [FromBody]JObject value)
        {
            var e = new ActivityReceivedEventArgsLocal();
            e.Activity = value.ToString();
            Connector.instance.InvokeActivityReceived(e);
            return JObject.FromObject(new { id = string.Empty });
        }

        public void Get(string conversationId)
        {
            var e = new ActivityReceivedEventArgsLocal();
            // e.Activity = value;
            Connector.instance.InvokeActivityReceived(e);
        }
    }
}
