using LiteNetLib;

namespace SpaceJam2024_Server;

public class Lobby
{
    public List<Client> Members { get; set; }
    public Client Host { get { return Members[0]; } }

    public Lobby()
    {
        Members = new List<Client>();
    }

    public List<Client> GetMembersExcept(Client clientToExclude)
    {
        List<Client> membersExcluding = new List<Client>();
        foreach (Client member in Members)
        {
            if (member != clientToExclude)
            {
                membersExcluding.Add(member);
            }
        }
        return membersExcluding;
    }
}