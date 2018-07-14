// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Pkcs
{
    public sealed class Pkcs12KeyBag : Pkcs12SafeBag
    {
        public Pkcs12KeyBag(ReadOnlyMemory<byte> pkcs8PrivateKey, bool skipCopy=false)
            : base(Oids.Pkcs12KeyBag, pkcs8PrivateKey, skipCopy)
        {
        }

        public ReadOnlyMemory<byte> Pkcs8PrivateKey => EncodedBagValue;
    }
}
