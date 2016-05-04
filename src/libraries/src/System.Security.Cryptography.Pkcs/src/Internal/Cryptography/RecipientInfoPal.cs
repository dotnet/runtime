// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Diagnostics;
using System.Security.Cryptography.Pkcs;

namespace Internal.Cryptography
{
    internal abstract class RecipientInfoPal
    {
        internal RecipientInfoPal()
        {
        }

        public abstract byte[] EncryptedKey { get; }
        public abstract AlgorithmIdentifier KeyEncryptionAlgorithm { get; }
        public abstract SubjectIdentifier RecipientIdentifier { get; }
        public abstract int Version { get; }
    }
}

