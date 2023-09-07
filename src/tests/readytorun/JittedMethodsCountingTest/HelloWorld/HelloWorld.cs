// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal class HelloWorld
{
    static int Main(string[] args)
    {
        Console.WriteLine("Hello World!");
        // We're fine converting to int here because if we got that many jitted
        // methods that would require a long, then that means something is
        // extremely wrong. Actually, I'm not even sure it's possible.
        int jits = (int) System.Runtime.JitInfo.GetCompiledMethodCount(false);
        return jits > 0 ? jits : -1;
    }
}
