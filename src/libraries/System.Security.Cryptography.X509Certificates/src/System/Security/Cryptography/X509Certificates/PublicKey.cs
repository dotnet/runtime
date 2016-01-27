// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class PublicKey
    {
        public PublicKey(Oid oid, AsnEncodedData parameters, AsnEncodedData keyValue)
        {
            _oid = new Oid(oid);
            EncodedParameters = new AsnEncodedData(parameters);
            EncodedKeyValue = new AsnEncodedData(keyValue);
        }

        public AsnEncodedData EncodedKeyValue { get; private set; }

        public AsnEncodedData EncodedParameters { get; private set; }

        public Oid Oid
        {
            get
            {
                return new Oid(_oid);
            }
        }

        private Oid _oid;
    }
}

