// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Encryption.Aes.Tests
{
    using Aes = System.Security.Cryptography.Aes;

    public class AesProvider : IAesProvider
    {
        public Aes Create()
        {
            return Aes.Create();
        }
    }

    public partial class AesFactory
    {
        private static readonly IAesProvider s_provider = new AesProvider();
    }
}
