using System.ComponentModel.DataAnnotations;

namespace BadeHava.DTOs;

public class AvatarRequest
{
    [Required]
    public string AvatarUrl { get; set; } = null!;
}