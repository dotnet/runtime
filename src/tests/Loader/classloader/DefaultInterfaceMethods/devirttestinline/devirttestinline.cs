using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

interface I<T> where T : IComparable<T>
{
    T GetAt(int i, T[] tx) => tx[i];
}

class C : I<string>
{
}

public static class Program
{
    private static string[] tx = new string[] { "test" };

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    static int Main()
    {
        I<string> c = new C();
        var dcs = c.GetAt(0, tx);
        if (dcs != "test") return 200;
        return 100;
    }
}
