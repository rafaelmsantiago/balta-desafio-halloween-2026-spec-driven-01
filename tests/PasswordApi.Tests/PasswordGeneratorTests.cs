using PasswordApi.Services;

namespace PasswordApi.Tests;

public class PasswordGeneratorTests
{
    private const int Iterations = 1_000;
    private const string Specials = "!@#$%^&*()-_=+[]{}";

    private readonly PasswordGenerator _generator = new();

    [Fact]
    public void Generate_AlwaysFollowsAllRules()
    {
        for (var i = 0; i < Iterations; i++)
        {
            var password = _generator.Generate();

            Assert.Equal(16, password.Length);
            Assert.Contains(password, char.IsAsciiLetterUpper);
            Assert.Contains(password, char.IsAsciiLetterLower);
            Assert.Contains(password, char.IsAsciiDigit);
            Assert.Contains(password, c => Specials.Contains(c));
            Assert.DoesNotContain(password, char.IsWhiteSpace);
        }
    }
}
