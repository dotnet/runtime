// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Utility class to strongly type the format of key blobs used with CNG. Since all CNG APIs which
    ///     require or return a key blob format take the name as a string, we use this string wrapper class to
    ///     specifically mark which parameters and return values are expected to be key blob formats.  We also
    ///     provide a list of well known blob formats, which helps Intellisense users find a set of good blob
    ///     formats to use.
    /// </summary>
    public sealed class CngKeyBlobFormat : IEquatable<CngKeyBlobFormat>
    {
        public CngKeyBlobFormat(string format)
        {
            ArgumentException.ThrowIfNullOrEmpty(format);
            _format = format;
        }

        /// <summary>
        ///     Name of the blob format
        /// </summary>
        public string Format
        {
            get
            {
                return _format;
            }
        }

        public static bool operator ==(CngKeyBlobFormat? left, CngKeyBlobFormat? right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(CngKeyBlobFormat? left, CngKeyBlobFormat? right)
        {
            if (left is null)
            {
                return right is not null;
            }

            return !left.Equals(right);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            Debug.Assert(_format != null);

            return Equals(obj as CngKeyBlobFormat);
        }

        public bool Equals([NotNullWhen(true)] CngKeyBlobFormat? other)
        {
            if (other is null)
            {
                return false;
            }

            return _format.Equals(other.Format);
        }

        public override int GetHashCode()
        {
            Debug.Assert(_format != null);
            return _format.GetHashCode();
        }

        public override string ToString()
        {
            Debug.Assert(_format != null);
            return _format;
        }

        //
        // Well known key blob formats
        //

        public static CngKeyBlobFormat EccPrivateBlob
        {
            get
            {
                return s_eccPrivate ??= new CngKeyBlobFormat("ECCPRIVATEBLOB"); // BCRYPT_ECCPRIVATE_BLOB
            }
        }

        public static CngKeyBlobFormat EccPublicBlob
        {
            get
            {
                return s_eccPublic ??= new CngKeyBlobFormat("ECCPUBLICBLOB"); // BCRYPT_ECCPUBLIC_BLOB
            }
        }

        public static CngKeyBlobFormat EccFullPrivateBlob
        {
            get
            {
                return s_eccFullPrivate ??= new CngKeyBlobFormat("ECCFULLPRIVATEBLOB"); // BCRYPT_ECCFULLPRIVATE_BLOB
            }
        }

        public static CngKeyBlobFormat EccFullPublicBlob
        {
            get
            {
                return s_eccFullPublic ??= new CngKeyBlobFormat("ECCFULLPUBLICBLOB"); // BCRYPT_ECCFULLPUBLIC_BLOB
            }
        }

        public static CngKeyBlobFormat GenericPrivateBlob
        {
            get
            {
                return s_genericPrivate ??= new CngKeyBlobFormat("PRIVATEBLOB"); // BCRYPT_PRIVATE_KEY_BLOB
            }
        }

        public static CngKeyBlobFormat GenericPublicBlob
        {
            get
            {
                return s_genericPublic ??= new CngKeyBlobFormat("PUBLICBLOB"); // BCRYPT_PUBLIC_KEY_BLOB
            }
        }

        /// <summary>
        ///   Gets a <see cref="CngKeyBlobFormat"/> object that specifies a Post-Quantum Digital Signature Algorithm
        ///   public key BLOB.
        /// </summary>
        /// <value>
        ///   A <see cref="CngKeyBlobFormat"/> object that specifies a Post-Quantum Digital Signature Algorithm
        ///   public key BLOB.
        /// </value>
        /// <remarks>
        ///   The value identified by this blob format is &quot;PQDSAPUBLICBLOB&quot;.
        /// </remarks>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static CngKeyBlobFormat PQDsaPublicBlob =>
            field ??= new CngKeyBlobFormat("PQDSAPUBLICBLOB"); // BCRYPT_PQDSA_PUBLIC_BLOB

        /// <summary>
        ///   Gets a <see cref="CngKeyBlobFormat"/> object that specifies a Post-Quantum Digital Signature Algorithm
        ///   private key BLOB.
        /// </summary>
        /// <value>
        ///   A <see cref="CngKeyBlobFormat"/> object that specifies a Post-Quantum Digital Signature Algorithm
        ///   private key BLOB.
        /// </value>
        /// <remarks>
        ///   The value identified by this blob format is &quot;PQDSAPRIVATEBLOB&quot;.
        /// </remarks>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static CngKeyBlobFormat PQDsaPrivateBlob =>
            field ??= new CngKeyBlobFormat("PQDSAPRIVATEBLOB"); // BCRYPT_PQDSA_PRIVATE_BLOB

        /// <summary>
        ///   Gets a <see cref="CngKeyBlobFormat"/> object that specifies a Post-Quantum Digital Signature Algorithm
        ///   private seed BLOB.
        /// </summary>
        /// <value>
        ///   A <see cref="CngKeyBlobFormat"/> object that specifies a Post-Quantum Digital Signature Algorithm
        ///   private seed BLOB.
        /// </value>
        /// <remarks>
        ///   The value identified by this blob format is &quot;PQDSAPRIVATESEEDBLOB&quot;.
        /// </remarks>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static CngKeyBlobFormat PQDsaPrivateSeedBlob =>
            field ??= new CngKeyBlobFormat("PQDSAPRIVATESEEDBLOB"); // BCRYPT_PQDSA_PRIVATE_SEED_BLOB

        /// <summary>
        ///   Gets a <see cref="CngKeyBlobFormat"/> object that specifies a Module-Lattice-Based Key-Encapsulation
        ///   Mechanism (ML-KEM) public key BLOB.
        /// </summary>
        /// <value>
        ///   A <see cref="CngKeyBlobFormat"/> object that specifies a Module-Lattice-Based Key-Encapsulation
        ///   Mechanism (ML-KEM) public key BLOB.
        /// </value>
        /// <remarks>
        ///   The value identified by this blob format is &quot;MLKEMPUBLICBLOB&quot;.
        /// </remarks>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static CngKeyBlobFormat MLKemPublicBlob => field ??= new CngKeyBlobFormat("MLKEMPUBLICBLOB");

        /// <summary>
        ///   Gets a <see cref="CngKeyBlobFormat"/> object that specifies a Module-Lattice-Based Key-Encapsulation
        ///   Mechanism (ML-KEM) private key BLOB.
        /// </summary>
        /// <value>
        ///   A <see cref="CngKeyBlobFormat"/> object that specifies a Module-Lattice-Based Key-Encapsulation
        ///   Mechanism (ML-KEM) private key BLOB.
        /// </value>
        /// <remarks>
        ///   The value identified by this blob format is &quot;MLKEMPRIVATEBLOB&quot;.
        /// </remarks>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static CngKeyBlobFormat MLKemPrivateBlob => field ??= new CngKeyBlobFormat("MLKEMPRIVATEBLOB");

        /// <summary>
        ///   Gets a <see cref="CngKeyBlobFormat"/> object that specifies a Module-Lattice-Based Key-Encapsulation
        ///   Mechanism (ML-KEM) private seed BLOB.
        /// </summary>
        /// <value>
        ///   A <see cref="CngKeyBlobFormat"/> object that specifies a Module-Lattice-Based Key-Encapsulation
        ///   Mechanism (ML-KEM) private seed BLOB.
        /// </value>
        /// <remarks>
        ///   The value identified by this blob format is &quot;MLKEMPRIVATESEEDBLOB&quot;.
        /// </remarks>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static CngKeyBlobFormat MLKemPrivateSeedBlob => field ??= new CngKeyBlobFormat("MLKEMPRIVATESEEDBLOB");

        public static CngKeyBlobFormat OpaqueTransportBlob
        {
            get
            {
                return s_opaqueTransport ??= new CngKeyBlobFormat("OpaqueTransport"); // NCRYPT_OPAQUETRANSPORT_BLOB
            }
        }

        public static CngKeyBlobFormat Pkcs8PrivateBlob
        {
            get
            {
                return s_pkcs8Private ??= new CngKeyBlobFormat("PKCS8_PRIVATEKEY"); // NCRYPT_PKCS8_PRIVATE_KEY_BLOB
            }
        }

        private static CngKeyBlobFormat? s_eccPrivate;
        private static CngKeyBlobFormat? s_eccPublic;
        private static CngKeyBlobFormat? s_eccFullPrivate;
        private static CngKeyBlobFormat? s_eccFullPublic;
        private static CngKeyBlobFormat? s_genericPrivate;
        private static CngKeyBlobFormat? s_genericPublic;
        private static CngKeyBlobFormat? s_opaqueTransport;
        private static CngKeyBlobFormat? s_pkcs8Private;

        private readonly string _format;
    }
}
