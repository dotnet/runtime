// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

public static class Program
{
    [UnmanagedCallersOnly(EntryPoint="HelloSteve")]
    public static void HelloSteve()
    {
        Console.WriteLine("Called from the outside!  Hello!");
    }

    public static int Main()
    {
        Console.WriteLine("Hello, Android!"); // logcat
        return 42;
    }
}
