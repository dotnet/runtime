// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.HelloManaged;

public static class Entrypoints
{
    [UnmanagedCallersOnly(EntryPoint="hellomanaged_Hello")]
    public static void Hello()
    {
        HelloManaged o = new();
        Console.WriteLine ($"Hello {o.GetMagic()}");
    }
}
