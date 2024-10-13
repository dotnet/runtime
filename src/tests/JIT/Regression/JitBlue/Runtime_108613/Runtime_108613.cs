// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Reduced test case from Antigen.
//
// Failure:
//   Assertion failed 'false && "found use of a node that is not in the LIR sequence"'

using System;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_108613
{
    Vector512<uint> v512_uint_104 = Vector512.Create((uint)2, 3, 3, 57, 57, 57, 5, 2, 0, 2, 57, 57, 57, 2, 5, 2);
    public int Method0()
    {
        Vector512<uint> x = Vector512.AndNot(Vector512.Equals(v512_uint_104, v512_uint_104), Vector512.Equals(v512_uint_104, v512_uint_104));
        return (int)x.GetElement(2);
    }

    [Fact]
    public static void TestEntryPoint()
    {
        if (Vector512.IsHardwareAccelerated)
        {
            Runtime_108613 t = new Runtime_108613();
            Assert.Equal(0, t.Method0());
        }
    }
}
