// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Runtime.ExceptionServices
{
    public static partial class ExceptionHandling
    {
        internal static IntPtr s_fatalErrorHandler;

        private static bool TrySetFatalErrorHandler(IntPtr handler)
        {
            return Interlocked.CompareExchange(ref s_fatalErrorHandler, handler, IntPtr.Zero) == IntPtr.Zero;
        }
    }
}
