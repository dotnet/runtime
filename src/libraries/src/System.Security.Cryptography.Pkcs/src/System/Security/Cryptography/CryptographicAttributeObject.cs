// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    public sealed class CryptographicAttributeObject
    {
        //
        // Constructors.
        //
        public CryptographicAttributeObject(Oid oid)
            : this(oid, new AsnEncodedDataCollection())
        {
        }

        public CryptographicAttributeObject(Oid oid, AsnEncodedDataCollection values)
        {
            _oid = new Oid(oid);
            if (values == null)
            {
                Values = new AsnEncodedDataCollection();
            }
            else
            {
                foreach (AsnEncodedData asn in values)
                {
                    if (!string.Equals(asn.Oid.Value, oid.Value, StringComparison.Ordinal))
                        throw new InvalidOperationException(SR.Format(SR.InvalidOperation_WrongOidInAsnCollection, oid.Value, asn.Oid.Value));
                }
                Values = values;
            }
        }

        //
        // Public properties.
        //

        public Oid Oid
        {
            get
            {
                return new Oid(_oid);
            }
        }

        public AsnEncodedDataCollection Values { get; }
        private readonly Oid _oid;
    }
}
