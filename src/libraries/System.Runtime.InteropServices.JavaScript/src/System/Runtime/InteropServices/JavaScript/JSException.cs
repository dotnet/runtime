// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Represents an Exception initiated from the JavaScript interop code.
    /// </summary>
    [SupportedOSPlatform("browser")] // @kg: Do we really need to platform-lock JSException?
    public sealed class JSException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the JSException class with a specified error message.
        /// </summary>
        /// <param name="msg">The message that describes the error.</param>
        public JSException(string msg) : base(msg)
        {
        }

        /// <inheritdoc />
        public override string? StackTrace
        {
            get
            {
                return null;
            }
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return base.Equals(obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            // we avoid expensive stack trace
            return Message;
        }
    }
}
