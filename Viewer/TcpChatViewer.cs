using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TcpChatViewer
{
    class TcpChatViewer
    {
        public readonly string ServerAddress;
        public readonly int Port;
        private TcpClient _client;
        public bool Running { get; private set; }
        private bool _disconnectRequested = false;

        public readonly int BufferSize = 2 * 1024; //2KB
        private NetworkStream _msgStream = null;

        public TcpChatViewer(string serverAddress, int port)
        {
            _client = new TcpClient();
            _client.SendBufferSize = BufferSize; 
            _client.ReceiveBufferSize = BufferSize;
            Running = false;

            ServerAddress = serverAddress;
            Port = port;
        }

        public void Connect()
        {
            _client.Connect(ServerAddress, Port);
            var endPoint = _client.Client.RemoteEndPoint;

            if (_client.Connected)
            {
                Console.WriteLine($"Connected to the server at {endPoint}.");

                _msgStream = _client.GetStream();
                byte[] msgBuffer = Encoding.UTF8.GetBytes($"viewer");
                _msgStream.Write(msgBuffer, 0, msgBuffer.Length);

                if (!_isDisconnected(_client))
                {
                    Running = true;
                    Console.WriteLine("Press Ctrl+C to exit the Viewer at any time.");
                }
                else
                {
                    _cleanupNetworkResources();
                    Console.WriteLine($"The server didn't recognise us as a Viewer.\n");
                }
                
            }
            else
            {
                _cleanupNetworkResources();
                Console.WriteLine($"Wasn't able to connect to the server at {endPoint}");
            }
        }

        public void Disconnect()
        {
            Running = false;
            _disconnectRequested = true;
            Console.WriteLine("Disconnecting from the chat...");
        }

        public void ListenForMessages()
        {
            while (Running)
            {
                int messageLength = _client.Available;
                if (messageLength > 0)
                {
                    byte[] msgBuffer = new byte[messageLength];
                    _msgStream.Read(msgBuffer, 0, msgBuffer.Length);

                    string msg = Encoding.UTF8.GetString(msgBuffer);
                    Console.WriteLine(msg);
                }

                Thread.Sleep(10);

                if (_isDisconnected(_client))
                {
                    Running = false;
                    Console.WriteLine("Server has disconnected from us.\n");
                }

                Running &= !_disconnectRequested;
            }

            _cleanupNetworkResources();
            Console.WriteLine("Disconnected");

        }

        private void _cleanupNetworkResources()
        {
            _msgStream?.Close();
            _msgStream = null;
            _client.Close();
        }

        private bool _isDisconnected(TcpClient client)
        {
            try
            {
                Socket s = client.Client;
                return s.Poll(10 * 1000, SelectMode.SelectRead) && (s.Available == 0);
            }
            catch(SocketException e)
            {
                return true;
            }
        }

        public static TcpChatViewer viewer;

        protected static void InterruptHandler(object sender, ConsoleCancelEventArgs args)
        {
            viewer.Disconnect();
            args.Cancel = true;
        }

        public static void Main(string[] args)
        {
            string host = "192.168.1.65";
            int port = 8080;
            viewer = new TcpChatViewer(host, port);
            Console.CancelKeyPress += InterruptHandler;

            viewer.Connect();
            viewer.ListenForMessages();
        }
    }
}