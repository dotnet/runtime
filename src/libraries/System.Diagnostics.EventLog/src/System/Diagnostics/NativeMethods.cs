// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32
{
    internal static class NativeMethods
    {
        public readonly static HandleRef NullHandleRef = new HandleRef(null, IntPtr.Zero);

        public const int WAIT_OBJECT_0 = 0x00000000;
        public const int WAIT_TIMEOUT = 0x00000102;
        public const int WAIT_ABANDONED = 0x00000080;
        public const int SEEK_READ = 0x2;
        public const int FORWARDS_READ = 0x4;
        public const int BACKWARDS_READ = 0x8;
        public const int ERROR_EVENTLOG_FILE_CHANGED = 1503;
        public const int ERROR_FILE_NOT_FOUND = 2;
    }
}
