// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

[StructLayout(LayoutKind.Sequential)]
public struct S1
{
}

[StructLayout(LayoutKind.Auto)]
public struct S2
{
}

public class Runtime_103628
{
    public static S1 Get_S1() => new S1();

    public static S2 Get_S2() => new S2();

    [Fact]
    public static void TestEntryPoint()
    {
        S1 s1 = Get_S1();
        S2 s2 = Get_S2();
    }
}
