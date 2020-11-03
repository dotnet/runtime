using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    static int Main()
    {
        var c = new C<string>();
        var dcs = ((I<string>)c).DefaultTypeOf();
        if (dcs != "C.String") return 200;
        return 100;
    }
}
