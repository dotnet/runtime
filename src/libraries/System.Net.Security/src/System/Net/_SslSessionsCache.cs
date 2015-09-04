// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace System.Net.Security
{
    // Implements SSL session caching mechanism based on a static table of SSL credentials.
    internal static class SslSessionsCache
    {
        private const int CheckExpiredModulo = 32;
        private static Hashtable s_CachedCreds = new Hashtable(32);

        //
        // Uses certificate thumb-print comparison.
        //
        private struct SslCredKey
        {
            private byte[] _CertThumbPrint;
            private int _AllowedProtocols;
            private EncryptionPolicy _EncryptionPolicy;
            private readonly int _HashCode;

            //
            // SECURITY: X509Certificate.GetCertHash() is virtual hence before going here,
            //           the caller of this ctor has to ensure that a user cert object was inspected and
            //           optionally cloned.
            //
            internal SslCredKey(byte[] thumbPrint, int allowedProtocols, EncryptionPolicy encryptionPolicy)
            {
                _CertThumbPrint = thumbPrint == null ? Array.Empty<byte>() : thumbPrint;
                _HashCode = 0;
                if (thumbPrint != null)
                {
                    _HashCode ^= _CertThumbPrint[0];
                    if (1 < _CertThumbPrint.Length)
                    {
                        _HashCode ^= (_CertThumbPrint[1] << 8);
                    }

                    if (2 < _CertThumbPrint.Length)
                    {
                        _HashCode ^= (_CertThumbPrint[2] << 16);
                    }

                    if (3 < _CertThumbPrint.Length)
                    {
                        _HashCode ^= (_CertThumbPrint[3] << 24);
                    }
                }

                _HashCode ^= allowedProtocols;
                _HashCode ^= (int)encryptionPolicy;
                _AllowedProtocols = allowedProtocols;
                _EncryptionPolicy = encryptionPolicy;
            }

            public override int GetHashCode()
            {
                return _HashCode;
            }

            public static bool operator ==(SslCredKey sslCredKey1,
                                            SslCredKey sslCredKey2)
            {
                return sslCredKey1.Equals(sslCredKey2);
            }

            public static bool operator !=(SslCredKey sslCredKey1,
                                            SslCredKey sslCredKey2)
            {
                return !sslCredKey1.Equals(sslCredKey2);
            }

            public override bool Equals(Object y)
            {
                SslCredKey other = (SslCredKey)y;

                if (_CertThumbPrint.Length != other._CertThumbPrint.Length)
                {
                    return false;
                }

                if (_HashCode != other._HashCode)
                {
                    return false;
                }

                if (_EncryptionPolicy != other._EncryptionPolicy)
                {
                    return false;
                }

                if (_AllowedProtocols != other._AllowedProtocols)
                {
                    return false;
                }

                for (int i = 0; i < _CertThumbPrint.Length; ++i)
                {
                    if (_CertThumbPrint[i] != other._CertThumbPrint[i])
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
        internal static SafeFreeCredentials TryCachedCredential(byte[] thumbPrint, int allowedProtocols, EncryptionPolicy encryptionPolicy)
        {
            if (s_CachedCreds.Count == 0)
            {
                GlobalLog.Print("TryCachedCredential() Not found, Current Cache Count = " + s_CachedCreds.Count);
                return null;
            }

            object key = new SslCredKey(thumbPrint, allowedProtocols, encryptionPolicy);

            SafeCredentialReference cached = s_CachedCreds[key] as SafeCredentialReference;

            if (cached == null || cached.IsClosed || cached._Target.IsInvalid)
            {
                GlobalLog.Print("TryCachedCredential() Not found or invalid, Current Cache Count = " + s_CachedCreds.Count);
                return null;
            }

            GlobalLog.Print("TryCachedCredential() Found a cached Handle = " + cached._Target.ToString());

            return cached._Target;
        }

        //
        // The app is calling this method after starting an SSL handshake.
        //
        // ATTN: The thumbPrint must be from inspected and possibly cloned user Cert object or we get a security hole in SslCredKey ctor.
        //
        internal static void CacheCredential(SafeFreeCredentials creds, byte[] thumbPrint, int allowedProtocols, EncryptionPolicy encryptionPolicy)
        {
            GlobalLog.Assert(creds != null, "CacheCredential|creds == null");
            if (creds.IsInvalid)
            {
                GlobalLog.Print("CacheCredential() Refused to cache an Invalid Handle = " + creds.ToString() + ", Current Cache Count = " + s_CachedCreds.Count);
                return;
            }

            object key = new SslCredKey(thumbPrint, allowedProtocols, encryptionPolicy);

            SafeCredentialReference cached = s_CachedCreds[key] as SafeCredentialReference;

            if (cached == null || cached.IsClosed || cached._Target.IsInvalid)
            {
                lock (s_CachedCreds)
                {
                    cached = s_CachedCreds[key] as SafeCredentialReference;

                    if (cached == null || cached.IsClosed)
                    {
                        cached = SafeCredentialReference.CreateReference(creds);

                        if (cached == null)
                        {
                            // Means the handle got closed in between, return it back and let caller deal with the issue.
                            return;
                        }

                        s_CachedCreds[key] = cached;
                        GlobalLog.Print("CacheCredential() Caching New Handle = " + creds.ToString() + ", Current Cache Count = " + s_CachedCreds.Count);

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
                        if ((s_CachedCreds.Count % CheckExpiredModulo) == 0)
                        {
                            DictionaryEntry[] toRemoveAttempt = new DictionaryEntry[s_CachedCreds.Count];
                            s_CachedCreds.CopyTo(toRemoveAttempt, 0);

                            for (int i = 0; i < toRemoveAttempt.Length; ++i)
                            {
                                cached = toRemoveAttempt[i].Value as SafeCredentialReference;

                                if (cached != null)
                                {
                                    creds = cached._Target;
                                    cached.Dispose();

                                    if (!creds.IsClosed && !creds.IsInvalid && (cached = SafeCredentialReference.CreateReference(creds)) != null)
                                    {
                                        s_CachedCreds[toRemoveAttempt[i].Key] = cached;
                                    }
                                    else
                                    {
                                        s_CachedCreds.Remove(toRemoveAttempt[i].Key);
                                    }
                                }
                            }
                            GlobalLog.Print("Scavenged cache, New Cache Count = " + s_CachedCreds.Count);
                        }
                    }
                    else
                    {
                        GlobalLog.Print("CacheCredential() (locked retry) Found already cached Handle = " + cached._Target.ToString());
                    }
                }
            }
            else
            {
                GlobalLog.Print("CacheCredential() Ignoring incoming handle = " + creds.ToString() + " since found already cached Handle = " + cached._Target.ToString());
            }
        }
    }
}
