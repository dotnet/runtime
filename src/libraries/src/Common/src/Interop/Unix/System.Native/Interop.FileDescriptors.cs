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
            internal static readonly SafeFileHandle STDIN_FILENO = CreateFileHandle(0);
            internal static readonly SafeFileHandle STDOUT_FILENO = CreateFileHandle(1);
            internal static readonly SafeFileHandle STDERR_FILENO = CreateFileHandle(2);

            private static SafeFileHandle CreateFileHandle(int fileNumber)
            {
                return new SafeFileHandle((IntPtr)fileNumber, ownsHandle: false);
            }
        }
    }
}
