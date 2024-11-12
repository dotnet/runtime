// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Diagnostics
{
    public static partial class Debugger
    {
        /// <summary>
        /// Signals a breakpoint to an attached debugger with the <paramref name="exception"/> details
        /// if a .NET debugger is attached with break on user-unhandled exception enabled and a method
        /// attributed with DebuggerDisableUserUnhandledExceptionsAttribute calls this method.
        /// </summary>
        /// <param name="exception">The user-unhandled exception.</param>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void BreakForUserUnhandledException(Exception exception)
        {
        }
    }
}
