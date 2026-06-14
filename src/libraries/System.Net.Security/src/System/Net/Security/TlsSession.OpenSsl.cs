// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security
{
    public sealed partial class TlsSession
    {
        // When true, socket-bound I/O delegates ciphertext directly to OpenSSL via
        // SSL_set_fd / SSL_do_handshake / SSL_read / SSL_write, bypassing the
        // managed ProcessHandshake/Encrypt/Decrypt loop and its scratch buffers.
        private bool _useFdMode;

        // Bind the socket directly to the SSL object so OpenSSL drives ciphertext
        // I/O itself. AllocateSslHandle inspects options.SocketHandle and skips
        // the ManagedSpanBio installation when set. With fd-mode active, no
        // managed Socket wrapper is needed - OpenSSL calls recv/send on the fd.
        partial void EnableNativeSocketBinding(SafeSocketHandle socket, ref bool nativeBindingEnabled)
        {
            _options.SocketHandle = socket;
            _useFdMode = true;
            nativeBindingEnabled = true;
        }

        partial void TryFastHandshake(ref TlsOperationStatus? result)
        {
            if (!_useFdMode)
            {
                return;
            }

            SafeSslHandle ssl = EnsureFdSslHandle();
            int ret = Interop.Ssl.SslDoHandshake(ssl, out Interop.Ssl.SslErrorCode err);
            if (ret == 1)
            {
                OnHandshakeCompleted();
                result = TlsOperationStatus.Complete;
                return;
            }
            result = MapSslError(err, "SSL_do_handshake");
        }

        partial void TryFastRead(Span<byte> buffer, ref int bytesRead, ref TlsOperationStatus? result)
        {
            if (!_useFdMode)
            {
                return;
            }

            if (buffer.IsEmpty)
            {
                result = TlsOperationStatus.Complete;
                return;
            }

            SafeSslHandle ssl = (SafeSslHandle)_securityContext!;
            int ret = Interop.Ssl.SslRead(ssl, ref MemoryMarshal.GetReference(buffer), buffer.Length, out Interop.Ssl.SslErrorCode err);
            if (ret > 0)
            {
                bytesRead = ret;
                result = TlsOperationStatus.Complete;
                return;
            }
            result = MapSslError(err, "SSL_read");
        }

        partial void TryFastWrite(ReadOnlySpan<byte> buffer, ref int bytesWritten, ref TlsOperationStatus? result)
        {
            if (!_useFdMode)
            {
                return;
            }

            if (buffer.IsEmpty)
            {
                result = TlsOperationStatus.Complete;
                return;
            }

            SafeSslHandle ssl = (SafeSslHandle)_securityContext!;
            int ret = Interop.Ssl.SslWrite(ssl, ref MemoryMarshal.GetReference(buffer), buffer.Length, out Interop.Ssl.SslErrorCode err);
            if (ret > 0)
            {
                bytesWritten = ret;
                result = TlsOperationStatus.Complete;
                return;
            }
            result = MapSslError(err, "SSL_write");
        }

        private SafeSslHandle EnsureFdSslHandle()
        {
            if (_securityContext is SafeSslHandle existing && !existing.IsInvalid)
            {
                return existing;
            }
            SafeSslHandle handle = Interop.OpenSsl.AllocateSslHandle(_options);
            _securityContext = handle;
            return handle;
        }

        private static TlsOperationStatus MapSslError(Interop.Ssl.SslErrorCode error, string op)
        {
            return error switch
            {
                Interop.Ssl.SslErrorCode.SSL_ERROR_WANT_READ => TlsOperationStatus.WantRead,
                Interop.Ssl.SslErrorCode.SSL_ERROR_WANT_WRITE => TlsOperationStatus.WantWrite,
                Interop.Ssl.SslErrorCode.SSL_ERROR_ZERO_RETURN => TlsOperationStatus.Closed,
                _ => throw new AuthenticationException($"OpenSSL {op} failed: {error}"),
            };
        }
    }
}
