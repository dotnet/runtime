// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.Encryption.RC2.Tests
{
    using RC2 = System.Security.Cryptography.RC2;

    public static partial class RC2Tests
    {
        [Fact]
        public static void RC2KeySize()
        {
            using (RC2 rc2 = RC2.Create())
            {
                rc2.KeySize = 40;
                Assert.Equal(40 / 8, rc2.Key.Length);
                Assert.Equal(40, rc2.KeySize);

                rc2.KeySize = 1024;
                Assert.Equal(1024 / 8, rc2.Key.Length);
                Assert.Equal(1024, rc2.KeySize);

                Assert.Throws<CryptographicException>(() => rc2.KeySize = 40 - 8);
                Assert.Throws<CryptographicException>(() => rc2.KeySize = 1024 + 8);
            }
        }

        [Fact]
        public static void RC2EffectiveKeySize_BaseClass()
        {
            using (RC2 rc2 = new RC2Minimal())
            {
                rc2.KeySize = 40;
                Assert.Equal(40, rc2.EffectiveKeySize);
                rc2.EffectiveKeySize = 40;

                // Setting to 0 uses KeySize
                rc2.EffectiveKeySize = 0;
                Assert.Equal(40, rc2.EffectiveKeySize);

                Assert.Throws<CryptographicException>(() => rc2.EffectiveKeySize = 40 - 8);
                Assert.Throws<CryptographicException>(() => rc2.EffectiveKeySize = 40 + 8);
                Assert.Throws<CryptographicException>(() => rc2.EffectiveKeySize = 35);
                Assert.Throws<CryptographicException>(() => rc2.EffectiveKeySize = 41);
            }
        }

        [Fact]
        public static void EnsureLegalSizesValuesIsolated()
        {
            new RC2LegalSizesBreaker().Dispose();

            using (RC2 rc2 = RC2.Create())
            {
                Assert.Equal(40, rc2.LegalKeySizes[0].MinSize);
                Assert.Equal(64, rc2.LegalBlockSizes[0].MinSize);

                rc2.Key = new byte[8];
            }
        }

        private class RC2LegalSizesBreaker : RC2Minimal
        {
            public RC2LegalSizesBreaker()
            {
                LegalKeySizesValue[0] = new KeySizes(1, 1, 0);
                LegalBlockSizesValue[0] = new KeySizes(1, 1, 0);
            }
        }

        private class RC2Minimal : RC2
        {
            // If the constructor uses a virtual call to any of the property setters
            // they will fail.
            private readonly bool _ready;

            public RC2Minimal()
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
