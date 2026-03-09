// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;

namespace System.Security.Cryptography
{
    internal sealed partial class CompositeMLDsaManaged
    {
        private sealed class RsaComponent : ComponentAlgorithm
#if DESIGNTIMEINTERFACES
#pragma warning disable SA1001 // Commas should be spaced correctly
            , IComponentAlgorithmFactory<RsaComponent, RsaAlgorithm>
#pragma warning restore SA1001 // Commas should be spaced correctly
#endif
        {
            private readonly HashAlgorithmName _hashAlgorithmName;
            private readonly RSASignaturePadding _padding;

            private RSA _rsa;

            private RsaComponent(RSA rsa, HashAlgorithmName hashAlgorithmName, RSASignaturePadding padding)
            {
                Debug.Assert(rsa is not null);
                Debug.Assert(padding is not null);

                _rsa = rsa;
                _hashAlgorithmName = hashAlgorithmName;
                _padding = padding;
            }

            public static bool IsAlgorithmSupported(RsaAlgorithm _) => true;

#if NETFRAMEWORK
            // RSA-PSS requires RSACng on .NET Framework
            private static RSACng CreateRSA() => new RSACng();
            private static RSACng CreateRSA(int keySizeInBits) => new RSACng(keySizeInBits);
#elif NETSTANDARD2_0
            private static RSA CreateRSA() => RSA.Create();

            private static RSA CreateRSA(int keySizeInBits)
            {
                RSA rsa = RSA.Create();

                try
                {
                    rsa.KeySize = keySizeInBits;
                    return rsa;
                }
                catch
                {
                    rsa.Dispose();
                    throw;
                }
            }
#else
            private static RSA CreateRSA() => RSA.Create();
            private static RSA CreateRSA(int keySizeInBits) => RSA.Create(keySizeInBits);
#endif

            internal override int SignData(
#if NET
                ReadOnlySpan<byte> data,
#else
                byte[] data,
#endif
                Span<byte> destination)
            {
#if NET
                return _rsa.SignData(data, destination, _hashAlgorithmName, _padding);
#else
                // Composite ML-DSA virtual methods only accept ROS<byte> so we need to allocate for signature
                byte[] signature = _rsa.SignData(data, _hashAlgorithmName, _padding);

                if (signature.AsSpan().TryCopyTo(destination))
                {
                    return signature.Length;
                }

                CryptographicOperations.ZeroMemory(destination);

                throw new CryptographicException();
#endif
            }

            internal override bool VerifyData(
#if NET
                ReadOnlySpan<byte> data,
#else
                byte[] data,
#endif
                ReadOnlySpan<byte> signature)
            {
#if NET
                return _rsa.VerifyData(data, signature, _hashAlgorithmName, _padding);
#else
                // Composite ML-DSA virtual methods only accept ROS<byte> so we need to use ToArray() for signature
                return _rsa.VerifyData(data, signature.ToArray(), _hashAlgorithmName, _padding);
#endif
            }

            public static RsaComponent GenerateKey(RsaAlgorithm algorithm)
            {
                RSA? rsa = null;

                try
                {
                    rsa = CreateRSA(algorithm.KeySizeInBits);

                    // RSA key generation is lazy, so we need to force it to happen eagerly.
                    _ = rsa.ExportParameters(includePrivateParameters: false);

                    return new RsaComponent(rsa, algorithm.HashAlgorithmName, algorithm.Padding);
                }
                catch (CryptographicException)
                {
                    rsa?.Dispose();
                    throw;
                }
            }

            public static RsaComponent ImportPrivateKey(RsaAlgorithm algorithm, ReadOnlySpan<byte> source)
            {
                Debug.Assert(IsAlgorithmSupported(algorithm));

                RSA? rsa = null;

                try
                {
                    int bytesRead;
                    rsa = CreateRSA();

#if NET
                    rsa.ImportRSAPrivateKey(source, out bytesRead);
#else
                    try
                    {
                        AsnDecoder.ReadEncodedValue(
                            source,
                            AsnEncodingRules.BER,
                            out _,
                            out _,
                            out bytesRead);

                        RSAKeyFormatHelper.FromPkcs1PrivateKey(
                            source.Slice(0, bytesRead),
                            rsaParameters =>
                            {
                                rsa.ImportParameters(rsaParameters);
                                return true;
                            });
                    }
                    catch (AsnContentException e)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                    }
#endif
                    if (rsa.KeySize != algorithm.KeySizeInBits)
                    {
                        throw new CryptographicException(SR.Argument_PrivateKeyWrongSizeForAlgorithm);
                    }

                    if (bytesRead != source.Length)
                    {
                        throw new CryptographicException(SR.Argument_PrivateKeyWrongSizeForAlgorithm);
                    }
                }
                catch (CryptographicException)
                {
                    rsa?.Dispose();
                    throw;
                }

                return new RsaComponent(rsa, algorithm.HashAlgorithmName, algorithm.Padding);
            }

