using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    static int Main()
    {
        var c = new C();
        var dcs = ((I<string>)c).DefaultTypeOf();
        if (dcs != "String") return 200;
        var dos = ((I<object>)c).DefaultTypeOf();
        if (dos != "Object") return 300;
        var dds = ((I<Dummy>)c).DefaultTypeOf();
        if (dds != "C.Dummy") return 300;
        return 100;
    }
}
