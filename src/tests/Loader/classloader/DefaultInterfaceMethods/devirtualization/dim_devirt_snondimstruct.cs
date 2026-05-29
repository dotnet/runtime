using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;


struct C<T>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public string DefaultTypeOf() => typeof(T).Name;
}

public static class Program
{
    [Fact]
    public static void TestEntryPoint()
    {
        var c = new C<string>();
        var dcs = c.DefaultTypeOf();
        Assert.Equal("String", dcs);
    }
}
