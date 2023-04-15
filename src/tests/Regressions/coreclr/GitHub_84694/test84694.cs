// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public class C0
{
}

public struct S0
{
    public C0 F0;
    public C0 F1;
    public S0(C0 f1) : this()
    {
    }
}

public class Program
{
    public static int Main()
    {
        GC.KeepAlive(new S0[,] { { new S0(new C0()) } });
        return 100;
    }
}
