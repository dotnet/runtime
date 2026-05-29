using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

interface I<T>
{
    string DefaultTypeOf() => typeof(T).Name;
}

class C : I<string>
{
    public string DefaultTypeOf() => "C.String";
}

public static class Program
{
    [Fact]
    public static void TestEntryPoint()
    {
        var c = new C();
        var dcs = ((I<string>)c).DefaultTypeOf();
        Assert.Equal("C.String", dcs);
    }
}
