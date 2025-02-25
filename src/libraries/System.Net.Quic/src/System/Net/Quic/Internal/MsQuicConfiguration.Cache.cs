// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Security.Authentication;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Quic;

namespace System.Net.Quic;

internal static partial class MsQuicConfiguration
{
    private const string DisableCacheEnvironmentVariable = "DOTNET_SYSTEM_NET_QUIC_DISABLE_CONFIGURATION_CACHE";
    private const string DisableCacheCtxSwitch = "System.Net.Quic.DisableConfigurationCache";

    internal static bool ConfigurationCacheEnabled { get; } = GetConfigurationCacheEnabled();

    private static bool GetConfigurationCacheEnabled()
    {
        // AppContext switch takes precedence
        if (AppContext.TryGetSwitch(DisableCacheCtxSwitch, out bool value))
        {
            return !value;
        }
        // check environment variable second
        else if (Environment.GetEnvironmentVariable(DisableCacheEnvironmentVariable) is string envVar)
        {
            return !(envVar == "1" || envVar.Equals("true", StringComparison.OrdinalIgnoreCase));
        }

        // enabled by default
        return true;
    }

    private static readonly MsQuicConfigurationCache s_configurationCache = new MsQuicConfigurationCache();

    private sealed class MsQuicConfigurationCache : SafeHandleCache<CacheKey, MsQuicConfigurationSafeHandle>
    {
    }

    private readonly struct CacheKey : IEquatable<CacheKey>
    {
        private const int ThumbprintSize = 64; // SHA512 size

        public readonly ReadOnlyMemory<byte> CertificateThumbprints;
        public readonly QUIC_CREDENTIAL_FLAGS Flags;
        public readonly QUIC_SETTINGS Settings;
        public readonly List<SslApplicationProtocol> ApplicationProtocols;
        public readonly QUIC_ALLOWED_CIPHER_SUITE_FLAGS AllowedCipherSuites;

        public CacheKey(QUIC_SETTINGS settings, QUIC_CREDENTIAL_FLAGS flags, X509Certificate? certificate, ReadOnlyCollection<X509Certificate2>? intermediates, List<SslApplicationProtocol> alpnProtocols, QUIC_ALLOWED_CIPHER_SUITE_FLAGS allowedCipherSuites)
        {
            int certCount = certificate == null ? 0 : 1;
            certCount += intermediates?.Count ?? 0;
            byte[] certificateThumbprints = new byte[certCount * ThumbprintSize];

            certCount = 0;
            if (certificate != null)
            {
                bool success = certificate.TryGetCertHash(HashAlgorithmName.SHA512, certificateThumbprints.AsSpan(0, ThumbprintSize), out _);
                Debug.Assert(success);
                certCount++;
            }

            if (intermediates != null)
            {
                foreach (X509Certificate2 intermediate in intermediates)
                {
                    bool success = intermediate.TryGetCertHash(HashAlgorithmName.SHA512, certificateThumbprints.AsSpan(certCount * ThumbprintSize, ThumbprintSize), out _);
                    Debug.Assert(success);
                    certCount++;
                }
            }

            CertificateThumbprints = certificateThumbprints;

            Flags = flags;
            Settings = settings;
            // make defensive copy to prevent modification (the list comes from user code)
            ApplicationProtocols = new List<SslApplicationProtocol>(alpnProtocols);
            AllowedCipherSuites = allowedCipherSuites;
        }

        public override bool Equals(object? obj) => obj is CacheKey key && Equals(key);

        public bool Equals(CacheKey other)
        {
            if (!CertificateThumbprints.Span.SequenceEqual(other.CertificateThumbprints.Span))
            {
                return false;
            }

            if (ApplicationProtocols.Count != other.ApplicationProtocols.Count)
            {
                return false;
            }

            for (int i = 0; i < ApplicationProtocols.Count; i++)
            {
                if (ApplicationProtocols[i] != other.ApplicationProtocols[i])
                {
                    return false;
                }
            }

            return
                Flags == other.Flags &&
                Settings.Equals(other.Settings) &&
                AllowedCipherSuites == other.AllowedCipherSuites;
        }

        public override int GetHashCode()
        {
            HashCode hash = default;

            hash.AddBytes(CertificateThumbprints.Span);
            hash.Add(Flags);
            hash.Add(Settings);

            foreach (var protocol in ApplicationProtocols)
            {
                hash.AddBytes(protocol.Protocol.Span);
            }

            hash.Add(AllowedCipherSuites);

            return hash.ToHashCode();
        }
    }

    private static MsQuicConfigurationSafeHandle GetCachedCredentialOrCreate(QUIC_SETTINGS settings, QUIC_CREDENTIAL_FLAGS flags, X509Certificate? certificate, ReadOnlyCollection<X509Certificate2>? intermediates, List<SslApplicationProtocol> alpnProtocols, QUIC_ALLOWED_CIPHER_SUITE_FLAGS allowedCipherSuites)
    {
        CacheKey key = new CacheKey(settings, flags, certificate, intermediates, alpnProtocols, allowedCipherSuites);

        return s_configurationCache.GetOrCreate(key, static (args) =>
        {
            var (settings, flags, certificate, intermediates, alpnProtocols, allowedCipherSuites) = args;
            return CreateInternal(settings, flags, certificate, intermediates, alpnProtocols, allowedCipherSuites);
        }, (settings, flags, certificate, intermediates, alpnProtocols, allowedCipherSuites));
    }
}
