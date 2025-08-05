// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal sealed partial class CompositeMLDsaManaged
    {
        private sealed class ECDsaComponent : ComponentAlgorithm
#if DESIGNTIMEINTERFACES
#pragma warning disable SA1001 // Commas should be spaced correctly
            , IComponentAlgorithmFactory<ECDsaComponent, ECDsaAlgorithm>
#pragma warning restore SA1001 // Commas should be spaced correctly
#endif
        {
            private readonly ECDsaAlgorithm _algorithm;

            private ECDsa _ecdsa;

            private ECDsaComponent(ECDsa ecdsa, ECDsaAlgorithm algorithm)
            {
                Debug.Assert(ecdsa != null);

                _ecdsa = ecdsa;
                _algorithm = algorithm;
            }

            // While some of our OSes support the brainpool curves, not all do.
            // Limit this implementation to the NIST curves until we have a better understanding
            // of where native implementations of composite are aligning.
            public static bool IsAlgorithmSupported(ECDsaAlgorithm algorithm) =>
#if NET
                algorithm.CurveOid is Oids.secp256r1 or Oids.secp384r1 or Oids.secp521r1;
#else
                false;
#endif

            public static ECDsaComponent GenerateKey(ECDsaAlgorithm algorithm)
            {
#if NET
                return new ECDsaComponent(ECDsa.Create(algorithm.Curve), algorithm);
#else
                throw new PlatformNotSupportedException();
#endif
            }

            public static unsafe ECDsaComponent ImportPrivateKey(ECDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
            {
                Helpers.ThrowIfAsnInvalidLength(source);

                fixed (byte* ptr = &MemoryMarshal.GetReference(source))
                {
                    using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, source.Length))
                    {
                        ECPrivateKey ecPrivateKey = ECPrivateKey.Decode(manager.Memory, AsnEncodingRules.BER);

                        if (ecPrivateKey.Version != 1)
                        {
                            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                        }

                        // If domain parameters are present, validate that they match the composite ML-DSA algorithm.
                        if (ecPrivateKey.Parameters is ECDomainParameters domainParameters)
                        {
                            if (domainParameters.Named is not string curveOid || curveOid != algorithm.CurveOid)
                            {
                                // The curve specified must be named and match the required curve for the composite ML-DSA algorithm.
                                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                            }
                        }

                        byte[]? x = null;
                        byte[]? y = null;

                        // If public key is present, add it to the parameters.
                        if (ecPrivateKey.PublicKey is ReadOnlyMemory<byte> publicKey)
                        {
                            EccKeyFormatHelper.GetECPointFromUncompressedPublicKey(publicKey.Span, algorithm.KeySizeInBytes, out x, out y);
                        }

                        byte[] d = new byte[ecPrivateKey.PrivateKey.Length];

                        using (PinAndClear.Track(d))
                        {
                            ecPrivateKey.PrivateKey.CopyTo(d);

#if NET
                            ECParameters parameters = new ECParameters
                            {
                                Curve = algorithm.Curve,
                                Q = new ECPoint
                                {
                                    X = x,
                                    Y = y,
                                },
                                D = d
                            };

                            parameters.Validate();

                            return new ECDsaComponent(ECDsa.Create(parameters), algorithm);
#else
                            throw new PlatformNotSupportedException();
#endif
                        }
                    }
                }
            }

            public static unsafe ECDsaComponent ImportPublicKey(ECDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
            {
                int fieldWidth = algorithm.KeySizeInBytes;

                if (source.Length != 1 + fieldWidth * 2)
                {
                    Debug.Fail("Public key format is fixed size, so caller needs to provide exactly correct sized buffer.");
                    throw new CryptographicException();
                }

                // Implementation limitation.
                // 04 (Uncompressed ECPoint) is almost always used.
                if (source[0] != 0x04)
                {
                    throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
                }

#if NET
                ECParameters parameters = new ECParameters()
                {
                    Curve = algorithm.Curve,
                    Q = new ECPoint()
                    {
                        X = source.Slice(1, fieldWidth).ToArray(),
                        Y = source.Slice(1 + fieldWidth).ToArray(),
                    }
                };

                return new ECDsaComponent(ECDsa.Create(parameters), algorithm);
#else
                throw new PlatformNotSupportedException();
#endif
            }

            internal override bool TryExportPrivateKey(Span<byte> destination, out int bytesWritten)
            {
#if NET
                ECParameters ecParameters = _ecdsa.ExportParameters(includePrivateParameters: true);

                Debug.Assert(ecParameters.D != null);

                using (PinAndClear.Track(ecParameters.D))
                {
                    ecParameters.Validate();

                    if (ecParameters.D.Length != _algorithm.KeySizeInBytes)
                    {
                        Debug.Fail("Unexpected key size.");
                        throw new CryptographicException();
                    }

                    // The curve OID must match the composite ML-DSA algorithm.
                    if (!ecParameters.Curve.IsNamed ||
                        (ecParameters.Curve.Oid.Value != _algorithm.Curve.Oid.Value && ecParameters.Curve.Oid.FriendlyName != _algorithm.Curve.Oid.FriendlyName))
                    {
                        Debug.Fail("Unexpected curve OID.");
                        throw new CryptographicException();
                    }

                    return TryWriteKey(ecParameters.D, ecParameters.Q.X, ecParameters.Q.Y, _algorithm.CurveOid, destination, out bytesWritten);
                }
#else
                throw new PlatformNotSupportedException();
#endif

#if NET
                static bool TryWriteKey(byte[] d, byte[]? x, byte[]? y, string curveOid, Span<byte> destination, out int bytesWritten)
                {
                    AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

                    try
                    {
                        // ECPrivateKey
                        using (writer.PushSequence())
                        {
                            // version 1
                            writer.WriteInteger(1);

                            // privateKey
                            writer.WriteOctetString(d);

                            // domainParameters
                            using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true)))
                            {
                                writer.WriteObjectIdentifier(curveOid);
                            }

                            // publicKey
                            if (x != null)
                            {
                                Debug.Assert(y != null);

                                using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1, isConstructed: true)))
                                {
                                    EccKeyFormatHelper.WriteUncompressedPublicKey(x, y, writer);
                                }
                            }
                        }

                        return writer.TryEncode(destination, out bytesWritten);
                    }
                    finally
                    {
                        writer.Reset();
                    }
                }
