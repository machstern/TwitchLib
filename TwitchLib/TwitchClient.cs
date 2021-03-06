﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Meebey.SmartIrc4net;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using TwitchLib.Exceptions;
using System.Text;
using TwitchLib.TwitchClientClasses;

namespace TwitchLib
{
    /// <summary>Represents a client connected to a Twitch channel.</summary>
    public class TwitchClient
    {
        private IrcConnection _client = new IrcConnection();
        private ConnectionCredentials _credentials;
        private ChannelState _state;
        private string _channel;
        private List<char> _chatCommandIdentifiers = new List<char>();
        private List<char> _whisperCommandIdentifiers = new List<char>();
        private ChatMessage _previousMessage;
        private WhisperMessage _previousWhisper;
        private bool _logging, _connected;
        private MessageEmoteCollection _channelEmotes = new MessageEmoteCollection();

        /// <summary>Object representing current state of channel (r9k, slow, etc).</summary>
        public ChannelState ChannelState => _state;
        /// <summary>The current channel the TwitcChatClient is connected to.</summary>
        public string Channel => _channel;
        /// <summary>Username of the user connected via this library.</summary>
        public string TwitchUsername => _credentials.TwitchUsername;
        /// <summary>The most recent message received.</summary>
        public ChatMessage PreviousMessage => _previousMessage;
        /// <summary>The most recent whisper received.</summary>
        public WhisperMessage PreviousWhisper => _previousWhisper;
        /// <summary>The current connection status of the client.</summary>
        public bool IsConnected => _connected;
        /// <summary>Assign this property a valid MessageThrottler to apply message throttling on chat messages.</summary>
        public Services.MessageThrottler ChatThrottler;
        /// <summary>Assign this property a valid MessageThrottler to apply message throttling on whispers.</summary>
        public Services.MessageThrottler WhisperThrottler;
        /// <summary>The emotes this channel replaces.</summary>
        /// <remarks>
        ///     Twitch-handled emotes are automatically added to this collection (which also accounts for
        ///     managing user emote permissions such as sub-only emotes). Third-party emotes will have to be manually
        ///     added according to the availability rules defined by the third-party.
        /// </remarks>
        public MessageEmoteCollection ChannelEmotes => _channelEmotes;

        /// <summary>Will disable the client from sending automatic PONG responses to PING</summary>
        public bool DisableAutoPong { get; set; } = false;

        /// <summary>Determines whether Emotes will be replaced in messages.</summary>
        public bool WillReplaceEmotes { get; set; } = false;

        /// <summary>
        /// Fires on listening and after joined channel, returns username and channel.
        /// </summary>
        public event EventHandler<OnConnectedArgs> OnConnected;

        /// <summary>
        /// Fires on logging in with incorrect details, returns ErrorLoggingInException.
        /// </summary>
        public event EventHandler<OnIncorrectLoginArgs> OnIncorrectLogin;

        /// <summary>
        /// Fires when connecting and channel state is changed, returns ChannelState.
        /// </summary>
        public event EventHandler<OnChannelStateChangedArgs> OnChannelStateChanged;

        /// <summary>
        /// Fires when a user state is received, returns UserState.
        /// </summary>
        public event EventHandler<OnUserStateChangedArgs> OnUserStateChanged;

        /// <summary>
        /// Fires when a new chat message arrives, returns ChatMessage.
        /// </summary>
        public event EventHandler<OnMessageReceivedArgs> OnMessageReceived;

        /// <summary>
        /// Fires when a new whisper arrives, returns WhisperMessage.
        /// </summary>
        public event EventHandler<OnWhisperReceivedArgs> OnWhisperReceived;

        /// <summary>
        /// Fires when a chat message is sent, returns username, channel and message.
        /// </summary>
        public event EventHandler<OnMessageSentArgs> OnMessageSent;

        /// <summary>
        /// Fires when a whisper message is sent, returns username and message.
        /// </summary>
        public event EventHandler<OnWhisperSentArgs> OnWhisperSent;

