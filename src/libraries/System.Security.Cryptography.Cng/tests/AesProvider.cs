// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Security.Cryptography.Encryption.Aes.Tests
{
    using Aes = System.Security.Cryptography.Aes;

    public class AesProvider : IAesProvider
    {
        public Aes Create()
        {
            return new AesCng();
        }
    }

    public partial class AesFactory
    {
        private static readonly IAesProvider s_provider = new AesProvider();
    }
}