#endif
            }

            internal override bool TryExportPublicKey(Span<byte> destination, out int bytesWritten)
            {
#if NET
                int fieldWidth = _algorithm.KeySizeInBytes;

                if (destination.Length < 1 + 2 * fieldWidth)
                {
                    Debug.Fail("Public key format is fixed size, so caller needs to provide exactly correct sized buffer.");

                    bytesWritten = 0;
                    return false;
                }

                ECParameters ecParameters = _ecdsa.ExportParameters(includePrivateParameters: false);

                ecParameters.Validate();

                if (ecParameters.Q.X?.Length != fieldWidth)
                {
                    Debug.Fail("Unexpected key size.");
                    throw new CryptographicException();
                }

                // Uncompressed ECPoint format
                destination[0] = 0x04;

                ecParameters.Q.X.CopyTo(destination.Slice(1, fieldWidth));
                ecParameters.Q.Y.CopyTo(destination.Slice(1 + fieldWidth));

                bytesWritten = 1 + 2 * fieldWidth;
                return true;
#else
                throw new PlatformNotSupportedException();
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
                return _ecdsa.VerifyData(data, signature, _algorithm.HashAlgorithmName, DSASignatureFormat.Rfc3279DerSequence);
#else
                throw new PlatformNotSupportedException();
#endif
            }

            internal override int SignData(
#if NET
                ReadOnlySpan<byte> data,
#else
                byte[] data,
#endif
                Span<byte> destination)
            {
#if NET
                if (!_ecdsa.TrySignData(data, destination, _algorithm.HashAlgorithmName, DSASignatureFormat.Rfc3279DerSequence, out int bytesWritten))
                {
                    Debug.Fail("Buffer size should have been validated by caller.");
                    throw new CryptographicException();
                }

                return bytesWritten;
#else
                throw new PlatformNotSupportedException();
#endif
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _ecdsa?.Dispose();
                    _ecdsa = null!;
                }

                base.Dispose(disposing);
            }
        }
    }
}
