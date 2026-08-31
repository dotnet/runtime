// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices.JavaScript;

namespace LibraryMode;

public partial class Test
{
    [JSExport]
    public static int MyExport()
    {
        Console.WriteLine("TestOutput -> WASM Library MyExport is called");
        return 100;
    }
}
