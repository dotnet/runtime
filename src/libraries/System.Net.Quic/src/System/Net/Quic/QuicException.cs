// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Net.Quic
{
    public sealed class QuicException : IOException
    {
        public QuicException(QuicError error, string message, long? applicationErrorCode, Exception? innerException)
            : base(message, innerException)
        {
            QuicError = error;
            ApplicationErrorCode = applicationErrorCode;
        }

        public QuicException(QuicError error, string message, long? applicationErrorCode, Exception? innerException, int result)
            : this(error, message, applicationErrorCode, innerException)
        {
            // HResult 0 means OK, so do not override the default value set by Exception ctor,
            // because in this case we don't have an HResult.
            if (result != 0)
            {
                HResult = result;
            }
        }

        public QuicError QuicError { get; }

        public long? ApplicationErrorCode { get; }
    }
}
