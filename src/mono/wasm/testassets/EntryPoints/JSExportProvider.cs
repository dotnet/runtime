// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

[assembly: SupportedOSPlatform("browser")]

namespace AssemblyProviderTest;

public static partial class Exports
{
    [JSExport]
    public static int Add(int left, int right) => left + right;

    [JSExport]
    public static string Echo(string value) => value;
}
