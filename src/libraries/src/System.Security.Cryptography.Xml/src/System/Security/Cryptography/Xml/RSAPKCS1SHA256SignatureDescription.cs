// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Xml
{
    internal class RSAPKCS1SHA256SignatureDescription : RSAPKCS1SignatureDescription
    {
        public RSAPKCS1SHA256SignatureDescription() : base("SHA256")
        {
        }

        public sealed override HashAlgorithm CreateDigest()
        {
            return SHA256.Create();
        }
    }
}
