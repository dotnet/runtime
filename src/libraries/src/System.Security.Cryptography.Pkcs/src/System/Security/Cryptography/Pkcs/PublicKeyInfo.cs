// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class PublicKeyInfo
    {
        internal PublicKeyInfo(AlgorithmIdentifier algorithm, byte[] keyValue)
        {
            Debug.Assert(algorithm != null);
            Debug.Assert(keyValue != null);

            Algorithm = algorithm;
            KeyValue = keyValue;
        }

        public AlgorithmIdentifier Algorithm { get; }

        public byte[] KeyValue { get; }
    }
}


