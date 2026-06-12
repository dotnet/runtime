// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Dsa.Tests
{
    public class DSACngProvider : DSAProvider
    {
        public static readonly DSACngProvider Instance = new DSACngProvider();

        public override DSA Create()
        {
            return new DSACng();
        }

        public override DSA Create(int keySize)
        {
            return new DSACng(keySize);
        }

        public override bool SupportsFips186_3 => true;
    }
}
