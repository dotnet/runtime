// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Tests
{
    /// <summary>
    /// Sha1Managed has a copy of the same implementation as SHA1
    /// </summary>
    public class Sha1ManagedTests : Sha1Tests<Sha1ManagedTests.Traits>
    {
        public sealed class Traits : IHashTrait
        {
            public static bool IsSupported => true;
            public static int HashSizeInBytes => SHA1.HashSizeInBytes;
            public static HashAlgorithm Create() => new SHA1Managed();
        }
    }
}
