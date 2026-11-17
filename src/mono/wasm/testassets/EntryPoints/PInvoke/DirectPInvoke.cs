// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

if (Imports.StrLen("hello") != 5)
    return 1;

try
{
    Imports.NotListed();
    return 2;
}
catch (DllNotFoundException)
{
}

Console.WriteLine("TestOutput -> DirectPInvoke sample passed");
return 42;

static class Imports
{
    [DllImport("libc", EntryPoint = "strlen")]
    public static extern nuint StrLen(string value);

    // Not listed in DirectPInvoke, so it stays unbound and fails only when called.
    [DllImport("libc", EntryPoint = "definitely_not_in_libc")]
    public static extern int NotListed();
}
