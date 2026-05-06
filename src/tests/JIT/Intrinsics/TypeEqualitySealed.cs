// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Make sure optimization works correctly under GT_JTRUE and GT_RETURN nodes
// Also, if it respects/preserves side-effects and doesn't optimize away
// possible NREs.

public sealed class SealedClass1
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Type GetTypeInlineable() => GetType();


    [MethodImpl(MethodImplOptions.NoInlining)]
    public object TestTernary1() => GetTypeInlineable() == typeof(SealedClass1) ? "Ok" : "Fail";

    [MethodImpl(MethodImplOptions.NoInlining)]
    public object TestTernary2() => GetTypeInlineable() == typeof(SealedClass2) ? "Fail" : "Ok";

    [MethodImpl(MethodImplOptions.NoInlining)]
    public object TestTernary3() => GetTypeInlineable() == typeof(NotSealedClass1) ? "Fail" : "Ok";

    [MethodImpl(MethodImplOptions.NoInlining)]
    public object TestTernary4() => GetType() == typeof(SealedClass1) ? "Ok" : "Fail";

    [MethodImpl(MethodImplOptions.NoInlining)]
    public object TestTernary5() => GetType() == typeof(SealedClass2) ? "Fail" : "Ok";

    [MethodImpl(MethodImplOptions.NoInlining)]
    public object TestTernary6() => GetType() == typeof(NotSealedClass1) ? "Fail" : "Ok";


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object TestTernary7(SealedClass1 instance) => instance.GetType() == typeof(SealedClass1) ? "Ok" : "Fail";

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object TestTernary8(SealedClass1 instance) => instance.GetType() == typeof(SealedClass2) ? "Fail" : "Ok";

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object TestTernary9(SealedClass1 instance) => instance.GetType() == typeof(NotSealedClass1) ? "Fail" : "Ok";

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object TestTernary10(SealedClass1 instance) => instance.GetTypeInlineable() == typeof(SealedClass1) ? "Ok" : "Fail";

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object TestTernary11(SealedClass1 instance) => instance.GetTypeInlineable() == typeof(SealedClass2) ? "Fail" : "Ok";

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object TestTernary12(SealedClass1 instance) => instance.GetTypeInlineable() == typeof(NotSealedClass1) ? "Fail" : "Ok";


    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool TestReturn1() => GetTypeInlineable() == typeof(SealedClass1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool TestReturn2() => GetTypeInlineable() == typeof(SealedClass2);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool TestReturn3() => GetTypeInlineable() == typeof(NotSealedClass1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool TestReturn4() => GetTypeInlineable() != typeof(SealedClass1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool TestReturn5() => GetTypeInlineable() != typeof(SealedClass2);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool TestReturn6() => GetTypeInlineable() != typeof(NotSealedClass1);


    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool TestReturn7() => GetType() == typeof(SealedClass1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool TestReturn8() => GetType() == typeof(SealedClass2);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool TestReturn9() => GetType() == typeof(NotSealedClass1);
}

public sealed class SealedClass2 { }
public class NotSealedClass1 { }

public static class Program
{
    private static int returnCode = 100;

    [Fact]
    public static int TestEntryPoint()
    {
        var tests = new SealedClass1();
        AssertEquals("Ok", tests.TestTernary1());
        AssertEquals("Ok", tests.TestTernary2());
        AssertEquals("Ok", tests.TestTernary3());
        AssertEquals("Ok", tests.TestTernary4());
        AssertEquals("Ok", tests.TestTernary5());
        AssertEquals("Ok", tests.TestTernary6());
        AssertEquals("Ok", SealedClass1.TestTernary7(new SealedClass1()));
        AssertEquals("Ok", SealedClass1.TestTernary8(new SealedClass1()));
        AssertEquals("Ok", SealedClass1.TestTernary9(new SealedClass1()));
        AssertEquals("Ok", SealedClass1.TestTernary10(new SealedClass1()));
        AssertEquals("Ok", SealedClass1.TestTernary11(new SealedClass1()));
        AssertEquals("Ok", SealedClass1.TestTernary12(new SealedClass1()));
        ThrowsNRE(() => SealedClass1.TestTernary7(null));
        ThrowsNRE(() => SealedClass1.TestTernary8(null));
        ThrowsNRE(() => SealedClass1.TestTernary9(null));
        ThrowsNRE(() => SealedClass1.TestTernary10(null));
        ThrowsNRE(() => SealedClass1.TestTernary11(null));
        ThrowsNRE(() => SealedClass1.TestTernary12(null));
        AssertIsTrue(tests.TestReturn1());
        AssertIsFalse(tests.TestReturn2());
        AssertIsFalse(tests.TestReturn3());
        AssertIsFalse(tests.TestReturn4());
        AssertIsTrue(tests.TestReturn5());
        AssertIsTrue(tests.TestReturn6());
        AssertIsTrue(tests.TestReturn7());
        AssertIsFalse(tests.TestReturn8());
        AssertIsFalse(tests.TestReturn9());
        return returnCode;
    }

    private static void ThrowsNRE(Action action)
    {
        try
        {
            action();
        }
        catch (NullReferenceException)
        {
            return;
        }

        returnCode++;
        Console.WriteLine($"Expected: NullReferenceException");
    }

    private static void AssertEquals(object expected, object actual, [CallerLineNumber] int line = 0)
    {
        if (expected != actual)
        {
            returnCode++;
            Console.WriteLine($"{expected} != {actual}, L:{line}");
        }
    }

    private static void AssertIsTrue(bool value, [CallerLineNumber] int line = 0)
    {
        if (!value)
        {
            returnCode++;
            Console.WriteLine($"Expected: True, L:{line}");
        }
    }

    private static void AssertIsFalse(bool value, [CallerLineNumber] int line = 0)
    {
        if (value)
        {
            returnCode++;
            Console.WriteLine($"Expected: False, L:{line}");
        }
    }
}
