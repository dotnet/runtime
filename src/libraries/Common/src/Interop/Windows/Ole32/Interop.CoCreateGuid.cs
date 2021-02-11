// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Ole32
    {
        [DllImport(Interop.Libraries.Ole32)]
        internal static extern int CoCreateGuid(out Guid guid);
    }
}
