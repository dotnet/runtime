using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

interface I<T>
{
    string DefaultTypeOf() => typeof(T).Name;
}

class Dummy { }

struct C : I<string>, I<object>, I<Dummy>
{
    string I<Dummy>.DefaultTypeOf() => "C.Dummy";
}

public static class Program
{
    [Fact]
    public static void TestEntryPoint()
    {
        var c = new C();
        var dcs = ((I<string>)c).DefaultTypeOf();
        Assert.Equal("String", dcs);
        var dos = ((I<object>)c).DefaultTypeOf();
        Assert.Equal("Object", dos);
        var dds = ((I<Dummy>)c).DefaultTypeOf();
        Assert.Equal("C.Dummy", dds);
    }
}
