// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    public sealed partial class Shake128 : IDisposable
    {
        private static partial LiteXof CreateHashProvider()
        {
            return LiteHashProvider.CreateXof(HashAlgorithmNames.SHAKE128);
        }

        private static partial bool GetIsSupported() => HashProviderDispenser.HashSupported(HashAlgorithmNames.SHAKE128);
    }
}
