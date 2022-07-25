// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System
{
    public static partial class Environment
    {
        internal static int CurrentNativeThreadId => unchecked((int)Interop.Kernel32.GetCurrentThreadId());

        public static long TickCount64 => (long)Interop.Kernel32.GetTickCount64();
    }
}
