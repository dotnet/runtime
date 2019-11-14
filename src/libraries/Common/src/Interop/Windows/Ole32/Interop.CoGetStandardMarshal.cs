// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Ole32
    {
        [DllImport(Interop.Libraries.Ole32)]
        internal static extern int CoGetStandardMarshal(ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags, out IntPtr ppMarshal);
    }
}
