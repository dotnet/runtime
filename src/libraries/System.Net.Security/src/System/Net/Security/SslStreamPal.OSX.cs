// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using PAL_TlsHandshakeState = Interop.AppleCrypto.PAL_TlsHandshakeState;
using PAL_TlsIo = Interop.AppleCrypto.PAL_TlsIo;

namespace System.Net.Security
{
    internal static class SslStreamPal
    {
        public static Exception GetException(SecurityStatusPal status)
        {
            return status.Exception ?? new Win32Exception((int)status.ErrorCode);
        }

        internal const bool StartMutualAuthAsAnonymous = true;

        // SecureTransport is okay with a 0 byte input, but it produces a 0 byte output.
        // Since ST is not producing the framed empty message just call this false and avoid the
        // special case of an empty array being passed to the `fixed` statement.
        internal const bool CanEncryptEmptyMessage = false;
        internal const bool CanGenerateCustomAlerts = false;

        public static void VerifyPackageInfo()
        {
        }

        public static bool IsAsyncSecurityContext(SafeDeleteContext securityContext)
        {
            return securityContext is SafeDeleteNwContext;
        }

        public static SecurityStatusPal SelectApplicationProtocol(
            SafeFreeCredentials? _,
            SafeDeleteContext securityContext,
            SslAuthenticationOptions sslAuthenticationOptions,
            ReadOnlySpan<byte> clientProtocols)
        {
            // Client did not provide ALPN or APLN is not needed
            if (clientProtocols.Length == 0 ||
                sslAuthenticationOptions.ApplicationProtocols == null || sslAuthenticationOptions.ApplicationProtocols.Count == 0)
            {
                return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
            }

            SafeDeleteSslContext context = (SafeDeleteSslContext)securityContext;

            // We do server side ALPN e.g. walk the intersect in server order
            foreach (SslApplicationProtocol applicationProtocol in sslAuthenticationOptions.ApplicationProtocols)
            {
                ReadOnlySpan<byte> protocols = clientProtocols;

                while (protocols.Length > 0)
                {
                    byte length = protocols[0];
                    if (protocols.Length < length + 1)
                    {
                        break;
                    }
                    ReadOnlySpan<byte> protocol = protocols.Slice(1, length);
                    if (protocol.SequenceCompareTo<byte>(applicationProtocol.Protocol.Span) == 0)
                    {
                        int osStatus = Interop.AppleCrypto.SslCtxSetAlpnProtocol(context.SslContext, applicationProtocol);
                        if (osStatus == 0)
                        {
                            context.SelectedApplicationProtocol = applicationProtocol;
                            if (NetEventSource.Log.IsEnabled())
                                NetEventSource.Info(context, $"Selected '{applicationProtocol}' ALPN");
                        }
                        else
                        {
                            if (NetEventSource.Log.IsEnabled())
                                NetEventSource.Error(context, $"Failed to set ALPN: {osStatus}");
                        }

                        // We ignore failure and we will move on with ALPN
                        return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
                    }

                    protocols = protocols.Slice(protocol.Length + 1);
                }
            }

            return new SecurityStatusPal(SecurityStatusPalErrorCode.ApplicationProtocolMismatch);
        }

#pragma warning disable IDE0060
        public static ProtocolToken AcceptSecurityContext(
            ref SafeFreeCredentials credential,
            ref SafeDeleteContext? context,
            ReadOnlySpan<byte> inputBuffer,
            out int consumed,
            SslAuthenticationOptions sslAuthenticationOptions)
        {
            return HandshakeInternal(ref context, inputBuffer, out consumed, sslAuthenticationOptions);
        }

        public static ProtocolToken InitializeSecurityContext(
            ref SafeFreeCredentials credential,
            ref SafeDeleteContext? context,
            string? _ /*targetName*/,
            ReadOnlySpan<byte> inputBuffer,
            out int consumed,
            SslAuthenticationOptions sslAuthenticationOptions)
        {
            return HandshakeInternal(ref context, inputBuffer, out consumed, sslAuthenticationOptions);
        }

        public static ProtocolToken Renegotiate(
            ref SafeFreeCredentials? credentialsHandle,
            ref SafeDeleteContext? context,
            SslAuthenticationOptions sslAuthenticationOptions)
        {
            throw new PlatformNotSupportedException();
        }

