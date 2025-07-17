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
#else
            private static RSA CreateRSA() => RSA.Create();
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

            public static RsaComponent GenerateKey(RsaAlgorithm algorithm) =>
                throw new NotImplementedException();

            public static RsaComponent ImportPrivateKey(RsaAlgorithm algorithm, ReadOnlySpan<byte> source)
            {
                Debug.Assert(IsAlgorithmSupported(algorithm));

                RSA? rsa = null;

                try
                {
                    rsa = CreateRSA();

#if NET
                    rsa.ImportRSAPrivateKey(source, out int bytesRead);

                    if (bytesRead != source.Length)
                    {
                        throw new CryptographicException(SR.Argument_PrivateKeyWrongSizeForAlgorithm);
                    }
#else
                    ConvertRSAPrivateKeyToParameters(algorithm, source, (in parameters) =>
                    {
                        rsa.ImportParameters(parameters);
                    });
#endif
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
                    rsa = CreateRSA();

#if NET
                    rsa.ImportRSAPublicKey(source, out int bytesRead);

                    if (bytesRead != source.Length)
                    {
                        throw new CryptographicException(SR.Argument_PublicKeyWrongSizeForAlgorithm);
                    }
#else
                    ConvertRSAPublicKeyToParameters(algorithm, source, (in parameters) =>
                    {
                        rsa.ImportParameters(parameters);
                    });
#endif
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

#if !NET
            private delegate void ConvertRSAKeyToParametersCallback(in RSAParameters source);

            private static unsafe void ConvertRSAPublicKeyToParameters(
                RsaAlgorithm algorithm,
                ReadOnlySpan<byte> key,
                ConvertRSAKeyToParametersCallback callback)
            {
                Debug.Assert(algorithm.KeySizeInBits % 8 == 0);
                int modulusLength = algorithm.KeySizeInBits / 8;
                RSAParameters parameters = default;

                try
                {
                    AsnValueReader reader = new AsnValueReader(key, AsnEncodingRules.BER);
                    AsnValueReader sequenceReader = reader.ReadSequence(Asn1Tag.Sequence);

                    parameters.Modulus = sequenceReader.ReadIntegerBytes().ToUnsignedIntegerBytes();

                    if (parameters.Modulus.Length != modulusLength)
                    {
                        throw new CryptographicException(SR.Cryptography_NotValidPrivateKey);
                    }

                    parameters.Exponent = sequenceReader.ReadIntegerBytes().ToUnsignedIntegerBytes();

                    sequenceReader.ThrowIfNotEmpty();
                    reader.ThrowIfNotEmpty();
                }
                catch (AsnContentException e)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                }

                callback(in parameters);
            }

            private static unsafe void ConvertRSAPrivateKeyToParameters(
                RsaAlgorithm algorithm,
                ReadOnlySpan<byte> key,
                ConvertRSAKeyToParametersCallback callback)
            {
                int modulusLength = algorithm.KeySizeInBits / 8;
                int halfModulusLength = modulusLength / 2;

                RSAParameters parameters = new()
                {
                    D = new byte[modulusLength],
                    P = new byte[halfModulusLength],
                    Q = new byte[halfModulusLength],
                    DP = new byte[halfModulusLength],
                    DQ = new byte[halfModulusLength],
                    InverseQ = new byte[halfModulusLength],
                };

                using (PinAndClear.Track(parameters.D))
                using (PinAndClear.Track(parameters.P))
                using (PinAndClear.Track(parameters.Q))
                using (PinAndClear.Track(parameters.DP))
                using (PinAndClear.Track(parameters.DQ))
                using (PinAndClear.Track(parameters.InverseQ))
                {
                    try
                    {
                        AsnValueReader reader = new AsnValueReader(key, AsnEncodingRules.BER);
                        AsnValueReader sequenceReader = reader.ReadSequence(Asn1Tag.Sequence);

                        if (!sequenceReader.TryReadInt32(out int version))
                        {
                            sequenceReader.ThrowIfNotEmpty();
                        }

                        const int MaxSupportedVersion = 0;

                        if (version > MaxSupportedVersion)
                        {
                            throw new CryptographicException(
                                SR.Format(
                                    SR.Cryptography_RSAPrivateKey_VersionTooNew,
                                    version,
                                    MaxSupportedVersion));
                        }

                        parameters.Modulus = sequenceReader.ReadIntegerBytes().ToUnsignedIntegerBytes();

                        if (parameters.Modulus.Length != modulusLength)
                        {
                            throw new CryptographicException(SR.Cryptography_NotValidPrivateKey);
                        }

                        parameters.Exponent = sequenceReader.ReadIntegerBytes().ToUnsignedIntegerBytes();

                        sequenceReader.ReadIntegerBytes().ToUnsignedIntegerBytes(parameters.D);
                        sequenceReader.ReadIntegerBytes().ToUnsignedIntegerBytes(parameters.P);
                        sequenceReader.ReadIntegerBytes().ToUnsignedIntegerBytes(parameters.Q);
                        sequenceReader.ReadIntegerBytes().ToUnsignedIntegerBytes(parameters.DP);
                        sequenceReader.ReadIntegerBytes().ToUnsignedIntegerBytes(parameters.DQ);
                        sequenceReader.ReadIntegerBytes().ToUnsignedIntegerBytes(parameters.InverseQ);

                        sequenceReader.ThrowIfNotEmpty();
                        reader.ThrowIfNotEmpty();
                    }
                    catch (AsnContentException e)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                    }

                    callback(in parameters);
                }
            }
#endif
        }
    }
}
