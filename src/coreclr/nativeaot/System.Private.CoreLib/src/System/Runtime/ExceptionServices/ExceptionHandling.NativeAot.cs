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

            // Route genuinely-unmanaged fatal exceptions (faults in native code that are
            // never translated to a managed exception) to the handler as well. The native
            // choke points only divert when this callback is registered, so the default
            // fatal handling is unchanged until a handler is installed.
            RuntimeImports.RhpRegisterFatalErrorHandlerForNativeException(
                &RuntimeExceptionHelpers.InvokeFatalErrorHandlerForNativeException);

            return true;
        }
    }
}
