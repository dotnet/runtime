// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using Internal.Cryptography;
using Internal.Cryptography.Pal;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X509EnhancedKeyUsageExtension : X509Extension
    {
        public X509EnhancedKeyUsageExtension()
            : base(Oids.EnhancedKeyUsage)
        {
            _enhancedKeyUsages = new OidCollection();
            _decoded = true;
        }

        public X509EnhancedKeyUsageExtension(AsnEncodedData encodedEnhancedKeyUsages, bool critical)
            : base(Oids.EnhancedKeyUsage, encodedEnhancedKeyUsages.RawData, critical)
        {
        }

        public X509EnhancedKeyUsageExtension(OidCollection enhancedKeyUsages, bool critical)
            : base(Oids.EnhancedKeyUsage, EncodeExtension(enhancedKeyUsages), critical)
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
                return _enhancedKeyUsages;
            }
        }

        public override void CopyFrom(AsnEncodedData asnEncodedData)
        {
            base.CopyFrom(asnEncodedData);
            _decoded = false;
        }

        private static byte[] EncodeExtension(OidCollection enhancedKeyUsages)
        {
            if (enhancedKeyUsages == null)
                throw new ArgumentNullException(nameof(enhancedKeyUsages));
            return X509Pal.Instance.EncodeX509EnhancedKeyUsageExtension(enhancedKeyUsages);
        }

        private OidCollection _enhancedKeyUsages;
        private bool _decoded;
    }
}

