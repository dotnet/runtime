// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;

namespace System.Security.Cryptography.Dsa.Tests
{
    public abstract class DSAProvider
    {
        public abstract DSA Create();
        public abstract DSA Create(int keySize);
        public abstract bool SupportsFips186_3 { get; }

        public DSA Create(in DSAParameters dsaParameters)
        {
            DSA dsa = Create();
            dsa.ImportParameters(dsaParameters);
            return dsa;
        }

        public void ThrowSkipTestExceptionIfFips186_3IsNotSupported()
        {
            if (!SupportsFips186_3)
            {
                throw new SkipTestException("Platform does not support FIPS 186-3.");
            }
        }
    }
}
