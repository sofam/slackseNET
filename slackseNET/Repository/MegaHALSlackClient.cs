using System;
using System.Threading;
using SlackAPI;

namespace slackseNET
{
    public class MegaHALSlackClientAuthenticationException : Exception
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

        public string ClientId
        {
            get
            {
                return SlackClient.MySelf.id;
            }
        }

        private static int PongMissed = 0;
        private static CancellationTokenSource PongCancellationTokenSource;
        private static Mutex PongMutex = new Mutex();
        public MegaHALSlackClient(SlackseConfiguration configuration)
        {
            SlackseConfig = configuration;
            SlackClient = new SlackSocketClient(SlackseConfig.Token);

        }

        public void Connect()
        {
            Console.WriteLine("Starting connect");
            try
            {
                TestAuth();
            }
            catch (MegaHALSlackClientAuthenticationException)
            {
                throw;
            }

            ManualResetEventSlim clientReady = new ManualResetEventSlim(false);
            SlackClient.Connect((connected) =>
            {
                clientReady.Set();
            }, () =>
            {
                Console.WriteLine("I am connected");
            });

            clientReady.Wait();
            PongMissed = 0;
            PongCancellationTokenSource = new CancellationTokenSource();
            System.Threading.Tasks.Task.Run(() => CheckAlive(PongCancellationTokenSource.Token));

            SlackClient.OnPongReceived += (pong) =>
            {
                PongHandler(pong);
            };
        }
        private static void PongHandler(SlackAPI.WebSocketMessages.Pong pong)
        {
            Console.WriteLine("Pong received, {0}", PongMissed);
            PongMutex.WaitOne();
            PongMissed = 0;
            PongMutex.ReleaseMutex();
        }

        private void CheckAlive(CancellationToken token)
        {
            try
            {
                while (true)
                {
                    if (PongMissed > 1)
                    {
                        Console.WriteLine("No pong received for 30000 ms, {0}", PongMissed);
                    }
                    if (PongMissed > 5)
                    {
                        Console.WriteLine("Missed {0} pongs, retrying connection", PongMissed);
                        this.Reconnect();
                        return;
                    }
                    Thread.Sleep(30000);
                    SlackClient.SendPing();
                    PongMutex.WaitOne();
                    PongMissed += 1;
                    PongMutex.ReleaseMutex();
                }
            }
            catch (System.OperationCanceledException)
            {
                Console.WriteLine("Cancelled the pong thread");
                return;
            }
        }

        public void Close()
        {
            PongCancellationTokenSource.Cancel();
            SlackClient.CloseSocket();
        }

        private void Reconnect()
        {
            SlackClient.CloseSocket();
            SlackClient = null;
            SlackClient = new SlackSocketClient(SlackseConfig.Token);
            SlackClient.OnMessageReceived += WrapperAction;
            PongCancellationTokenSource.Cancel();
            this.Connect();
        }

        public string GetChannelId(string name)
        {
            SlackClient.GetChannelList((cl) => { });
            var c = SlackClient.Channels.Find((channel) => (channel.name.Equals(name)));
            return c.id;
        }

        public void SendMessage(string message, string channelId)
        {
            SlackClient.SendMessage((mr) => { }, channelId, message);
        }

        private Action<SlackAPI.WebSocketMessages.NewMessage> WrapperAction;

        public event Action<SlackAPI.WebSocketMessages.NewMessage> OnMessageReceived
        {
            add
            {
                WrapperAction = value;
                SlackClient.OnMessageReceived += WrapperAction;
            }
            remove
            {
                SlackClient.OnMessageReceived -= WrapperAction;
                WrapperAction = null;
            }
        }

        private static void TestAuth()
        {
            ManualResetEventSlim authReady = new ManualResetEventSlim(false);
            SlackClient.TestAuth((authTestResponse) =>
            {
                if (authTestResponse.ok)
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