// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>
    /// An exception that should be thrown on code-paths that are unreachable.
    /// </summary>
    internal sealed class UnreachableException : Exception
    {
        public UnreachableException() { }

        public UnreachableException(string message) : base(message) { }
    }
}
