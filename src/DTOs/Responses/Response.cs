namespace BadeHava.DTOs;

public class Response<T>
{
    public bool Success { get; set; }
    public required string Message { get; set; }
    public T? Data { get; set; }
}