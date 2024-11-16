using LiteNetLib;

namespace SpaceJam2024_Server;

public class PacketReceiver
{
    public static PacketReceiver Instance { get; } = new PacketReceiver();

    public void Name(Client client, Packet packet)
    {
        if (client.Name != null)
        {
            Console.WriteLine("Client " + client.RemoteEP.ToString() + " attempted to set their name despite already having one");
            PacketSender.Instance.Invalid(client, "Client already has name");
            return;
        }

        string name = packet.ReadString();
        client.Name = name;

        Console.WriteLine("Client " + client.RemoteEP.ToString() + " set their name to: " + name);
    }

    public void JoinLobby(Client client, Packet packet)
    {
        if (client.IsMember)
        {
            Console.WriteLine("Client " + client.RemoteEP.ToString() + " attempted to join the lobby despite already being in it");
            PacketSender.Instance.Invalid(client, "Client already in lobby");
            return;
        }

        Console.WriteLine("Client " + client.RemoteEP.ToString() + " joined the lobby");
        Server.Instance.JoinLobby(client);
    }
}