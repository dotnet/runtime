// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using static System.Net.Quic.Implementations.MsQuic.Internal.MsQuicNativeMethods;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal sealed class SafeMsQuicConfigurationHandle : SafeHandle
    {
        public override bool IsInvalid => handle == IntPtr.Zero;

        private SafeMsQuicConfigurationHandle() : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            MsQuicApi.Api.ConfigurationCloseDelegate(handle);
            return true;
        }

        public static unsafe SafeMsQuicConfigurationHandle Create(QuicClientConnectionOptions options)
        {
            // TODO: lots of ClientAuthenticationOptions are not yet supported by MsQuic.
            return Create(options, QUIC_CREDENTIAL_FLAGS.CLIENT, certificate: null, options.ClientAuthenticationOptions?.ApplicationProtocols);
        }

        public static unsafe SafeMsQuicConfigurationHandle Create(QuicListenerOptions options)
        {
            // TODO: lots of ServerAuthenticationOptions are not yet supported by MsQuic.
            return Create(options, QUIC_CREDENTIAL_FLAGS.NONE, options.ServerAuthenticationOptions?.ServerCertificate, options.ServerAuthenticationOptions?.ApplicationProtocols);
        }

        private static unsafe SafeMsQuicConfigurationHandle Create(QuicOptions options, QUIC_CREDENTIAL_FLAGS flags, X509Certificate? certificate, List<SslApplicationProtocol>? alpnProtocols)
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

            Debug.Assert(!MsQuicApi.Api.Registration.IsInvalid);

            var settings = new Settings
            {
                IsSetFlags =
                    (ulong)SettingsFlags.PeerBidiStreamCount
                    | (ulong)SettingsFlags.PeerUnidiStreamCount,
                PeerBidiStreamCount = (ushort)options.MaxBidirectionalStreams,
                PeerUnidiStreamCount = (ushort)options.MaxUnidirectionalStreams
            };

            if (options.IdleTimeout != Timeout.InfiniteTimeSpan)
            {
                if (options.IdleTimeout <= TimeSpan.Zero) throw new Exception("IdleTimeout must not be negative.");

                ulong ms = (ulong)options.IdleTimeout.Ticks / TimeSpan.TicksPerMillisecond;
                if (ms > (1ul << 62) - 1) throw new Exception("IdleTimeout is too large (max 2^62-1 milliseconds)");

                settings.IsSetFlags |= (ulong)SettingsFlags.IdleTimeoutMs;
                settings.IdleTimeoutMs = (ulong)options.IdleTimeout.TotalMilliseconds;
            }

            uint status;
            SafeMsQuicConfigurationHandle? configurationHandle;

            MemoryHandle[]? handles = null;
            QuicBuffer[]? buffers = null;
            try
            {
                MsQuicAlpnHelper.Prepare(alpnProtocols, out handles, out buffers);
                status = MsQuicApi.Api.ConfigurationOpenDelegate(MsQuicApi.Api.Registration, ref MemoryMarshal.GetReference(buffers.AsSpan()), (uint)alpnProtocols.Count, ref settings, (uint)sizeof(Settings), context: IntPtr.Zero, out configurationHandle);
            }
            finally
            {
                MsQuicAlpnHelper.Destroy(ref handles, ref buffers);
            }

            QuicExceptionHelpers.ThrowIfFailed(status, "ConfigurationOpen failed.");

            try
            {
                // TODO: find out what to do for OpenSSL here -- passing handle won't work, because
                // MsQuic has a private copy of OpenSSL so the SSL_CTX will be incompatible.

                CredentialConfig config = default;

                config.Flags = (uint)flags; // TODO: consider using LOAD_ASYNCHRONOUS with a callback.

                if (certificate != null)
                {
#if true
                    // If using stub TLS.
                    config.Type = (uint)QUIC_CREDENTIAL_TYPE.STUB_NULL;
#else
                    config.Type = (uint)QUIC_CREDENTIAL_TYPE.CONTEXT;
                    config.Certificate = certificate.Handle;
#endif
                }
                else
                {
                    config.Type = (uint)QUIC_CREDENTIAL_TYPE.NONE;
                }

                status = MsQuicApi.Api.ConfigurationLoadCredentialDelegate(configurationHandle, &config);
                QuicExceptionHelpers.ThrowIfFailed(status, "ConfigurationLoadCredential failed.");
            }
            catch
            {
                configurationHandle.Dispose();
                throw;
            }

            return configurationHandle;
        }
    }
}
