// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Authentication;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Quic;

namespace System.Net.Quic;

internal static class MsQuicConfiguration
{
    private static bool HasPrivateKey(this X509Certificate certificate)
        => certificate is X509Certificate2 certificate2 && certificate2.Handle != IntPtr.Zero && certificate2.HasPrivateKey;

    public static MsQuicSafeHandle Create(QuicClientConnectionOptions options)
    {
        SslClientAuthenticationOptions authenticationOptions = options.ClientAuthenticationOptions;

        QUIC_CREDENTIAL_FLAGS flags = QUIC_CREDENTIAL_FLAGS.NONE;
        flags |= QUIC_CREDENTIAL_FLAGS.CLIENT;
        flags |= QUIC_CREDENTIAL_FLAGS.INDICATE_CERTIFICATE_RECEIVED;
        flags |= QUIC_CREDENTIAL_FLAGS.NO_CERTIFICATE_VALIDATION;
        if (MsQuicApi.UsesSChannelBackend)
        {
            flags |= QUIC_CREDENTIAL_FLAGS.USE_SUPPLIED_CREDENTIALS;
        }

        // Find the first certificate with private key, either from selection callback or from a provided collection.
        X509Certificate? certificate = null;
        if (authenticationOptions.LocalCertificateSelectionCallback != null)
        {
            X509Certificate selectedCertificate = authenticationOptions.LocalCertificateSelectionCallback(
                options,
                authenticationOptions.TargetHost ?? string.Empty,
                authenticationOptions.ClientCertificates ?? new X509CertificateCollection(),
                null,
                Array.Empty<string>());
            if (selectedCertificate.HasPrivateKey())
            {
                certificate = selectedCertificate;
            }
            else
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Info(options, $"'{certificate}' not selected because it doesn't have a private key.");
                }
            }
        }
        else if (authenticationOptions.ClientCertificates != null)
        {
            foreach (X509Certificate clientCertificate in authenticationOptions.ClientCertificates)
            {
                if (clientCertificate.HasPrivateKey())
                {
                    certificate = clientCertificate;
                    break;
                }
                else
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        NetEventSource.Info(options, $"'{certificate}' not selected because it doesn't have a private key.");
                    }
                }
            }
        }

        return Create(options, flags, certificate, null, authenticationOptions.ApplicationProtocols, authenticationOptions.CipherSuitesPolicy, authenticationOptions.EncryptionPolicy);
    }

    public static MsQuicSafeHandle Create(QuicServerConnectionOptions options, string? targetHost)
    {
        SslServerAuthenticationOptions authenticationOptions = options.ServerAuthenticationOptions;

        QUIC_CREDENTIAL_FLAGS flags = QUIC_CREDENTIAL_FLAGS.NONE;
        if (authenticationOptions.ClientCertificateRequired)
        {
            flags |= QUIC_CREDENTIAL_FLAGS.REQUIRE_CLIENT_AUTHENTICATION;
            flags |= QUIC_CREDENTIAL_FLAGS.INDICATE_CERTIFICATE_RECEIVED;
            flags |= QUIC_CREDENTIAL_FLAGS.NO_CERTIFICATE_VALIDATION;
        }

        X509Certificate? certificate = null;
        ReadOnlyCollection<X509Certificate2>? intermediates = default;
        if (authenticationOptions.ServerCertificateContext is not null)
        {
            certificate = authenticationOptions.ServerCertificateContext.TargetCertificate;
            intermediates = authenticationOptions.ServerCertificateContext.IntermediateCertificates;
        }

        certificate ??= authenticationOptions.ServerCertificate ?? authenticationOptions.ServerCertificateSelectionCallback?.Invoke(authenticationOptions, targetHost);
        if (certificate is null)
        {
            throw new ArgumentException(SR.Format(SR.net_quic_not_null_ceritifcate, nameof(SslServerAuthenticationOptions.ServerCertificate), nameof(SslServerAuthenticationOptions.ServerCertificateContext), nameof(SslServerAuthenticationOptions.ServerCertificateSelectionCallback)), nameof(options));
        }

        return Create(options, flags, certificate, intermediates, authenticationOptions.ApplicationProtocols, authenticationOptions.CipherSuitesPolicy, authenticationOptions.EncryptionPolicy);
    }

    private static unsafe MsQuicSafeHandle Create(QuicConnectionOptions options, QUIC_CREDENTIAL_FLAGS flags, X509Certificate? certificate, ReadOnlyCollection<X509Certificate2>? intermediates, List<SslApplicationProtocol>? alpnProtocols, CipherSuitesPolicy? cipherSuitesPolicy, EncryptionPolicy encryptionPolicy)
    {
        // Validate options and SSL parameters.
        if (alpnProtocols is null || alpnProtocols.Count <= 0)
        {
            throw new ArgumentException(SR.Format(SR.net_quic_not_null_not_empty_connection, nameof(SslApplicationProtocol)), nameof(options));
        }

#pragma warning disable SYSLIB0040 // NoEncryption and AllowNoEncryption are obsolete
        if (encryptionPolicy == EncryptionPolicy.NoEncryption)
        {
            throw new PlatformNotSupportedException(SR.Format(SR.net_quic_ssl_option, encryptionPolicy));
        }
#pragma warning restore SYSLIB0040

        QUIC_SETTINGS settings = default(QUIC_SETTINGS);

        settings.IsSet.PeerUnidiStreamCount = 1;
        settings.PeerUnidiStreamCount = (ushort)options.MaxInboundUnidirectionalStreams;

        settings.IsSet.PeerBidiStreamCount = 1;
        settings.PeerBidiStreamCount = (ushort)options.MaxInboundBidirectionalStreams;

        if (options.IdleTimeout != TimeSpan.Zero)
        {
            settings.IsSet.IdleTimeoutMs = 1;
            settings.IdleTimeoutMs = options.IdleTimeout != Timeout.InfiniteTimeSpan
                ? (ulong)options.IdleTimeout.TotalMilliseconds
                : 0; // 0 disables the timeout
        }

        if (options.KeepAliveInterval != TimeSpan.Zero)
        {
            settings.IsSet.KeepAliveIntervalMs = 1;
            settings.KeepAliveIntervalMs = options.KeepAliveInterval != Timeout.InfiniteTimeSpan
                ? (uint)options.KeepAliveInterval.TotalMilliseconds
                : 0; // 0 disables the keepalive
        }

        settings.IsSet.ConnFlowControlWindow = 1;
        settings.ConnFlowControlWindow = (uint)(options._initialRecieveWindowSizes?.Connection ?? QuicDefaults.DefaultConnectionMaxData);

        settings.IsSet.StreamRecvWindowBidiLocalDefault = 1;
        settings.StreamRecvWindowBidiLocalDefault = (uint)(options._initialRecieveWindowSizes?.LocallyInitiatedBidirectionalStream ?? QuicDefaults.DefaultStreamMaxData);

        settings.IsSet.StreamRecvWindowBidiRemoteDefault = 1;
        settings.StreamRecvWindowBidiRemoteDefault = (uint)(options._initialRecieveWindowSizes?.RemotelyInitiatedBidirectionalStream ?? QuicDefaults.DefaultStreamMaxData);

        settings.IsSet.StreamRecvWindowUnidiDefault = 1;
        settings.StreamRecvWindowUnidiDefault = (uint)(options._initialRecieveWindowSizes?.UnidirectionalStream ?? QuicDefaults.DefaultStreamMaxData);

        if (options.HandshakeTimeout != TimeSpan.Zero)
        {
            settings.IsSet.HandshakeIdleTimeoutMs = 1;
            settings.HandshakeIdleTimeoutMs = options.HandshakeTimeout != Timeout.InfiniteTimeSpan
                    ? (ulong)options.HandshakeTimeout.TotalMilliseconds
                    : 0; // 0 disables the timeout
        }

        QUIC_HANDLE* handle;

        using MsQuicBuffers msquicBuffers = new MsQuicBuffers();
        msquicBuffers.Initialize(alpnProtocols, alpnProtocol => alpnProtocol.Protocol);
        ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.ConfigurationOpen(
            MsQuicApi.Api.Registration,
            msquicBuffers.Buffers,
            (uint)alpnProtocols.Count,
            &settings,
            (uint)sizeof(QUIC_SETTINGS),
            (void*)IntPtr.Zero,
            &handle),
            "ConfigurationOpen failed");
        MsQuicSafeHandle configurationHandle = new MsQuicSafeHandle(handle, SafeHandleType.Configuration);

        try
        {
            QUIC_CREDENTIAL_CONFIG config = new QUIC_CREDENTIAL_CONFIG { Flags = flags };
            config.Flags |= (MsQuicApi.UsesSChannelBackend ? QUIC_CREDENTIAL_FLAGS.NONE : QUIC_CREDENTIAL_FLAGS.USE_PORTABLE_CERTIFICATES);

            if (cipherSuitesPolicy != null)
            {
                config.Flags |= QUIC_CREDENTIAL_FLAGS.SET_ALLOWED_CIPHER_SUITES;
                config.AllowedCipherSuites = CipherSuitePolicyToFlags(cipherSuitesPolicy);
            }

            int status;
            if (certificate is null)
            {
                config.Type = QUIC_CREDENTIAL_TYPE.NONE;
                status = MsQuicApi.Api.ConfigurationLoadCredential(configurationHandle, &config);
            }
            else if (MsQuicApi.UsesSChannelBackend)
            {
                config.Type = QUIC_CREDENTIAL_TYPE.CERTIFICATE_CONTEXT;
                config.CertificateContext = (void*)certificate.Handle;
                status = MsQuicApi.Api.ConfigurationLoadCredential(configurationHandle, &config);
            }
            else
            {
                config.Type = QUIC_CREDENTIAL_TYPE.CERTIFICATE_PKCS12;

                byte[] certificateData;

                if (intermediates != null && intermediates.Count > 0)
                {
                    X509Certificate2Collection collection = new X509Certificate2Collection();
                    collection.Add(certificate);
                    foreach (X509Certificate2 intermediate in intermediates)
                    {
                        collection.Add(intermediate);
                    }
                    certificateData = collection.Export(X509ContentType.Pkcs12)!;
                }
                else
                {
                    certificateData = certificate.Export(X509ContentType.Pkcs12);
                }

                fixed (byte* ptr = certificateData)
                {
                    QUIC_CERTIFICATE_PKCS12 pkcs12Certificate = new QUIC_CERTIFICATE_PKCS12
                    {
                        Asn1Blob = ptr,
                        Asn1BlobLength = (uint)certificateData.Length,
                        PrivateKeyPassword = (sbyte*)IntPtr.Zero
                    };
                    config.CertificatePkcs12 = &pkcs12Certificate;
                    status = MsQuicApi.Api.ConfigurationLoadCredential(configurationHandle, &config);
                }
            }

#if TARGET_WINDOWS
            if ((Interop.SECURITY_STATUS)status == Interop.SECURITY_STATUS.AlgorithmMismatch &&
               ((flags & QUIC_CREDENTIAL_FLAGS.CLIENT) == 0 ? MsQuicApi.Tls13ServerMayBeDisabled : MsQuicApi.Tls13ClientMayBeDisabled))
            {
                ThrowHelper.ThrowIfMsQuicError(status, SR.net_quic_tls_version_notsupported);
            }

            if (status == MsQuic.QUIC_STATUS_CERT_NO_CERT && certificate != null && certificate.HasPrivateKey())
            {
                using Microsoft.Win32.SafeHandles.SafeCertContextHandle safeCertContextHandle = Interop.Crypt32.CertDuplicateCertificateContext(certificate.Handle);
                if (safeCertContextHandle.HasEphemeralPrivateKey)
                {
                    throw new AuthenticationException(SR.net_auth_ephemeral);
                }
            }
#endif

            ThrowHelper.ThrowIfMsQuicError(status, "ConfigurationLoadCredential failed");
        }
        catch
        {
            configurationHandle.Dispose();
            throw;
        }

        return configurationHandle;
    }

    private static QUIC_ALLOWED_CIPHER_SUITE_FLAGS CipherSuitePolicyToFlags(CipherSuitesPolicy cipherSuitesPolicy)
    {
        QUIC_ALLOWED_CIPHER_SUITE_FLAGS flags = QUIC_ALLOWED_CIPHER_SUITE_FLAGS.NONE;
#pragma warning disable CA1416  // not supported on all platforms
        foreach (TlsCipherSuite cipher in cipherSuitesPolicy.AllowedCipherSuites)
        {
            switch (cipher)
            {
                case TlsCipherSuite.TLS_AES_128_GCM_SHA256:
                    flags |= QUIC_ALLOWED_CIPHER_SUITE_FLAGS.AES_128_GCM_SHA256;
                    break;
                case TlsCipherSuite.TLS_AES_256_GCM_SHA384:
                    flags |= QUIC_ALLOWED_CIPHER_SUITE_FLAGS.AES_256_GCM_SHA384;
                    break;
                case TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256:
                    flags |= QUIC_ALLOWED_CIPHER_SUITE_FLAGS.CHACHA20_POLY1305_SHA256;
                    break;
                case TlsCipherSuite.TLS_AES_128_CCM_SHA256: // not supported by MsQuic (yet?), but QUIC RFC allows it so we ignore it.
                default:
                    // ignore
                    break;
            }
#pragma warning restore
        }

        if (flags == QUIC_ALLOWED_CIPHER_SUITE_FLAGS.NONE)
        {
            throw new ArgumentException(SR.net_quic_empty_cipher_suite, nameof(SslClientAuthenticationOptions.CipherSuitesPolicy));
        }

        return flags;
    }
}
