// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X509BasicConstraintsExtension : X509Extension
    {
        public X509BasicConstraintsExtension()
            : base(Oids.BasicConstraints2Oid)
        {
            _decoded = true;
        }

        public X509BasicConstraintsExtension(bool certificateAuthority, bool hasPathLengthConstraint, int pathLengthConstraint, bool critical)
            : base(Oids.BasicConstraints2Oid, EncodeExtension(certificateAuthority, hasPathLengthConstraint, pathLengthConstraint), critical, skipCopy: true)
        {
        }

        public X509BasicConstraintsExtension(AsnEncodedData encodedBasicConstraints, bool critical)
            : base(Oids.BasicConstraints2Oid, encodedBasicConstraints.RawData, critical)
        {
        }

        public bool CertificateAuthority
        {
            get
            {
                if (!_decoded)
                    DecodeExtension();

                return _certificateAuthority;
            }
        }

        public bool HasPathLengthConstraint
        {
            get
            {
                if (!_decoded)
                    DecodeExtension();

                return _hasPathLenConstraint;
            }
        }

        public int PathLengthConstraint
        {
            get
            {
                if (!_decoded)
                    DecodeExtension();

                return _pathLenConstraint;
            }
        }

        public override void CopyFrom(AsnEncodedData asnEncodedData)
        {
            base.CopyFrom(asnEncodedData);
            _decoded = false;
        }

        /// <summary>
        ///   Creates an instance of <see cref="X509BasicConstraintsExtension"/> appropriate for
        ///   a certificate authority, optionally including a path length constraint value.
        /// </summary>
        /// <param name="pathLengthConstraint">
        ///   The longest valid length of a certificate chain between the certificate containing
        ///   this extension and an end-entity certificate.
        ///   The default is <see langword="null" />, an unconstrained length.
        /// </param>
        /// <returns>
        ///   The configured basic constraints extension.
        /// </returns>
        /// <remarks>
        ///   Following the guidance from IETF RFC 3280, the extension returned from this method
        ///   will have the <see cref="X509Extension.Critical"/> property set to <see langword="true" />.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="pathLengthConstraint"/> is a non-null value less than zero.
        /// </exception>
        public static X509BasicConstraintsExtension CreateForCertificateAuthority(int? pathLengthConstraint = null)
        {
            return new X509BasicConstraintsExtension(
                true,
                pathLengthConstraint.HasValue,
                pathLengthConstraint.GetValueOrDefault(),
                true);
        }

        /// <summary>
        ///   Creates an instance of <see cref="X509BasicConstraintsExtension"/> appropriate for
        ///   an end-entity certificate, optionally marking the extension as critical.
        /// </summary>
        /// <param name="critical">
        ///   <see langword="true" /> to mark the extension as critical; <see langword="false" /> otherwise.
        ///   The default is <see langword="false" />.
        /// </param>
        /// <returns>
        ///   The configured basic constraints extension.
        /// </returns>
        public static X509BasicConstraintsExtension CreateForEndEntity(bool critical = false)
        {
            return new X509BasicConstraintsExtension(false, false, 0, critical);
        }

        private static byte[] EncodeExtension(bool certificateAuthority, bool hasPathLengthConstraint, int pathLengthConstraint)
        {
            if (hasPathLengthConstraint && pathLengthConstraint < 0)
                throw new ArgumentOutOfRangeException(nameof(pathLengthConstraint), SR.ArgumentOutOfRange_NeedNonNegNum);

            return X509Pal.Instance.EncodeX509BasicConstraints2Extension(certificateAuthority, hasPathLengthConstraint, pathLengthConstraint);
        }

        private void DecodeExtension()
        {
            if (Oid!.Value == Oids.BasicConstraints)
                X509Pal.Instance.DecodeX509BasicConstraintsExtension(RawData, out _certificateAuthority, out _hasPathLenConstraint, out _pathLenConstraint);
            else
                X509Pal.Instance.DecodeX509BasicConstraints2Extension(RawData, out _certificateAuthority, out _hasPathLenConstraint, out _pathLenConstraint);

            _decoded = true;
        }

        private bool _certificateAuthority;
        private bool _hasPathLenConstraint;
        private int _pathLenConstraint;
        private bool _decoded;
    }
}
