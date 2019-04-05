// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetTimestampResolution", ExactSpelling = true)]
        internal static extern ulong GetTimestampResolution();

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetTimestamp", ExactSpelling = true)]
        internal static extern ulong GetTimestamp();
    }
}
