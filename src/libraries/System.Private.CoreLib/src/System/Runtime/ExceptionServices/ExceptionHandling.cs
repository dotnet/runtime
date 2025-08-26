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
        /// <remarks>
        /// The handler will be called when an unhandled exception occurs.
        /// The handler should return true if the exception was handled, or false if it was not.
        /// If the handler returns false, the exception will continue to propagate as unhandled.
        ///
        /// The intent of this handler is to allow the user to handle unhandled exceptions
        /// gracefully when the runtime is being used in certain scenarios. Scenarios such
        /// as REPLs or game scripting that host plug-ins are not able to handle unhandled
        /// exceptions thrown by those plug-ins.
        /// </remarks>
        public static void SetUnhandledExceptionHandler(Func<Exception, bool> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            if (Interlocked.CompareExchange(ref s_handler, handler, null) != null)
            {
                throw new InvalidOperationException(SR.InvalidOperation_CannotRegisterSecondHandler);
            }
        }

        /// <summary>
        /// Raises the <see cref="AppDomain.UnhandledException"/> event.
        /// </summary>
        /// <param name="exception">Exception to pass to event handlers.</param>
        /// <remarks>
        /// This method will raise the <see cref="AppDomain.UnhandledException"/>
        /// event and then return.
        ///
        /// It will not raise the the handler registered with <see cref="SetUnhandledExceptionHandler"/>.
        ///
        /// This API is thread safe and can be called from multiple threads. However, only one thread
        /// will trigger the event handlers, while other threads will wait indefinitely without raising
        /// the event.
        /// </remarks>
        public static void RaiseAppDomainUnhandledExceptionEvent(object exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            AppContext.OnUnhandledException(exception);
        }
    }
}
