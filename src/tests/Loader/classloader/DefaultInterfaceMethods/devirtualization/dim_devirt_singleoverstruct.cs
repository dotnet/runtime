using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

interface I<T>
{
    string DefaultTypeOf() => typeof(T).Name;
}

class C : I<int>
{
}

public static class Program
{
    [Fact]
    public static void TestEntryPoint()
    {
        var c = new C();
        var dcs = ((I<int>)c).DefaultTypeOf();
        Assert.Equal("Int32", dcs);
    }
}
