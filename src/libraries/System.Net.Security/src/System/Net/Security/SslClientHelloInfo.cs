// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Authentication;

namespace System.Net.Security
{
    /// <summary>
    /// Specifies families of TLS signature algorithms.
    /// </summary>
    [Flags]
    public enum TlsSignatureAlgorithmFamilies
    {
        /// <summary>
        /// No signature algorithm family is specified.
        /// </summary>
        None = 0,

        /// <summary>
        /// The RSA signature algorithm family.
        /// </summary>
        Rsa = 1 << 0,

        /// <summary>
        /// The ECDSA signature algorithm family.
        /// </summary>
        ECDsa = 1 << 1,

        /// <summary>
        /// The EdDSA signature algorithm family.
        /// </summary>
        EdDsa = 1 << 2,

        /// <summary>
        /// The ML-DSA signature algorithm family.
        /// </summary>
        MLDsa = 1 << 3,

        /// <summary>
        /// The SLH-DSA signature algorithm family.
        /// </summary>
        SlhDsa = 1 << 4,
    }

    /// <summary>
    /// This struct contains information from received TLS Client Hello frame.
    /// </summary>
    public readonly struct SslClientHelloInfo
    {
        public readonly string ServerName { get; }
        public readonly SslProtocols SslProtocols { get; }

        /// <summary>
        /// Gets the signature algorithm families advertised by the client.
        /// </summary>
        /// <remarks>
        /// This property indicates broad compatibility with a server certificate's public key.
        /// It does not guarantee that every certificate or certificate chain using an advertised
        /// family will be accepted by the client. The value is <see cref="TlsSignatureAlgorithmFamilies.None"/>
        /// when this information is unavailable or the client advertises no recognized family.
        /// </remarks>
        public readonly TlsSignatureAlgorithmFamilies SignatureAlgorithmFamilies { get; }

        public SslClientHelloInfo(string serverName, SslProtocols sslProtocols)
            : this(serverName, sslProtocols, TlsSignatureAlgorithmFamilies.None)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SslClientHelloInfo"/> struct.
        /// </summary>
        /// <param name="serverName">The server name requested by the client.</param>
        /// <param name="sslProtocols">A bitwise combination of the enumeration values that specifies the TLS protocols advertised by the client.</param>
        /// <param name="signatureAlgorithmFamilies">A bitwise combination of the enumeration values that specifies the signature algorithm families advertised by the client.</param>
        public SslClientHelloInfo(
            string serverName,
            SslProtocols sslProtocols,
            TlsSignatureAlgorithmFamilies signatureAlgorithmFamilies)
        {
            ServerName = serverName;
            SslProtocols = sslProtocols;
            SignatureAlgorithmFamilies = signatureAlgorithmFamilies;
        }
    }
}
