using LiteNetLib;

namespace SpaceJam2024_Server;

public class Client
{
    public Guid ID { get; }
    public string Name { get; set; }
    public NetPeer RemoteEP { get; }
    public bool IsHost { get => Server.Instance.MainLobby.Host == this; }
    public bool IsMember { get => Server.Instance.MainLobby.Members.Contains(this); }

    public Client(Guid id, NetPeer remoteEP)
    {
        ID = id;
        RemoteEP = remoteEP;
    }
}