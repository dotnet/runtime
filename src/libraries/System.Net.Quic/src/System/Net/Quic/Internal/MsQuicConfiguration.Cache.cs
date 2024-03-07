// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    private static readonly ConcurrentDictionary<CacheKey, MsQuicSafeHandle> s_configurationCache = new();

    private readonly struct CacheKey : IEquatable<CacheKey>
    {
        public readonly List<byte[]> CertificateThumbprints;
        public readonly QUIC_CREDENTIAL_FLAGS Flags;
        public readonly QUIC_SETTINGS Settings;
        public readonly List<SslApplicationProtocol> ApplicationProtocols;
        public readonly QUIC_ALLOWED_CIPHER_SUITE_FLAGS AllowedCipherSuites;

        public CacheKey(List<byte[]> certificateThumbprints, QUIC_CREDENTIAL_FLAGS flags, QUIC_SETTINGS settings, List<SslApplicationProtocol> applicationProtocols, QUIC_ALLOWED_CIPHER_SUITE_FLAGS allowedCipherSuites)
        {
            CertificateThumbprints = certificateThumbprints;
            Flags = flags;
            Settings = settings;
            ApplicationProtocols = applicationProtocols;
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
                hash.Add(thumbprint);
            }

            hash.Add(Flags);
            hash.Add(Settings);

            foreach (var protocol in ApplicationProtocols)
            {
                hash.Add(protocol);
            }

            hash.Add(AllowedCipherSuites);

            return hash.ToHashCode();
        }
    }

    private static MsQuicSafeHandle? TryGetCachedConfigurationHandle(CacheKey key)
    {
        if (s_configurationCache.TryGetValue(key, out MsQuicSafeHandle? handle))
        {
            try
            {
                //
                // This races with potential cache cleanup, which may close the
                // handle before we claim ownership.
                //
                bool ignore = false;
                handle.DangerousAddRef(ref ignore);
                return handle;
            }
            catch (ObjectDisposedException)
            {
                // we lost the race, behave as if the handle was not in the cache in the first place
            }
        }

        return null;
    }

    private static void CacheConfigurationHandle(CacheKey key, MsQuicSafeHandle handle)
    {
        s_configurationCache.AddOrUpdate(
            key,
            (k, h) =>
            {
                // add-ref the handle to signify that it is statically cached
                bool ignore = false;
                h.DangerousAddRef(ref ignore);
                return h;
            },
            (k, existing, h) =>
            {
                // there already is an existing handle for this key, perhaps we lost a race,
                // make this a no-op
                return existing;
            },
            handle);

        if (s_configurationCache.Count % 32 == 0)
        {
            // let only one thread perform cleanup at a time
            lock (s_configurationCache)
            {
                if (s_configurationCache.Count % 32 == 0)
                {
                    CleanupCachedCredentials();
                }
            }
        }
    }

    private static void CleanupCachedCredentials()
    {
        KeyValuePair<CacheKey, MsQuicSafeHandle>[] toRemoveAttempt = s_configurationCache.ToArray();

        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Cleaning up MsQuicConfiguration cache, current size: {toRemoveAttempt.Length}");

        foreach (KeyValuePair<CacheKey, MsQuicSafeHandle> kvp in toRemoveAttempt)
        {
            var handle = kvp.Value;

            //
            // Unfortunately, we can't get the current refcount of the handle,
            // so we try to release and see if the handle closes.
            //
            handle.DangerousRelease();
            bool inUse = false;
            try
            {
                if (!handle.IsClosed)
                {
                    // This add-ref races with QuicConnection.Dispose();
                    handle.DangerousAddRef(ref inUse);
                }
            }
            catch (ObjectDisposedException)
            {
                // we lost the race, the handle is closed, we can proceed to remove it from
                // the cache.
            }

            if (!inUse)
            {
                s_configurationCache.TryRemove(kvp.Key, out _);
            }
        }

        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Cleaning up MsQuicConfiguration cache, new size: {s_configurationCache.Count}");
    }
}
