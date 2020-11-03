using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    static int Main()
    {
        var c = new C();
        var dcs = ((I<string>)c).DefaultTypeOf();
        if (dcs != "C.String") return 200;
        return 100;
    }
}
