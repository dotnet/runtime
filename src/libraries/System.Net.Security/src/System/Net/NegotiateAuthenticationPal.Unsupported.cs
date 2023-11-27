// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Net.Security;
using System.Security.Principal;

namespace System.Net
{
    internal abstract partial class NegotiateAuthenticationPal
    {
        internal sealed class UnsupportedNegotiateAuthenticationPal : NegotiateAuthenticationPal
        {
            private string _package;
            private string? _targetName;
            private NegotiateAuthenticationStatusCode _statusCode;

            public override bool IsAuthenticated => false;
            public override bool IsSigned => false;
            public override bool IsEncrypted => false;
            public override bool IsMutuallyAuthenticated => false;
            public override string Package => _package;
            public override string? TargetName => _targetName;
            public override IIdentity RemoteIdentity => throw new InvalidOperationException();
            public override System.Security.Principal.TokenImpersonationLevel ImpersonationLevel => System.Security.Principal.TokenImpersonationLevel.Impersonation;

            public UnsupportedNegotiateAuthenticationPal(NegotiateAuthenticationClientOptions clientOptions, NegotiateAuthenticationStatusCode statusCode = NegotiateAuthenticationStatusCode.Unsupported)
            {
                _package = clientOptions.Package;
                _targetName = clientOptions.TargetName;
                _statusCode = statusCode;
            }

            public UnsupportedNegotiateAuthenticationPal(NegotiateAuthenticationServerOptions serverOptions, NegotiateAuthenticationStatusCode statusCode = NegotiateAuthenticationStatusCode.Unsupported)
            {
                _package = serverOptions.Package;
                _statusCode = statusCode;
            }

            public override void Dispose()
            {
            }

            public override byte[]? GetOutgoingBlob(ReadOnlySpan<byte> incomingBlob, out NegotiateAuthenticationStatusCode statusCode)
            {
                statusCode = _statusCode;
                return null;
            }

            public override NegotiateAuthenticationStatusCode Wrap(ReadOnlySpan<byte> input, IBufferWriter<byte> outputWriter, bool requestEncryption, out bool isEncrypted) => throw new InvalidOperationException();
            public override NegotiateAuthenticationStatusCode Unwrap(ReadOnlySpan<byte> input, IBufferWriter<byte> outputWriter, out bool wasEncrypted) => throw new InvalidOperationException();
            public override NegotiateAuthenticationStatusCode UnwrapInPlace(Span<byte> input, out int unwrappedOffset, out int unwrappedLength, out bool wasEncrypted) => throw new InvalidOperationException();
            public override void GetMIC(ReadOnlySpan<byte> message, IBufferWriter<byte> signature) => throw new InvalidOperationException();
            public override bool VerifyMIC(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature) => throw new InvalidOperationException();
        }
    }
}
