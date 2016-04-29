// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class AlgorithmIdentifier
    {
        public AlgorithmIdentifier()
            : this(Oid.FromOidValue(Oids.TripleDesCbc, OidGroup.EncryptionAlgorithm), 0)
        {
        }

        public AlgorithmIdentifier(Oid oid)
            : this(oid, 0)
        {
        }

        public AlgorithmIdentifier(Oid oid, int keyLength)
        {
            Oid = oid;
            KeyLength = keyLength;
        }

        public Oid Oid { get; set; }

        public int KeyLength { get; set; }
    }
}

