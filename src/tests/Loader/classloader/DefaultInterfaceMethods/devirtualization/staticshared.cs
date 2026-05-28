using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

class C<T>
{
    public static string DefaultTypeOf() => typeof(T).Name;
}

public static class Program
{
    [Fact]
    public static void TestEntryPoint()
    {
        var dcs = C<string>.DefaultTypeOf();
        Assert.Equal("String", dcs);
    }
}
