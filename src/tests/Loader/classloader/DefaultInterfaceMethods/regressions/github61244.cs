// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

// In GH issue 61244 the mono runtime aborted when inflating the default
// interface method because the context used was from the base interface.

// The OneArgBaseInterface portion of this test handles the original bug
// where the base interface has less generic arguments than the derived 
// interface and the runtime aborts.

// The SecondInterface portion of this test handles an additional scenario
// where the number of generic arguments are the same in the base and
// derived interface contexts, but the order is changed (or different.)
// When this occurs the generic info is incorrect for the inflated method.

// TestClass2 tests a regression due to the fix for the previous
// regression that caused Mono to incorrectly instantiate generic
// interfaces that appeared in the MethodImpl table

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        int result = new TestClass().DoTest();
        if (result != 100)
                return result;
        result = new TestClass2().DoTest();
        return result;
    }
}

public interface OneArgBaseInterface<T1>
{
    int SomeFunc1(T1 someParam1, Type someParam1Type);
}

public interface TwoArgBaseInterface<T1, T2>
{
    int SomeFunc1(T1 someParam1, T2 someParam2, Type someParam1Type, Type someParam2Type);
}

public interface SecondInterface<TParam2Type, TParam1Type> :  OneArgBaseInterface<TParam1Type>, TwoArgBaseInterface<TParam1Type, TParam2Type>
{
    int OneArgBaseInterface<TParam1Type>.SomeFunc1(TParam1Type someParam1, Type someParam1Type)
    {
        if (typeof(TParam1Type) != someParam1Type)
        {
            Console.WriteLine("Failed => 101");
            return 101;
        }

        return 100;
    }

    int TwoArgBaseInterface<TParam1Type, TParam2Type>.SomeFunc1(TParam1Type someParam1, TParam2Type someParam2, Type someParam1Type, Type someParam2Type)
    {
        if (typeof(TParam1Type) != someParam1Type)
        {
            Console.WriteLine("Failed => 102");
            return 102;
        }

        if (typeof(TParam2Type) != someParam2Type)
        {
            Console.WriteLine("Failed => 103");
            return 103;
        }

        return 100;
    }
}

public class TestClass : SecondInterface<int, string>
{
    public int DoTest ()
    {
        int ret = (this as OneArgBaseInterface<string>).SomeFunc1("test string", typeof(string));
        if (ret != 100)
            return ret;

        ret = (this as TwoArgBaseInterface<string, int>).SomeFunc1("test string", 0, typeof(string), typeof(int));
        if (ret != 100)
            return ret;

        Console.WriteLine("Passed => 100");
        return 100;
    }
}

public interface IA
{
    public int Foo();
}

public interface IB<T> : IA
{
        int IA.Foo() { return 104; }
}

public interface IC<H1, H2> : IB<H2>
{
        int IA.Foo() { return 105; }
}

public class C<U, V, W> : IC<V, W>
{
        int IA.Foo() { return 100; }
}

public class TestClass2
{
    public int DoTest()
    {
        IA c = new C<byte, short, int>();
        return c.Foo();
    }
}
