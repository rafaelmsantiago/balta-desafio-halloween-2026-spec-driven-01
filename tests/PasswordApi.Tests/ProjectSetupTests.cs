namespace PasswordApi.Tests;

public class ProjectSetupTests
{
    [Fact]
    public void TestProject_ReferencesPasswordApi()
    {
        var assembly = typeof(Program).Assembly;

        Assert.Equal("PasswordApi", assembly.GetName().Name);
    }

    [Fact]
    public void PasswordApi_TargetsNet10()
    {
        var assembly = typeof(Program).Assembly;
        var targetFramework = assembly
            .GetCustomAttributes(typeof(System.Runtime.Versioning.TargetFrameworkAttribute), false)
            .Cast<System.Runtime.Versioning.TargetFrameworkAttribute>()
            .Single();

        Assert.Equal(".NETCoreApp,Version=v10.0", targetFramework.FrameworkName);
    }
}
