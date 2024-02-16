// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Internal.Cryptography;

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

        public CryptographicAttributeObject(Oid oid, AsnEncodedDataCollection? values)
        {
            _oid = oid.CopyOid();

            if (values == null)
            {
                Values = new AsnEncodedDataCollection();
            }
            else
            {
                foreach (AsnEncodedData asn in values)
                {
                    if (asn.Oid is null)
                    {
                        throw new ArgumentException(SR.Argument_InvalidOidValue, nameof(values));
                    }

                    if (!string.Equals(asn.Oid.Value, oid.Value, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(SR.Format(SR.InvalidOperation_WrongOidInAsnCollection, oid.Value, asn.Oid.Value));
                    }
                }
                Values = values;
            }
        }

        //
        // Public properties.
        //

        public Oid Oid => _oid.CopyOid();

        public AsnEncodedDataCollection Values { get; }
        private readonly Oid _oid;
    }
}
