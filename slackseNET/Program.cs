using System;
using System.Threading;
using System.IO;
using SlackAPI;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace slackseNET
{
    class Program
    {
        public class SlackseConfiguration
        {
            public string Token { get; set; }
            public string Channel { get; set; }
            public string ChannelId { get; set; }
            public bool SendMessageToChannelOnSave { get; set; } = false;
        }
        private static Mutex StandardInputMutex = new Mutex();
        private static Mutex PongMutex = new Mutex();
        private static int PongMissed = 0;
        static ManualResetEvent _quitEvent = new ManualResetEvent(false);
        static MegaHALHandler MegaHAL = new MegaHALHandler();
        static MegaHALSlackWrapper SlackWrapper = new MegaHALSlackWrapper(MegaHAL);
        static StreamReader MegaHALOutput = SlackWrapper.GetStandardOutput();
        static StreamWriter MegaHALInput = SlackWrapper.GetStandardInput();

        static private SlackseConfiguration SlackseNETConfiguration = new SlackseConfiguration();

        static SlackSocketClient client;
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder().AddEnvironmentVariables("SLACKSE_").Build();
            config.Bind(SlackseNETConfiguration);
            // Exit if we don't have a token or a channel
            if (SlackseNETConfiguration.Token == "" || SlackseNETConfiguration.Channel == "")
            {
                Console.WriteLine("Need to set environment variables SLACKSE_TOKEN and SLACKSE_CHANNEL");
                return;
            }

            // Handle exits gracefully
            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("Shutting down MegaHAL...");
                StandardInputMutex.WaitOne();
                MegaHALInput.WriteLine("#SAVE");
                MegaHALInput.Flush();
                MegaHAL.Close();
                StandardInputMutex.ReleaseMutex();
            };

            // Connect the client
            ManualResetEventSlim authReady = new ManualResetEventSlim(false);
            ManualResetEventSlim clientReady = new ManualResetEventSlim(false);
            bool authOk = false;
            client = new SlackSocketClient(SlackseNETConfiguration.Token);
            // Check if the token is valid
            client.TestAuth((authTestResponse) =>
            {
                if (!authTestResponse.ok)
                {
                    Console.WriteLine("Auth Error: {0}", authTestResponse.error);
                    authOk = authTestResponse.ok;
                    authReady.Set();
                }
                else
                {
                    authOk = authTestResponse.ok;
                    authReady.Set();
                }
            });
            authReady.Wait();
            if (!authOk)
            {
                return;
            }

            client.Connect((connected) =>
            {
                clientReady.Set();
            }, () =>
            {
                Console.WriteLine("I am connected");
            });

            clientReady.Wait();
            // Get the channel id and store it in the config object
            try
            {
                client.GetChannelList(null);
                var c = client.Channels.Find((channel) => (channel.name.Equals(SlackseNETConfiguration.Channel)));
                SlackseNETConfiguration.ChannelId = c.id;

                if (c == null)
                {
                    Console.WriteLine("Channel {0} not found, exiting", SlackseNETConfiguration.Channel);
                    return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

            // Register event handlers
            client.OnMessageReceived += (message) =>
            {
          // If it's not the bot speaking, silently learn what's being said on the channel
          if (message.user != null)
                {
                    StandardInputMutex.WaitOne();
                    MegaHALInput.WriteLine("#LEARN");
                    MegaHALInput.WriteLine(message.text);
                    MegaHALInput.Flush();
                    StandardInputMutex.ReleaseMutex();
                }
          // If someone is talking to me specifically, learn what's being said and reply
          if (message.text.Contains(client.MySelf.id))
                {
                    StandardInputMutex.WaitOne();
                    MegaHALInput.WriteLine(message.text);
                    MegaHALInput.Flush();
                    StandardInputMutex.ReleaseMutex();
                }
            };

            client.OnPongReceived += (pong) =>
            {
                Console.WriteLine("Ping interval: {0}, Timeouts: {1}", pong.ping_interv_ms.ToString(), PongMissed);
                PongMutex.WaitOne();
                PongMissed = 0;
                PongMutex.ReleaseMutex();
            };
            // Start the client thread stuff, just read output from megahal and periodically save the brain
            ReadOutput();
            System.Threading.Tasks.Task.Run(() => SaveBrain());
            System.Threading.Tasks.Task.Run(() => CheckAlive());
            _quitEvent.WaitOne();



        }
        static async void ReadOutput()
        {
            while (true)
            {
                var Response = await MegaHALOutput.ReadLineAsync();
                client.SendMessage((mr) => Console.WriteLine("Got response {0}", Response), SlackseNETConfiguration.ChannelId, Response);
                Console.WriteLine(Response);
            }
        }

        private static void SaveBrain()
        {
            while (true)
            {
                Thread.Sleep(300000);
                StandardInputMutex.WaitOne();
                MegaHALInput.WriteLine("#SAVE");
                MegaHALInput.Flush();
                if (SlackseNETConfiguration.SendMessageToChannelOnSave)
                {
                    client.SendMessage((mr) => Console.WriteLine("Saving brain..."), SlackseNETConfiguration.ChannelId, "Saving brain...");
                }
                StandardInputMutex.ReleaseMutex();
            }
        }

        private static void CheckAlive()
        {
            while (true)
            {
                if (PongMissed > 0)
                {
                  Console.WriteLine("No pong received for 30000 ms");
                }
                Thread.Sleep(30000);
                client.SendPing();
                PongMutex.WaitOne();
                PongMissed += 1;
                PongMutex.ReleaseMutex();
            }
        }
    }
}
