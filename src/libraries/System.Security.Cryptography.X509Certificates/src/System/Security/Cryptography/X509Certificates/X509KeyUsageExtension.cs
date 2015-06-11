// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    public sealed class X509KeyUsageExtension : X509Extension
    {
        public X509KeyUsageExtension()
            : base(Oids.KeyUsage)
        {
            _decoded = true;
        }

        public X509KeyUsageExtension(AsnEncodedData encodedKeyUsage, bool critical)
            : base(Oids.KeyUsage, encodedKeyUsage.RawData, critical)
        {
        }

        public X509KeyUsageExtension(X509KeyUsageFlags keyUsages, bool critical)
            : base(Oids.KeyUsage, X509Pal.Instance.EncodeX509KeyUsageExtension(keyUsages), critical)
        {
        }

        public X509KeyUsageFlags KeyUsages
        {
            get
            {
                if (!_decoded)
                {
                    X509Pal.Instance.DecodeX509KeyUsageExtension(RawData, out _keyUsages);
                    _decoded = true;
                }
                return _keyUsages;
            }
        }

        public override void CopyFrom(AsnEncodedData asnEncodedData)
        {
            base.CopyFrom(asnEncodedData);
            _decoded = false;
            return;
        }

        private bool _decoded = false;
        private X509KeyUsageFlags _keyUsages;
    }
}

