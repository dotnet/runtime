// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography.Pkcs
{
    public class Pkcs9AttributeObject : AsnEncodedData
    {
        //
        // Constructors.
        //

        public Pkcs9AttributeObject()
            : base()
        {
        }

        public Pkcs9AttributeObject(string oid, byte[] encodedData)
            : this(new AsnEncodedData(oid, encodedData))
        {
        }

        public Pkcs9AttributeObject(Oid oid, byte[] encodedData)
            : this(new AsnEncodedData(oid, encodedData))
        {
        }

        public Pkcs9AttributeObject(AsnEncodedData asnEncodedData)
            : base(asnEncodedData)
        {
            if (asnEncodedData.Oid == null)
                throw new ArgumentNullException(nameof(asnEncodedData.Oid));
            string szOid = base.Oid.Value;
            if (szOid == null)
                throw new ArgumentNullException("oid.Value");
            if (szOid.Length == 0)
                throw new ArgumentException(SR.Arg_EmptyOrNullString, "oid.Value");
        }

        internal Pkcs9AttributeObject(Oid oid)
        {
            base.Oid = oid;
        }

        //
        // Public properties.
        //

        public new Oid Oid
        {
            get
            {
                return base.Oid;
            }
        }

        public override void CopyFrom(AsnEncodedData asnEncodedData)
        {
            if (asnEncodedData == null)
                throw new ArgumentNullException(nameof(asnEncodedData));
            if (!(asnEncodedData is Pkcs9AttributeObject))
                throw new ArgumentException(SR.Cryptography_Pkcs9_AttributeMismatch);

            base.CopyFrom(asnEncodedData);
        }
    }
}
