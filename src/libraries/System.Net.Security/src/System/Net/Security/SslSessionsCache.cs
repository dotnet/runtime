// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Authentication;

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

            //
            // SECURITY: X509Certificate.GetCertHash() is virtual hence before going here,
            //           the caller of this ctor has to ensure that a user cert object was inspected and
            //           optionally cloned.
            //
            internal SslCredKey(byte[]? thumbPrint, int allowedProtocols, bool isServerMode, EncryptionPolicy encryptionPolicy)
            {
                _thumbPrint = thumbPrint ?? Array.Empty<byte>();
                _allowedProtocols = allowedProtocols;
                _encryptionPolicy = encryptionPolicy;
                _isServerMode = isServerMode;
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

                return hashCode;
            }

            public override bool Equals(object? obj) => (obj is SslCredKey && Equals((SslCredKey)obj));

            public bool Equals(SslCredKey other)
            {
                byte[] thumbPrint = _thumbPrint;
                byte[] otherThumbPrint = other._thumbPrint;

                if (thumbPrint.Length != otherThumbPrint.Length)
                {
                    return false;
                }

                if (_encryptionPolicy != other._encryptionPolicy)
                {
                    return false;
                }

                if (_allowedProtocols != other._allowedProtocols)
                {
                    return false;
                }

                if (_isServerMode != other._isServerMode)
                {
                    return false;
                }

                for (int i = 0; i < thumbPrint.Length; ++i)
                {
                    if (thumbPrint[i] != otherThumbPrint[i])
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        //
        // Returns null or previously cached cred handle.
        //
        // ATTN: The returned handle can be invalid, the callers of InitializeSecurityContext and AcceptSecurityContext
        // must be prepared to execute a back-out code if the call fails.
        //
        internal static SafeFreeCredentials? TryCachedCredential(byte[]? thumbPrint, SslProtocols sslProtocols, bool isServer, EncryptionPolicy encryptionPolicy)
        {
            if (s_cachedCreds.IsEmpty)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Not found, Current Cache Count = {s_cachedCreds.Count}");
                return null;
            }

            var key = new SslCredKey(thumbPrint, (int)sslProtocols, isServer, encryptionPolicy);

            SafeCredentialReference? cached;
            if (!s_cachedCreds.TryGetValue(key, out cached) || cached.IsClosed || cached.Target.IsInvalid)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Not found or invalid, Current Cache Coun = {s_cachedCreds.Count}");
                return null;
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Found a cached Handle = {cached.Target}");

            return cached.Target;
        }

        //
        // The app is calling this method after starting an SSL handshake.
        //
        // ATTN: The thumbPrint must be from inspected and possibly cloned user Cert object or we get a security hole in SslCredKey ctor.
        //
        internal static void CacheCredential(SafeFreeCredentials creds, byte[]? thumbPrint, SslProtocols sslProtocols, bool isServer, EncryptionPolicy encryptionPolicy)
        {
            if (creds == null)
            {
                NetEventSource.Fail(null, "creds == null");
            }

            if (creds!.IsInvalid)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Refused to cache an Invalid Handle {creds}, Current Cache Count = {s_cachedCreds.Count}");
                return;
            }

            var key = new SslCredKey(thumbPrint, (int)sslProtocols, isServer, encryptionPolicy);

            SafeCredentialReference? cached;

            if (!s_cachedCreds.TryGetValue(key, out cached) || cached.IsClosed || cached.Target.IsInvalid)
            {
                lock (s_cachedCreds)
                {
                    if (!s_cachedCreds.TryGetValue(key, out cached) || cached.IsClosed)
                    {
                        cached = SafeCredentialReference.CreateReference(creds);

                        if (cached == null)
                        {
                            // Means the handle got closed in between, return it back and let caller deal with the issue.
                            return;
                        }

                        s_cachedCreds[key] = cached;
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Caching New Handle = {creds}, Current Cache Count = {s_cachedCreds.Count}");

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
                                cached = toRemoveAttempt[i].Value;

                                if (cached != null)
                                {
                                    creds = cached.Target;
                                    cached.Dispose();

                                    if (!creds.IsClosed && !creds.IsInvalid && (cached = SafeCredentialReference.CreateReference(creds)) != null)
                                    {
                                        s_cachedCreds[toRemoveAttempt[i].Key] = cached;
                                    }
                                    else
                                    {
                                        s_cachedCreds.TryRemove(toRemoveAttempt[i].Key, out cached);
                                    }
                                }
                            }
                            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Scavenged cache, New Cache Count = {s_cachedCreds.Count}");
                        }
                    }
                    else
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"CacheCredential() (locked retry) Found already cached Handle = {cached.Target}");
                    }
                }
            }
            else
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"CacheCredential() Ignoring incoming handle = {creds} since found already cached Handle = {cached.Target}");
            }
        }
    }
}
