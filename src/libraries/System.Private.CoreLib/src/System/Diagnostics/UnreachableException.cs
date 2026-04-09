// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>
    /// Exception thrown when the program executes an instruction that was thought to be unreachable.
    /// </summary>
    public sealed class UnreachableException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnreachableException"/> class with the default error message.
        /// </summary>
        public UnreachableException()
            : base(SR.Arg_UnreachableException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnreachableException"/>
        /// class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public UnreachableException(string? message)
            : base(message ?? SR.Arg_UnreachableException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnreachableException"/>
        /// class with a specified error message and a reference to the inner exception that is the cause of
        /// this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public UnreachableException(string? message, Exception? innerException)
            : base(message ?? SR.Arg_UnreachableException, innerException)
        {
        }
    }
}
