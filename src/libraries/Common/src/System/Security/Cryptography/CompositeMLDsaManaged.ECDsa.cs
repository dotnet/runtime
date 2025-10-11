// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

#if NETFRAMEWORK
using KeyBlobMagicNumber = Interop.BCrypt.KeyBlobMagicNumber;
#endif

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

#if NET || NETSTANDARD
            private ECDsa _ecdsa;
#else
            private ECDsaCng _ecdsa;
#endif

            private ECDsaComponent(
#if NET || NETSTANDARD
                ECDsa ecdsa,
#else
                ECDsaCng ecdsa,
#endif
                ECDsaAlgorithm algorithm)
            {
                Debug.Assert(ecdsa != null);

                _ecdsa = ecdsa;
                _algorithm = algorithm;
            }

            // While some of our OSes support the brainpool curves, not all do.
            // Limit this implementation to the NIST curves until we have a better understanding
            // of where native implementations of composite are aligning.
            public static bool IsAlgorithmSupported(ECDsaAlgorithm algorithm) =>
                algorithm.CurveOidValue is Oids.secp256r1 or Oids.secp384r1 or Oids.secp521r1;

            public static ECDsaComponent GenerateKey(ECDsaAlgorithm algorithm)
            {
#if NET || NETSTANDARD
                return new ECDsaComponent(ECDsa.Create(algorithm.Curve), algorithm);
#else
                return new ECDsaComponent(CreateKey(algorithm.CurveOid.FriendlyName), algorithm);
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

                        if (ecPrivateKey.Version != 1 ||
                            ecPrivateKey.PublicKey is not null)
                        {
                            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                        }

                        if (ecPrivateKey.Parameters is not ECDomainParameters domainParameters ||
                            domainParameters.Named is not string curveOid ||
                            curveOid != algorithm.CurveOidValue)
                        {
                            // The curve specified must be named and match the required curve for the Composite ML-DSA algorithm.
                            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                        }

                        byte[] d = new byte[ecPrivateKey.PrivateKey.Length];

                        using (PinAndClear.Track(d))
                        {
                            ecPrivateKey.PrivateKey.CopyTo(d);

#if NET || NETSTANDARD
                            ECParameters parameters = new ECParameters
                            {
                                Curve = algorithm.Curve,
                                Q = new ECPoint
                                {
                                    X = null,
                                    Y = null,
                                },
                                D = d
                            };

                            parameters.Validate();

                            return new ECDsaComponent(ECDsa.Create(parameters), algorithm);
#else // NETFRAMEWORK
#if NET472_OR_GREATER
#error ECDsa.Create(ECParameters) is avaliable in .NET Framework 4.7.2 and later, so this workaround is not needed anymore.
#endif
                            Debug.Assert(!string.IsNullOrEmpty(algorithm.CurveOid.FriendlyName));

                            byte[] zero = new byte[d.Length];
                            byte[] x = zero;
                            byte[] y = zero;

                            if (!TryValidateNamedCurve(x, y, d))
                            {
                                throw new CryptographicException(SR.Cryptography_InvalidECPrivateKeyParameters);
                            }

                            return new ECDsaComponent(
                                ECCng.EncodeEccKeyBlob(
                                    algorithm.PrivateKeyBlobMagicNumber,
                                    x,
                                    y,
                                    d,
                                    blob => ImportKeyBlob(blob, algorithm.CurveOid.FriendlyName, includePrivateParameters: true)),
                                algorithm);
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

                byte[] x = source.Slice(1, fieldWidth).ToArray();
                byte[] y = source.Slice(1 + fieldWidth).ToArray();

#if NET || NETSTANDARD
                ECParameters parameters = new ECParameters()
                {
                    Curve = algorithm.Curve,
                    Q = new ECPoint()
                    {
                        X = x,
                        Y = y,
                    }
                };

                return new ECDsaComponent(ECDsa.Create(parameters), algorithm);
#else // NETFRAMEWORK
#if NET472_OR_GREATER
#error ECDsa.Create(ECParameters) is available in .NET Framework 4.7.2 and later, so this workaround is not needed anymore.
#endif
                Debug.Assert(!string.IsNullOrEmpty(algorithm.CurveOid.FriendlyName));

                return new ECDsaComponent(
                    ECCng.EncodeEccKeyBlob(
                        algorithm.PublicKeyBlobMagicNumber,
                        x,
                        y,
                        d: null,
                        blob => ImportKeyBlob(blob, algorithm.CurveOid.FriendlyName, includePrivateParameters: false)),
                    algorithm);
#endif
            }

            internal override bool TryExportPrivateKey(Span<byte> destination, out int bytesWritten)
            {
#if NET || NETSTANDARD
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
                        (ecParameters.Curve.Oid.Value != _algorithm.CurveOidValue && ecParameters.Curve.Oid.FriendlyName != _algorithm.CurveOid.FriendlyName))
                    {
                        Debug.Fail("Unexpected curve OID.");
                        throw new CryptographicException();
                    }

                    AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

                    try
                    {
                        WriteKey(ecParameters.D, _algorithm.CurveOidValue, writer);
                        return writer.TryEncode(destination, out bytesWritten);
                    }
                    finally
                    {
                        writer.Reset();
                    }
                }
