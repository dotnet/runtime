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
    public sealed class PublicKey
    {
        public PublicKey(Oid oid, AsnEncodedData parameters, AsnEncodedData keyValue)
        {
            _oid = new Oid(oid);
            this.EncodedParameters = new AsnEncodedData(parameters);
            this.EncodedKeyValue = new AsnEncodedData(keyValue);
            return;
        }

        public AsnEncodedData EncodedKeyValue { get; private set; }

        public AsnEncodedData EncodedParameters { get; private set; }

        public AsymmetricAlgorithm Key
        {
            get
            {
                AsymmetricAlgorithm key = _lazyKey;
                if (key == null)
                    key = _lazyKey = X509Pal.Instance.DecodePublicKey(this.Oid, this.EncodedKeyValue.RawData, this.EncodedParameters.RawData);
                return key;
            }
        }

        public Oid Oid
        {
            get
            {
                return new Oid(_oid);
            }
        }

        private Oid _oid;
        private AsymmetricAlgorithm _lazyKey;
    }
}

