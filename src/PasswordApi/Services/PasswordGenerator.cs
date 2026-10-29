using System.Security.Cryptography;

namespace PasswordApi.Services;

public class PasswordGenerator : IPasswordGenerator
{
    private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
    private const string Digits = "0123456789";
    private const string Specials = "!@#$%^&*()-_=+[]{}";

    public string Generate()
    {
        char[] chars =
        [
            Pick(Uppercase),
            Pick(Lowercase),
            Pick(Digits),
            Pick(Specials),
        ];

        return new string(chars);
    }

    private static char Pick(string set) => set[RandomNumberGenerator.GetInt32(set.Length)];
}
