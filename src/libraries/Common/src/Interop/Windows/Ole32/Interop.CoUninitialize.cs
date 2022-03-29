// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Ole32
    {
        [GeneratedDllImport(Interop.Libraries.Ole32, ExactSpelling = true)]
        internal static partial int CoUninitialize();
    }
}
