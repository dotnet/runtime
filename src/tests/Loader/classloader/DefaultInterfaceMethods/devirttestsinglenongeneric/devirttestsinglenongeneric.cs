using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

interface I
{
    string DefaultTypeOf() => typeof(string).Name;
}

class C : I
{
}

public static class Program
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    static int Main()
    {
        var c = new C();
        var dcs = ((I)c).DefaultTypeOf();
        if (dcs != "String") return 200;
        return 100;
    }
}
