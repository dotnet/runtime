// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.Csp.Tests
{
    public static class CspParametersTests
    {
        const int PROV_RSA_FULL = 1;
        const int PROV_RSA_AES = 24;

        [Fact]
        public static void DefaultProvider()
        {
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
            AssertExtensions.Throws<ArgumentException>(null, "value", () => cspParameters.Flags = (CspProviderFlags)0x0100);

            // Unmapped values (> 0xFF) throw, even when combined with known values.
            AssertExtensions.Throws<ArgumentException>(null, "value", () => cspParameters.Flags = (CspProviderFlags)0x0100 | CspProviderFlags.NoPrompt);
        }

        [Fact]
        public static void KeyPassword_SetGet()
        {
            var cspParameters = new CspParameters();
            using (var pwd = new SecureString())
            {
                pwd.AppendChar('p');
                cspParameters.KeyPassword = pwd;
                Assert.Same(pwd, cspParameters.KeyPassword);
            }
        }

        //Manual test - requires Smart Card - read instructions
        //[Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void KeyPassword_SmartCard_Manual_Test(bool correctPassword)
        {
            // Find info about your smart card:
            // > certutil -scinfo -silent
            //   You should see something like:
            //   Provider = Microsoft Base Smart Card Crypto Provider
            //   Key Container = {123ABCDE-1234-ABCD-1234-ABCD1234ABCD}
            // Fill consts below
            // Run this test - enter correct/incorrect password for your smart card when asked
            const string provider = "Microsoft Base Smart Card Crypto Provider";
            const string container = "{123ABCDE-1234-ABCD-1234-ABCD1234ABCD}";

            Console.Write($"Enter {(correctPassword ? "correct" : "incorrect")} password: ");

            using (var pwd = ReadPassword())
            {
                var cspParameters = new CspParameters(1, provider, container)
                {
                    KeyNumber = (int)KeyNumber.Exchange,
                    Flags = CspProviderFlags.UseExistingKey | CspProviderFlags.UseMachineKeyStore,
                    KeyPassword = pwd
                };

                Action sign = () =>
                {
                    using (var rsa = new RSACryptoServiceProvider(cspParameters))
                    {
                        var signed = rsa.SignData(new byte[3] { 1, 2, 3 }, "sha256");
                        Console.WriteLine(Convert.ToBase64String(signed));
                    }
                };

                if (correctPassword)
                    sign();
                else
                    Assert.ThrowsAny<CryptographicException>(sign);
            }
        }

        private static SecureString ReadPassword()
        {
            var ret = new SecureString();

            while (true)
            {
                ConsoleKeyInfo c = Console.ReadKey(true);
                if (c.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                else if (c.Key == ConsoleKey.Backspace)
                {
                    if (ret.Length > 0)
                    {
                        Console.Write("\b \b");
                        ret.RemoveAt(ret.Length - 1);
                    }
                }
                else
                {
                    Console.Write("*");
                    ret.AppendChar(c.KeyChar);
                }
            }

            return ret;
        }
    }
}
