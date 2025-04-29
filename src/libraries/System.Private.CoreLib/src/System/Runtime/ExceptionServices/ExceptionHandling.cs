// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Runtime.ExceptionServices
{
    public static class ExceptionHandling
    {
        private static Func<Exception, bool>? s_handler;

        internal static bool IsHandledByGlobalHandler(Exception ex)
        {
            return s_handler?.Invoke(ex) == true;
        }

        /// <summary>
        /// Sets a handler for unhandled exceptions.
        /// </summary>
        /// <exception cref="ArgumentNullException">If handler is null</exception>
        /// <exception cref="InvalidOperationException">If a handler is already set</exception>
        public static void SetUnhandledExceptionHandler(Func<Exception, bool> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            if (Interlocked.CompareExchange(ref s_handler, handler, null) != null)
            {
                throw new InvalidOperationException(SR.InvalidOperation_CannotRegisterSecondHandler);
            }
        }
    }
}
