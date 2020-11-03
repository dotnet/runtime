using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

interface I<T>
{
    string DefaultTypeOf() => typeof(T).Name;
}

class C : I<int>
{
}

public static class Program
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    static int Main()
    {
        var c = new C();
        var dcs = ((I<int>)c).DefaultTypeOf();
        if (dcs != "Int32") return 200;
        return 100;
    }
}
