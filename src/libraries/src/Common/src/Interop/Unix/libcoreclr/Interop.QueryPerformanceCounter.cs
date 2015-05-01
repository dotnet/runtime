// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libcoreclr
    {
        [DllImport(Libraries.LibCoreClr)]
        internal static extern bool QueryPerformanceCounter(out long value);

        [DllImport(Libraries.LibCoreClr)]
        internal static extern bool QueryPerformanceFrequency(out long value);
    }
}
