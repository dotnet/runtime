using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

interface I
{
    string DefaultTypeOf() => typeof(string).Name;
}

class C : I
{
}

public static class Program
{
    [Fact]
    public static void TestEntryPoint()
    {
        var c = new C();
        var dcs = ((I)c).DefaultTypeOf();
        Assert.Equal("String", dcs);
    }
}
