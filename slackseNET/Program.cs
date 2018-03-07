using System;
using System.Threading;
using System.IO;
using SlackAPI;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace slackseNET
{
    public class SlackseConfiguration
    {
        public string Token { get; set; }
        public string Channel { get; set; }
        public string ChannelId { get; set; }
        public bool SendMessageToChannelOnSave { get; set; } = false;
    }
    class Program
    {

        private static Mutex StandardInputMutex = new Mutex();
        static ManualResetEvent _quitEvent = new ManualResetEvent(false);
        static MegaHALHandler MegaHAL = new MegaHALHandler();
        static MegaHALSlackWrapper SlackWrapper = new MegaHALSlackWrapper(MegaHAL);
        static StreamReader MegaHALOutput = SlackWrapper.GetStandardOutput();
        static StreamWriter MegaHALInput = SlackWrapper.GetStandardInput();

        static private SlackseConfiguration SlackseNETConfiguration = new SlackseConfiguration();

        static MegaHALSlackClient client;
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

            client = new MegaHALSlackClient(SlackseNETConfiguration);
            // Check if the token is valid
            client.Connect();
            // Get the channel id and store it in the config object

            SlackseNETConfiguration.ChannelId = client.GetChannelId(SlackseNETConfiguration.Channel);


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
                if (message.text.Contains(client.ClientId))
                {
                    StandardInputMutex.WaitOne();
                    MegaHALInput.WriteLine(message.text);
                    MegaHALInput.Flush();
                    StandardInputMutex.ReleaseMutex();
                }
            };

            // Start the client thread stuff, just read output from megahal and periodically save the brain
            ReadOutput();
            System.Threading.Tasks.Task.Run(() => SaveBrain());
            
            _quitEvent.WaitOne();



        }
        static async void ReadOutput()
        {
            while (true)
            {
                var Response = await MegaHALOutput.ReadLineAsync();
                client.SendMessage(Response, SlackseNETConfiguration.ChannelId);
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
                    client.SendMessage("Saving brain...", SlackseNETConfiguration.ChannelId);
                }
                StandardInputMutex.ReleaseMutex();
            }
        }
    }
}