        /// <summary>
        /// Fires when command (uses custom chat command identifier) is received, returns channel, command, ChatMessage, arguments as string, arguments as list.
        /// </summary>
        public event EventHandler<OnChatCommandReceivedArgs> OnChatCommandReceived;

        /// <summary>
        /// Fires when command (uses custom whisper command identifier) is received, returns command, Whispermessage.
        /// </summary>
        public event EventHandler<OnWhisperCommandReceivedArgs> OnWhisperCommandReceived;

        /// <summary>
        /// Fires when a new viewer/chatter joined the channel's chat room, returns username and channel.
        /// </summary>
        public event EventHandler<OnViewerJoinedArgs> OnViewerJoined;

        /// <summary>
        /// Fires when a moderator joined the channel's chat room, returns username and channel.
        /// </summary>
        public event EventHandler<OnModeratorJoinedArgs> OnModeratorJoined;

        /// <summary>
        /// Fires when a moderator joins the channel's chat room, returns username and channel.
        /// </summary>
        public event EventHandler<OnModeratorLeftArgs> OnModeratorLeft;

        /// <summary>
        /// Fires when new subscriber is announced in chat, returns Subscriber.
        /// </summary>
        public event EventHandler<OnNewSubscriberArgs> OnNewSubscriber;

        /// <summary>
        /// Fires when current subscriber renews subscription, returns ReSubscriber.
        /// </summary>
        public event EventHandler<OnReSubscriberArgs> OnReSubscriber;

        /// <summary>
        /// Fires when a hosted streamer goes offline and hosting is killed.
        /// </summary>
        public event EventHandler OnHostLeft;

        /// <summary>
        /// Fires when Twitch notifies client of existing users in chat.
        /// </summary>
        public event EventHandler<OnExistingUsersDetectedArgs> OnExistingUsersDetected;

        /// <summary>
        /// Fires when a PART message is received from Twitch regarding a particular viewer
        /// </summary>
        public event EventHandler<OnViewerLeftArgs> OnViewerLeft;

        /// <summary>
        /// Fires when a channel got hosted by another channel.
        /// </summary>
        public event EventHandler<OnHostingStartedArgs> OnHostingStarted;

        /// <summary>
        /// Fires when a channel is not being streamed by another channel anymore.
        /// </summary>
        public event EventHandler<OnHostingStoppedArgs> OnHostingStopped;

        /// <summary>Args representing on connected event.</summary>
        public class OnConnectedArgs : EventArgs
        {
            /// <summary>Property representing bot username.</summary>
            public string Username;
            /// <summary>Property representing connected channel.</summary>
            public string Channel;
        }

        /// <summary>Args representing an incorrect login event.</summary>
        public class OnIncorrectLoginArgs : EventArgs
        {
            /// <summary>Property representing exception object.</summary>
            public ErrorLoggingInException Exception;
        }

        /// <summary>Args representing on channel state changed event.</summary>
        public class OnChannelStateChangedArgs : EventArgs
        {
            /// <summary>Property representing the current channel state.</summary>
            public ChannelState ChannelState;
            /// <summary>Property representing the channel received state from.</summary>
            public string Channel;
        }

        /// <summary>Args representing on user state changed event.</summary>
        public class OnUserStateChangedArgs : EventArgs
        {
            /// <summary>Property representing user state object.</summary>
            public UserState UserState;
        }

        /// <summary>Args representing message received event.</summary>
        public class OnMessageReceivedArgs : EventArgs
        {
            /// <summary>Property representing received chat message.</summary>
            public ChatMessage ChatMessage;
        }

        /// <summary></summary>
        public class OnWhisperReceivedArgs : EventArgs
        {
            /// <summary></summary>
            public WhisperMessage WhisperMessage;
        }

        /// <summary>Args representing message sent event.</summary>
        public class OnMessageSentArgs : EventArgs
        {
            /// <summary>Property representing username of bot.</summary>
            public string Username;
            /// <summary>Property representing channel of connected bot.</summary>
            public string Channel;
            /// <summary>Property representing sent message contents.</summary>
            public string Message;
        }

