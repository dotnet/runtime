// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    public sealed partial class ECDiffieHellmanCng : ECDiffieHellman
    {
        public void FromXmlString(string xml, ECKeyXmlFormat format)
        {
            throw new PlatformNotSupportedException();
        }

        public string ToXmlString(ECKeyXmlFormat format)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
