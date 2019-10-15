// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.Encryption.Des.Tests
{
    public static partial class DesTests
    {
        [Fact]
        public static void EnsureLegalSizesValuesIsolated()
        {
            new DESLegalSizesBreaker().Dispose();

            using (DES des = DES.Create())
            {
                Assert.Equal(64, des.LegalKeySizes[0].MinSize);
                Assert.Equal(64, des.LegalBlockSizes[0].MinSize);
            }
        }

        private class DESLegalSizesBreaker : DESMinimal
        {
            public DESLegalSizesBreaker()
            {
                LegalKeySizesValue[0] = new KeySizes(1, 1, 0);
                LegalBlockSizesValue[0] = new KeySizes(1, 1, 0);
            }
        }

        private class DESMinimal : DES
        {
            // If the constructor uses a virtual call to any of the property setters
            // they will fail.
            private readonly bool _ready;

            public DESMinimal()
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
