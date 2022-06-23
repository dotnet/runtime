// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Quic;
using static Microsoft.Quic.MsQuic;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal sealed class SafeMsQuicConfigurationHandle : MsQuicSafeHandle
    {
        public unsafe SafeMsQuicConfigurationHandle(QUIC_HANDLE* handle)
            : base(handle, ptr => MsQuicApi.Api.ApiTable->ConfigurationClose((QUIC_HANDLE*)ptr), SafeHandleType.Configuration)
        { }

        // TODO: consider moving the static code from here to keep all the handle classes small and simple.
        public static SafeMsQuicConfigurationHandle Create(QuicClientConnectionOptions options)
        {
            X509Certificate? certificate = null;

            if (options.ClientAuthenticationOptions != null)
            {
#pragma warning disable SYSLIB0040 // NoEncryption and AllowNoEncryption are obsolete
                if (options.ClientAuthenticationOptions.EncryptionPolicy == EncryptionPolicy.NoEncryption)
                {
                    throw new PlatformNotSupportedException(SR.Format(SR.net_quic_ssl_option, nameof(options.ClientAuthenticationOptions.EncryptionPolicy)));
                }
#pragma warning restore SYSLIB0040

                if (options.ClientAuthenticationOptions.ClientCertificates != null)
                {
                    foreach (var cert in options.ClientAuthenticationOptions.ClientCertificates)
                    {
                        try
                        {
                            if (((X509Certificate2)cert).HasPrivateKey)
                            {
                                // Pick first certificate with private key.
                                certificate = cert;
                                break;
                            }
                        }
                        catch { }
                    }
                }
            }

            QUIC_CREDENTIAL_FLAGS flags = QUIC_CREDENTIAL_FLAGS.CLIENT;
            if (OperatingSystem.IsWindows())
            {
                flags |= QUIC_CREDENTIAL_FLAGS.USE_SUPPLIED_CREDENTIALS;
            }
            return Create(options, flags, certificate: certificate, certificateContext: null, options.ClientAuthenticationOptions?.ApplicationProtocols, options.ClientAuthenticationOptions?.CipherSuitesPolicy);
        }

        public static SafeMsQuicConfigurationHandle Create(QuicOptions options, SslServerAuthenticationOptions? serverAuthenticationOptions, string? targetHost = null)
        {
            QUIC_CREDENTIAL_FLAGS flags = QUIC_CREDENTIAL_FLAGS.NONE;
            X509Certificate? certificate = serverAuthenticationOptions?.ServerCertificate;

            if (serverAuthenticationOptions != null)
            {
#pragma warning disable SYSLIB0040 // NoEncryption and AllowNoEncryption are obsolete
                if (serverAuthenticationOptions.EncryptionPolicy == EncryptionPolicy.NoEncryption)
                {
                    throw new PlatformNotSupportedException(SR.Format(SR.net_quic_ssl_option, nameof(serverAuthenticationOptions.EncryptionPolicy)));
                }
#pragma warning restore SYSLIB0040

                if (serverAuthenticationOptions.ClientCertificateRequired)
                {
                    flags |= QUIC_CREDENTIAL_FLAGS.REQUIRE_CLIENT_AUTHENTICATION | QUIC_CREDENTIAL_FLAGS.INDICATE_CERTIFICATE_RECEIVED | QUIC_CREDENTIAL_FLAGS.NO_CERTIFICATE_VALIDATION;
                }

                if (certificate == null && serverAuthenticationOptions?.ServerCertificateSelectionCallback != null && targetHost != null)
                {
                    certificate = serverAuthenticationOptions.ServerCertificateSelectionCallback(options, targetHost);
                }
            }

            return Create(options, flags, certificate, serverAuthenticationOptions?.ServerCertificateContext, serverAuthenticationOptions?.ApplicationProtocols, serverAuthenticationOptions?.CipherSuitesPolicy);
        }

        // TODO: this is called from MsQuicListener and when it fails it wreaks havoc in MsQuicListener finalizer.
        //       Consider moving bigger logic like this outside of constructor call chains.
        private static unsafe SafeMsQuicConfigurationHandle Create(QuicOptions options, QUIC_CREDENTIAL_FLAGS flags, X509Certificate? certificate, SslStreamCertificateContext? certificateContext, List<SslApplicationProtocol>? alpnProtocols, CipherSuitesPolicy? cipherSuitesPolicy)
        {
            // TODO: some of these checks should be done by the QuicOptions type.
            if (alpnProtocols == null || alpnProtocols.Count == 0)
            {
                throw new Exception("At least one SslApplicationProtocol value must be present in SslClientAuthenticationOptions or SslServerAuthenticationOptions.");
            }

            if (options.MaxBidirectionalStreams > ushort.MaxValue)
            {
                throw new Exception("MaxBidirectionalStreams overflow.");
            }

            if (options.MaxBidirectionalStreams > ushort.MaxValue)
            {
                throw new Exception("MaxBidirectionalStreams overflow.");
            }

            if ((flags & QUIC_CREDENTIAL_FLAGS.CLIENT) == 0)
            {
                if (certificate == null && certificateContext == null)
                {
                    throw new Exception("Server must provide certificate");
                }
            }
            else
            {
                flags |= QUIC_CREDENTIAL_FLAGS.INDICATE_CERTIFICATE_RECEIVED | QUIC_CREDENTIAL_FLAGS.NO_CERTIFICATE_VALIDATION;
            }

            if (!OperatingSystem.IsWindows())
            {
                // Use certificate handles on Windows, fall-back to ASN1 otherwise.
                flags |= QUIC_CREDENTIAL_FLAGS.USE_PORTABLE_CERTIFICATES;
            }

            Debug.Assert(!MsQuicApi.Api.Registration.IsInvalid);

            QUIC_SETTINGS settings = default(QUIC_SETTINGS);
            settings.IsSet.PeerUnidiStreamCount = 1;
            settings.PeerUnidiStreamCount = (ushort)options.MaxUnidirectionalStreams;
            settings.IsSet.PeerBidiStreamCount = 1;
            settings.PeerBidiStreamCount = (ushort)options.MaxBidirectionalStreams;

            settings.IsSet.IdleTimeoutMs = 1;
            if (options.IdleTimeout != Timeout.InfiniteTimeSpan)
            {
                if (options.IdleTimeout <= TimeSpan.Zero) throw new Exception("IdleTimeout must not be negative.");
                settings.IdleTimeoutMs = (ulong)options.IdleTimeout.TotalMilliseconds;
            }
            else
            {
                settings.IdleTimeoutMs = 0;
            }

            SafeMsQuicConfigurationHandle configurationHandle;
            X509Certificate2[]? intermediates = null;

            QUIC_HANDLE* handle;
            using var msquicBuffers = new MsQuicBuffers();
            msquicBuffers.Initialize(alpnProtocols, alpnProtocol => alpnProtocol.Protocol);
            ThrowIfFailure(MsQuicApi.Api.ApiTable->ConfigurationOpen(
                MsQuicApi.Api.Registration.QuicHandle,
                msquicBuffers.Buffers,
                (uint)alpnProtocols.Count,
                &settings,
                (uint)sizeof(QUIC_SETTINGS),
                (void*)IntPtr.Zero,
                &handle), "ConfigurationOpen failed");
            configurationHandle = new SafeMsQuicConfigurationHandle(handle);

            try
            {
                QUIC_CREDENTIAL_CONFIG config = default;
                config.Flags = flags; // TODO: consider using LOAD_ASYNCHRONOUS with a callback.

                if (cipherSuitesPolicy != null)
                {
                    config.Flags |= QUIC_CREDENTIAL_FLAGS.SET_ALLOWED_CIPHER_SUITES;
                    config.AllowedCipherSuites = CipherSuitePolicyToFlags(cipherSuitesPolicy);
                }

                if (certificateContext != null)
                {
                    certificate = certificateContext.Certificate;
                    intermediates = certificateContext.IntermediateCertificates;
                }

                int status;
                if (certificate != null)
                {
                    if (OperatingSystem.IsWindows())
                    {
                        config.Type = QUIC_CREDENTIAL_TYPE.CERTIFICATE_CONTEXT;
                        config.CertificateContext = (void*)certificate.Handle;
                        status = MsQuicApi.Api.ApiTable->ConfigurationLoadCredential(configurationHandle.QuicHandle, &config);
                    }
                    else
                    {
                        byte[] asn1;

                        if (intermediates?.Length > 0)
                        {
                            X509Certificate2Collection collection = new X509Certificate2Collection();
                            collection.Add(certificate);
                            for (int i = 0; i < intermediates?.Length; i++)
                            {
                                collection.Add(intermediates[i]);
                            }

                            asn1 = collection.Export(X509ContentType.Pkcs12)!;
                        }
                        else
                        {
                            asn1 = certificate.Export(X509ContentType.Pkcs12);
                        }

                        fixed (byte* ptr = asn1)
                        {
                            QUIC_CERTIFICATE_PKCS12 pkcs12Config = new QUIC_CERTIFICATE_PKCS12
                            {
                                Asn1Blob = ptr,
                                Asn1BlobLength = (uint)asn1.Length,
                                PrivateKeyPassword = (sbyte*)IntPtr.Zero
                            };

                            config.Type = QUIC_CREDENTIAL_TYPE.CERTIFICATE_PKCS12;
                            config.CertificatePkcs12 = &pkcs12Config;
                            status = MsQuicApi.Api.ApiTable->ConfigurationLoadCredential(configurationHandle.QuicHandle, &config);
                        }
                    }
                }
                else
                {
                    config.Type = QUIC_CREDENTIAL_TYPE.NONE;
                    status = MsQuicApi.Api.ApiTable->ConfigurationLoadCredential(configurationHandle.QuicHandle, &config);
                }

#if TARGET_WINDOWS
                if ((Interop.SECURITY_STATUS)status == Interop.SECURITY_STATUS.AlgorithmMismatch && MsQuicApi.Tls13MayBeDisabled)
                {
                    throw new MsQuicException(status, SR.net_ssl_app_protocols_invalid);
                }
#endif

                ThrowIfFailure(status, "ConfigurationLoadCredential failed");
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
            }

            if (flags == QUIC_ALLOWED_CIPHER_SUITE_FLAGS.NONE)
            {
                throw new ArgumentException(SR.net_quic_empty_cipher_suite, nameof(SslClientAuthenticationOptions.CipherSuitesPolicy));
            }

            return flags;
        }
    }
}
