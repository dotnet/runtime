// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Net.Quic
{
    public class QuicException : Exception
    {
        public QuicException(string? message) { throw null; }
        public QuicException(string? message, Exception? innerException) { throw null; }
    }
    public class QuicConnectionAbortedException : QuicException
    {
        public QuicConnectionAbortedException(string message, long errorCode) : base(default) { throw null; }
        public long ErrorCode { get { throw null; } }
    }
    public class QuicOperationAbortedException : QuicException
    {
        public QuicOperationAbortedException(string message) : base(default) { throw null; }
    }
    public class QuicStreamAbortedException : QuicException
    {
        public QuicStreamAbortedException(string message, long errorCode) : base(default) { throw null; }
        public long ErrorCode { get { throw null; } }
    }
}
