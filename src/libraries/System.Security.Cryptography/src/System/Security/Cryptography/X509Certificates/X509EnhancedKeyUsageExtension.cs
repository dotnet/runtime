// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X509EnhancedKeyUsageExtension : X509Extension
    {
        public X509EnhancedKeyUsageExtension()
            : base(Oids.EnhancedKeyUsageOid)
        {
            _enhancedKeyUsages = new OidCollection();
            _decoded = true;
        }

        public X509EnhancedKeyUsageExtension(AsnEncodedData encodedEnhancedKeyUsages, bool critical)
            : base(Oids.EnhancedKeyUsageOid, encodedEnhancedKeyUsages.RawData, critical)
        {
        }

        public X509EnhancedKeyUsageExtension(OidCollection enhancedKeyUsages, bool critical)
            : base(Oids.EnhancedKeyUsageOid, EncodeExtension(enhancedKeyUsages), critical)
        {
        }

        public OidCollection EnhancedKeyUsages
        {
            get
            {
                if (!_decoded)
                {
                    X509Pal.Instance.DecodeX509EnhancedKeyUsageExtension(RawData, out _enhancedKeyUsages);
                    _decoded = true;
                }
                OidCollection oids = new OidCollection();
                foreach (Oid oid in _enhancedKeyUsages!)
                    oids.Add(oid);
                return oids;
            }
        }

        public override void CopyFrom(AsnEncodedData asnEncodedData)
        {
            base.CopyFrom(asnEncodedData);
            _decoded = false;
        }

        private static byte[] EncodeExtension(OidCollection enhancedKeyUsages)
        {
            ArgumentNullException.ThrowIfNull(enhancedKeyUsages);

            return X509Pal.Instance.EncodeX509EnhancedKeyUsageExtension(enhancedKeyUsages);
        }

        private OidCollection? _enhancedKeyUsages;
        private bool _decoded;
    }
}
