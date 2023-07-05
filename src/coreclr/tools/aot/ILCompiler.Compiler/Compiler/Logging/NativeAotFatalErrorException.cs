// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using ILCompiler.Logging;

namespace ILCompiler
{
    /// <summary>
    /// Represents a known error that occurred during compilation time which is not solvable by the user.
    /// This is used when we want to present the non-recoverable error with a specific error code.
    /// </summary>
    public class NativeAotFatalErrorException : Exception
    {
        public MessageContainer MessageContainer { get; }

        /// <param name="message">Error message with a description of what went wrong</param>
        public NativeAotFatalErrorException(MessageContainer message)
            : base(message.ToString())
        {
            Debug.Assert(message.Category == MessageCategory.Error, $"'{nameof(NativeAotFatalErrorException)}' ought to be used for errors only");
            Debug.Assert(message.Code != null && message.Code.Value != 0, $"'{nameof(NativeAotFatalErrorException)}' must have a code that indicates a failure");
            MessageContainer = message;
        }

        /// <param name="message">Error message with a description of what went wrong</param>
        /// <param name="innerException"></param>
        public NativeAotFatalErrorException(MessageContainer message, Exception innerException)
            : base(message.ToString(), innerException)
        {
            Debug.Assert(message.Category == MessageCategory.Error, $"'{nameof(NativeAotFatalErrorException)}' ought to be used for errors only");
            Debug.Assert(message.Code != null && message.Code.Value != 0, $"'{nameof(NativeAotFatalErrorException)}' must have a code that indicates failure");
            MessageContainer = message;
        }
    }
}
