using System.Security.Cryptography;

namespace PasswordApi.Services;

public class PasswordGenerator : IPasswordGenerator
{
    private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
    private const string Digits = "0123456789";
    private const string Specials = "!@#$%^&*()-_=+[]{}";
    private const string All = Uppercase + Lowercase + Digits + Specials;

    public const int Length = 16;

    public string Generate()
    {
        var chars = new char[Length];
        chars[0] = Pick(Uppercase);
        chars[1] = Pick(Lowercase);
        chars[2] = Pick(Digits);
        chars[3] = Pick(Specials);

        for (var i = 4; i < Length; i++)
        {
            chars[i] = Pick(All);
        }

        return new string(chars);
    }

    private static char Pick(string set) => set[RandomNumberGenerator.GetInt32(set.Length)];
}