        public static SafeFreeCredentials? AcquireCredentialsHandle(SslAuthenticationOptions _1, bool _2)
        {
            return null;
        }

#pragma warning restore IDE0060

        public static ProtocolToken EncryptMessage(
            SafeDeleteContext securityContext,
            ReadOnlyMemory<byte> input,
            int _ /*headerSize*/,
            int _1 /*trailerSize*/)
        {
            Debug.Assert(input.Length > 0, $"{nameof(input.Length)} > 0 since {nameof(CanEncryptEmptyMessage)} is false");

            Debug.Assert(securityContext is SafeDeleteSslContext, "SafeDeleteSslContext expected");
            SafeDeleteSslContext sslContext = (SafeDeleteSslContext)securityContext;

            ProtocolToken token = default;

            try
            {
                SafeSslHandle sslHandle = sslContext.SslContext;

                unsafe
                {
                    MemoryHandle memHandle = input.Pin();
                    try
                    {
                        PAL_TlsIo status = Interop.AppleCrypto.SslWrite(
                                sslHandle,
                                (byte*)memHandle.Pointer,
                                input.Length,
                                out int written);

                        if (status < 0)
                        {
                            token.Status = new SecurityStatusPal(
                                SecurityStatusPalErrorCode.InternalError,
                                Interop.AppleCrypto.CreateExceptionForOSStatus((int)status));
                            return token;
                        }

                        switch (status)
                        {
                            case PAL_TlsIo.Success:
                                token.Status = new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
                                break;
                            case PAL_TlsIo.WouldBlock:
                                token.Status = new SecurityStatusPal(SecurityStatusPalErrorCode.ContinueNeeded);
                                break;
                            default:
                                Debug.Fail($"Unknown status value: {status}");
                                token.Status = new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError);
                                break;
                        }

                        sslContext.ReadPendingWrites(ref token);
                    }
                    finally
                    {
                        memHandle.Dispose();
                    }
                }
            }
            catch (Exception e)
            {
                token.Status = new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, e);
            }

