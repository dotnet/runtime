// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// GitHub12398: Lowering is inconsistent in checking safety of RegOptional.

using System;
using System.Runtime.CompilerServices;
using Xunit;

struct S0
{
    public sbyte F1;
    public sbyte F2;
}

public class GitHub_12398
{
    [Fact]
    public static int TestEntryPoint()
    {
        int result = 100;
        if (TestBinary() != 0) {
            Console.WriteLine("Failed TestBinary");
            result = -1;
        }

        if (TestCompare()) {
            Console.WriteLine("Failed TestCompare");
            result = -1;
        }

        if (TestMul() != 1) {
            Console.WriteLine("Failed TestMul");
            result = -1;
        }

        if (TestMulTypeSize() != 0) {
            Console.WriteLine ("Failed TestMulTypeSize");
            result = -1;
        }

        if (result == 100) {
            Console.WriteLine("PASSED");
        }
        else {
            Console.WriteLine("FAILED");
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long TestBinary()
    {
        long l = 0;
        long result = l ^ System.Threading.Interlocked.Exchange(ref l, 1);
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool TestCompare()
    {
        long l = 0;
        bool result = l > System.Threading.Interlocked.Exchange(ref l, 1);
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long TestMul()
    {
        long l = 1;
        long result = l * System.Threading.Interlocked.Exchange(ref l, 0);
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int TestMulTypeSize()
    {
        S0 s = new S0();
        s.F2--;
        int i = System.Threading.Volatile.Read(ref s.F1) * 100;
        return i;
    }
}

