// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
