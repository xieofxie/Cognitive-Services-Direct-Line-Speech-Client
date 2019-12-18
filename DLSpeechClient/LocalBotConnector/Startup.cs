// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace DLSpeechClient.LocalBotConnector
{
    using Owin;
    using System.Web.Http;

    public class Startup
    {
        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public void Configuration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "v3/{controller}/{conversationId}/activities",
                defaults: new { id = RouteParameter.Optional }
            );

            appBuilder.UseWebApi(config);
        }
    }
}
