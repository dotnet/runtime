// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    /// <summary>Provides a set of static methods for working with Exceptions.</summary>
    internal static class ExceptionHelpers
    {
        public static TException InitializeStackTrace<TException>(this TException e) where TException : Exception
        {
            Debug.Assert(e != null);

            // Ideally we'd be able to populate e.StackTrace with the current stack trace.
            // We could throw the exception and catch it, but the populated StackTrace would
            // not extend beyond this frame.  Instead, we grab a stack trace and store it into
            // the exception's data dictionary, at least making the info available for debugging,
            // albeit not part of the string returned by e.ToString() or e.StackTrace.

            // Issue https://github.com/dotnet/corefx/issues/8866, avoid a BadImageFormatException
            // when trying to get the stack trace.
            // e.Data["StackTrace"] = Environment.StackTrace;

            return e;
        }
    }
}
