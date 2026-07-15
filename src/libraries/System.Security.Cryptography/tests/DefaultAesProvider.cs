// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Encryption.Aes.Tests
{
    using Aes = System.Security.Cryptography.Aes;

    public sealed class DefaultAesProvider : AesProvider
    {
        public static readonly DefaultAesProvider Instance = new DefaultAesProvider();

        private DefaultAesProvider() { }

        public override Aes Create() => Aes.Create();
    }
}
