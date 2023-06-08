using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class Program
{
    private static int s_result;
    [Fact]
    public static int TestEntryPoint()
    {
        C c = new();
        for (int i = 0; i < 100; i++)
        {
            Foo(c);
            Thread.Sleep(15);
        }

        s_result = -1;
        try
        {
            Foo(null);
            Console.WriteLine("FAIL: No exception thrown");
            return -2;
        }
        catch (NullReferenceException)
        {
            if (s_result == 100)
            {
                Console.WriteLine("PASS");
            }
            else
            {
                Console.WriteLine("FAIL: Result is {0}", s_result);
            }

            return s_result;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Foo(Base b)
    {
        b.Test(SideEffect(10));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long SideEffect(long i)
    {
        s_result = 100;
        return i;
    }
}

public interface Base
{
    void Test(long arg);
}

public class C : Base
{
    public void Test(long arg)
    {
    }
}
