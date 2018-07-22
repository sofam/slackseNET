using System;
using System.Threading;
using System.Threading.Tasks;
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

        static CancellationTokenSource ReadOutputCancellationTokenSource;
        static CancellationTokenSource SaveBrainCancellationTokenSource;

        static MegaHALSlackClient client;
        static void Main(string[] args)
        {
            SaveBrainCancellationTokenSource = new CancellationTokenSource();
            ReadOutputCancellationTokenSource = new CancellationTokenSource();


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
                ReadOutputCancellationTokenSource.Cancel();
                SaveBrainCancellationTokenSource.Cancel();
                Console.WriteLine("Threads aborted");
                Console.WriteLine("stdin mutex locked");
                MegaHALInput.WriteLine("#SAVE");
                MegaHALInput.Flush();
                MegaHALInput.Close();
                MegaHALOutput.Close();
                MegaHAL.Close();
            };

            client = new MegaHALSlackClient(SlackseNETConfiguration);
            // Check if the token is valid
            client.Connect();
            // Get the channel id and store it in the config object

            SlackseNETConfiguration.ChannelId = client.GetChannelId(SlackseNETConfiguration.Channel);


            // Register event handlers
            client.OnMessageReceived += (message) =>
            {
                // If the message starts with a slack quote sign, just ignore it 
                if (message.text.StartsWith('>'))
                {
                    return;
                }
                
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


            ReadOutput(ReadOutputCancellationTokenSource.Token);
            System.Threading.Tasks.Task.Run(() => SaveBrain(SaveBrainCancellationTokenSource.Token));


            _quitEvent.WaitOne();



        }
        static async void ReadOutput(CancellationToken token)
        {
            try
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    var Response = await MegaHALOutput.ReadLineAsync();
                    if(Response == null)
                    {
                        return;
                    }
                    client.SendMessage(Response, SlackseNETConfiguration.ChannelId);
                    Console.WriteLine(Response);
                }
            }
            catch (System.OperationCanceledException)
            {
                Console.WriteLine("ReadOutput thread aborted");
                return;
            }
        }

        private static void SaveBrain(CancellationToken token)
        {
            try
            {
                while (true)
                {
                    token.WaitHandle.WaitOne(300000);
                    token.ThrowIfCancellationRequested();
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
            catch (System.OperationCanceledException)
            {
                Console.WriteLine("SaveBrain thread aborted");
                return;
            }
        }
    }
}
