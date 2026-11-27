// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Dsa.Tests
{
    public class DSACryptoServiceProviderProvider : DSAProvider
    {
        public static readonly DSACryptoServiceProviderProvider Instance = new DSACryptoServiceProviderProvider();

        private DSACryptoServiceProviderProvider() { }

        public override DSA Create()
        {
            return new DSACryptoServiceProvider();
        }

        public override DSA Create(int keySize)
        {
            return new DSACryptoServiceProvider(keySize);
        }

        public override bool SupportsFips186_3 => false;
    }
}
