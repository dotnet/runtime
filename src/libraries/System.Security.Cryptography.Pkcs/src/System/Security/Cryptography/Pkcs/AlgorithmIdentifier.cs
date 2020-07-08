// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class AlgorithmIdentifier
    {
        public AlgorithmIdentifier()
            : this(Oids.TripleDesCbcOid.CopyOid(), 0)
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

        public byte[] Parameters { get; set; } = Array.Empty<byte>();
    }
}