        /// <summary>Args representing whisper sent event.</summary>
        public class OnWhisperSentArgs : EventArgs
        {
            /// <summary>Property representing username of bot.</summary>
            public string Username;
            /// <summary>Property representing receiver of the whisper.</summary>
            public string Receiver;
            /// <summary>Property representing sent message contents.</summary>
            public string Message;
        }

        /// <summary>Args representing chat command received event.</summary>
        public class OnChatCommandReceivedArgs : EventArgs
        {
            /// <summary>Property representing chat message object.</summary>
            public ChatMessage ChatMessage;
            /// <summary>Property representing channel of connected bot.</summary>
            public string Channel;
            /// <summary>Property representing received command.</summary>
            public string Command;
            /// <summary>Property representing arguements in form of string.</summary>
            public string ArgumentsAsString;
            /// <summary>Property representing arguements in form of string list.</summary>
            public List<string> ArgumentsAsList;
            /// <summary>Property representing the character command identifier.</summary>
            public char CommandIdentifier;
        }

        /// <summary>Args representing whisper command received event.</summary>
        public class OnWhisperCommandReceivedArgs : EventArgs
        {
            /// <summary>Property representing chat message object.</summary>
            public WhisperMessage WhisperMessage;
            /// <summary>Property representing received command.</summary>
            public string Command;
            /// <summary>Property representing arguements in form of string.</summary>
            public string ArgumentsAsString;
            /// <summary>Property representing arguements in form of string list.</summary>
            public List<string> ArgumentsAsList;
            /// <summary>Property representing the character command identifier.</summary>
            public char CommandIdentifier;
        }

        /// <summary>Args representing viewer joined event.</summary>
        public class OnViewerJoinedArgs : EventArgs
        {
            /// <summary>Property representing username of joined viewer.</summary>
            public string Username;
            /// <summary>Property representing channel bot is connected to.</summary>
            public string Channel;
        }

        /// <summary>Args representing moderator joined event.</summary>
        public class OnModeratorJoinedArgs : EventArgs
        {
            /// <summary>Property representing username of joined moderator.</summary>
            public string Username;
            /// <summary>Property representing channel bot is connected to.</summary>
            public string Channel;
        }

        /// <summary>Args representing moderator leave event.</summary>
        public class OnModeratorLeftArgs : EventArgs
        {
            /// <summary>Property representing username of moderator that left..</summary>
            public string Username;
            /// <summary>Property representing channel bot is connected to.</summary>
            public string Channel;
        }

        /// <summary>Args representing new subscriber event.</summary>
        public class OnNewSubscriberArgs : EventArgs
        {
            /// <summary>Property representing subscriber object.</summary>
            public NewSubscriber Subscriber;
            /// <summary>Property representing channel bot is connected to.</summary>
            public string Channel;
        }

        /// <summary>Args representing resubscriber event.</summary>
        public class OnReSubscriberArgs : EventArgs
        {
            /// <summary>Property representing resubscriber object.</summary>
            public ReSubscriber ReSubscriber;
        }

        /// <summary>Args representing existing user(s) detected event.</summary>
        public class OnExistingUsersDetectedArgs : EventArgs
        {
            /// <summary>Property representing string list of existing users.</summary>
            public List<string> ExistingUsers;
            /// <summary>Property representing channel bot is connected to.</summary>
            public string Channel;
        }

        /// <summary>Args representing viewer left event.</summary>
        public class OnViewerLeftArgs : EventArgs
        {
            /// <summary>Property representing username of user that left.</summary>
            public string Username;
            /// <summary>Property representing channel bot is connected to.</summary>
            public string Channel;
        }

        /// <summary>Args representing hosting started event.</summary>
        public class OnHostingStartedArgs
        {
            /// <summary>Property representing channel that started hosting.</summary>
            public string HostingChannel;
            /// <summary>Property representing targeted channel, channel being hosted.</summary>
            public string TargetChannel;
            /// <summary>Property representing number of viewers in channel hosting target channel.</summary>
            public int Viewers;
        }

