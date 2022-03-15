// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Quic
{
    public class QuicTlsVersionException : QuicException
    {
        internal QuicTlsVersionException(long errorCode)
            : this(SR.net_quic_tls_version_notsupported, errorCode)
        {
        }

        public QuicTlsVersionException(string message, long errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public long ErrorCode { get; }

        public Version MinimumRequiredVersion => new Version(1, 3);
    }
}
