// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;

public static class Program
{
    public static int Main(string[] args)
    {
        var culture = new CultureInfo("es-ES", false);
        // https://github.com/dotnet/runtime/blob/main/docs/design/features/globalization-invariant-mode.md#cultures-and-culture-data
        int result = culture.LCID == 0x1000 && culture.NativeName == "Invariant Language (Invariant Country)" ? 42 : 1;

        return result;
    }
}
