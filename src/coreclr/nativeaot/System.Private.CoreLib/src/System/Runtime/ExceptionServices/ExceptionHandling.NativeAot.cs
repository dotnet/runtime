// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Runtime.ExceptionServices
{
    public static partial class ExceptionHandling
    {
        internal static IntPtr s_fatalErrorHandler;

        private static unsafe bool TrySetFatalErrorHandler(IntPtr handler)
        {
            if (Interlocked.CompareExchange(ref s_fatalErrorHandler, handler, IntPtr.Zero) != IntPtr.Zero)
                return false;

            // Register the user callback with the native runtime so genuinely-unmanaged
            // fatal exceptions can invoke it without transitioning into managed code.
            RuntimeImports.RhpRegisterFatalErrorHandler((void*)handler);

            return true;
        }
    }
}
