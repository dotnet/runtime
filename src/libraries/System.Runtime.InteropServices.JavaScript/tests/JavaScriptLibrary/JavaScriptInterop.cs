// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices.JavaScript;

namespace JavaScriptLibrary;

public partial class JavaScriptInterop
{
    [JSExport]
    public static int ExportedMethod(int a, int b) => a + b;

    internal static int ValidationMethod(int a, int b) => a + b;
}