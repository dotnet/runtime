// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Encryption.Aes.Tests
{
    using Aes = System.Security.Cryptography.Aes;

    public interface IAesProvider
    {
        Aes Create();
    }

    public static partial class AesFactory
    {
        public static Aes Create()
        {
            return s_provider.Create();
        }
    }
}
