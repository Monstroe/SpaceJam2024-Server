using CNet;

namespace SpaceJam2024_Server;

public class PacketSender
{
    public static PacketSender Instance { get; } = new PacketSender();

    public void ID(Client client, Guid id)
    {
        using (NetPacket packet = new NetPacket(Server.Instance.Listener.System, PacketProtocol.TCP))
        {
            packet.Write((short)-1); // Insert the command key at the start of the packet
            packet.Write((short)ServiceSendType.ID);
            packet.Write(id.ToString());
            Server.Instance.Send(client, packet, PacketProtocol.TCP);
        }
    }

    public void Members(List<Client> members)
    {
        using (NetPacket packet = new NetPacket(Server.Instance.Listener.System, PacketProtocol.TCP))
        {
            packet.Write((short)-1); // Insert the command key at the start of the packet
            packet.Write((short)ServiceSendType.Members);
            packet.Write(members.Count);
            foreach (Client member in members)
            {
                packet.Write(member.ID.ToString());
                packet.Write(member.Name);
            }
            Server.Instance.Send(members, packet, PacketProtocol.TCP);
        }
    }

    public void Invalid(Client client, string errorMessage)
    {
        using (NetPacket packet = new NetPacket(Server.Instance.Listener.System, PacketProtocol.TCP))
        {
            packet.Write((short)-1); // Insert the command key at the start of the packet
            packet.Write((short)ServiceSendType.Invalid);
            packet.Write(errorMessage);
            Server.Instance.Send(client, packet, PacketProtocol.TCP);
        }
    }
}