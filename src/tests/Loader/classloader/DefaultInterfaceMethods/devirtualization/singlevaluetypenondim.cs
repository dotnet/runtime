using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

interface I<T>
{
    string DefaultTypeOf();
}

struct C<T> : I<T>
{
    public string DefaultTypeOf() => "C." + typeof(T).Name;
}

public static class Program
{
    [Fact]
    public static void TestEntryPoint()
    {
        var c = new C<string>();
        var dcs = ((I<string>)c).DefaultTypeOf();
        Assert.Equal("C.String", dcs);
    }
}
