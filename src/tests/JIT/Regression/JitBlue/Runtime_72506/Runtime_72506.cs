// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

public class Runtime_72506
{
    private static int retCode = 100;

    public static int Main()
    {
        // float
        AssertEqual(Vector256.Create(1f).ToString(), "<1, 1, 1, 1, 1, 1, 1, 1>");
        AssertEqual(Vector256.CreateScalar(1f).ToString(), "<1, 0, 0, 0, 0, 0, 0, 0>");
        AssertEqual(Vector256.CreateScalarUnsafe(1f).ToScalar().ToString(), "1");
        AssertEqual(Vector256.Create(0.0f, 1, 2, 3, 4, 5, 6, 7).ToString(), "<0, 1, 2, 3, 4, 5, 6, 7>");

        // double
        AssertEqual(Vector256.Create(1.0).ToString(), "<1, 1, 1, 1>");
        AssertEqual(Vector256.CreateScalar(1.0).ToString(), "<1, 0, 0, 0>");
        AssertEqual(Vector256.CreateScalarUnsafe(1.0).ToScalar().ToString(), "1");
        AssertEqual(Vector256.Create(0.0, 1, 2, 3).ToString(), "<0, 1, 2, 3>");

        // ushort
        AssertEqual(Vector256.Create((ushort)1).ToString(), "<1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1>");
        AssertEqual(Vector256.CreateScalar((ushort)1).ToString(), "<1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0>");
        AssertEqual(Vector256.CreateScalarUnsafe((ushort)1).ToScalar().ToString(), "1");
        AssertEqual(Vector256.Create((ushort)0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15).ToString(), "<0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15>");

        // long
        AssertEqual(Vector256.Create((long)1).ToString(), "<1, 1, 1, 1>");
        AssertEqual(Vector256.CreateScalar((long)1).ToString(), "<1, 0, 0, 0>");
        AssertEqual(Vector256.CreateScalarUnsafe((long)1).ToScalar().ToString(), "1");
        AssertEqual(Vector256.Create((long)0, 1, 2, 3).ToString(), "<0, 1, 2, 3>");
        return retCode;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AssertEqual(string s1, string s2)
    {
        if (s1 != s2)
        {
            Console.WriteLine($"{s1} != {s2}");
            retCode++;
        }
    }
}