        /// <summary>Args representing hosting stopped event.</summary>
        public class OnHostingStoppedArgs
        {
            /// <summary>Property representing hosting channel.</summary>
            public string HostingChannel;
            /// <summary>Property representing number of viewers that were in hosting channel.</summary>
            public int Viewers;
        }

        /// <summary>
        /// Initializes the TwitchChatClient class.
        /// </summary>
        /// <param name="channel">The channel to connect to.</param>
        /// <param name="credentials">The credentials to use to log in.</param>
        /// <param name="commandIdentifier">The identifier to be used for reading and writing commands.</param>
        /// <param name="logging">Whether or not logging to console should be enabled.</param>
        public TwitchClient(string channel, ConnectionCredentials credentials, char commandIdentifier = '\0',
            bool logging = false)
        {
            _channel = channel.ToLower();
            _credentials = credentials;
            if(commandIdentifier != '\0')
                _chatCommandIdentifiers.Add(commandIdentifier);
            _logging = logging;

            _client.AutoReconnect = true;
            _client.OnConnected += Connected;
            _client.OnReadLine += OnReadLine;
        }

        /// <summary>
        /// Depending in the parameter, either enables or disables logging to the debug console.
        /// </summary>
        /// <param name="loggingStatus">True to enable logging, false to disable logging.</param>
        public void SetLoggingStatus(bool loggingStatus)
        {
            _logging = loggingStatus;
        }

        /// <summary>
        /// Sends a RAW IRC message.
        /// </summary>
        /// <param name="message">The RAW message to be sent.</param>
        public void SendRaw(string message)
        {
            if(ChatThrottler == null || !ChatThrottler.ApplyThrottlingToRawMessages || ChatThrottler.MessagePermitted(message))
                _client.WriteLine(message);
        }

        /// <summary>
        /// Sends a formatted Twitch channel chat message.
        /// </summary>
        /// <param name="message">The message to be sent.</param>
        /// <param name="dryRun">If set to true, the message will not actually be sent for testing purposes.</param>
        public void SendMessage(string message, bool dryRun = false)
        {
            if (dryRun) return;
            if (ChatThrottler != null && !ChatThrottler.MessagePermitted(message)) return;
            string twitchMessage = $":{_credentials.TwitchUsername}!{_credentials.TwitchUsername}@{_credentials.TwitchUsername}" +
                $".tmi.twitch.tv PRIVMSG #{_channel} :{message}";
            // This is a makeshift hack to encode it with accomodations for at least cyrillic characters, and possibly others
            _client.WriteLine(Encoding.Default.GetString(Encoding.UTF8.GetBytes(twitchMessage)));
            OnMessageSent?.Invoke(null,
                new OnMessageSentArgs {Username = _credentials.TwitchUsername, Channel = _channel, Message = message});
        }

        /// <summary>
        /// Sends a formatted whisper message to someone.
        /// </summary>
        /// <param name="receiver">The receiver of the whisper.</param>
        /// <param name="message">The message to be sent.</param>
        /// <param name="dryRun">If set to true, the message will not actually be sent for testing purposes.</param>
        public void SendWhisper(string receiver, string message, bool dryRun = false)
        {
            if (dryRun) return;
            if (WhisperThrottler != null && !WhisperThrottler.MessagePermitted(message)) return;
            string twitchMessage = $":{_credentials.TwitchUsername}~{_credentials.TwitchUsername}@{_credentials.TwitchUsername}" +
                $".tmi.twitch.tv PRIVMSG #jtv :/w {receiver} {message}";
            // This is a makeshift hack to encode it with accomodations for at least cyrillic, and possibly others
            _client.WriteLine(Encoding.Default.GetString(Encoding.UTF8.GetBytes(twitchMessage)));
            OnWhisperSent?.Invoke(null, new OnWhisperSentArgs { Receiver = receiver, Message = message });
        }

