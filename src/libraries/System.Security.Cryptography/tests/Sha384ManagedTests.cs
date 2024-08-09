// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Tests
{
    /// <summary>
    /// Sha384Managed has a copy of the same implementation as SHA384
    /// </summary>
    public class Sha384ManagedTests : Sha384Tests<Sha384ManagedTests.Traits>
    {
        public sealed class Traits : IHashTrait
        {
            public static bool IsSupported => true;
            public static int HashSizeInBytes => SHA384.HashSizeInBytes;
            public static HashAlgorithm Create() => new SHA384Managed();
        }
    }
}
