// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Dsa.Tests
{
    public interface IDSAProvider
    {
        DSA Create();
        DSA Create(int keySize);
        bool SupportsFips186_3 { get; }
        bool SupportsKeyGeneration { get; }
    }

    public static partial class DSAFactory
    {
        public static DSA Create()
        {
            return s_provider.Create();
        }

        public static DSA Create(int keySize)
        {
            return s_provider.Create(keySize);
        }

        public static DSA Create(in DSAParameters dsaParameters)
        {
            DSA dsa = s_provider.Create();
            dsa.ImportParameters(dsaParameters);
            return dsa;
        }

        /// <summary>
        /// If false, 186-2 is assumed which implies key size of 1024 or less and only SHA-1
        /// If true, 186-3 includes support for keysizes >1024 and SHA-2 algorithms
        /// </summary>
        public static bool SupportsFips186_3 => s_provider.SupportsFips186_3;

        public static bool SupportsKeyGeneration => s_provider.SupportsKeyGeneration;
    }
}
