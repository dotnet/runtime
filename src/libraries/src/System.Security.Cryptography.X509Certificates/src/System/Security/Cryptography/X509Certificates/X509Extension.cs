// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    public class X509Extension : AsnEncodedData
    {
        protected X509Extension()
            : base()
        {
        }

        public X509Extension(AsnEncodedData encodedExtension, bool critical)
            : this(encodedExtension.Oid, encodedExtension.RawData, critical)
        {
        }

        public X509Extension(Oid oid, byte[] rawData, bool critical)
            : base(oid, rawData)
        {
            if (base.Oid == null || base.Oid.Value == null)
                throw new ArgumentNullException("oid");
            if (base.Oid.Value.Length == 0)
                throw new ArgumentException(SR.Arg_EmptyOrNullString, "oid.Value");
            Critical = critical;
        }

        public X509Extension(String oid, byte[] rawData, bool critical)
            : this(new Oid(oid), rawData, critical)
        {
        }

        public bool Critical { get; set; }

        public override void CopyFrom(AsnEncodedData asnEncodedData)
        {
            if (asnEncodedData == null)
                throw new ArgumentNullException("asnEncodedData");

            X509Extension extension = asnEncodedData as X509Extension;
            if (extension == null)
                throw new ArgumentException(SR.Cryptography_X509_ExtensionMismatch);
            base.CopyFrom(asnEncodedData);
            Critical = extension.Critical;
        }

        internal X509Extension(String oidValue)
        {
            base.Oid = Oid.FromOidValue(oidValue, OidGroup.ExtensionOrAttribute);
            return;
        }
    }
}

