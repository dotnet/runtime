// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    // Implements SSL session caching mechanism based on a static table of SSL credentials.
    internal static class SslSessionsCache
    {
        private const int CheckExpiredModulo = 32;
        private static readonly ConcurrentDictionary<SslCredKey, SafeCredentialReference> s_cachedCreds =
            new ConcurrentDictionary<SslCredKey, SafeCredentialReference>();

        //
        // Uses certificate thumb-print comparison.
        //
        private readonly struct SslCredKey : IEquatable<SslCredKey>
        {
            private readonly byte[] _thumbPrint;
            private readonly int _allowedProtocols;
            private readonly EncryptionPolicy _encryptionPolicy;
            private readonly bool _isServerMode;
            private readonly bool _sendTrustList;
            private readonly bool _checkRevocation;

            //
            // SECURITY: X509Certificate.GetCertHash() is virtual hence before going here,
            //           the caller of this ctor has to ensure that a user cert object was inspected and
            //           optionally cloned.
            //
            internal SslCredKey(
                byte[]? thumbPrint,
                int allowedProtocols,
                bool isServerMode,
                EncryptionPolicy encryptionPolicy,
                bool sendTrustList,
                bool checkRevocation)
            {
                _thumbPrint = thumbPrint ?? Array.Empty<byte>();
                _allowedProtocols = allowedProtocols;
                _encryptionPolicy = encryptionPolicy;
                _isServerMode = isServerMode;
                _checkRevocation = checkRevocation;
                _sendTrustList = sendTrustList;
            }

            public override int GetHashCode()
            {
                int hashCode = 0;

                if (_thumbPrint.Length > 0)
                {
                    hashCode ^= _thumbPrint[0];
                    if (1 < _thumbPrint.Length)
                    {
                        hashCode ^= (_thumbPrint[1] << 8);
                    }

                    if (2 < _thumbPrint.Length)
                    {
                        hashCode ^= (_thumbPrint[2] << 16);
                    }

                    if (3 < _thumbPrint.Length)
                    {
                        hashCode ^= (_thumbPrint[3] << 24);
                    }
                }

                hashCode ^= _allowedProtocols;
                hashCode ^= (int)_encryptionPolicy;
                hashCode ^= _isServerMode ? 0x10000 : 0x20000;
                hashCode ^= _sendTrustList ? 0x40000 : 0x80000;
                hashCode ^= _checkRevocation ? 0x100000 : 0x200000;

                return hashCode;
            }

            public override bool Equals([NotNullWhen(true)] object? obj) =>
                obj is SslCredKey other && Equals(other);

            public bool Equals(SslCredKey other)
            {
                byte[] thumbPrint = _thumbPrint;
                byte[] otherThumbPrint = other._thumbPrint;

                return
                    thumbPrint.Length == otherThumbPrint.Length &&
                    _encryptionPolicy == other._encryptionPolicy &&
                    _allowedProtocols == other._allowedProtocols &&
                    _isServerMode == other._isServerMode &&
                    _sendTrustList == other._sendTrustList &&
                    _checkRevocation == other._checkRevocation &&
                    thumbPrint.AsSpan().SequenceEqual(otherThumbPrint);
            }
        }

        //
        // Returns null or previously cached cred handle.
        //
        // ATTN: The returned handle can be invalid, the callers of InitializeSecurityContext and AcceptSecurityContext
        // must be prepared to execute a back-out code if the call fails.
        //
        internal static SafeFreeCredentials? TryCachedCredential(
            byte[]? thumbPrint,
            SslProtocols sslProtocols,
            bool isServer,
            EncryptionPolicy encryptionPolicy,
            bool checkRevocation,
            bool sendTrustList = false)
        {
            if (s_cachedCreds.IsEmpty)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Not found, Current Cache Count = {s_cachedCreds.Count}");
                return null;
            }

            var key = new SslCredKey(thumbPrint, (int)sslProtocols, isServer, encryptionPolicy, sendTrustList, checkRevocation);

            //SafeCredentialReference? cached;
            SafeFreeCredentials? credentials = GetCachedCredential(key);
            if (credentials == null || credentials.IsClosed || credentials.IsInvalid || credentials.Expiry < DateTime.UtcNow)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Not found or invalid, Current Cache Count = {s_cachedCreds.Count}");
                return null;
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Found a cached Handle = {credentials}");

            return credentials;
        }

        private static SafeFreeCredentials? GetCachedCredential(SslCredKey key)
        {
            return s_cachedCreds.TryGetValue(key, out SafeCredentialReference? cached) ? cached.Target : null;
        }

        //
        // The app is calling this method after starting an SSL handshake.
        //
        // ATTN: The thumbPrint must be from inspected and possibly cloned user Cert object or we get a security hole in SslCredKey ctor.
        //
        internal static void CacheCredential(
            SafeFreeCredentials creds,
            byte[]? thumbPrint,
            SslProtocols sslProtocols,
            bool isServer,
            EncryptionPolicy encryptionPolicy,
            bool checkRevocation,
            bool sendTrustList = false)
        {
            Debug.Assert(creds != null, "creds == null");

            if (creds.IsInvalid)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Refused to cache an Invalid Handle {creds}, Current Cache Count = {s_cachedCreds.Count}");
                return;
            }

            SslCredKey key = new SslCredKey(thumbPrint, (int)sslProtocols, isServer, encryptionPolicy, sendTrustList, checkRevocation);

            SafeFreeCredentials? credentials = GetCachedCredential(key);

            DateTime utcNow = DateTime.UtcNow;
            if (credentials == null || credentials.IsClosed || credentials.IsInvalid || credentials.Expiry < utcNow)
            {
                lock (s_cachedCreds)
                {
                    credentials = GetCachedCredential(key);
                    if (credentials == null || credentials.IsClosed || credentials.IsInvalid || credentials.Expiry < utcNow)
                    {
                        SafeCredentialReference? cached = SafeCredentialReference.CreateReference(creds);

                        if (cached == null)
                        {
                            // Means the handle got closed in between, return it back and let caller deal with the issue.
                            return;
                        }

                        s_cachedCreds[key] = cached;
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Caching New Handle = {creds}, Current Cache Count = {s_cachedCreds.Count}");

                        ShrinkCredentialCache();

                    }
                    else
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"CacheCredential() (locked retry) Found already cached Handle = {credentials}");
                    }
                }
            }
            else
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"CacheCredential() Ignoring incoming handle = {creds} since found already cached Handle = {credentials}");
            }

            static void ShrinkCredentialCache()
            {

                //
                // A simplest way of preventing infinite cache grows.
                //
                // Security relief (DoS):
                //     A number of active creds is never greater than a number of _outstanding_
                //     security sessions, i.e. SSL connections.
                //     So we will try to shrink cache to the number of active creds once in a while.
                //
                //    We won't shrink cache in the case when NO new handles are coming to it.
                //
                if ((s_cachedCreds.Count % CheckExpiredModulo) == 0)
                {
                    KeyValuePair<SslCredKey, SafeCredentialReference>[] toRemoveAttempt = s_cachedCreds.ToArray();

                    for (int i = 0; i < toRemoveAttempt.Length; ++i)
                    {
                        SafeCredentialReference? cached = toRemoveAttempt[i].Value;
                        SafeFreeCredentials? creds = cached.Target;

                        if (creds == null)
                        {
                            s_cachedCreds.TryRemove(toRemoveAttempt[i].Key, out _);
                            continue;
                        }

                        cached.Dispose();
                        cached = SafeCredentialReference.CreateReference(creds);
                        if (cached != null)
                        {
                            s_cachedCreds[toRemoveAttempt[i].Key] = cached;
                        }
                        else
                        {
                            s_cachedCreds.TryRemove(toRemoveAttempt[i].Key, out _);
                        }

                    }
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Scavenged cache, New Cache Count = {s_cachedCreds.Count}");
                }
            }
        }
    }
}
