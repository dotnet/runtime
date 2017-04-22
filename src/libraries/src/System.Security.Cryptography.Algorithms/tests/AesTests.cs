// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.Algorithms.Tests
{
    public partial class AesTests
    {
        [Fact]
        public static void AesDefaultCtor()
        {
            using (Aes aes = new AesMinimal())
            {
                Assert.Equal(256, aes.KeySize);
                Assert.Equal(128, aes.BlockSize);
                Assert.Equal(CipherMode.CBC, aes.Mode);
                Assert.Equal(PaddingMode.PKCS7, aes.Padding);
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Causing other tests to intermittently fail on netfx (https://github.com/dotnet/corefx/issues/18690)")]
        public static void EnsureLegalSizesValuesIsolated()
        {
            new AesLegalSizesBreaker().Dispose();

            using (Aes aes = Aes.Create())
            {
                Assert.Equal(128, aes.LegalKeySizes[0].MinSize);
                Assert.Equal(128, aes.LegalBlockSizes[0].MinSize);

                aes.Key = new byte[16];
            }
        }

        private class AesLegalSizesBreaker : AesMinimal
        {
            public AesLegalSizesBreaker()
            {
                LegalKeySizesValue[0] = new KeySizes(1, 1, 0);
                LegalBlockSizesValue[0] = new KeySizes(1, 1, 0);
            }
        }

        private class AesMinimal : Aes
        {
            // If the constructor uses a virtual call to any of the property setters
            // they will fail.
            private readonly bool _ready;

            public AesMinimal()
            {
                // Don't set this as a field initializer, otherwise it runs before the base ctor.
                _ready = true;
            }

            public override int KeySize
            {
                set
                {
                    if (!_ready)
                    {
                        throw new InvalidOperationException();
                    }

                    base.KeySize = value;
                }
            }

            public override int BlockSize
            {
                set
                {
                    if (!_ready)
                    {
                        throw new InvalidOperationException();
                    }

                    base.BlockSize = value;
                }
            }

            public override byte[] IV
            {
                set
                {
                    if (!_ready)
                    {
                        throw new InvalidOperationException();
                    }

                    base.IV = value;
                }
            }

            public override byte[] Key
            {
                set
                {
                    if (!_ready)
                    {
                        throw new InvalidOperationException();
                    }

                    base.Key = value;
                }
            }

            public override CipherMode Mode
            {
                set
                {
                    if (!_ready)
                    {
                        throw new InvalidOperationException();
                    }

                    base.Mode = value;
                }
            }

            public override PaddingMode Padding
            {
                set
                {
                    if (!_ready)
                    {
                        throw new InvalidOperationException();
                    }

                    base.Padding = value;
                }
            }

            public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
            {
                throw new NotImplementedException();
            }

            public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
            {
                throw new NotImplementedException();
            }

            public override void GenerateIV()
            {
                throw new NotImplementedException();
            }

            public override void GenerateKey()
            {
                throw new NotImplementedException();
            }
        }
    }
}
