// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.Serialization;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// The exception that is thrown upon <see cref="IHost"/> abortion.
    /// </summary>
    [Serializable]
    public sealed class HostAbortedException : Exception
    {
#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
        private HostAbortedException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="HostAbortedException"/> class
        /// with a system-supplied error message.
        /// </summary>
        public HostAbortedException() : base(SR.HostAbortedExceptionMessage) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="HostAbortedException"/> class
        /// with a specified error message.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        /// <remarks>
        /// The content of <paramref name="message"/> is intended to be understood by humans.
        /// The caller of this constructor is required to ensure that this string has been localized for the
        /// current system culture.
        /// </remarks>
        public HostAbortedException(string? message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="HostAbortedException"/> class
        /// with a specified error message and a reference to the inner exception that
        /// is the cause of this exception.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception.
        /// </param>
        /// <remarks>
        /// The content of <paramref name="message"/> is intended to be understood by humans.
        /// The caller of this constructor is required to ensure that this string has been localized for the
        /// current system culture.
        /// </remarks>
        public HostAbortedException(string? message, Exception? innerException) : base(message, innerException) { }
    }
}
