using System.Security.Principal;
using Xunit;

public class WindowsPrincipalTests
{
    [Fact]
    public static void WindowsPrincipalIsInRoleNeg()
    {
        WindowsIdentity windowsIdentity = WindowsIdentity.GetAnonymous();
        WindowsPrincipal windowsPrincipal = new WindowsPrincipal(windowsIdentity);
        var ret = windowsPrincipal.IsInRole("FAKEDOMAIN\\nonexist");
        Assert.False(ret);
    }
}
