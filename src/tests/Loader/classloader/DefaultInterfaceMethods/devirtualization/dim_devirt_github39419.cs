using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

interface IM<T>
{
    bool UseDefaultM { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => true; }
    ValueTask M(T instance) => throw new NotImplementedException("M must be implemented if UseDefaultM is false");
    static ValueTask DefaultM(T instance)
    {
        return default;
    }
}

struct M : IM<int> { }

public static class Program
{
    [Fact]
    public static void TestEntryPoint()
    {
        var m = new M();
        if (((IM<int>)m).UseDefaultM)
        {
            IM<int>.DefaultM(42);
            return;
        }
        else
        {
            ((IM<int>)m).M(42);
        }
        throw new UnreachableException();
    }
}
