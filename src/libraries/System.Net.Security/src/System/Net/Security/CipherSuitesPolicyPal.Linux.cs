// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Authentication;
using System.Text;
using Ssl = Interop.Ssl;
using OpenSsl = Interop.OpenSsl;

namespace System.Net.Security
{
    internal sealed class CipherSuitesPolicyPal
    {
        private static readonly byte[] AllowNoEncryptionDefault =
            Encoding.ASCII.GetBytes("ALL:eNULL\0");

        private static readonly byte[] NoEncryptionDefault =
            Encoding.ASCII.GetBytes("eNULL\0");

        private byte[] _cipherSuites;
        private byte[] _tls13CipherSuites;
        private List<TlsCipherSuite> _tlsCipherSuites = new List<TlsCipherSuite>();

        internal IEnumerable<TlsCipherSuite> GetCipherSuites() => _tlsCipherSuites;

        internal CipherSuitesPolicyPal(IEnumerable<TlsCipherSuite> allowedCipherSuites)
        {
            if (!Interop.Ssl.Capabilities.Tls13Supported)
            {
                throw new PlatformNotSupportedException(SR.net_ssl_ciphersuites_policy_not_supported);
            }

            using (SafeSslContextHandle innerContext = Ssl.SslCtxCreate(Ssl.SslMethods.SSLv23_method))
            {
                if (innerContext.IsInvalid)
                {
                    throw OpenSsl.CreateSslException(SR.net_allocate_ssl_context_failed);
                }

                using (SafeSslHandle ssl = SafeSslHandle.Create(innerContext, false))
                {
                    if (ssl.IsInvalid)
                    {
                        throw OpenSsl.CreateSslException(SR.net_allocate_ssl_context_failed);
                    }

                    using (var tls13CipherSuites = new OpenSslStringBuilder())
                    using (var cipherSuites = new OpenSslStringBuilder())
                    {
                        foreach (TlsCipherSuite cs in allowedCipherSuites)
                        {
                            string? name = Interop.Ssl.GetOpenSslCipherSuiteName(
                                ssl,
                                cs,
                                out bool isTls12OrLower);

                            if (name == null)
                            {
                                // we do not have a corresponding name
                                // allowing less than user requested is OK
                                continue;
                            }

                            _tlsCipherSuites.Add(cs);
                            (isTls12OrLower ? cipherSuites : tls13CipherSuites).AllowCipherSuite(name);
                        }

                        _cipherSuites = cipherSuites.GetOpenSslString();
                        _tls13CipherSuites = tls13CipherSuites.GetOpenSslString();
                    }
                }
            }
        }

        internal static bool ShouldOptOutOfTls13(CipherSuitesPolicy? policy, EncryptionPolicy encryptionPolicy)
        {
            // if TLS 1.3 was explicitly requested the underlying code will throw
            // if default option (SslProtocols.None) is used we will opt-out of TLS 1.3

            if (encryptionPolicy == EncryptionPolicy.NoEncryption)
            {
                // TLS 1.3 uses different ciphersuite restrictions than previous versions.
                // It has no equivalent to a NoEncryption option.
                return true;
            }

            if (policy == null)
            {
                // null means default, by default OpenSSL will choose if it wants to opt-out or not
                return false;
            }

            Debug.Assert(
                policy.Pal._tls13CipherSuites.Length != 0 &&
                    policy.Pal._tls13CipherSuites[policy.Pal._tls13CipherSuites.Length - 1] == 0,
                "null terminated string expected");

            // we should opt out only when policy is empty
            return policy.Pal._tls13CipherSuites.Length == 1;
        }

        internal static bool ShouldOptOutOfLowerThanTls13(CipherSuitesPolicy? policy, EncryptionPolicy encryptionPolicy)
        {
            if (policy == null)
            {
                // null means default, by default OpenSSL will choose if it wants to opt-out or not
                return false;
            }

            Debug.Assert(
                policy.Pal._cipherSuites.Length != 0 &&
                    policy.Pal._cipherSuites[policy.Pal._cipherSuites.Length - 1] == 0,
                "null terminated string expected");

            // we should opt out only when policy is empty
            return policy.Pal._cipherSuites.Length == 1;
        }

        private static bool IsOnlyTls13(SslProtocols protocols)
            => protocols == SslProtocols.Tls13;

        internal static bool WantsTls13(SslProtocols protocols)
            => protocols == SslProtocols.None || (protocols & SslProtocols.Tls13) != 0;

        internal static byte[]? GetOpenSslCipherList(
            CipherSuitesPolicy? policy,
            SslProtocols protocols,
            EncryptionPolicy encryptionPolicy)
        {
            if (IsOnlyTls13(protocols))
            {
                // older cipher suites will be disabled through protocols
                return null;
            }

            if (policy == null)
            {
                return CipherListFromEncryptionPolicy(encryptionPolicy);
            }

            if (encryptionPolicy == EncryptionPolicy.NoEncryption)
            {
                throw new PlatformNotSupportedException(SR.net_ssl_ciphersuites_policy_not_supported);
            }

            return policy.Pal._cipherSuites;
        }

        internal static byte[]? GetOpenSslCipherSuites(
            CipherSuitesPolicy? policy,
            SslProtocols protocols,
            EncryptionPolicy encryptionPolicy)
        {
            if (!WantsTls13(protocols) || policy == null)
            {
                // do not call TLS 1.3 API, let OpenSSL choose what to do
                return null;
            }

            if (encryptionPolicy == EncryptionPolicy.NoEncryption)
            {
                throw new PlatformNotSupportedException(SR.net_ssl_ciphersuites_policy_not_supported);
            }

            return policy.Pal._tls13CipherSuites;
        }

        private static byte[]? CipherListFromEncryptionPolicy(EncryptionPolicy policy)
        {
            switch (policy)
            {
                case EncryptionPolicy.RequireEncryption:
                    return null;
                case EncryptionPolicy.AllowNoEncryption:
                    return AllowNoEncryptionDefault;
                case EncryptionPolicy.NoEncryption:
                    return NoEncryptionDefault;
                default:
                    Debug.Fail($"Unknown EncryptionPolicy value ({policy})");
                    return null;
            }
        }

        private sealed class OpenSslStringBuilder : StreamWriter
        {
            private const string SSL_TXT_Separator = ":";
            private static readonly byte[] EmptyString = new byte[1] { 0 };

            private MemoryStream _ms;
            private bool _first = true;

            public OpenSslStringBuilder() : base(new MemoryStream(), Encoding.ASCII)
            {
                _ms = (MemoryStream)BaseStream;
            }

            public void AllowCipherSuite(string cipherSuite)
            {
                if (_first)
                {
                    _first = false;
                }
                else
                {
                    Write(SSL_TXT_Separator);
                }

                Write(cipherSuite);
            }

            public byte[] GetOpenSslString()
            {
                if (_first)
                {
                    return EmptyString;
                }

                Flush();
                _ms.WriteByte(0);
                return _ms.ToArray();
            }
        }
    }
}
