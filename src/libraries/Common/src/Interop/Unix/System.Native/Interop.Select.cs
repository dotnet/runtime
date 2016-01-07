// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_Select")]
        internal static extern unsafe Error Select(int fdCount, uint* readFds, uint* writeFds, uint* errorFds, int microseconds, int* selected);
    }
}
