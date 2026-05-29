using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

interface I<T> where T : IComparable<T>
{
    T GetAt(int i, T[] tx) => tx[i];
}

class C : I<string>
{
}

public static class Program
{
    private static string[] tx = new string[] { "test" };

    [Fact]
    public static void TestEntryPoint()
    {
        I<string> c = new C();
        var dcs = c.GetAt(0, tx);
        Assert.Equal("test", dcs);
    }
}
