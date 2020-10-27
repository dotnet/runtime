using System;
using System.Runtime.CompilerServices;

class G<T>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual bool MVIRT() => typeof(T) == typeof(string);
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public bool MINST() => typeof(T) == typeof(string);
}

class Program
{
    static int Main()
    {
        var g = new G<string>();
        var mvirt = g.MVIRT() ? 50 : -1;
        //var minst = g.MINST() ? 500 : -1;
        return mvirt + 11;
    }
}
