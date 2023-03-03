// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

[assembly: PrimaryInteropAssembly(1, 0)]

public struct ValueTypeWithStaticMethod
{
    public int F;
    public static void M() { }
}

public struct ValueTypeWithInstanceMethod
{
    public int F;
    public void M() { }
}