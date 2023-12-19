// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Net.Security;
using System.Security.Principal;

namespace System.Net
{
    internal abstract partial class NegotiateAuthenticationPal : IDisposable
    {
        public abstract bool IsAuthenticated { get; }
        public abstract bool IsSigned { get; }
        public abstract bool IsEncrypted { get; }
        public abstract bool IsMutuallyAuthenticated { get; }
        public abstract string Package { get; }
        public abstract string? TargetName { get; }
        public abstract IIdentity RemoteIdentity { get; }
        public abstract System.Security.Principal.TokenImpersonationLevel ImpersonationLevel { get; }
        public abstract void Dispose();
        public abstract byte[]? GetOutgoingBlob(ReadOnlySpan<byte> incomingBlob, out NegotiateAuthenticationStatusCode statusCode);
        public abstract NegotiateAuthenticationStatusCode Wrap(ReadOnlySpan<byte> input, IBufferWriter<byte> outputWriter, bool requestEncryption, out bool isEncrypted);
        public abstract NegotiateAuthenticationStatusCode Unwrap(ReadOnlySpan<byte> input, IBufferWriter<byte> outputWriter, out bool wasEncrypted);
        public abstract NegotiateAuthenticationStatusCode UnwrapInPlace(Span<byte> input, out int unwrappedOffset, out int unwrappedLength, out bool wasEncrypted);
        public abstract void GetMIC(ReadOnlySpan<byte> message, IBufferWriter<byte> signature);
        public abstract bool VerifyMIC(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature);
    }
}
