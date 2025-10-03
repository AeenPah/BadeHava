namespace BadeHava.DTOs;


public class SearchResponse
{
    public List<UserSearchResult> Users { get; set; } = new();
}

public class UserSearchResult
{
    public int UserId { get; set; }
    public required string Username { get; set; }
    public string? AvatarPicUrl { get; set; }
}