        /// <summary>
        /// Start connecting to the Twitch IRC chat.
        /// </summary>
        public void Connect()
        {
            if (_logging)
                Console.WriteLine("Connecting to: " + _credentials.TwitchHost + ":" + _credentials.TwitchPort);
            _client.Connect(_credentials.TwitchHost, _credentials.TwitchPort);
        }

        /// <summary>
        /// Start disconnecting from the Twitch IRC chat.
        /// </summary>
        public void Disconnect()
        {
            if (_logging)
                Console.WriteLine("Disconnect Twitch Chat Client...");
            _client.Disconnect();
            _connected = false;
        }

        /// <summary>
        /// Reconnects to Twitch channel given existing login credentials
        /// </summary>
        public void Reconnect()
        {
            if (_logging)
                Console.WriteLine("Reconnecting to: " + _credentials.TwitchHost + ":" + _credentials.TwitchPort);
            _client.Connect(_credentials.TwitchHost, _credentials.TwitchPort);
        }

        /// <summary>
        /// Adds a character to a list of characters that if found at the start of a message, fires command received event.
        /// </summary>
        /// <param name="identifier">Character, that if found at start of message, fires command received event.</param>
        public void AddChatCommandIdentifier(char identifier)
        {
            _chatCommandIdentifiers.Add(identifier);
        }

        /// <summary>
        /// Removes a character from a list of characters that if found at the start of a message, fires command received event.
        /// </summary>
        /// <param name="identifier">Command identifier to removed from identifier list.</param>
        public void RemoveChatCommandIdentifier(char identifier)
        {
            _chatCommandIdentifiers.Remove(identifier);
        }

        /// <summary>
        /// Adds a character to a list of characters that if found at the start of a whisper, fires command received event.
        /// </summary>
        /// <param name="identifier">Character, that if found at start of message, fires command received event.</param>
        public void AddWhisperCommandIdentifier(char identifier)
        {
            _whisperCommandIdentifiers.Add(identifier);
        }

        /// <summary>
        /// Removes a character to a list of characters that if found at the start of a whisper, fires command received event.
        /// </summary>
        /// <param name="identifier">Command identifier to removed from identifier list.</param>
        public void RemoveWhisperCommandIdentifier(char identifier)
        {
            _whisperCommandIdentifiers.Remove(identifier);
        }

        /// <summary>
        /// Join the Twitch IRC chat of <paramref name="channel"/>.
        /// </summary>
        /// <param name="channel">The channel to join.</param>
        public void JoinChannel(string channel)
        {
            if (_logging)
                Console.WriteLine("[TwitchLib] Joining channel: " + channel);
            _client.WriteLine($"/join #{channel}");
        }

        private void Connected(object sender, EventArgs e)
        {
            _client.WriteLine(Rfc2812.Pass(_credentials.TwitchOAuth), Priority.Critical);
            _client.WriteLine(Rfc2812.Nick(_credentials.TwitchUsername), Priority.Critical);
            _client.WriteLine(Rfc2812.User(_credentials.TwitchUsername, 0, _credentials.TwitchUsername),
                Priority.Critical);

            _client.WriteLine("CAP REQ twitch.tv/membership");
            _client.WriteLine("CAP REQ twitch.tv/commands");
            _client.WriteLine("CAP REQ twitch.tv/tags");

            _client.WriteLine(Rfc2812.Join($"#{_channel}"));

            Task.Factory.StartNew(() => _client.Listen());
        }

