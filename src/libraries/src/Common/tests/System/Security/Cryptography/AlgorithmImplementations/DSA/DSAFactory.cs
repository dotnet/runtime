// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Dsa.Tests
{
    public interface IDSAProvider
    {
        DSA Create();
        DSA Create(int keySize);
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
    }
}
