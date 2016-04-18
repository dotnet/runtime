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
            Debug.Assert(e.StackTrace == null);

            try { throw e; }
            catch { return e; }
        }
    }
}
