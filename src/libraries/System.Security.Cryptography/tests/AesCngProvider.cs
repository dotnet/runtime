// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Encryption.Aes.Tests
{
    using Aes = System.Security.Cryptography.Aes;

    public sealed class AesCngProvider : AesProvider
    {
        public static readonly AesCngProvider Instance = new AesCngProvider();

        private AesCngProvider() { }

        public override Aes Create() => new AesCng();
    }
}
