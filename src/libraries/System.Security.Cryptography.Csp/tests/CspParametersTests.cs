// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.Csp.Tests
{
    public static class CspParametersTests
    {
        [Fact]
        public static void DefaultProvider()
        {
            const int PROV_RSA_AES = 24;

            CspParameters cspParameters = new CspParameters();

            // An awful lot of work goes into this calculation in the product code,
            // but on all supported operating systems PROV_RSA_AES should be the
            // conclusion:
            Assert.Equal(PROV_RSA_AES, cspParameters.ProviderType);
        }

        [Fact]
        public static void SetFlags_ValidatesInput()
        {
            CspParameters cspParameters = new CspParameters();

            // Unmapped values (> 0xFF) throw
            Assert.Throws<ArgumentException>(() => cspParameters.Flags = (CspProviderFlags)0x0100);

            // Unmapped values (> 0xFF) throw, even when combined with known values.
            Assert.Throws<ArgumentException>(
                () => cspParameters.Flags = (CspProviderFlags)0x0100 | CspProviderFlags.NoPrompt);
        }

        [Fact]
        public static void KeyPassword()
        {
            var cspParameters = new CspParameters();
            var pwd = new SecureString();
            pwd.AppendChar('p');
            cspParameters.KeyPassword = pwd;
            Assert.Same(pwd, cspParameters.KeyPassword);
        }
    }
}