#else
                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

                try
                {
                    _ecdsa.Key.ExportKeyBlob(
                        CngKeyBlobFormat.EccPrivateBlob.Format,
                        blob =>
                        {
                            ECCng.DecodeEccKeyBlob(
                                blob,
                                (magic, x, y, d) =>
                                {
                                    if (magic != _algorithm.PrivateKeyBlobMagicNumber)
                                    {
                                        Debug.Fail("Unexpected magic number.");
                                        throw new CryptographicException();
                                    }

                                    if (!TryValidateNamedCurve(x, y, d))
                                    {
                                        Debug.Fail("Invalid EC parameters.");
                                        throw new CryptographicException();
                                    }

                                    WriteKey(d, _algorithm.CurveOidValue, writer);
                                    return true;
                                });
                        });

                    return writer.TryEncode(destination, out bytesWritten);
                }
                finally
                {
                    writer.Reset();
                }
#endif

                static void WriteKey(byte[] d, string curveOid, AsnWriter writer)
                {
                    // ECPrivateKey
                    using (writer.PushSequence())
                    {
                        // version 1
                        writer.WriteInteger(1);

                        // privateKey
                        writer.WriteOctetString(d);

                        // parameters
                        using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true)))
                        {
                            writer.WriteObjectIdentifier(curveOid);
                        }
                    }
                }
            }

            internal override bool TryExportPublicKey(Span<byte> destination, out int bytesWritten)
            {
                int fieldWidth = _algorithm.KeySizeInBytes;

                if (destination.Length < 1 + 2 * fieldWidth)
                {
                    Debug.Fail("Public key format is fixed size, so caller needs to provide exactly correct sized buffer.");

                    bytesWritten = 0;
                    return false;
                }

                byte[]? x = null;
                byte[]? y = null;

#if NET || NETSTANDARD
                ECParameters ecParameters = _ecdsa.ExportParameters(includePrivateParameters: false);

                ecParameters.Validate();

                x = ecParameters.Q.X;
                y = ecParameters.Q.Y;
#else
                // Public parameters, x and y, don't need pinning so they can be pulled out of the inner lambdas.
                _ecdsa.Key.ExportKeyBlob(
                    CngKeyBlobFormat.EccPublicBlob.Format,
                    blob =>
                    {
                        ECCng.DecodeEccKeyBlob(
                            blob,
                            (magic, localX, localY, _) =>
                            {
                                if (magic != _algorithm.PublicKeyBlobMagicNumber)
                                {
                                    Debug.Fail("Unexpected magic number.");
                                    throw new CryptographicException();
                                }

                                if (!TryValidateNamedCurve(localX, localY, null))
                                {
                                    Debug.Fail("Invalid EC parameters.");
                                    throw new CryptographicException();
                                }

                                x = localX;
                                y = localY;
                                return true;
                            });
                    });
#endif

                Debug.Assert(x is not null);
                Debug.Assert(y is not null);

                if (x.Length != fieldWidth || y.Length != fieldWidth)
                {
                    Debug.Fail("Unexpected key size.");
                    throw new CryptographicException();
                }

                // Uncompressed ECPoint format
                destination[0] = 0x04;

                x.CopyTo(destination.Slice(1, fieldWidth));
                y.CopyTo(destination.Slice(1 + fieldWidth));

                bytesWritten = 1 + 2 * fieldWidth;
                return true;
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
                byte[] ieeeSignature = null;

                try
                {
                    ieeeSignature = AsymmetricAlgorithmHelpers.ConvertDerToIeee1363(signature, _algorithm.KeySizeInBits);
                }
                catch (CryptographicException)
                {
                    return false;
                }

                return _ecdsa.VerifyData(data, ieeeSignature, _algorithm.HashAlgorithmName);
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
                byte[] ieeeSignature = _ecdsa.SignData(data, _algorithm.HashAlgorithmName);

                if (!AsymmetricAlgorithmHelpers.TryConvertIeee1363ToDer(ieeeSignature, destination, out int bytesWritten))
                {
                    Debug.Fail("Buffer size should have been validated by caller.");
                    throw new CryptographicException();
                }

                return bytesWritten;
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

#if NETFRAMEWORK
#if NET472_OR_GREATER
#error ECDsa.Create(ECParameters) is available in .NET Framework 4.7.2 and later, so this workaround is not needed anymore.
#endif
            private static ECDsaCng ImportKeyBlob(byte[] ecKeyBlob, string curveName, bool includePrivateParameters)
            {
                CngKeyBlobFormat blobFormat = includePrivateParameters ? CngKeyBlobFormat.EccPrivateBlob : CngKeyBlobFormat.EccPublicBlob;
                CngProvider provider = CngProvider.MicrosoftSoftwareKeyStorageProvider;

                using (SafeNCryptProviderHandle providerHandle = provider.OpenStorageProvider())
                using (SafeNCryptKeyHandle keyHandle = ECCng.ImportKeyBlob(blobFormat.Format, ecKeyBlob, curveName, providerHandle))
                using (CngKey key = CngKey.Open(keyHandle, CngKeyHandleOpenOptions.EphemeralKey))
                {
                    key.SetExportPolicy(CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport);
                    return new ECDsaCng(key);
                }
            }

            private static ECDsaCng CreateKey(string curveName)
            {
                CngKeyCreationParameters creationParameters = new CngKeyCreationParameters()
                {
                    ExportPolicy = CngExportPolicies.AllowPlaintextExport,
                };

                byte[] curveNameBytes = new byte[(curveName.Length + 1) * sizeof(char)]; // +1 to add trailing null
                System.Text.Encoding.Unicode.GetBytes(curveName, 0, curveName.Length, curveNameBytes, 0);
                creationParameters.Parameters.Add(new CngProperty(KeyPropertyName.ECCCurveName, curveNameBytes, CngPropertyOptions.None));

                using (CngKey key = CngKey.Create(new CngAlgorithm("ECDSA"), null, creationParameters))
                {
                    return new ECDsaCng(key);
                }
            }

            private static bool TryValidateNamedCurve(byte[]? x, byte[]? y, byte[]? d)
            {
                bool hasErrors = true;

                if (d is not null && y is null && x is null)
                {
                    hasErrors = false;
                }
                else if (y is not null && x is not null && y.Length == x.Length)
                {
                    hasErrors = (d is not null && (d.Length != x.Length));
                }

                return !hasErrors;
            }
#endif
        }
    }
}
