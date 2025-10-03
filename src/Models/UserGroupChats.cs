using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BadeHava.Models;

public class UserGroupChat
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public string GroupChatId { get; set; } = null!;


    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}