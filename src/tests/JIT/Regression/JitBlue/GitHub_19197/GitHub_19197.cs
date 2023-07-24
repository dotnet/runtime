// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This test was extracted from the corefx System.Numerics.Vectors tests,
// and was failing with minOpts because a SIMD12 was being spilled using
// a 16-byte load, but only a 12-byte location had been allocated.

using System;
using System.Numerics;
using Xunit;

public class GitHub_19171
{
    static int returnVal = 100;

    static internal void Vector3EqualsTest()
    {
        Vector3 a = new Vector3(1.0f, 2.0f, 3.0f);
        Vector3 b = new Vector3(1.0f, 2.0f, 3.0f);

        // case 1: compare between same values
        object obj = b;

        bool expected = true;
        bool actual = a.Equals(obj);
        Equal(expected, actual);

        // case 2: compare between different values
        b.X = 10.0f;
        obj = b;
        expected = false;
        actual = a.Equals(obj);
        Equal(expected, actual);

        // case 3: compare between different types.
        obj = new Quaternion();
        expected = false;
        actual = a.Equals(obj);
        Equal(expected, actual);

        // case 3: compare against null.
        obj = null;
        expected = false;
        actual = a.Equals(obj);
        Equal(expected, actual);
    }

    static void Equal(bool a, bool b)
    {
        Console.WriteLine(a == b ? "ok" : "bad");
        if (a != b)
        {
            returnVal = -1;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Vector3EqualsTest();
        return returnVal;
    }
}
