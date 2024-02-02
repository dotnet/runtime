// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using System.Threading;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Represents an exception initiated from the JavaScript interop code.
    /// </summary>
    [SupportedOSPlatform("browser")] // @kg: Do we really need to platform-lock JSException?
    public sealed class JSException : Exception
    {
        internal JSObject? jsException;
        internal string? combinedStackTrace;

        /// <summary>
        /// Initializes a new instance of the JSException class with a specified error message.
        /// </summary>
        /// <param name="msg">The message that describes the error.</param>
        public JSException(string msg) : base(msg)
        {
            jsException = null;
            combinedStackTrace = null;
        }

        internal JSException(string msg, JSObject? jsException) : base(msg)
        {
            this.jsException = jsException;
            this.combinedStackTrace = null;
        }

        /// <inheritdoc />
        public override string? StackTrace
        {
            get
            {
                if (combinedStackTrace != null)
                {
                    return combinedStackTrace;
                }
                var bs = base.StackTrace;
                if (jsException == null)
                {
                    return bs;
                }

#if FEATURE_WASM_MANAGED_THREADS
                if (!jsException.ProxyContext.IsCurrentThread())
                {
                    // if we are on another thread, it would be too expensive and risky to obtain lazy stack trace.
                    return bs + Environment.NewLine + "... omitted JavaScript stack trace from another thread.";
                }
#endif
                string? jsStackTrace = jsException.GetPropertyAsString("stack");

                // after this, we don't need jsException proxy anymore
                jsException.Dispose();
                jsException = null;

                if (jsStackTrace == null)
                {
                    combinedStackTrace = bs;
                    return combinedStackTrace;
                }
                else if (jsStackTrace.StartsWith(Message + "\n"))
                {
                    // Some JS runtimes insert the error message at the top of the stack, some don't,
                    // so normalize it by using the stack as the result if it already contains the error
                    jsStackTrace = jsStackTrace.Substring(Message.Length + 1);
                }

                if (bs == null)
                {
                    combinedStackTrace = jsStackTrace;
                }

                combinedStackTrace = bs != null
                    ? bs + Environment.NewLine + jsStackTrace
                    : jsStackTrace;

                return combinedStackTrace;
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
