using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;


struct C<T>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public string DefaultTypeOf() => typeof(T).Name;
}

public static class Program
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    static int Main()
    {
        var c = new C<string>();
        // IN0002: 000010 mov      rdx, 0x7FF95B110710
        var dcs = c.DefaultTypeOf();
        if (dcs != "String") return 200;
        return 100;
    }
}
