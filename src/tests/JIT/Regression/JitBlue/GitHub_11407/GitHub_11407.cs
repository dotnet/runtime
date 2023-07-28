// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test has two effectively identical initializations of an
// array of byte vs. an array of structs containing a single byte field.
// They should generate the same code.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class GitHub_11407
{
    struct foo { public byte b1, b2, b3, b4; }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static foo getfoo() { return new foo(); }

    [Fact]
    public static int TestEntryPoint()
    {
        int returnVal = 100;
        foo myFoo = getfoo();
        if (myFoo.b1 != 0 || myFoo.b2 != 0 || myFoo.b3 != 0 || myFoo.b4 != 0)
        {
            returnVal = -1;
        }
        return returnVal;
    }
}
