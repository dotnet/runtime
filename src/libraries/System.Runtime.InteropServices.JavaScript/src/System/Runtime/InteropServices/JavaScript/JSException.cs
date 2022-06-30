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
        /// Initializes a new instance of the Exception class with a specified error message.
        /// </summary>
        /// <param name="msg">The message that describes the error.</param>
        public JSException(string msg) : base(msg)
        {
        }

        /// <inheritdoc />
        public override string StackTrace
        {
            get => throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            throw new NotImplementedException();
        }
    }
}
