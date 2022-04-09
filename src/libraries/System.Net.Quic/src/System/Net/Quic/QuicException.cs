// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Quic
{
    public class QuicException : Exception
    {
        public QuicException(string? message)
            : base(message)
        {
        }
        public QuicException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }

        public QuicException(string? message, Exception? innerException, int result)
            : base(message, innerException)
        {
            // HResult 0 means OK, so do not override the default value set by Exception ctor,
            // because in this case we don't have an HResult.
            if (result != 0)
            {
                HResult = result;
            }
        }
    }
}
