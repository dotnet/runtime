// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal sealed partial class CompositeMLKemManaged
    {
        private sealed class ECDiffieHellmanKem : TraditionalKem
#if DESIGNTIMEINTERFACES
#pragma warning disable SA1001 // Commas should be spaced correctly
            , ITraditionalKemFactory<ECDiffieHellmanKem, ECDiffieHellmanKemAlgorithm>
#pragma warning restore SA1001 // Commas should be spaced correctly
#endif
        {
            private readonly ECDiffieHellmanKemAlgorithm _algorithm;
            private ECDiffieHellman _ecdh;

            private ECDiffieHellmanKem(ECDiffieHellman ecdh, ECDiffieHellmanKemAlgorithm algorithm)
            {
                _ecdh = ecdh;
                _algorithm = algorithm;
            }

            // While some of our OSes support the brainpool curves, not all do. Supporting them
            // only in the managed implementation could make migrating to a native implementation
            // that omits them a breaking change. This is a minor concern, but be conservative and
            // limit this implementation to the NIST curves until native implementations align.
            public static bool IsAlgorithmSupported(ECDiffieHellmanKemAlgorithm algorithm) =>
                algorithm.CurveOidValue is Oids.secp256r1 or Oids.secp384r1 or Oids.secp521r1;

            public static ECDiffieHellmanKem GenerateKey(ECDiffieHellmanKemAlgorithm algorithm) =>
                new ECDiffieHellmanKem(ECDiffieHellman.Create(algorithm.Curve), algorithm);

            public static ECDiffieHellmanKem ImportPublicKey(ECDiffieHellmanKemAlgorithm algorithm, ReadOnlySpan<byte> source)
            {
                int fieldWidth = algorithm.FieldSizeInBytes;

                // draft-ietf-lamps-pq-composite-kem-19, 4
                // public key MUST be encoded as an uncompressed elliptic curve point as in
                // section 2.2 of [RFC5480], including the leading byte 0x04
                if (source.Length != 1 + 2 * fieldWidth || source[0] != 0x04)
                {
                    throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
                }

                ECParameters parameters = new()
                {
                    Curve = algorithm.Curve,
                    Q = new ECPoint
                    {
                        X = source.Slice(1, fieldWidth).ToArray(),
                        Y = source.Slice(1 + fieldWidth, fieldWidth).ToArray(),
                    },
                };

                return new ECDiffieHellmanKem(ECDiffieHellman.Create(parameters), algorithm);
            }

            public static ECDiffieHellmanKem ImportPrivateKey(ECDiffieHellmanKemAlgorithm algorithm, ReadOnlySpan<byte> source)
            {
                Helpers.ThrowIfAsnInvalidLength(source);

                ValueECPrivateKey.Decode(source, AsnEncodingRules.DER, out ValueECPrivateKey ecPrivateKey);

                if (ecPrivateKey.Version != 1 ||
                    ecPrivateKey.HasPublicKey ||
                    !ecPrivateKey.HasParameters ||
                    ecPrivateKey.Parameters.Named != algorithm.CurveOidValue ||
                    ecPrivateKey.PrivateKey.Length != algorithm.OrderSizeInBytes)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                byte[] d = new byte[ecPrivateKey.PrivateKey.Length];

                using (PinAndClear.Track(d))
                {
                    ecPrivateKey.PrivateKey.CopyTo(d);

                    ECParameters parameters = new()
                    {
                        Curve = algorithm.Curve,
                        D = d,
                    };

                    parameters.Validate();

                    return new ECDiffieHellmanKem(ECDiffieHellman.Create(parameters), algorithm);
                }
            }

            internal override void Encapsulate(Span<byte> ciphertext, Span<byte> sharedSecret)
            {
                // draft-ietf-lamps-pq-composite-kem-19, 2.2
                // DHKEM.Encaps(pkR):
                //   (skE, pkE) = GenerateKeyPair()
                //   ss = DH(skE, pkR)
                //   ct = SerializePublicKey(pkE)
                //
                //   return ss, ct

                using (ECDiffieHellman ephemeral = ECDiffieHellman.Create(_algorithm.Curve))
                using (ECDiffieHellmanPublicKey recipientPublicKey = _ecdh.PublicKey)
                {
                    ExportPublicKey(ephemeral, _algorithm, ciphertext);

                    // The caller clears sharedSecret, even when this method throws, so no cleanup is needed here.
                    DeriveSecret(ephemeral, recipientPublicKey, sharedSecret);
                }
            }

            internal override void Decapsulate(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret)
            {
                // draft-ietf-lamps-pq-composite-kem-19, 2.2
                // DHKEM.Decaps(skR, ct):
                //   pkE = DeserializePublicKey(ct)
                //   ss = DH(skR, pkE)
                //
                //   return ss

                using (ECDiffieHellmanKem ephemeral = ImportPublicKey(_algorithm, ciphertext))
                using (ECDiffieHellmanPublicKey ephemeralPublicKey = ephemeral._ecdh.PublicKey)
                {
                    // The caller clears sharedSecret, even when this method throws, so no cleanup is needed here.
                    DeriveSecret(_ecdh, ephemeralPublicKey, sharedSecret);
                }
            }

            internal override int ExportPublicKey(Span<byte> destination)
            {
                ExportPublicKey(_ecdh, _algorithm, destination);
                return 1 + 2 * _algorithm.FieldSizeInBytes;
            }

            internal override int ExportPrivateKey(Span<byte> destination)
            {
                ECParameters parameters = _ecdh.ExportParameters(includePrivateParameters: true);

                Debug.Assert(parameters.D is not null);

                using (PinAndClear.Track(parameters.D))
                {
                    if (parameters.D.Length != _algorithm.OrderSizeInBytes ||
                        !parameters.Curve.IsNamed ||
                        parameters.Curve.Oid.Value != _algorithm.CurveOidValue)
                    {
                        Debug.Fail("ECDH exported unexpected private key parameters.");
                        throw new CryptographicException();
                    }

                    AsnWriter writer = new(AsnEncodingRules.DER);

                    try
                    {
                        ValueECPrivateKey ecPrivateKey = new()
                        {
                            Version = 1,
                            PrivateKey = parameters.D,
                            Parameters = new ValueECDomainParameters
                            {
                                Named = _algorithm.CurveOidValue,
                            },
                        };

                        ecPrivateKey.Encode(writer);

                        if (!writer.TryEncode(destination, out int bytesWritten))
                        {
                            Debug.Fail("The ECDH private key destination was unexpectedly too small.");
                            throw new CryptographicException();
                        }

                        return bytesWritten;
                    }
                    finally
                    {
                        writer.Reset();
                    }
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _ecdh?.Dispose();
                    _ecdh = null!;
                }

                base.Dispose(disposing);
            }

            private static void DeriveSecret(ECDiffieHellman ownKey, ECDiffieHellmanPublicKey otherParty, Span<byte> destination)
            {
                byte[] secret = ownKey.DeriveRawSecretAgreement(otherParty);

                try
                {
                    if (secret.Length != destination.Length)
                    {
                        Debug.Fail("ECDH produced an unexpected shared secret length.");
                        throw new CryptographicException();
                    }

                    secret.CopyTo(destination);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(secret);
                }
            }

            private static void ExportPublicKey(ECDiffieHellman key, ECDiffieHellmanKemAlgorithm algorithm, Span<byte> destination)
            {
                int fieldWidth = algorithm.FieldSizeInBytes;
                Debug.Assert(destination.Length >= 1 + 2 * fieldWidth);

                ECParameters parameters = key.ExportParameters(includePrivateParameters: false);
                byte[]? x = parameters.Q.X;
                byte[]? y = parameters.Q.Y;

                if (x is null ||
                    y is null ||
                    x.Length != fieldWidth ||
                    y.Length != fieldWidth ||
                    !parameters.Curve.IsNamed ||
                    parameters.Curve.Oid.Value != algorithm.CurveOidValue)
                {
                    Debug.Fail("ECDH exported unexpected public key parameters.");
                    throw new CryptographicException();
                }

                destination[0] = 0x04;
                x.CopyTo(destination.Slice(1, fieldWidth));
                y.CopyTo(destination.Slice(1 + fieldWidth, fieldWidth));
            }
        }
    }
}