            public static RsaComponent ImportPublicKey(RsaAlgorithm algorithm, ReadOnlySpan<byte> source)
            {
                Debug.Assert(IsAlgorithmSupported(algorithm));

                RSA? rsa = null;

                try
                {
                    int bytesRead;
                    rsa = CreateRSA();

#if NET
                    rsa.ImportRSAPublicKey(source, out bytesRead);
#else
                    try
                    {
                        AsnDecoder.ReadEncodedValue(
                            source,
                            AsnEncodingRules.BER,
                            out _,
                            out _,
                            out bytesRead);

                        RSAKeyFormatHelper.FromPkcs1PublicKey(
                            source.Slice(0, bytesRead),
                            rsaParameters =>
                            {
                                rsa.ImportParameters(rsaParameters);
                                return true;
                            });
                    }
                    catch (AsnContentException e)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                    }
#endif
                    if (rsa.KeySize != algorithm.KeySizeInBits)
                    {
                        throw new CryptographicException(SR.Argument_PublicKeyWrongSizeForAlgorithm);
                    }

                    if (bytesRead != source.Length)
                    {
                        throw new CryptographicException(SR.Argument_PublicKeyWrongSizeForAlgorithm);
                    }
                }
                catch (CryptographicException)
                {
                    rsa?.Dispose();
                    throw;
                }

                return new RsaComponent(rsa, algorithm.HashAlgorithmName, algorithm.Padding);
            }

            internal override bool TryExportPublicKey(Span<byte> destination, out int bytesWritten)
            {
#if NET
                return _rsa.TryExportRSAPublicKey(destination, out bytesWritten);
#else
                RSAParameters parameters = _rsa.ExportParameters(includePrivateParameters: false);
                AsnWriter writer = RSAKeyFormatHelper.WritePkcs1PublicKey(in parameters);
                return writer.TryEncode(destination, out bytesWritten);
#endif
            }

            internal override bool TryExportPrivateKey(Span<byte> destination, out int bytesWritten)
            {
#if NET
                return _rsa.TryExportRSAPrivateKey(destination, out bytesWritten);
#else
                RSAParameters parameters = _rsa.ExportParameters(includePrivateParameters: true);

                using (PinAndClear.Track(parameters.D))
                using (PinAndClear.Track(parameters.P))
                using (PinAndClear.Track(parameters.Q))
                using (PinAndClear.Track(parameters.DP))
                using (PinAndClear.Track(parameters.DQ))
                using (PinAndClear.Track(parameters.InverseQ))
                {
                    AsnWriter? writer = null;

                    try
                    {
                        writer = RSAKeyFormatHelper.WritePkcs1PrivateKey(in parameters);
                        return writer.TryEncode(destination, out bytesWritten);
                    }
                    finally
                    {
                        writer?.Reset();
                    }
                }
#endif
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
        }
    }
}
