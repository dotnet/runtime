// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace System.Net
{
    internal sealed class SafeDeleteSslContext : SafeDeleteContext
    {
        private const int InitialBufferSize = 2048;
        private readonly SafeSslHandle _sslContext;

        private ArrayBuffer _inputBuffer = new ArrayBuffer(InitialBufferSize);
        private ArrayBuffer _outputBuffer = new ArrayBuffer(InitialBufferSize);

        public SafeSslHandle SslContext => _sslContext;

        public SafeDeleteSslContext(SafeFreeSslCredentials credential, SslAuthenticationOptions authOptions)
            : base(credential)
        {
            Debug.Assert((credential != null) && !credential.IsInvalid, "Invalid credential used in SafeDeleteSslContext");

            _sslContext = new SafeSslHandle();
            throw new NotImplementedException(nameof(SafeDeleteSslContext));
        }

        public override bool IsInvalid => _sslContext?.IsInvalid ?? true;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SafeSslHandle sslContext = _sslContext;
                if (sslContext != null)
                {
                    _inputBuffer.Dispose();
                    _outputBuffer.Dispose();
                    sslContext.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        private unsafe void WriteToConnection(byte* data, int offset, int dataLength)
        {
            var inputBuffer = new ReadOnlySpan<byte>(data, dataLength);

            _outputBuffer.EnsureAvailableSpace(dataLength);
            inputBuffer.CopyTo(_outputBuffer.AvailableSpan);
            _outputBuffer.Commit(dataLength);
        }

        private unsafe int ReadFromConnection(byte* data, int offset, int dataLength)
        {
            if (dataLength == 0)
                return 0;

            if (_inputBuffer.ActiveLength == 0)
                return 0;

            int toRead = Math.Min(dataLength, _inputBuffer.ActiveLength);

            _inputBuffer.ActiveSpan.Slice(0, toRead).CopyTo(new Span<byte>(data, toRead));
            _inputBuffer.Discard(toRead);
            return toRead;
        }

        internal void Write(ReadOnlySpan<byte> buf)
        {
            _inputBuffer.EnsureAvailableSpace(buf.Length);
            buf.CopyTo(_inputBuffer.AvailableSpan);
            _inputBuffer.Commit(buf.Length);
        }

        internal int BytesReadyForConnection => _outputBuffer.ActiveLength;

        internal byte[]? ReadPendingWrites()
        {
            if (_outputBuffer.ActiveLength == 0)
            {
                return null;
            }

            byte[] buffer = _outputBuffer.ActiveSpan.ToArray();
            _outputBuffer.Discard(_outputBuffer.ActiveLength);

            return buffer;
        }

        internal int ReadPendingWrites(byte[] buf, int offset, int count)
        {
            Debug.Assert(buf != null);
            Debug.Assert(offset >= 0);
            Debug.Assert(count >= 0);
            Debug.Assert(count <= buf.Length - offset);

            int limit = Math.Min(count, _outputBuffer.ActiveLength);

            _outputBuffer.ActiveSpan.Slice(0, limit).CopyTo(new Span<byte>(buf, offset, limit));
            _outputBuffer.Discard(limit);

            return limit;
        }
    }
}
