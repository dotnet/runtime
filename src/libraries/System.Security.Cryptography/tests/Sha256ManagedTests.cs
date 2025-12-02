// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Tests
{
    /// <summary>
    /// Sha256Managed has a copy of the same implementation as SHA256
    /// </summary>
    public class Sha256ManagedTests : Sha256Tests<Sha256ManagedTests.Traits>
    {
        public sealed class Traits : IHashTrait
        {
            public static bool IsSupported => true;
            public static int HashSizeInBytes => SHA256.HashSizeInBytes;
            public static HashAlgorithm Create() => new SHA256Managed();
        }
    }
}
