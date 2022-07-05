// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;

public class Test11611
{    
    struct TestStruct
    {
        public int a;
        public int b;
    }

    delegate void testDelegate(TypedReference tr);

    static void test(TypedReference tr)
    {
        Type t = __reftype(tr);
        Console.WriteLine($"tr = {t.Name}");
    }
    public static void Test(testDelegate d)
    {
        TestStruct s = default;
        var tr = __makeref(s);
        test(tr);
        d(tr); // this will crash due to __reftype(tr)
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Main(string[] args)
    {
        Console.WriteLine("About to run test");
        Test(test);
        Console.WriteLine("Test complete run test");
    }
}
