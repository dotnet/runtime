// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Dsa.Tests
{
    public class DefaultDSAProvider : IDSAProvider
    {
        public DSA Create()
        {
            return DSA.Create();
        }

        public DSA Create(int keySize)
        {
#if netcoreapp
            return DSA.Create(keySize);
#else
            DSA dsa = Create();
            dsa.KeySize = keySize;
            return dsa;
#endif
        }

        public bool SupportsFips186_3
        {
            get
            {
                return !(PlatformDetection.IsWindows7 || PlatformDetection.IsOSX);
            }
        }

        public bool SupportsKeyGeneration => !PlatformDetection.IsOSX;
    }

    public partial class DSAFactory
    {
        private static readonly IDSAProvider s_provider = new DefaultDSAProvider();
    }
}
