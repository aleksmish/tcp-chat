using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TcpChatMessenger
{
    class TcpChatMessenger
    {
        public readonly string ServerAddress;
        public readonly int Port;
        private TcpClient _client;
        public bool Running { get; private set; }

        public readonly int BufferSize = 2 * 1024; //2KB
        private NetworkStream _msgStream = null;

        public readonly string Name;

        public TcpChatMessenger(string serverAddress, int port, string name)
        {
            _client = new TcpClient();
            _client.SendBufferSize = BufferSize;
            _client.ReceiveBufferSize = BufferSize;
            Running = false;

            ServerAddress = serverAddress;
            Port = port;
            Name = name;
        }

        public void Connect()
        {
            _client.Connect(IPEndPoint.Parse($"{ServerAddress}:{Port}"));
            EndPoint endPoint = _client.Client.RemoteEndPoint;

            if (_client.Connected)
            {
                Console.WriteLine("Connected to the server at {0}.", endPoint);

                _msgStream = _client.GetStream();
                byte[] msgBuffer = Encoding.UTF8.GetBytes($"name:{Name}");
                _msgStream.Write(msgBuffer, 0, msgBuffer.Length);

                if (!_isDisconnected(_client))
                    Running = true;
                else
                {
                    _cleanupNetworkResources();
                    Console.WriteLine($"The server rejected us; \"{Name}\" is probably taken");
                }
            }
            else
            {
                _cleanupNetworkResources();
                Console.WriteLine("Wasn't able to connect to the server at {0}", endPoint);
            }
        }
        
        public void SendMessages()
        {
            bool wasRunning = Running;

            while (Running)
            {
                Console.WriteLine($"{Name}> ");
                string msg = Console.ReadLine();

                if ((msg.ToLower() == "quit") || msg.ToLower() == "exit")
                {
                    Console.WriteLine("Disconnecting");
                    Running = false;
                }
                else if(msg != string.Empty)
                {
                    byte[] msgBuffer = Encoding.UTF8.GetBytes(msg);
                    _msgStream.Write(msgBuffer, 0, msgBuffer.Length);
                }

                if (_isDisconnected(_client))
                {
                    Running = false;
                    Console.WriteLine("Server has disconnected from us.\n");
                }
            }

            _cleanupNetworkResources();
            if (wasRunning)
            {
                Console.WriteLine("Disconnected");
            }

        }

        private void _cleanupNetworkResources()
        {
            _msgStream?.Close();
            _msgStream = null;
            _client.Close();
        }

        private static bool _isDisconnected(TcpClient client)
        {
            try
            {
                Socket s = client.Client;
                return s.Poll(10 * 1000, SelectMode.SelectRead) && (s.Available == 0);
            }catch(SocketException e)
            {
                return true;
            }
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("Enter a name to use");
            string name = Console.ReadLine();


            string host = "192.168.1.65";
            int port = 8080;
            TcpChatMessenger messenger = new TcpChatMessenger(host,port,name);


            messenger.Connect();
            messenger.SendMessages();

        }
    }
}