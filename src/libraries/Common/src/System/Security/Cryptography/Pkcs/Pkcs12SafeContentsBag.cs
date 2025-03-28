// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;

namespace System.Security.Cryptography.Pkcs
{
#if BUILDING_PKCS
    public
#else
    #pragma warning disable CA1510, CA1512
    internal
#endif
    sealed class Pkcs12SafeContentsBag : Pkcs12SafeBag
    {
        public Pkcs12SafeContents? SafeContents { get; private set; }

        private Pkcs12SafeContentsBag(ReadOnlyMemory<byte> encoded)
            : base(Oids.Pkcs12SafeContentsBag, encoded)
        {
        }

        internal static Pkcs12SafeContentsBag Create(Pkcs12SafeContents copyFrom)
        {
            Debug.Assert(copyFrom != null);
            Debug.Assert(copyFrom.ConfidentialityMode == Pkcs12ConfidentialityMode.None);

            AsnWriter writer = copyFrom.Encode();
            return Decode(writer.Encode());
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
