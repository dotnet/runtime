// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class Pkcs12SafeContentsBag : Pkcs12SafeBag
    {
        public Pkcs12SafeContents SafeContents { get; private set; }

        private Pkcs12SafeContentsBag(ReadOnlyMemory<byte> encoded)
            : base(Oids.Pkcs12SafeContentsBag, encoded)
        {
        }

        internal static Pkcs12SafeContentsBag Create(Pkcs12SafeContents copyFrom)
        {
            Debug.Assert(copyFrom != null);
            Debug.Assert(copyFrom.ConfidentialityMode == Pkcs12ConfidentialityMode.None);

            using (AsnWriter writer = copyFrom.Encode())
            {
                return Decode(writer.Encode());
            }
        }

        internal static Pkcs12SafeContentsBag Decode(ReadOnlyMemory<byte> encodedValue)
        {
            Pkcs12SafeContents contents = new Pkcs12SafeContents(encodedValue);

            return new Pkcs12SafeContentsBag(encodedValue)
            {
                SafeContents = contents
            };
        }
    }
}
