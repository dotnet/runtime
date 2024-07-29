// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test is covering an issue where we would incorrectly compute the
// argument layout of the static method delegate thunk, due to an issue
// where we failed to handle ELEMENT_TYPE_TYPEDBYREF like the valuetype it is.
// This would not reproduce only Windows X64 due to the particular abi of that
// platform, but was found on Unix X64.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Test11611
{    
    struct TestStruct
    {
        public int a;
        public int b;
    }


   public delegate void testDelegate(TypedReference tr);
   public static testDelegate d;

    static void test(TypedReference tr)
    {
        Type t = __reftype(tr);
        Console.WriteLine($"tr = {t.Name}");
    }
    public static void Test()
    {
        TestStruct s = default;
        var tr = __makeref(s);
        test(tr);
        d(tr); // this will crash due to __reftype(tr)
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Fact]
    public static void TestEntryPoint()
    {
        Console.WriteLine("About to run test");
        d = test;
        Test();
        Console.WriteLine("Test complete run test");
    }
}
