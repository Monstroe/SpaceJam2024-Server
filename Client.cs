using CNet;

namespace SpaceJam2024_Server;

public class Client
{
    public Guid ID { get; }
    public string Name { get; set; }
    public NetEndPoint RemoteEP { get; }
    public bool IsHost { get => Server.Instance.MainLobby.Host == this; }
    public bool IsMember { get => Server.Instance.MainLobby.Members.Contains(this); }

    public Client(Guid id, NetEndPoint remoteEP)
    {
        ID = id;
        RemoteEP = remoteEP;
    }
}