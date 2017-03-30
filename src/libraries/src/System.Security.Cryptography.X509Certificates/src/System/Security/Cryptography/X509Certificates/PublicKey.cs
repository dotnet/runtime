// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;
using Internal.Cryptography.Pal;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class PublicKey
    {
        private Oid _oid;
        private AsymmetricAlgorithm _key = null;

        public PublicKey(Oid oid, AsnEncodedData parameters, AsnEncodedData keyValue)
        {
            _oid = new Oid(oid);
            EncodedParameters = new AsnEncodedData(parameters);
            EncodedKeyValue = new AsnEncodedData(keyValue);
        }

        public AsnEncodedData EncodedKeyValue { get; private set; }

        public AsnEncodedData EncodedParameters { get; private set; }

        public AsymmetricAlgorithm Key
        {
            get
            {
                if (_key == null)
                {
                    switch (_oid.Value)
                    {
                        case Oids.RsaRsa:
                        case Oids.DsaDsa:
                            _key = X509Pal.Instance.DecodePublicKey(_oid, EncodedKeyValue.RawData, EncodedParameters.RawData, null);
                            break;

                        default:
                            // This includes ECDSA, because an Oids.Ecc key can be
                            // many different algorithm kinds, not necessarily with mutual exclusion.
                            //
                            // Plus, .NET Framework only supports RSA and DSA in this property.
                            throw new NotSupportedException(SR.NotSupported_KeyAlgorithm);
                    }
                }

                return _key;
            }
        }

        public Oid Oid
        {
            get
            {
                return new Oid(_oid);
            }
        }
    }
}
