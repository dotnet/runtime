// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("browser")]

namespace LegacyExports;

public static partial class Exports
{
    [JSExport]
    public static int Add(int left, int right) => left + right;

    [JSExport]
    public static string Echo(string value) => value;

    [JSExport]
    public static JSObject EchoObject(JSObject value) => value;

    [JSExport]
    public static void Throw() => throw new InvalidOperationException("Legacy export exception");

    [JSExport]
    public static async Task<int> AddAsync(int left, int right)
    {
        await Task.Yield();
        return left + right;
    }

    public static partial class Nested
    {
        [JSExport]
        public static int Negate(int value) => -value;
    }
}
