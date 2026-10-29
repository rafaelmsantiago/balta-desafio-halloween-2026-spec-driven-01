namespace PasswordApi.Data;

public class Password
{
    public Guid Id { get; set; }
    public string Value { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}
