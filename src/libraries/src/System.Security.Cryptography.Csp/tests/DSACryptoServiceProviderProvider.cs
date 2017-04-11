// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Dsa.Tests
{
    public class DSACryptoServiceProviderProvider : IDSAProvider
    {
        public DSA Create()
        {
            return new DSACryptoServiceProvider();
        }

        public DSA Create(int keySize)
        {
            return new DSACryptoServiceProvider(keySize);
        }

        public bool SupportsFips186_3 => false;
        public bool SupportsKeyGeneration => !PlatformDetection.IsOSX;
    }

    public partial class DSAFactory
    {
        private static readonly IDSAProvider s_provider = new DSACryptoServiceProviderProvider();
    }
}
