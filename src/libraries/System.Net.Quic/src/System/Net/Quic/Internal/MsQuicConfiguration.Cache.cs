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
    private const int CheckExpiredModulo = 32;

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

    private static MsQuicSafeHandle? TryGetCachedConfigurationHandle(CacheKey key)
    {
        if (s_configurationCache.TryGetValue(key, out MsQuicSafeHandle? handle))
        {
            try
            {
                //
                // This races with a potential cache cleanup, which may close the
                // handle before we claim it.
                //
                bool ignore = false;
                handle.DangerousAddRef(ref ignore);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Using cached MsQuicConfiguration {handle}.");
                return handle;
            }
            catch (ObjectDisposedException)
            {
                // we lost the race, behave as if the handle was not in the cache.
            }
        }

        return null;
    }

    private static void CacheConfigurationHandle(CacheKey key, ref MsQuicSafeHandle handle)
    {
        var cached = s_configurationCache.AddOrUpdate(
            key,
            (_, newHandle) =>
            {
                // The cache now holds the ownership of the handle.
                bool ignore = false;
                newHandle.DangerousAddRef(ref ignore);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Caching MsQuicConfiguration {newHandle}.");
                return newHandle;
            },
            (_, existingHandle, newHandle) =>
            {
                // another thread was faster in creating the configuration, check if we can
                // use the cached one
                bool ignore = false;
                try
                {
                    //
                    // This also races with the cache cleanup but should be rare since the
                    // configuration was just added to the cache and is likely still being used.
                    //
                    existingHandle.DangerousAddRef(ref ignore);
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Found existing MsQuicConfiguration {existingHandle} in cache.");
                    return existingHandle;
                }
                catch (ObjectDisposedException)
                {
                    // we lost the race with cleanup, the existing configuration handle is closed,
                    // keep the one we created.
                    newHandle.DangerousAddRef(ref ignore);
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Caching MsQuicConfiguration {newHandle}.");
                    return newHandle;
                }
            },
            handle);

        if (cached != handle)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Discarding MsQuicConfiguration {handle} (preferring cached {cached}).");
            handle.Dispose();
            handle = cached;
            return;
        }

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
    }

    private static void CleanupCachedCredentials()
    {
        KeyValuePair<CacheKey, MsQuicSafeHandle>[] toRemoveAttempt = s_configurationCache.ToArray();

        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Cleaning up MsQuicConfiguration cache, current size: {toRemoveAttempt.Length}.");

        foreach (KeyValuePair<CacheKey, MsQuicSafeHandle> kvp in toRemoveAttempt)
        {
            var handle = kvp.Value;

            //
            // We can't directly get the current refcount of the handle, we know it's at least 1,
            // so we decrement it and if it does not close, then it must be in use, so we increment
            // it back.
            //

            handle.DangerousRelease();
            bool inUse = false;
            try
            {
                if (!handle.IsClosed)
                {
                    // handle is in use, add the ref back.
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
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Removing cached MsQuicConfiguration {handle}.");
                s_configurationCache.TryRemove(kvp.Key, out _);
                // The handle is closed, but we did not call Dispose on it. Doing so would throw ODE,
                // suppress finalization to prevent Dispose from being called in a Finalizer thread.
                GC.SuppressFinalize(handle);
            }
        }

        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Cleaning up MsQuicConfiguration cache, new size: {s_configurationCache.Count}.");
    }
}
