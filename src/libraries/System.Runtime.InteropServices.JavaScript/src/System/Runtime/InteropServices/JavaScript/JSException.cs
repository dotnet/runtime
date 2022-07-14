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
        internal JSObject? jsException;

        /// <summary>
        /// Initializes a new instance of the JSException class with a specified error message.
        /// </summary>
        /// <param name="msg">The message that describes the error.</param>
        public JSException(string msg) : base(msg)
        {
            jsException = null;
        }

        internal JSException(string msg, JSObject? jsException) : base(msg)
        {
            this.jsException = jsException;
        }

        /// <inheritdoc />
        public override string? StackTrace
        {
            get
            {
                var bs = base.StackTrace;
                if (jsException == null)
                {
                    return bs;
                }
                string? jsStackTrace = jsException.GetPropertyAsString("stack");
                if (jsStackTrace == null)
                {
                    if (bs == null)
                    {
                        return null;
                    }
                }
                else if (jsStackTrace.StartsWith(Message + "\n"))
                {
                    // Some JS runtimes insert the error message at the top of the stack, some don't,
                    // so normalize it by using the stack as the result if it already contains the error
                    jsStackTrace = jsStackTrace.Substring(Message.Length + 1);
                }

                if (bs == null)
                {
                    return jsStackTrace;
                }
                return base.StackTrace + "\r\n" + jsStackTrace;
            }

        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is JSException other && other.jsException == jsException;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return jsException == null
                ? base.GetHashCode()
                : base.GetHashCode() * jsException.GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            // we avoid expensive stack trace
            return Message;
        }
    }
}
