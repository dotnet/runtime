// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Dsa.Tests
{
    public class DSAOpenSslProvider : DSAProvider
    {
        public static readonly DSAOpenSslProvider Instance = new DSAOpenSslProvider();

        private DSAOpenSslProvider() { }

        public override DSA Create()
        {
            return new DSAOpenSsl();
        }

        public override DSA Create(int keySize)
        {
            return new DSAOpenSsl(keySize);
        }

        public override bool SupportsFips186_3 => true;
    }
}
