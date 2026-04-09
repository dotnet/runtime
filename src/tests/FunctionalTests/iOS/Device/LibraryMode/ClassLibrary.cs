// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

public static class ClassLibrary
{
    [UnmanagedCallersOnly(EntryPoint = nameof(SayHello))]
    public static int SayHello()
    {
        Console.WriteLine("Called from native!  Hello!");
        return 42;
    }
}
