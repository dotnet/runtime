// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Security.Cryptography.Encryption.Tests.Asymmetric
{
    public static class TrivialTests
    {
        [Fact]
        public static void TestKeySize()
        {
            using (Trivial s = new Trivial())
            {
                Assert.Equal(0, s.KeySize);

                // Testing KeySize.
                int[] validKeySizes = { 40, 104, 152, 808, 809, 810, 816, 824, 832 };
                for (int keySize = -10; keySize < 200 * 8; keySize++)
                {
                    if (validKeySizes.Contains(keySize))
                    {
                        s.KeySize = keySize;
                        Assert.Equal(keySize, s.KeySize);
                    }
                    else
                    {
                        Assert.Throws<CryptographicException>(() => s.KeySize = keySize);
                    }
                }

                const int UnusualKeySize = 37;
                Assert.Throws<CryptographicException>(() => s.KeySize = UnusualKeySize);
                s.SetKeySize(37);
                Assert.Equal(UnusualKeySize, s.KeySize);

                // The value is illegal.
                Assert.Throws<CryptographicException>(() => s.KeySize = s.KeySize);
            }
        }

        [Fact]
        public static void ValidKeySizeUsesProperty()
        {
            using (AsymmetricAlgorithm aa = new DoesNotSetLegalKeySizesField())
            {
                // If the implementation relies on validating against the field
                // this will result in an exception.
                aa.KeySize = 1048576;
            }
        }

#if netcoreapp
        [Fact]
        public static void ClearCallsDispose()
        {
            Trivial s = new Trivial();
            Assert.False(s.IsDisposed);
            s.Clear();
            Assert.True(s.IsDisposed);
        }
#endif

        [Fact]
        public static void TestInvalidAlgorithm()
        {
            var invalid = new Invalid();
            Assert.Throws<NullReferenceException>(() => invalid.LegalKeySizes);
        }

        private class Invalid : AsymmetricAlgorithm
        {
            // Valid algorithms must override LegalKeySizes
        }

        private class DoesNotSetLegalKeySizesField : AsymmetricAlgorithm
        {
            public DoesNotSetLegalKeySizesField()
            {
                // Verify the base class protected field default values.
                Assert.Null(LegalKeySizesValue);
                Assert.Equal(0, KeySizeValue);
            }

            public override KeySizes[] LegalKeySizes
            {
                get { return new[] { new KeySizes(1024, 1024 * 1024, 1024) }; }
            }
        }

        private class Trivial : AsymmetricAlgorithm
        {
            bool _disposed;

            public Trivial()
            {
                LegalKeySizesValue = new KeySizes[]
                    {
                        new KeySizes(5*8, -99*8, 0*8),
                        new KeySizes(13*8, 22*8, 6*8),
                        new KeySizes(101*8, 104*8, 1*8),
                        new KeySizes(101*8 + 1, 101*8 + 2, 1),
                    };
            }

            public void SetKeySize(int keySize)
            {
                KeySizeValue = keySize;
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (disposing)
                {
                    _disposed = true;
                }
            }

            public bool IsDisposed => _disposed;
        }
    }
}