        private void OnReadLine(object sender, ReadLineEventArgs e)
        {
            // Hack to accomodate at least cyrillic characters, possibly more
            string decodedMessage = Encoding.UTF8.GetString(Encoding.Default.GetBytes(e.Line));
            if (_logging)
                Console.WriteLine(decodedMessage);
            
            #region Chat Parsing
            // On Connected
            if (ChatParsing.detectConnected(decodedMessage))
            {
                _connected = true;
                OnConnected?.Invoke(null, new OnConnectedArgs { Channel = _channel, Username = TwitchUsername });
                return;
            }

            // On New Subscriber
            if (ChatParsing.detectNewSubscriber(decodedMessage, _channel))
            {
                OnNewSubscriber?.Invoke(null, new OnNewSubscriberArgs { Subscriber = new NewSubscriber(decodedMessage), Channel = _channel });
                return;
            }

            // On Command Received (PURPOSELY DROP THROUGH WITHOUT RETURN)
            if (ChatParsing.detectCommandReceived(decodedMessage, _channel, _channelEmotes, WillReplaceEmotes, _chatCommandIdentifiers))
            {
                var chatMessage = new ChatMessage(decodedMessage, ref _channelEmotes, WillReplaceEmotes);
                string command = chatMessage.Message.Split(' ')?[0].Substring(1, chatMessage.Message.Split(' ')[0].Length - 1) ?? chatMessage.Message.Substring(1, chatMessage.Message.Length - 1);
                var argumentsAsList = chatMessage.Message.Split(' ')?.Where(arg => arg != chatMessage.Message[0] + command).ToList<string>() ?? new List<string>();
                string argumentsAsString = chatMessage.Message.Replace(chatMessage.Message.Split(' ')?[0] + " ", "") ?? "";
                OnChatCommandReceived?.Invoke(null, new OnChatCommandReceivedArgs { Command = command, ChatMessage = chatMessage, Channel = _channel, ArgumentsAsList = argumentsAsList, ArgumentsAsString = argumentsAsString });
                // purposely drop through without return
            }

            // On Message Received
            if (ChatParsing.detectMessageReceived(decodedMessage, _channel))
            {
                var chatMessage = new ChatMessage(decodedMessage, ref _channelEmotes, WillReplaceEmotes);
                _previousMessage = chatMessage;
                OnMessageReceived?.Invoke(null, new OnMessageReceivedArgs { ChatMessage = chatMessage });
                return;
            }

            // On Viewer Joined
            if (ChatParsing.detectViewerJoined(decodedMessage, _channel))
            {
                OnViewerJoined?.Invoke(null, new OnViewerJoinedArgs { Username = decodedMessage.Split('!')[1].Split('@')[0], Channel = _channel });
                return;
            }

            // On Viewer Left
            if (ChatParsing.detectedViewerLeft(decodedMessage, _channel))
            {
                OnViewerLeft?.Invoke(null, new OnViewerLeftArgs { Username = decodedMessage.Split(':')[1].Split('!')[0], Channel = _channel });
                return;
            }

            // On Moderator Joined
            if (ChatParsing.detectedModeratorJoined(decodedMessage, _channel))
            {
                OnModeratorJoined?.Invoke(null, new OnModeratorJoinedArgs { Username = decodedMessage.Split(' ')[4], Channel = _channel });
                return;
            }

            // On Moderator Left
            if(ChatParsing.detectedModeatorLeft(decodedMessage, _channel))
            {
                OnModeratorLeft?.Invoke(null, new OnModeratorLeftArgs { Username = decodedMessage.Split(' ')[4], Channel = _channel });
                return;
            }

            // On Incorrect login
            if (ChatParsing.detectedIncorrectLogin(decodedMessage, _channel))
            {
                _client.Disconnect();
                OnIncorrectLogin?.Invoke(null, new OnIncorrectLoginArgs { Exception = new ErrorLoggingInException(decodedMessage, _credentials.TwitchUsername) });
                return;
            }

            // On Host Left
            if (ChatParsing.detectedHostLeft(decodedMessage, _channel))
            {
                OnHostLeft?.Invoke(null, null);
                return;
            }

            // On Channel State Changed
            if (ChatParsing.detectedChannelStateChanged(decodedMessage, _channel))
            {
                OnChannelStateChanged?.Invoke(null, new OnChannelStateChangedArgs { ChannelState = new ChannelState(decodedMessage) });
                return;
            }

            // On User State Changed
            if(ChatParsing.detectedUserStateChanged(decodedMessage, _channel))
            {
                OnUserStateChanged?.Invoke(null, new OnUserStateChangedArgs { UserState = new UserState(decodedMessage) });
                return;
            }

            // On ReSubscriber
            if (ChatParsing.detectedReSubscriber(decodedMessage, _channel))
            {
                OnReSubscriber?.Invoke(null, new OnReSubscriberArgs { ReSubscriber = new ReSubscriber(decodedMessage) });
                return;
            }

            // On PING received
            if(ChatParsing.detectedPing(decodedMessage) && !DisableAutoPong)
            {
                SendRaw("PONG :tmi.twitch.tv");
                return;
            }

            // On Hosting Stopped
            if(ChatParsing.detectedHostingStopped(decodedMessage))
            {
                int viewers;
                int.TryParse(decodedMessage.Split(' ')[4], out viewers);
                OnHostingStopped?.Invoke(null, new OnHostingStoppedArgs() { Viewers = viewers, HostingChannel = decodedMessage.Split(' ')[2].Remove(0, 1) });
                return;
            }

            // On Hosting Started
            if(ChatParsing.detectedHostingStarted(decodedMessage))
            {
                int viewers;
                int.TryParse(decodedMessage.Split(' ')[4], out viewers);
                OnHostingStarted?.Invoke(null, new OnHostingStartedArgs() { Viewers = viewers, HostingChannel = decodedMessage.Split(' ')[2].Remove(0, 1), TargetChannel = decodedMessage.Split(' ')[3].Remove(0, 1) });
                return;
            }

            // On Existing Users Detected
            if(ChatParsing.detectedExistingUsers(decodedMessage, _credentials.TwitchUsername))
            {
                var parsedUsers = decodedMessage.Replace($":{_credentials.TwitchUsername}.tmi.twitch.tv 353 {_credentials.TwitchUsername} = #{_channel} :", "").Split(' ');
                OnExistingUsersDetected?.Invoke(null, new OnExistingUsersDetectedArgs { Channel = _channel,
                    ExistingUsers = decodedMessage.Replace($":{_credentials.TwitchUsername}.tmi.twitch.tv 353 {_credentials.TwitchUsername} = #{_channel} :", "").Split(' ').ToList<string>() });
                return;
            }
            #endregion

            #region Whisper Parsing
            if(decodedMessage.Split(' ').Count() > 2 && (decodedMessage.Split(' ')[1] == "WHISPER" || decodedMessage.Split(' ')[2] == "WHISPER")) {

                // On Whisper Message Received
                if(WhisperParsing.detectedWhisperReceived(decodedMessage, _credentials.TwitchUsername))
                {
                    OnWhisperReceived?.Invoke(null, new OnWhisperReceivedArgs { WhisperMessage = new WhisperMessage(decodedMessage, _credentials.TwitchUsername) });
                    return;
                }

                // On Whisper Command Received
                if(WhisperParsing.detectedWhisperCommandReceived(decodedMessage, _credentials.TwitchUsername, _whisperCommandIdentifiers))
                {
                    var whisperMessage = new WhisperMessage(decodedMessage, _credentials.TwitchUsername);
                    string command = whisperMessage.Message.Split(' ')?[0].Substring(1, whisperMessage.Message.Split(' ')[0].Length - 1) ?? whisperMessage.Message.Substring(1, whisperMessage.Message.Length - 1);
                    var argumentsAsList = whisperMessage.Message.Split(' ')?.Where(arg => arg != whisperMessage.Message[0] + command).ToList<string>() ?? new List<string>();
                    string argumentsAsString = whisperMessage.Message.Replace(whisperMessage.Message.Split(' ')?[0] + " ", "") ?? "";
                    OnWhisperCommandReceived?.Invoke(null, new OnWhisperCommandReceivedArgs { Command = command, WhisperMessage = whisperMessage, ArgumentsAsList = argumentsAsList, ArgumentsAsString = argumentsAsString });
                }
                
            }
            #endregion  

            // Any other messages here
            if (_logging)
                Console.WriteLine($"Unaccounted for: {decodedMessage}");
            
        }
    }
}