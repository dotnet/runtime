using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

interface IM<T>
{
    bool UseDefaultM { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => true; }
    ValueTask M(T instance) => throw new NotImplementedException("M must be implemented if UseDefaultM is false");
    static ValueTask DefaultM(T instance)
    {
        Console.WriteLine("Default Behaviour");
        return default;
    }
}

struct M : IM<int> { }

public static class Program
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    static int Main()
    {
        var m = new M();
        if (((IM<int>)m).UseDefaultM)
        {
            IM<int>.DefaultM(42);
            return 100;
        }
        else
        {
            ((IM<int>)m).M(42);
        }
        return 200;
    }
}
