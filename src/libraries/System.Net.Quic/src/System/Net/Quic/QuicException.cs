// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Net.Quic
{
    public sealed class QuicException : IOException
    {
        public QuicException(QuicError error, long? applicationErrorCode, string message, Exception? innerException)
            : base(message, innerException)
        {
            QuicError = error;
            ApplicationErrorCode = applicationErrorCode;
        }

        public QuicError QuicError { get; }

        public long? ApplicationErrorCode { get; }
    }
}
