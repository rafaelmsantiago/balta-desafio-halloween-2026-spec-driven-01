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

        Shuffle(chars);

        return new string(chars);
    }

    private static char Pick(string set) => set[RandomNumberGenerator.GetInt32(set.Length)];

    // Fisher-Yates: evita que os 4 primeiros caracteres sigam sempre a mesma ordem de categorias.
    private static void Shuffle(char[] chars)
    {
        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
    }
}
