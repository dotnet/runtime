// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices.JavaScript;

[assembly:System.Runtime.Versioning.SupportedOSPlatform("browser")]

namespace LazyLibrary;

public partial class Foo
{
    [JSExport]
    public static int Bar()
    {
        Console.WriteLine("Hello from Foo.Bar!");
        return 42;
    }
}
