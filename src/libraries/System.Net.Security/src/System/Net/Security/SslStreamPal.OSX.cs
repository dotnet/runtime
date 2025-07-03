// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using PAL_TlsHandshakeState = Interop.AppleCrypto.PAL_TlsHandshakeState;
using PAL_TlsIo = Interop.AppleCrypto.PAL_TlsIo;

namespace System.Net.Security
{
    internal static partial class SslStreamPal
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

        public static void VerifyPackageInfo()
        {
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
            ReadOnlyMemory<byte> inputBuffer,
            out int consumed,
            SslAuthenticationOptions sslAuthenticationOptions)
        {
            var (token, ctx) = HandshakeInternal(context, inputBuffer, sslAuthenticationOptions).AsTask().GetAwaiter().GetResult();
            consumed = inputBuffer.Length;
            context = ctx;
            return token;
        }

        public static ProtocolToken InitializeSecurityContext(
            ref SafeFreeCredentials credential,
            ref SafeDeleteContext? context,
            string? _ /*targetName*/,
            ReadOnlyMemory<byte> inputBuffer,
            out int consumed,
            SslAuthenticationOptions sslAuthenticationOptions)
        {
            var (token, ctx) = HandshakeInternal(context, inputBuffer, sslAuthenticationOptions).AsTask().GetAwaiter().GetResult();
            consumed = inputBuffer.Length;
            context = ctx;
            return token;
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
            int headerSize,
            int trailerSize)
        {
            Debug.Assert(input.Length > 0, $"{nameof(input.Length)} > 0 since {nameof(CanEncryptEmptyMessage)} is false");

            return securityContext switch
            {
                SafeDeleteSslContext sslContext => EncryptMessage(sslContext, input, headerSize, trailerSize),
                SafeDeleteNwContext nwContext => NetworkFramework.EncryptAsync(nwContext, input, headerSize, trailerSize).GetAwaiter().GetResult(),
                _ => throw new PlatformNotSupportedException()
            };
        }

        public static ProtocolToken EncryptMessage(
            SafeDeleteSslContext securityContext,
            ReadOnlyMemory<byte> input,
            int _ /*headerSize*/,
            int _1 /*trailerSize*/)
        {
            ProtocolToken token = default;

            try
            {
                SafeDeleteSslContext context = (SafeDeleteSslContext)securityContext;
                SafeSslHandle sslHandle = context.SslContext;

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

                        context.ReadPendingWrites(ref token);
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
            Memory<byte> buffer,
            out int offset,
            out int count)
        {
            switch (securityContext)
            {
                case SafeDeleteSslContext sslContext:
                    return DecryptMessage(sslContext, buffer.Span, out offset, out count);

                case SafeDeleteNwContext nwContext:
                    (SecurityStatusPal status, int o, int c) = NetworkFramework.DecryptAsync(nwContext, buffer).GetAwaiter().GetResult();
                    offset = o;
                    count = c;

                    return status;

                default:
                    throw new PlatformNotSupportedException();
            }
        }

        public static SecurityStatusPal DecryptMessage(
            SafeDeleteSslContext securityContext,
            Span<byte> buffer,
            out int offset,
            out int count)
        {
            offset = 0;
            count = 0;

            try
            {
                SafeDeleteSslContext context = (SafeDeleteSslContext)securityContext;
                SafeSslHandle sslHandle = context.SslContext;

                context.Write(buffer);

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
            SafeDeleteSslContext? sslContext = ((SafeDeleteSslContext?)context);

            if (sslAuthenticationOptions.CertificateContext != null)
            {
                SafeDeleteSslContext.SetCertificate(sslContext!.SslContext, sslAuthenticationOptions.CertificateContext);
            }

            return true;
        }

        private static async ValueTask<(ProtocolToken, SafeDeleteContext? context)> HandshakeInternal(
            SafeDeleteContext? context,
            ReadOnlyMemory<byte> inputBuffer,
            SslAuthenticationOptions sslAuthenticationOptions)
        {
            ProtocolToken token = default;

            try
            {
                SafeDeleteContext securityContext = context!;

                if ((null == context) || context.IsInvalid)
                {
                    bool useNetworkFramework = false;
                    switch (sslAuthenticationOptions.EnabledSslProtocols)
                    {
                        case SslProtocols.None:
                        case SslProtocols.Tls12:
                        case SslProtocols.Tls13:
                        case SslProtocols.Tls12 | SslProtocols.Tls13:
                            useNetworkFramework = sslAuthenticationOptions.CipherSuitesPolicy == null &&
                            sslAuthenticationOptions.ClientCertificates == null &&
                            sslAuthenticationOptions.CertificateContext == null &&
                            sslAuthenticationOptions.CertSelectionDelegate == null;
                            break;
                    }

                    if (sslAuthenticationOptions.IsClient && !sslAuthenticationOptions.IsServer &&
                        useNetworkFramework &&
                        SafeDeleteNwContext.IsNetworkFrameworkAvailable)
                    {
                        if (NetEventSource.Log.IsEnabled())
                            NetEventSource.Info(null, $"Using Network Framework (SafeDeleteNwContext) for TLS connection - Protocols: {sslAuthenticationOptions.EnabledSslProtocols}");
                        securityContext = new SafeDeleteNwContext(sslAuthenticationOptions);
                    }
                    else
                    {
                        if (NetEventSource.Log.IsEnabled())
                            NetEventSource.Info(null, $"Using SecureTransport (SafeDeleteSslContext) for TLS connection - Protocols: {sslAuthenticationOptions.EnabledSslProtocols}, IsClient: {sslAuthenticationOptions.IsClient}, NetworkFrameworkAvailable: {SafeDeleteNwContext.IsNetworkFrameworkAvailable}");
                        // Server side, we can't use Network Framework, so we use Secure Transport.
                        securityContext = new SafeDeleteSslContext(sslAuthenticationOptions);
                    }
                    context = securityContext;
                }

                if (inputBuffer.Length > 0)
                {
                    switch (securityContext)
                    {
                        case SafeDeleteSslContext sslContext:
                            sslContext.Write(inputBuffer.Span);
                            break;
                        case SafeDeleteNwContext nwContext:
                            await nwContext.WriteInboundWireDataAsync(inputBuffer).ConfigureAwait(false);
                            break;
                    }
                }

                switch (securityContext)
                {
                    case SafeDeleteSslContext secureTransportContext:
                        token.Status = PerformHandshake(secureTransportContext.SslContext);
                        break;

                    case SafeDeleteNwContext nwContext:
                        token.Status = await nwContext.PerformHandshakeAsync().ConfigureAwait(false);
                        break;
                }

                if (token.Status.ErrorCode == SecurityStatusPalErrorCode.CredentialsNeeded)
                {
                    // this should happen only for clients
                    Debug.Assert(sslAuthenticationOptions.IsClient);
                    return (token, context);
                }

                switch (securityContext)
                {
                    case SafeDeleteSslContext sslContext:
                        sslContext.ReadPendingWrites(ref token);
                        break;
                    case SafeDeleteNwContext nwContext:
                        // token = await nwContext.ReadPendingWritesAsync(token);
                        nwContext.ReadPendingWrites(ref token);
                        break;
                }
                return (token, context);
            }
            catch (Exception exc)
            {
                token.Status = new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, exc);
                return (token, context);
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
            return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
        }
#pragma warning restore IDE0060

        public static SecurityStatusPal ApplyShutdownToken(
            SafeDeleteContext securityContext)
        {
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
    }
}
