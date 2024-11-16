using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;

namespace SpaceJam2024_Server;

public class Server
{
    public static Server Instance { get; } = new Server();

    public const int POLL_RATE = 15;

    public delegate void PacketHandler(Client client, Packet packet);

    public int Port { get; set; }
    public string ConnectionKey { get; set; }

    private EventBasedNetListener listener;
    public NetManager Manager { get; }
    public Dictionary<NetPeer, Client> Clients { get; }
    public Lobby MainLobby { get; }

    private Dictionary<ServiceReceiveType, PacketHandler> packetHandlers;
    private bool running;

    private Server()
    {
        listener = new EventBasedNetListener();
        listener.ConnectionRequestEvent += OnConnectionRequest;
        listener.PeerConnectedEvent += OnPeerConnected;
        listener.PeerDisconnectedEvent += OnPeerDisconnected;
        listener.NetworkReceiveEvent += OnNetworkReceive;
        listener.NetworkErrorEvent += OnNetworkError;

        Manager = new NetManager(listener);

        Clients = new Dictionary<NetPeer, Client>();
        MainLobby = new Lobby();
        packetHandlers = new Dictionary<ServiceReceiveType, PacketHandler>()
        {
            { ServiceReceiveType.Name, PacketReceiver.Instance.Name },
            { ServiceReceiveType.JoinLobby, PacketReceiver.Instance.JoinLobby }
        };

        running = false;
    }

    public void Start(int port, string connectionKey)
    {
        Console.WriteLine("Starting Server...");
        Port = port;
        ConnectionKey = connectionKey;
        Manager.Start(IPAddress.Any, IPAddress.IPv6Any, port);
        running = true;
        Console.WriteLine("Server Started, waiting for connections...");

        while (running && (Console.IsOutputRedirected || !Console.KeyAvailable))
        {
            Manager.PollEvents();
            Thread.Sleep(POLL_RATE);
        }

        Close();
    }

    public void Stop()
    {
        running = false;
    }

    public void Close()
    {
        Console.WriteLine("Closing Server...");
        Manager.DisconnectAll();
        Manager.Stop();
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        Console.WriteLine("Connection Request from " + request.RemoteEndPoint.ToString());
        request.AcceptIfKey(ConnectionKey);
    }

    public void OnPeerConnected(NetPeer peer)
    {
        Console.WriteLine("Client " + peer.ToString() + " Connected");
        var client = new Client(Guid.NewGuid(), peer);
        Clients.Add(peer, client);
        PacketSender.Instance.ID(client, client.ID);
        Console.WriteLine("Number of Clients Online: " + Clients.Count);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Console.WriteLine("Client " + peer.ToString() + " Disconnected: " + disconnectInfo.Reason.ToString());

        if (Clients.ContainsKey(peer))
        {
            if (Clients[peer].IsMember)
            {
                var client = Clients[peer];
                LeaveLobby(client);
            }

            Clients.Remove(peer);
        }

        Console.WriteLine("Number of Clients Online: " + Clients.Count);
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        byte[] data = new byte[reader.AvailableBytes];
        reader.GetBytes(data, reader.AvailableBytes);
        Packet packet = new Packet(data);
        // Why this has to exist, I don't know. But it needs to be here (this is why CNet is better).
        int length = packet.ReadInt();

        if (packet.Length < (sizeof(short) + sizeof(short)))
        {
            Console.Error.WriteLine("Client " + peer.ToString() + " sent too short of a packet");
            return;
        }

        // Check if the packet is a command packet (they all start with 0)
        if (packet.ReadShort() == -1)
        {
            ServiceReceiveType command = (ServiceReceiveType)packet.ReadShort();
            Console.WriteLine("Received Command: " + command.ToString() + " from " + peer.ToString());
            if (packetHandlers.TryGetValue(command, out PacketHandler? handler))
            {
                handler(Clients[peer], packet);
            }
            else
            {
                Console.Error.WriteLine("Invalid Command Received from " + peer.ToString());
            }
        }
        else
        {
            if (Clients[peer].IsMember)
            {
                packet.CurrentIndex -= 2;
                // Need to remove the length of the packet from the start of the packet, because it will get re-inserted when sending
                packet.Remove(0, sizeof(int));

                var client = Clients[peer];
                Send(MainLobby.GetMembersExcept(client), packet, deliveryMethod);
            }
            else
            {
                Console.Error.WriteLine("Client " + peer.ToString() + " is not a member of the lobby.");
            }
        }
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        Console.Error.WriteLine("Network Error from " + endPoint + ": " + socketError.ToString());
    }

    public void Send(Client client, Packet packet, DeliveryMethod method)
    {
        try
        {
            if (packet.ReadShort() == -1)
            {
                Console.WriteLine("Sending Command Packet to " + client.RemotePeer.ToString() + " of type " + (ServiceSendType)packet.ReadShort(false));
                packet.CurrentIndex -= 2;
            }

            client.RemotePeer.Send(packet.ByteArray, method);
        }
        catch (SocketException e)
        {
            Console.Error.WriteLine("Socket Exception While Sending: " + e.SocketErrorCode.ToString());
        }
    }

    public void Send(List<Client> clients, Packet packet, DeliveryMethod method)
    {
        foreach (Client client in clients)
        {
            Send(client, packet, method);
        }
    }

    public void JoinLobby(Client client)
    {
        MainLobby.Members.Add(client);
        Console.WriteLine("MEMBER COUNT: " + MainLobby.Members.Count);
        PacketSender.Instance.Members(MainLobby.Members);
    }

    public void LeaveLobby(Client client)
    {
        MainLobby.Members.Remove(client);
        Console.WriteLine("MEMBER COUNT: " + MainLobby.Members.Count);
        PacketSender.Instance.Members(MainLobby.Members);
    }

    static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: Referrer <port> <connectionKey>");
            return;
        }

        string connectionKey = args[1];
        if (int.TryParse(args[0], out int port))
        {
            Console.WriteLine("Passed Port: " + port);
            Console.WriteLine("Passed Connection Key: " + connectionKey + "\n");
            Server.Instance.Start(port, connectionKey);
        }
        else
        {
            Console.Error.WriteLine("Invalid Port");
        }
    }
}
