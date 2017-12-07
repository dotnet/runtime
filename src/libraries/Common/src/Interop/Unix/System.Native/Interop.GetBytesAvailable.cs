// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetBytesAvailable")]
        internal static extern unsafe Error GetBytesAvailable(SafeHandle socket, int* available);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetAtOutOfBandMark")]
        internal static extern unsafe Error GetAtOutOfBandMark(SafeHandle socket, int* atMark);
    }
}
