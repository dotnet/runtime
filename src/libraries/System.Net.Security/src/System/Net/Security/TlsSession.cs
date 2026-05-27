// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    /// <summary>
    /// Non-blocking TLS state machine wrapper around the existing
    /// <see cref="SslStreamPal"/>. PoC scope: detached mode only (caller owns I/O),
    /// Linux-only (OpenSSL). Provides <see cref="ProcessHandshake"/>,
    /// <see cref="Encrypt"/>, <see cref="Decrypt"/>, and a pending-output queue.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The session never performs any I/O. The caller drives ciphertext in/out
    /// via byte spans. Any ciphertext the TLS layer needs to send (handshake
    /// records, alerts, encrypted application data) is staged in an internal
    /// pending-output buffer and drained via <see cref="DrainPendingOutput"/>.
    /// </para>
    /// <para>
    /// Contract: any operation may return <see cref="TlsOperationStatus.WantWrite"/>
    /// to indicate the caller must drain pending output before further progress
    /// is possible. The session does not consume new input while pending output
    /// is non-empty.
    /// </para>
    /// </remarks>
    public sealed class TlsSession : IDisposable
    {
        // Matches StreamSizes.Default on Unix; conservative upper bound for a
        // single TLS record's plaintext payload.
        internal const int MaxRecordPlaintext = 16354;

        private readonly TlsContext _context;
        private SafeFreeCredentials? _credentialsHandle;
        private SafeDeleteSslContext? _securityContext;

        private byte[]? _pending;
        private int _pendingOffset;
        private int _pendingLength;

        private byte[]? _decryptScratch;

        private bool _isHandshakeComplete;
        private bool _disposed;
        private SslConnectionInfo _connectionInfo;

        private TlsSession(TlsContext context)
        {
            _context = context;
        }

        public static TlsSession Create(TlsContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (!OperatingSystem.IsLinux() && !OperatingSystem.IsFreeBSD())
            {
                throw new PlatformNotSupportedException(
                    "TlsSession is currently implemented only on Linux/FreeBSD (OpenSSL).");
            }

            return new TlsSession(context);
        }

        // ── State ─────────────────────────────────────────────────────────

        public bool IsServer => _context.IsServer;

        public bool IsHandshakeComplete => _isHandshakeComplete;

        public bool HasPendingOutput => _pendingLength > 0;

        public string? TargetHostName
        {
            get => _context.Options.TargetHost;
            set => _context.Options.TargetHost = value ?? string.Empty;
        }

        public SslProtocols NegotiatedProtocol =>
            _isHandshakeComplete ? (SslProtocols)_connectionInfo.Protocol : SslProtocols.None;

        [System.CLSCompliant(false)]
        public TlsCipherSuite NegotiatedCipherSuite =>
            _isHandshakeComplete ? (TlsCipherSuite)_connectionInfo.TlsCipherSuite : default;

        public SslApplicationProtocol NegotiatedApplicationProtocol
        {
            get
            {
                if (!_isHandshakeComplete || _connectionInfo.ApplicationProtocol == null)
                {
                    return default;
                }
                return new SslApplicationProtocol(_connectionInfo.ApplicationProtocol);
            }
        }

        public X509Certificate2? GetRemoteCertificate()
        {
            if (_securityContext == null || _securityContext.IsInvalid)
            {
                return null;
            }
            return CertificateValidationPal.GetRemoteCertificate(_securityContext);
        }

        // ── Handshake ─────────────────────────────────────────────────────

        public TlsOperationStatus ProcessHandshake(
            ReadOnlySpan<byte> input,
            Span<byte> output,
            out int consumed,
            out int produced)
        {
            ThrowIfDisposed();
            consumed = 0;
            produced = 0;

            if (_isHandshakeComplete)
            {
                throw new InvalidOperationException("Handshake has already completed.");
            }

            // Drain pending first; do not consume new input while output is owed.
            if (_pendingLength > 0)
            {
                produced = DrainTo(output);
                return _pendingLength > 0 ? TlsOperationStatus.WantWrite : TlsOperationStatus.Complete;
            }

            ProtocolToken token = default;
            token.RentBuffer = true;
            try
            {
                if (_context.IsServer)
                {
                    token = SslStreamPal.AcceptSecurityContext(
                        ref _credentialsHandle,
                        ref _securityContext,
                        input,
                        out consumed,
                        _context.Options);
                }
                else
                {
                    string hostName = TargetHostNameHelper.NormalizeHostName(_context.Options.TargetHost);
                    token = SslStreamPal.InitializeSecurityContext(
                        ref _credentialsHandle,
                        ref _securityContext,
                        hostName,
                        input,
                        out consumed,
                        _context.Options);
                }

                // Stage any handshake bytes the PAL produced.
                if (token.Size > 0)
                {
                    Debug.Assert(token.Payload != null);
                    AppendPending(new ReadOnlySpan<byte>(token.Payload, 0, token.Size));
                }

                if (token.Failed)
                {
                    throw new AuthenticationException(SR.net_auth_SSPI, token.GetException());
                }

                bool done = token.Status.ErrorCode == SecurityStatusPalErrorCode.OK;

                if (done)
                {
                    _isHandshakeComplete = true;
                    SslStreamPal.QueryContextConnectionInfo(_securityContext!, ref _connectionInfo);
                }

                if (_pendingLength > 0)
                {
                    produced = DrainTo(output);
                    if (_pendingLength > 0)
                    {
                        return TlsOperationStatus.WantWrite;
                    }
                }

                if (done)
                {
                    return TlsOperationStatus.Complete;
                }

                // We made progress but still need more peer data.
                return TlsOperationStatus.WantRead;
            }
            finally
            {
                token.ReleasePayload();
            }
        }

        // ── Encrypt ───────────────────────────────────────────────────────

        public TlsOperationStatus Encrypt(
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            out int consumed,
            out int produced)
        {
            ThrowIfDisposed();
            consumed = 0;
            produced = 0;

            if (!_isHandshakeComplete)
            {
                throw new InvalidOperationException("Handshake has not yet completed.");
            }

            if (_pendingLength > 0)
            {
                produced = DrainTo(ciphertext);
                return _pendingLength > 0 ? TlsOperationStatus.WantWrite : TlsOperationStatus.Complete;
            }

            if (plaintext.IsEmpty)
            {
                return TlsOperationStatus.Complete;
            }

            int chunk = Math.Min(plaintext.Length, MaxRecordPlaintext);
            byte[] rented = ArrayPool<byte>.Shared.Rent(chunk);
            try
            {
                plaintext.Slice(0, chunk).CopyTo(rented);

                ProtocolToken token = SslStreamPal.EncryptMessage(
                    _securityContext!,
                    new ReadOnlyMemory<byte>(rented, 0, chunk),
                    0,
                    0);

                try
                {
                    if (token.Status.ErrorCode != SecurityStatusPalErrorCode.OK)
                    {
                        throw new IOException(SR.net_io_encrypt, SslStreamPal.GetException(token.Status));
                    }

                    consumed = chunk;

                    if (token.Size > 0)
                    {
                        Debug.Assert(token.Payload != null);
                        AppendPending(new ReadOnlySpan<byte>(token.Payload, 0, token.Size));
                    }
                }
                finally
                {
                    token.ReleasePayload();
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }

            produced = DrainTo(ciphertext);
            return _pendingLength > 0 ? TlsOperationStatus.WantWrite : TlsOperationStatus.Complete;
        }

        // ── Decrypt ───────────────────────────────────────────────────────

        public TlsOperationStatus Decrypt(
            ReadOnlySpan<byte> ciphertext,
            Span<byte> plaintext,
            out int consumed,
            out int produced)
        {
            ThrowIfDisposed();
            consumed = 0;
            produced = 0;

            if (!_isHandshakeComplete)
            {
                throw new InvalidOperationException("Handshake has not yet completed.");
            }

            if (_pendingLength > 0)
            {
                // Caller must drain before we accept new input.
                return TlsOperationStatus.WantWrite;
            }

            // Need at least a frame header.
            if (ciphertext.Length < TlsFrameHelper.HeaderSize)
            {
                return TlsOperationStatus.WantRead;
            }

            TlsFrameHeader header = default;
            if (!TlsFrameHelper.TryGetFrameHeader(ciphertext, ref header))
            {
                throw new IOException(SR.net_io_decrypt);
            }

            int frameSize = header.Length;
            if (ciphertext.Length < frameSize)
            {
                return TlsOperationStatus.WantRead;
            }

            // PAL decrypts in place; copy into a writable scratch buffer.
            EnsureDecryptScratch(frameSize);
            ciphertext.Slice(0, frameSize).CopyTo(_decryptScratch);

            SecurityStatusPal status = SslStreamPal.DecryptMessage(
                _securityContext!,
                _decryptScratch.AsSpan(0, frameSize),
                out int outOffset,
                out int outCount);

            switch (status.ErrorCode)
            {
                case SecurityStatusPalErrorCode.OK:
                    consumed = frameSize;
                    if (outCount > plaintext.Length)
                    {
                        throw new InvalidOperationException(
                            $"Plaintext buffer too small: needed {outCount}, got {plaintext.Length}.");
                    }
                    _decryptScratch.AsSpan(outOffset, outCount).CopyTo(plaintext);
                    produced = outCount;
                    return TlsOperationStatus.Complete;

                case SecurityStatusPalErrorCode.ContextExpired:
                    consumed = frameSize;
                    return TlsOperationStatus.Closed;

                case SecurityStatusPalErrorCode.Renegotiate:
                    // Out of scope for the PoC.
                    throw new NotSupportedException(
                        "Renegotiation surfaced from Decrypt is not yet supported by TlsSession.");

                default:
                    throw new IOException(SR.net_io_decrypt, SslStreamPal.GetException(status));
            }
        }

        // ── Pending output ────────────────────────────────────────────────

        public TlsOperationStatus DrainPendingOutput(Span<byte> ciphertext, out int produced)
        {
            ThrowIfDisposed();
            produced = DrainTo(ciphertext);
            return _pendingLength > 0 ? TlsOperationStatus.WantWrite : TlsOperationStatus.Complete;
        }

        // ── Internals ─────────────────────────────────────────────────────

        private void AppendPending(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
            {
                return;
            }

            // Compact if anything was already drained.
            if (_pending != null && _pendingOffset > 0)
            {
                if (_pendingLength > 0)
                {
                    Buffer.BlockCopy(_pending, _pendingOffset, _pending, 0, _pendingLength);
                }
                _pendingOffset = 0;
            }

            int needed = _pendingLength + data.Length;
            if (_pending == null || _pending.Length < needed)
            {
                byte[] bigger = ArrayPool<byte>.Shared.Rent(Math.Max(needed, 4096));
                if (_pending is byte[] old)
                {
                    if (_pendingLength > 0)
                    {
                        Buffer.BlockCopy(old, 0, bigger, 0, _pendingLength);
                    }
                    ArrayPool<byte>.Shared.Return(old);
                }
                _pending = bigger;
            }

            data.CopyTo(_pending.AsSpan(_pendingLength));
            _pendingLength += data.Length;
        }

        private int DrainTo(Span<byte> output)
        {
            if (_pendingLength == 0)
            {
                return 0;
            }

            int n = Math.Min(output.Length, _pendingLength);
            _pending!.AsSpan(_pendingOffset, n).CopyTo(output);
            _pendingOffset += n;
            _pendingLength -= n;

            if (_pendingLength == 0)
            {
                ArrayPool<byte>.Shared.Return(_pending!);
                _pending = null;
                _pendingOffset = 0;
            }

            return n;
        }

        private void EnsureDecryptScratch(int size)
        {
            if (_decryptScratch == null || _decryptScratch.Length < size)
            {
                if (_decryptScratch != null)
                {
                    ArrayPool<byte>.Shared.Return(_decryptScratch);
                }
                _decryptScratch = ArrayPool<byte>.Shared.Rent(size);
            }
        }

        private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            _securityContext?.Dispose();
            _securityContext = null;
            _credentialsHandle?.Dispose();
            _credentialsHandle = null;

            if (_pending != null)
            {
                ArrayPool<byte>.Shared.Return(_pending);
                _pending = null;
            }
            if (_decryptScratch != null)
            {
                ArrayPool<byte>.Shared.Return(_decryptScratch);
                _decryptScratch = null;
            }
        }
    }
}
