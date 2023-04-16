// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public struct S0
{
    public object F0;
    public object F1;
    public S0(object f1) : this()
    {
    }
}

public class Program
{
    public static int Main()
    {
        GC.KeepAlive(new S0[,] { { new S0(new object()) } });
        return 100;
    }
}
