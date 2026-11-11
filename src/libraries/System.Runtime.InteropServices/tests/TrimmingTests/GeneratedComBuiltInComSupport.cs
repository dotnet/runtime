// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;

internal static class Program
{
    private static int Main()
    {
        return BuiltInComSupported(null!) ? -1 : 100;
    }

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "get_BuiltInComSupported")]
    private static extern bool BuiltInComSupported(ComObject _);
}
