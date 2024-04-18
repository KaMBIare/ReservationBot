namespace Domain;

public class User 
{
    /// <summary>
    /// явно указал id тк с какого-то хера без этого EF выбрасовал ексепшины типа у вас нету id, хотя у базового класа мы его наследуем
    /// </summary>
    public string Id { get; set; }
    public string DiscordNickname { get; set; }

    public User(string id,string discordNickname)
    {
        Id = id;
        DiscordNickname = discordNickname;
    }

    public User()
    {
        
    }
}