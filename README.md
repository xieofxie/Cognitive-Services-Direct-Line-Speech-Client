[![Build Status](https://msasg.visualstudio.com/Skyman/_apis/build/status/Azure-Samples.Cognitive-Services-Direct-Line-Speech-Client?branchName=master)](https://msasg.visualstudio.com/Skyman/_build/latest?definitionId=10284&branchName=master)

# Direct Line Speech Client

The Direct Line Speech client is a Windows Presentation Foundation (WPF) application in C# that makes it easy to test interactions with your bot before creating a custom client application. It demonstrates how to use the [Azure Speech Services Speech SDK](https://docs.microsoft.com/azure/cognitive-services/speech-service/speech-sdk) to manage communication with your Azure Bot-Framework bot. To use this client, you need to register your bot with the [Direct Line Speech](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-channel-connect-directlinespeech?view=azure-bot-service-4.0) channel.

## Features

* Fully configurable to support any bot registered with the Direct Line Speech channel
* Accepts typed text and speech captured by a microphone as inputs for your bot
* Supports playback of audio response
* Supports use of [custom wake-words](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-devices-sdk-create-kws)
* Supports sending custom [Bot-Framework Activities](https://github.com/Microsoft/botframework-sdk/blob/master/specs/botframework-activity/botframework-activity.md) as JSON
* Displays [Adaptive Cards](https://adaptivecards.io/) sent from your bot (with some limitations)
* Exports the transcript and activity logs to a file

## Getting Started

### Prerequisites

Let's review the hardware, software, and subscriptions that you'll need to use this client application.

- Windows 10 PC
- Visual Studio 2017 or higher
- Microphone access
- An Azure [Speech Services Key](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/get-started)
- A [Bot-Framework](https://dev.botframework.com/) bot service registered with Direct Line Speech channel

### Quickstart

1. The first step is to clone the repository:
   ```bash
   git clone https://github.com/Azure-Samples/Cognitive-Services-Direct-Line-Speech-Client.git
   ```
2. Then change directories:
   ```bash
   cd Cognitive-Services-Direct-Line-Speech-Client
   ```
3. Launch Visual Studio 2017, then open the solution for the Direct Line Speech client: `DLSpeechClient.sln`. The solution is in the root of the cloned repository.
4. Run the executable. For example, for Release x64 build: `DLSpeechClient\bin\x64\Release\DLSpeechClient.exe`.
5. When you first run the application, the **Setting** page will open. The first two fields are required (all others are optional):
    - Enter _Subscription key_. This is your Azure [Speech Services Key](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/get-started)
    - Enter _Subscription key region_. This is the Azure region of your key (e.g. "westus")
    - The default input language is "en-us" (US English). Update the _Language_ field as needed to select a different [language code from the "Speech-to-text" list](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support).
    - Press _Ok_ when you're done.
    - Your entires will be saved and populated automatically when you launch the app again.
    ![Setting page](docs/media/SettingsPage.png)
6. In the main window, enter your **Bot Secret**. This is one of the two channel secret keys provided when you
registered your Bot-Framework bot with the Direct Line Speech channel.
   ![Main Page](docs/media/MainPage.png)
7. Press **Reconnect**. The application will try to connect to your bot via Direct Line Speech channel.
The message **New conversation started -- type or press the microphone button** will appear below the text bar if the connection succeeded.
8. You'll be prompted to allow microphone access. If you want to use the microphone, allow access.
9. Press the microphone icon to begin recording. While speaking, intermediate recognition results will be shown in the application. The microphone icon will turn red while recording is in progress. It will automatically detect end of speech and stop recording.
10. If everything works, you should see your bot's response on the screen and hear it speak the response. You can click on lines in the **Activity Log** window to see the full activity payload from the bot in JSON.
    **Note**: You'll only hear the bot's voice response if the [**Speak** field](https://github.com/Microsoft/botframework-sdk/blob/master/specs/botframework-activity/botframework-activity.md#speak) in the bot's output activity was set.

    ![Main Page](docs/media/MainPageWithActivity.png)

## Troubleshooting

See _Debugging_ section in [Voice-first virtual assistants Preview: Frequently asked questions](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/faq-voice-first-virtual-assistants)

## Resources
- [Bot Framework](https://dev.botframework.com/) docs:
  - [About Direct Line](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/get-started)
  - [Connect a bot to Direct Line Speech](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-channel-connect-directlinespeech?view=azure-bot-service-4.0)
  - [Use Direct Line Speech in your bot](https://docs.microsoft.com/en-us/azure/bot-service/directline-speech-bot?view=azure-bot-service-4.0)
  - [Bot-Framework Activities](https://github.com/Microsoft/botframework-sdk/blob/master/specs/botframework-activity/botframework-activity.md)
- [Speech SDK](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-sdk) docs:
  - [About custom voice-first virtual assistants](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/voice-first-virtual-assistants)
  - [Voice-first virtual assistants: Frequently asked questions](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/faq-voice-first-virtual-assistants)
  - [Troubleshoot the Speech SDK](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/troubleshooting)
  - [Quickstart: Create a voice-first virtual assistant with the Speech SDK, UWP](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/quickstart-virtual-assistant-csharp-uwp)
  - [Quickstart: Create a voice-first virtual assistant with the Speech SDK, Java](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/quickstart-virtual-assistant-java-jre)
  - [Quickstart: Create a voice-first virtual assistant in Java on Android by using the Speech SDK](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/quickstart-virtual-assistant-java-android)
  - [Create a custom wake word by using the Speech service](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-devices-sdk-create-kws)
  - [Try Speech Services for free](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/get-started)
  - [Language and region support for the Speech Services](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support)
- [Adaptive Cards](https://adaptivecards.io/)
