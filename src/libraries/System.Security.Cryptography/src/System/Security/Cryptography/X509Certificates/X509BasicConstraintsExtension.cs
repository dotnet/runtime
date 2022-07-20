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
