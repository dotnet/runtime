// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Dsa.Tests
{
    public class DSACngProvider : IDSAProvider
    {
        public DSA Create()
        {
            return new DSACng();
        }

        public DSA Create(int keySize)
        {
            return new DSACng(keySize);
        }

        public bool SupportsFips186_3 => (!PlatformDetection.IsWindows7);
        public bool SupportsKeyGeneration => true;
    }

    public partial class DSAFactory
    {
        private static readonly IDSAProvider s_provider = new DSACngProvider();
    }
}
