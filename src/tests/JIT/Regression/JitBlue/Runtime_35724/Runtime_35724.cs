// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

// SIMD8 could be retyped as a long in the past and if that long value was CSE-ed together with original SIMD8
// values we could hit an assert `IsCompatibleType(cseLclVarTyp, expTyp)`.

public class Runtime_35724
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	static Vector2 Test()
    {
        Vector2 a = new Vector2(1);
        Vector2 b = new Vector2(2);
        Vector2 c = a / b;
        Vector2 d = a / b;
        Console.WriteLine(c.X + d.Y);
        return a / b;
    }
	
    [Fact]
    public static int TestEntryPoint()
    {
        Test();
        return 100;
    }
}