            return token;
        }

        public static SecurityStatusPal DecryptMessage(
            SafeDeleteContext securityContext,
            Span<byte> buffer,
            out int offset,
            out int count)
        {
            Debug.Assert(securityContext is SafeDeleteSslContext, "SafeDeleteSslContext expected");
            SafeDeleteSslContext sslContext = (SafeDeleteSslContext)securityContext;

            offset = 0;
            count = 0;

            try
            {
                SafeSslHandle sslHandle = sslContext.SslContext;

                sslContext.Write(buffer);

                unsafe
                {
                    fixed (byte* ptr = buffer)
                    {
                        PAL_TlsIo status = Interop.AppleCrypto.SslRead(sslHandle, ptr, buffer.Length, out int written);
                        if (status < 0)
                        {
                            return new SecurityStatusPal(
                                SecurityStatusPalErrorCode.InternalError,
                                Interop.AppleCrypto.CreateExceptionForOSStatus((int)status));
                        }

                        count = written;
                        offset = 0;

                        switch (status)
                        {
                            case PAL_TlsIo.Success:
                            case PAL_TlsIo.WouldBlock:
                                return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
                            case PAL_TlsIo.ClosedGracefully:
                                return new SecurityStatusPal(SecurityStatusPalErrorCode.ContextExpired);
                            case PAL_TlsIo.Renegotiate:
                                return new SecurityStatusPal(SecurityStatusPalErrorCode.Renegotiate);
                            default:
                                Debug.Fail($"Unknown status value: {status}");
                                return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, e);
            }
        }

        public static ChannelBinding? QueryContextChannelBinding(
            SafeDeleteContext securityContext,
            ChannelBindingKind attribute)
        {
            switch (attribute)
            {
                case ChannelBindingKind.Endpoint:
                    return EndpointChannelBindingToken.Build(securityContext);
            }

            // SecureTransport doesn't expose the Finished messages, so a Unique binding token
            // cannot be built.
            //
            // Windows/netfx compat says to return null for not supported kinds (including unmapped enum values).
            return null;
        }

        public static void QueryContextStreamSizes(
            SafeDeleteContext? _ /*securityContext*/,
            out StreamSizes streamSizes)
        {
            streamSizes = StreamSizes.Default;
        }

        public static void QueryContextConnectionInfo(
            SafeDeleteContext securityContext,
            ref SslConnectionInfo connectionInfo)
        {
            connectionInfo.UpdateSslConnectionInfo(securityContext);
        }

        public static bool TryUpdateClintCertificate(
            SafeFreeCredentials? _,
            SafeDeleteContext? context,
            SslAuthenticationOptions sslAuthenticationOptions)
        {
            if (context == null)
            {
                return false;
            }

            if (context is SafeDeleteNwContext)
            {
                // We are being called from Network Framework, we will retrieve
                // the selected certificate from higher frame in the callstack
                // and return it as return value of the callback
                return true;
            }

            SafeDeleteSslContext sslContext = ((SafeDeleteSslContext)context);

            if (sslAuthenticationOptions.CertificateContext != null)
            {
                SafeDeleteSslContext.SetCertificate(sslContext!.SslContext, sslAuthenticationOptions.CertificateContext);
            }

            return true;
        }

        private static ProtocolToken HandshakeInternal(
            ref SafeDeleteContext? context,
            ReadOnlySpan<byte> inputBuffer,
            out int consumed,
            SslAuthenticationOptions sslAuthenticationOptions)
        {
            ProtocolToken token = default;
            consumed = 0;

            try
            {
                if ((null == context) || context.IsInvalid)
                {
                    Debug.Assert(!ShouldUseAsyncSecurityContext(sslAuthenticationOptions));

                    if (NetEventSource.Log.IsEnabled())
                        NetEventSource.Info(null, $"Using SecureTransport (SafeDeleteSslContext) for TLS connection - Protocols: {sslAuthenticationOptions.EnabledSslProtocols}, IsClient: {sslAuthenticationOptions.IsClient}, NetworkFrameworkAvailable: {SafeDeleteNwContext.IsNetworkFrameworkAvailable}");

                    context = new SafeDeleteSslContext(sslAuthenticationOptions);
                }

                Debug.Assert(context is SafeDeleteSslContext, "SafeDeleteSslContext expected");
                SafeDeleteSslContext sslContext = (SafeDeleteSslContext)context;

                if (inputBuffer.Length > 0)
                {
                    sslContext.Write(inputBuffer);
                }

                consumed = inputBuffer.Length;

                token.Status = PerformHandshake(sslContext.SslContext);

                if (token.Status.ErrorCode == SecurityStatusPalErrorCode.CredentialsNeeded)
                {
                    // this should happen only for clients
                    Debug.Assert(sslAuthenticationOptions.IsClient);
                    return token;
                }

                sslContext.ReadPendingWrites(ref token);
                return token;
            }
            catch (Exception exc)
            {
                token.Status = new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, exc);
                return token;
            }
        }

        private static SecurityStatusPal PerformHandshake(SafeSslHandle sslHandle)
        {
            while (true)
            {
                PAL_TlsHandshakeState handshakeState = Interop.AppleCrypto.SslHandshake(sslHandle);

                switch (handshakeState)
                {
                    case PAL_TlsHandshakeState.Complete:
                        return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
                    case PAL_TlsHandshakeState.WouldBlock:
                        return new SecurityStatusPal(SecurityStatusPalErrorCode.ContinueNeeded);
                    case PAL_TlsHandshakeState.ServerAuthCompleted:
                    case PAL_TlsHandshakeState.ClientAuthCompleted:
                        // The standard flow would be to call the verification callback now, and
                        // possibly abort.  But the library is set up to call this "success" and
                        // do verification between "handshake complete" and "first send/receive".
                        //
                        // So, call SslHandshake again to indicate to Secure Transport that we've
                        // accepted this handshake and it should go into the ready state.
                        break;
                    case PAL_TlsHandshakeState.ClientCertRequested:
                        return new SecurityStatusPal(SecurityStatusPalErrorCode.CredentialsNeeded);
                    case PAL_TlsHandshakeState.ClientHelloReceived:
                        return new SecurityStatusPal(SecurityStatusPalErrorCode.HandshakeStarted);
                    default:
                        return new SecurityStatusPal(
                            SecurityStatusPalErrorCode.InternalError,
                            Interop.AppleCrypto.CreateExceptionForOSStatus((int)handshakeState));
                }
            }
        }

#pragma warning disable IDE0060
        public static SecurityStatusPal ApplyAlertToken(
            SafeDeleteContext? securityContext,
            TlsAlertType alertType,
            TlsAlertMessage alertMessage)
        {
            // There doesn't seem to be an exposed API for writing an alert,
            // the API seems to assume that all alerts are generated internally by
            // SSLHandshake.
            Debug.Assert(CanGenerateCustomAlerts);
            return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
        }
#pragma warning restore IDE0060

        public static SecurityStatusPal ApplyShutdownToken(
            SafeDeleteContext securityContext)
        {
            if (securityContext is SafeDeleteNwContext nwContext)
            {
                nwContext.Shutdown();
                return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
            }

            Debug.Assert(securityContext is SafeDeleteSslContext, "SafeDeleteSslContext expected");
            SafeDeleteSslContext context = (SafeDeleteSslContext)securityContext;
            SafeSslHandle sslHandle = context.SslContext;

            int osStatus = Interop.AppleCrypto.SslShutdown(sslHandle);

            if (osStatus == 0)
            {
                return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
            }

            return new SecurityStatusPal(
                SecurityStatusPalErrorCode.InternalError,
                Interop.AppleCrypto.CreateExceptionForOSStatus(osStatus));
        }

        internal static bool ShouldUseAsyncSecurityContext(SslAuthenticationOptions sslAuthenticationOptions)
        {
            return ShouldUseNetworkFramework(sslAuthenticationOptions);
        }

        private static bool ShouldUseNetworkFramework(
            SslAuthenticationOptions sslAuthenticationOptions)
        {
            return
                sslAuthenticationOptions.IsClient &&
                SafeDeleteNwContext.IsNetworkFrameworkAvailable &&
                (sslAuthenticationOptions.EnabledSslProtocols == SslProtocols.None ||
                   sslAuthenticationOptions.EnabledSslProtocols == SslProtocols.Tls13 ||
                    (sslAuthenticationOptions.EnabledSslProtocols == (SslProtocols.Tls12 | SslProtocols.Tls13)));
        }

        private static SafeDeleteNwContext CreateAsyncSecurityContext(SslStream stream)
        {
            Debug.Assert(ShouldUseAsyncSecurityContext(stream._sslAuthenticationOptions),
                "ShouldUseAsyncSecurityContext should be true when creating an async security context.");

            if (NetEventSource.Log.IsEnabled())
                NetEventSource.Info(null, $"Using Network Framework (SafeDeleteNwContext) for TLS connection - Protocols: {stream._sslAuthenticationOptions.EnabledSslProtocols}");
            return new SafeDeleteNwContext(stream);
        }

        internal static Task<Exception?> AsyncHandshakeAsync(ref SafeDeleteContext? context, SslStream stream, CancellationToken cancellationToken)
        {
            Debug.Assert(context == null);
            try
            {
                SafeDeleteNwContext nwContext = CreateAsyncSecurityContext(stream);
                context = nwContext;
                return nwContext.HandshakeAsync(cancellationToken);
            }
            catch (Exception e)
            {
                return Task.FromResult<Exception?>(e);
            }
        }

        internal static Task AsyncWriteAsync(SafeDeleteContext securityContext, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            Debug.Assert(securityContext is SafeDeleteNwContext, "SafeDeleteNwContext expected for async write");
            SafeDeleteNwContext nwContext = (SafeDeleteNwContext)securityContext;
            return nwContext.WriteAsync(buffer, cancellationToken);
        }

        internal static ValueTask<int> AsyncReadAsync(SafeDeleteContext securityContext, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            Debug.Assert(securityContext is SafeDeleteNwContext, "SafeDeleteNwContext expected for async read");
            SafeDeleteNwContext nwContext = (SafeDeleteNwContext)securityContext;
            return nwContext.ReadAsync(buffer, cancellationToken);
        }
    }
}
