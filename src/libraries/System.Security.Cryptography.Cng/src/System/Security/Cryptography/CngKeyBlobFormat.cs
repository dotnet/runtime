// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Utility class to strongly type the format of key blobs used with CNG. Since all CNG APIs which
    ///     require or return a key blob format take the name as a string, we use this string wrapper class to
    ///     specifically mark which parameters and return values are expected to be key blob formats.  We also
    ///     provide a list of well known blob formats, which helps Intellisense users find a set of good blob
    ///     formats to use.
    /// </summary>
    [Serializable]
    public sealed class CngKeyBlobFormat : IEquatable<CngKeyBlobFormat>
    {
        public CngKeyBlobFormat(string format)
        {
            if (format == null)
                throw new ArgumentNullException(nameof(format));
            if (format.Length == 0)
                throw new ArgumentException(SR.Format(SR.Cryptography_InvalidKeyBlobFormat, format), nameof(format));

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

        public static bool operator ==(CngKeyBlobFormat left, CngKeyBlobFormat right)
        {
            if (object.ReferenceEquals(left, null))
            {
                return object.ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(CngKeyBlobFormat left, CngKeyBlobFormat right)
        {
            if (object.ReferenceEquals(left, null))
            {
                return !object.ReferenceEquals(right, null);
            }

            return !left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            Debug.Assert(_format != null);

            return Equals(obj as CngKeyBlobFormat);
        }

        public bool Equals(CngKeyBlobFormat other)
        {
            if (object.ReferenceEquals(other, null))
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
                return s_eccPrivate ?? (s_eccPrivate = new CngKeyBlobFormat("ECCPRIVATEBLOB")); // BCRYPT_ECCPRIVATE_BLOB
            }
        }

        public static CngKeyBlobFormat EccPublicBlob
        {
            get
            {
                return s_eccPublic ?? (s_eccPublic = new CngKeyBlobFormat("ECCPUBLICBLOB")); // BCRYPT_ECCPUBLIC_BLOB
            }
        }

        public static CngKeyBlobFormat EccFullPrivateBlob
        {
            get
            {
                return s_eccFullPrivate ?? (s_eccFullPrivate = new CngKeyBlobFormat("ECCFULLPRIVATEBLOB")); // BCRYPT_ECCFULLPRIVATE_BLOB
            }
        }

        public static CngKeyBlobFormat EccFullPublicBlob
        {
            get
            {
                return s_eccFullPublic ?? (s_eccFullPublic = new CngKeyBlobFormat("ECCFULLPUBLICBLOB")); // BCRYPT_ECCFULLPUBLIC_BLOB
            }
        }

        public static CngKeyBlobFormat GenericPrivateBlob
        {
            get
            {
                return s_genericPrivate ?? (s_genericPrivate = new CngKeyBlobFormat("PRIVATEBLOB")); // BCRYPT_PRIVATE_KEY_BLOB
            }
        }

        public static CngKeyBlobFormat GenericPublicBlob
        {
            get
            {
                return s_genericPublic ?? (s_genericPublic = new CngKeyBlobFormat("PUBLICBLOB")); // BCRYPT_PUBLIC_KEY_BLOB
            }
        }

        public static CngKeyBlobFormat OpaqueTransportBlob
        {
            get
            {
                return s_opaqueTransport ?? (s_opaqueTransport = new CngKeyBlobFormat("OpaqueTransport")); // NCRYPT_OPAQUETRANSPORT_BLOB
            }
        }

        public static CngKeyBlobFormat Pkcs8PrivateBlob
        {
            get
            {
                return s_pkcs8Private ?? (s_pkcs8Private = new CngKeyBlobFormat("PKCS8_PRIVATEKEY")); // NCRYPT_PKCS8_PRIVATE_KEY_BLOB
            }
        }


        private static CngKeyBlobFormat s_eccPrivate;
        private static CngKeyBlobFormat s_eccPublic;
        private static CngKeyBlobFormat s_eccFullPrivate;
        private static CngKeyBlobFormat s_eccFullPublic;
        private static CngKeyBlobFormat s_genericPrivate;
        private static CngKeyBlobFormat s_genericPublic;
        private static CngKeyBlobFormat s_opaqueTransport;
        private static CngKeyBlobFormat s_pkcs8Private;

        private readonly string _format;
    }
}

