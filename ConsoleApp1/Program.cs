using Steamworks;
using Steamworks.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Test Steam Server!");

            var source = new CancellationTokenSource();
            new Program().StartSteamServer(source);

            Console.ReadKey();

            source.Cancel();
        }

        private async void StartSteamServer(CancellationTokenSource source)
        {
            var token = source.Token;

            void InitializeSteamServer()
            {
                const int GAME_PORT = 30001;
                const int APP_ID = 480;
                const int QUERY_PORT = GAME_PORT + 1;

                Console.WriteLine($"########### STARTING STEAM SREVER ##################");
                SteamServerInit serverInit = new SteamServerInit("TestInitSteamServer", "TestInitSteamServer TEST")
                {
                    GamePort = GAME_PORT,
                    Secure = true,
                    QueryPort = QUERY_PORT,
                    VersionString = "0.0.0.1"
                };

                try
                {
                    //SteamServer.DedicatedServer = true;

                    SteamServer.OnCallbackException += OnCallbackException;
                    SteamServer.OnSteamServerConnectFailure += OnSteamServerConnectFailure;
                    SteamServer.OnSteamServersConnected += OnSteamServersConnected;
                    SteamServer.OnSteamServersDisconnected += OnSteamServersDisconnected;
                    SteamServer.OnValidateAuthTicketResponse += OnValidateAuthTicketResponse;
                    SteamServer.Init(APP_ID, serverInit);
                    SteamServer.DedicatedServer = true;
                    SteamServer.LogOnAnonymous();
                    SocketInterface socket = SteamNetworkingSockets.CreateNormalSocket<SocketInterface>(NetAddress.AnyIp(GAME_PORT));

                }
                catch (System.Exception e)
                {
                    Console.WriteLine($"#### SteamServer e:{e.Message}\n:{e.StackTrace}");
                    // Couldn't init for some reason (dll errors, blocked ports)
                    SteamServer.Shutdown();
                }
                Console.WriteLine($"#### SteamServer IsValid:{SteamServer.IsValid} IsLogedOn:{SteamServer.LoggedOn}");
            }

            void UpdateSteamServerCallbacks()
            {
                const long UPDATE_INTERVAL_IN_MS = 1000;
                long lastTimeInMs = long.MinValue;
                long currentTimeInMs;
                try
                {
                    while (true)
                    {
                        token.ThrowIfCancellationRequested();

                        if (lastTimeInMs + UPDATE_INTERVAL_IN_MS < (currentTimeInMs = DateTime.UtcNow.ToTotalMillisUTC()))
                        {
                            lastTimeInMs = currentTimeInMs;
                            Console.WriteLine($"\tSteamServer Updating {lastTimeInMs}");
                            SteamServer.RunCallbacks();
                        }
                    }
                }
                finally
                {
                    SteamServer.Shutdown();
                    Console.WriteLine($"#### SteamServer Shutdown");
                }
            }

            await Task.Run(() => InitializeSteamServer());
            await Task.Factory.StartNew((x) => UpdateSteamServerCallbacks(), token, TaskCreationOptions.LongRunning);
        }
        
        private void OnValidateAuthTicketResponse(SteamId arg1, SteamId arg2, AuthResponse arg3)
        {
            Console.WriteLine($"#### SteamServer OnValidateAuthTicketResponse arg1:{arg1} arg2:{arg2} arg3:{arg3} ");
        }

        private void OnSteamServersDisconnected(Result obj)
        {
            Console.WriteLine($"#### SteamServer OnSteamServersDisconnected obj:{obj}");
        }

        private void OnSteamServersConnected()
        {
            Console.WriteLine($"#### SteamServer OnSteamServersConnected");
        }

        private void OnSteamServerConnectFailure(Result arg1, bool arg2)
        {
            Console.WriteLine($"#### SteamServer OnSteamServerConnectFailure arg1:{arg1} arg2:{arg2}");
        }

        private void OnCallbackException(Exception ex)
        {
            Console.WriteLine($"#### SteamServer OnCallbackException ex:{ex.Message}");
        }
    }

    public static class Ext
    {
        private static readonly DateTime START_TIME = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static long ToTotalMillisUTC(this DateTime dateTime)
        {
            long timeSpan = (long)dateTime.ToUniversalTime().Subtract(START_TIME).TotalMilliseconds;
            return timeSpan;
        }
    }
}
