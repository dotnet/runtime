using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class C<T>
{
    public static string DefaultTypeOf() => typeof(T).Name;
}

public static class Program
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    static int Main()
    {
        var dcs = C<string>.DefaultTypeOf();
        if (dcs != "String") return 200;
        return 100;
    }
}
