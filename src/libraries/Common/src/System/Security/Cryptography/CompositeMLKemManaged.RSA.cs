// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;

namespace System.Security.Cryptography
{
    internal sealed partial class CompositeMLKemManaged
    {
        private sealed class RsaKem : TraditionalKem
#if DESIGNTIMEINTERFACES
#pragma warning disable SA1001 // Commas should be spaced correctly
            , ITraditionalKemFactory<RsaKem, RsaKemAlgorithm>
#pragma warning restore SA1001 // Commas should be spaced correctly
#endif
        {
            private readonly RsaKemAlgorithm _algorithm;
            private RSA _rsa;

            private RsaKem(RSA rsa, RsaKemAlgorithm algorithm)
            {
                _rsa = rsa;
                _algorithm = algorithm;
            }

            public static bool IsAlgorithmSupported(RsaKemAlgorithm _) => true;

            public static RsaKem GenerateKey(RsaKemAlgorithm algorithm)
            {
                RSA? rsa = null;

                try
                {
                    rsa = RSA.Create(algorithm.KeySizeInBits);

                    // RSA key generation is lazy, so force it to happen eagerly
                    // so that key generation failures happen in GenerateKey rather than
                    // the first method to use the key.
                    _ = rsa.ExportParameters(includePrivateParameters: false);

                    return new RsaKem(rsa, algorithm);
                }
                catch
                {
                    rsa?.Dispose();
                    throw;
                }
            }

            public static RsaKem ImportPublicKey(RsaKemAlgorithm algorithm, ReadOnlySpan<byte> source)
            {
                RSA rsa = RSAKeyFormatHelper.FromPkcs1PublicKey(source, CreateRSA, AsnEncodingRules.DER);

                if (rsa.KeySize != algorithm.KeySizeInBits)
                {
                    rsa.Dispose();
                    throw new CryptographicException(SR.Argument_PublicKeyWrongSizeForAlgorithm);
                }

                return new RsaKem(rsa, algorithm);
            }

            public static RsaKem ImportPrivateKey(RsaKemAlgorithm algorithm, ReadOnlySpan<byte> source)
            {
                RSA rsa = RSAKeyFormatHelper.FromPkcs1PrivateKey(source, CreateRSA, ruleSet: AsnEncodingRules.DER);

                if (rsa.KeySize != algorithm.KeySizeInBits)
                {
                    rsa.Dispose();
                    throw new CryptographicException(SR.Argument_PrivateKeyWrongSizeForAlgorithm);
                }

                return new RsaKem(rsa, algorithm);
            }

            internal override void Encapsulate(Span<byte> ciphertext, Span<byte> sharedSecret)
            {
                // draft-ietf-lamps-pq-composite-kem-19, 2.1
                // RSAOAEPKEM.Encaps(pkR):
                //   shared_secret = SecureRandom(ss_len)
                //   enc = RSAES-OAEP-ENCRYPT(pkR, shared_secret)
                //
                //   return shared_secret, enc

                Debug.Assert(sharedSecret.Length == RsaKemAlgorithm.SecretSizeInBytes);
                Debug.Assert(ciphertext.Length == _algorithm.KeySizeInBits / 8);

                // The caller clears sharedSecret, even when this method throws, so no cleanup is needed here.
                RandomNumberGenerator.Fill(sharedSecret);

                if (!_rsa.TryEncrypt(sharedSecret, ciphertext, RSAEncryptionPadding.OaepSHA256, out int bytesWritten) ||
                    bytesWritten != ciphertext.Length)
                {
                    Debug.Fail("RSA encryption produced an unexpected ciphertext length.");
                    throw new CryptographicException();
                }
            }

            internal override void Decapsulate(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret)
            {
                // draft-ietf-lamps-pq-composite-kem-19, 2.1
                // RSAOAEPKEM.Decaps(skR, enc):
                //   shared_secret = RSAES-OAEP-DECRYPT(skR, enc)
                //
                //   return shared_secret

                Debug.Assert(sharedSecret.Length == RsaKemAlgorithm.SecretSizeInBytes);
                Debug.Assert(ciphertext.Length == _algorithm.KeySizeInBits / 8);

                // The caller clears sharedSecret, even when this method throws, so no cleanup is needed here.
                if (!_rsa.TryDecrypt(ciphertext, sharedSecret, RSAEncryptionPadding.OaepSHA256, out int bytesWritten) ||
                    bytesWritten != sharedSecret.Length)
                {
                    throw new CryptographicException(SR.Cryptography_CompositeKemRsaDecapsulationFailed);
                }
            }

            internal override int ExportPublicKey(Span<byte> destination)
            {
                if (!_rsa.TryExportRSAPublicKey(destination, out int bytesWritten))
                {
                    Debug.Fail("The RSA public key destination was unexpectedly too small.");
                    throw new CryptographicException();
                }

                return bytesWritten;
            }

            internal override int ExportPrivateKey(Span<byte> destination)
            {
                if (!_rsa.TryExportRSAPrivateKey(destination, out int bytesWritten))
                {
                    Debug.Fail("The RSA private key destination was unexpectedly too small.");
                    throw new CryptographicException();
                }

                return bytesWritten;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _rsa?.Dispose();
                    _rsa = null!;
                }

                base.Dispose(disposing);
            }

            private static RSA CreateRSA(RSAParameters parameters)
            {
                RSA rsa = RSA.Create();

                try
                {
                    rsa.ImportParameters(parameters);
                    return rsa;
                }
                catch
                {
                    rsa.Dispose();
                    throw;
                }
            }
        }
    }
}
