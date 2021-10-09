// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Xml
{
    internal sealed class RSAPKCS1SHA512SignatureDescription : RSAPKCS1SignatureDescription
    {
        public RSAPKCS1SHA512SignatureDescription() : base("SHA512")
        {
        }

        public sealed override HashAlgorithm CreateDigest()
        {
            return SHA512.Create();
        }
    }
}
