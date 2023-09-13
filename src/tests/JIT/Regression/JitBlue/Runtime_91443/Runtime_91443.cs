// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_91443
{
    [Fact]
    public static void TestEntryPoint()
    {
        new Runtime_91443().Method0();
    }
    
    static Vector3 s;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Method0()
    {
        Vector3.Cross(s, s);
    }
}
