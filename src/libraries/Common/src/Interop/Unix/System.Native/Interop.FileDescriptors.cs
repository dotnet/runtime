// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal static class FileDescriptors
        {
            internal static readonly SafeFileHandle STDIN_FILENO = new SafeFileHandle((IntPtr)0, false);
            internal static readonly SafeFileHandle STDOUT_FILENO = new SafeFileHandle((IntPtr)1, false);
            internal static readonly SafeFileHandle STDERR_FILENO = new SafeFileHandle((IntPtr)2, false);
        }
    }
}
