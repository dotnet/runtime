// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    public sealed partial class Shake128 : IDisposable
    {
        private static partial LiteXof CreateHashProvider()
        {
            // Windows doesn't expose SHAKE. It exposes cSHAKE. But SHAKE is just cSHAKE with an empty
            // customization string (S) and and function name (N). (See FIPS 180-185 Paragraph 3.2)
            // In Windows terms, SHAKE is cSHAKE with an empty BCRYPT_FUNCTION_NAME_STRING and
            // BCRYPT_CUSTOMIZATION_STRING.
            // So for SHAKE, we create a cSHAKE instance and don't specify S or N.
            return LiteHashProvider.CreateXof(HashAlgorithmNames.CSHAKE128);
        }

        private static partial bool GetIsSupported() => HashProviderDispenser.HashSupported(HashAlgorithmNames.CSHAKE128);
    }
}
