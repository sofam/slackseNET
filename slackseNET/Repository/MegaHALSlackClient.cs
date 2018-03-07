using System;
using System.Threading;
using SlackAPI;

namespace slackseNET
{
    public class MegaHALSlackClientAuthenticationException: Exception
    {
        public MegaHALSlackClientAuthenticationException()
        {

        }

        public MegaHALSlackClientAuthenticationException(string message) 
        : base(message)
        {

        }

        public MegaHALSlackClientAuthenticationException(string message, Exception inner)
        : base(message, inner)
        {

        }

    }
    public class MegaHALSlackClient
    {
        private static SlackseConfiguration SlackseConfig; 
        private static SlackSocketClient SlackClient;

        public string ClientId {
            get
            {
                return SlackClient.MySelf.id;
            }
        }
        
        private static int PongMissed = 0;

        private static Mutex PongMutex = new Mutex();
        public MegaHALSlackClient(SlackseConfiguration configuration)
        {
            SlackseConfig = configuration;
            SlackClient = new SlackSocketClient(SlackseConfig.Token);
            
        }

        public void Connect() 
        {
            try
            {
                TestAuth();
            }
            catch (MegaHALSlackClientAuthenticationException)
            {
                throw;
            }

            ManualResetEventSlim clientReady = new ManualResetEventSlim(false);
            SlackClient.Connect((connected) => {
                clientReady.Set();
            }, () => 
            {
                
            });

            clientReady.Wait();
            System.Threading.Tasks.Task.Run(() => CheckAlive());
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
                SlackClient.SendPing();
                PongMutex.WaitOne();
                PongMissed += 1;
                PongMutex.ReleaseMutex();
            }
        }

        public void Close()
        {
            SlackClient.CloseSocket();
        }

        public string GetChannelId(string name)
        {
            SlackClient.GetChannelList((cl) => {});
            var c = SlackClient.Channels.Find((channel) => (channel.name.Equals(name)));
            return c.id;
        }

        public void SendMessage(string message, string channelId)
        {
            SlackClient.SendMessage((mr) => {}, channelId, message);
        }

        public event Action<SlackAPI.WebSocketMessages.NewMessage> OnMessageReceived
        {
            add
            {
                SlackClient.OnMessageReceived += value;
            }
            remove
            {
                SlackClient.OnMessageReceived -= value;
            }
        }

        private void TestAuth()
        {
            ManualResetEventSlim authReady = new ManualResetEventSlim(false);
            SlackClient.TestAuth((authTestResponse) => {
                if(!authTestResponse.ok)
                {
                    authReady.Set();
                }
                else
                {
                    throw new MegaHALSlackClientAuthenticationException();
                }
            });
            authReady.Wait();
        }
    }
}