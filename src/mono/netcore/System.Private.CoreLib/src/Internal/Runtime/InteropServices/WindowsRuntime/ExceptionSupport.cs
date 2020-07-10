// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Internal.Runtime.InteropServices.WindowsRuntime
{
    public static class ExceptionSupport
    {
        /// <summary>
        /// Attach restricted error information to the exception if it may apply to that exception, returning
        /// back the input value
        /// </summary>
        [return: NotNullIfNotNull("e")]
        public static Exception? AttachRestrictedErrorInfo(Exception? e) => throw new PlatformNotSupportedException();

        /// <summary>
        /// Report that an exception has occurred which went user unhandled.  This allows the global error handler
        /// for the application to be invoked to process the error.
        /// </summary>
        /// <returns>true if the error was reported, false if not (ie running on Win8)</returns>
        public static bool ReportUnhandledError(Exception? ex) => throw new PlatformNotSupportedException();
    }
}
