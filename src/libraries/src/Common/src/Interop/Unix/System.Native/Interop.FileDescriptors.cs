// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
