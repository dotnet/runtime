using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

interface I<in T>
{
    string DefaultTypeOf() => typeof(T).Name;
}

class C : I<object>
{
}

public static class Program
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    static int Main()
    {
        var c = new C();
        var dcs = ((I<string>)c).DefaultTypeOf();
        if (dcs != "Object") return 200;
        return 100;
    }
}
