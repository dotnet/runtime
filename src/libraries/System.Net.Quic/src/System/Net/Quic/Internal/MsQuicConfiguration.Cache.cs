// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Security.Authentication;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Quic;

namespace System.Net.Quic;

internal static partial class MsQuicConfiguration
{
    private const int CheckExpiredModulo = 32;

    private const string DisableCacheEnvironmentVariable = "DOTNET_SYSTEM_NET_QUIC_DISABLE_CONFIGURATION_CACHE";
    private const string DisableCacheCtxSwitch = "System.Net.Quic.DisableConfigurationCache";

    private static volatile int s_configurationCacheEnabled = -1;
    internal static bool ConfigurationCacheEnabled
    {
        get
        {
            int enabled = s_configurationCacheEnabled;
            if (enabled != -1)
            {
                return enabled != 0;
            }

            // AppContext switch takes precedence
            if (AppContext.TryGetSwitch(DisableCacheCtxSwitch, out bool value))
            {
                s_configurationCacheEnabled = value ? 0 : 1;
            }
            else
            {
                // check environment variable
                s_configurationCacheEnabled = Environment.GetEnvironmentVariable(DisableCacheEnvironmentVariable) is string envVar && (envVar == "1" || envVar.Equals("true", StringComparison.OrdinalIgnoreCase)) ? 0 : 1;
            }

            return s_configurationCacheEnabled != 0;
        }
    }

    private static readonly ConcurrentDictionary<CacheKey, SafeMsQuicConfigurationHandle> s_configurationCache = new();

    private readonly struct CacheKey : IEquatable<CacheKey>
    {
        public readonly List<byte[]> CertificateThumbprints;
        public readonly QUIC_CREDENTIAL_FLAGS Flags;
        public readonly QUIC_SETTINGS Settings;
        public readonly List<SslApplicationProtocol> ApplicationProtocols;
        public readonly QUIC_ALLOWED_CIPHER_SUITE_FLAGS AllowedCipherSuites;

        public CacheKey(QUIC_CREDENTIAL_FLAGS flags, QUIC_SETTINGS settings, X509Certificate? certificate, ReadOnlyCollection<X509Certificate2>? intermediates, List<SslApplicationProtocol> alpnProtocols, QUIC_ALLOWED_CIPHER_SUITE_FLAGS allowedCipherSuites)
        {
            CertificateThumbprints = certificate == null ? new List<byte[]>() : new List<byte[]> { certificate.GetCertHash() };

            if (intermediates != null)
            {
                foreach (X509Certificate2 intermediate in intermediates)
                {
                    CertificateThumbprints.Add(intermediate.GetCertHash());
                }
            }

            Flags = flags;
            Settings = settings;
            // make defensive copy to prevent modification (the list comes from user code)
            ApplicationProtocols = new List<SslApplicationProtocol>(alpnProtocols);
            AllowedCipherSuites = allowedCipherSuites;
        }

        public override bool Equals(object? obj) => obj is CacheKey key && Equals(key);

        public bool Equals(CacheKey other)
        {
            if (CertificateThumbprints.Count != other.CertificateThumbprints.Count)
            {
                return false;
            }

            for (int i = 0; i < CertificateThumbprints.Count; i++)
            {
                if (!CertificateThumbprints[i].AsSpan().SequenceEqual(other.CertificateThumbprints[i]))
                {
                    return false;
                }
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

            foreach (var thumbprint in CertificateThumbprints)
            {
                hash.AddBytes(thumbprint);
            }

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

    private static SafeMsQuicConfigurationHandle GetCachedCredentialOrCreate(QUIC_CREDENTIAL_FLAGS flags, QUIC_SETTINGS settings, X509Certificate? certificate, ReadOnlyCollection<X509Certificate2>? intermediates, List<SslApplicationProtocol> alpnProtocols, QUIC_ALLOWED_CIPHER_SUITE_FLAGS allowedCipherSuites)
    {
        CacheKey key = new CacheKey(flags, settings, certificate, intermediates, alpnProtocols, allowedCipherSuites);

        SafeMsQuicConfigurationHandle? handle;

        if (s_configurationCache.TryGetValue(key, out handle) && handle.TryAddRentCount())
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Found cached MsQuicConfiguration: {handle}.");
            return handle;
        }

        //
        // if we get here, the handle is either not in the cache, or we lost the race between
        // TryAddRentCount on this thread and MarkForDispose on another thread doing cache cleanup.
        // In either case, we need to create a new handle.
        //

        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"MsQuicConfiguration not found in cache, creating new.");

        handle = CreateInternal(flags, settings, certificate, intermediates, alpnProtocols, allowedCipherSuites);
        handle.TryAddRentCount(); // we are the first renter

        SafeMsQuicConfigurationHandle cached;
        do
        {
            cached = s_configurationCache.GetOrAdd(key, handle);
        }
        //
        // If we get the same handle back, we successfully added it to the cache and we are done.
        // If we get a different handle back, we need to increase the rent count.
        // If we fail to add the rent count, then the existing/cached handle is in process of
        // being removed from the and we can try again, eventually either succeeding to add our
        // new handle or getting a fresh handle inserted by another thread meanwhile.
        //
        while (cached != handle && !cached.TryAddRentCount());

        if (cached != handle)
        {
            // we lost a race with another thread to insert new handle into the cache
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Discarding MsQuicConfiguration {handle} (preferring cached {cached}).");

            // First dispose decrements the rent count we added before attempting the cache insertion
            // and second closes the handle
            handle.Dispose();
            handle.Dispose();
            Debug.Assert(handle.IsClosed, "Handle should be closed.");

            return cached;
        }

        // we added a new handle, check if we need to cleanup
        if (s_configurationCache.Count % CheckExpiredModulo == 0)
        {
            // let only one thread perform cleanup at a time
            lock (s_configurationCache)
            {
                if (s_configurationCache.Count % CheckExpiredModulo == 0)
                {
                    CleanupCachedCredentials();
                }
            }
        }

        return handle;
    }

    private static void CleanupCachedCredentials()
    {
        KeyValuePair<CacheKey, SafeMsQuicConfigurationHandle>[] toRemoveAttempt = s_configurationCache.ToArray();

        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Cleaning up MsQuicConfiguration cache, current size: {toRemoveAttempt.Length}.");

        foreach ((CacheKey key, SafeMsQuicConfigurationHandle handle) in toRemoveAttempt)
        {
            if (!handle.TryMarkForDispose())
            {
                // handle in use
                continue;
            }

            // the handle is not in use and has been marked such that no new rents can be added.
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Removing cached MsQuicConfiguration {handle}.");
            bool removed = s_configurationCache.TryRemove(key, out _);
            Debug.Assert(removed, "Handle was marked for disposal, but was not found in the cache.");
            handle.Dispose();
            Debug.Assert(handle.IsClosed, "Handle should be closed.");
        }

        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Cleaning up MsQuicConfiguration cache, new size: {s_configurationCache.Count}.");
    }
}
