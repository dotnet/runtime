// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;

namespace System.Security.Cryptography.Dsa.Tests
{
    public class DefaultDSAProvider : DSAProvider
    {
        public static readonly DefaultDSAProvider Instance = new DefaultDSAProvider();

        public override DSA Create()
        {
            return DSA.Create();
        }

        public override DSA Create(int keySize)
        {
#if NET
            return DSA.Create(keySize);
#else
            DSA dsa = Create();
            dsa.KeySize = keySize;
            return dsa;
#endif
        }

        public override bool SupportsFips186_3
        {
            get
            {
                return PlatformSupport.IsDSASupported;
            }
        }
    }
}
