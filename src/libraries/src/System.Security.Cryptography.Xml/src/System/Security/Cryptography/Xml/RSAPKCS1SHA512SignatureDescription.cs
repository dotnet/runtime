// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Xml
{
    internal class RSAPKCS1SHA512SignatureDescription : RSAPKCS1SignatureDescription
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
