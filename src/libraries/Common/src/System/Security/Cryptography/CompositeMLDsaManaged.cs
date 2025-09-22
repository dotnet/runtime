// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using Internal.Cryptography;

#if NETFRAMEWORK
using KeyBlobMagicNumber = Interop.BCrypt.KeyBlobMagicNumber;
#endif

namespace System.Security.Cryptography
{
#if !SYSTEM_SECURITY_CRYPTOGRAPHY
    // System.Security.Cryptography excludes browser at build time, but we need to rely on UnsupportedOSPlatform for Microsoft.Bcl.Cryptography.
    [UnsupportedOSPlatform("browser")]
#endif
    [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    internal sealed partial class CompositeMLDsaManaged : CompositeMLDsa
    {
        private static readonly Dictionary<CompositeMLDsaAlgorithm, AlgorithmMetadata> s_algorithmMetadata = CreateAlgorithmMetadata();
        private static readonly ConcurrentDictionary<CompositeMLDsaAlgorithm, bool> s_algorithmSupport = new();

        private static ReadOnlySpan<byte> MessageRepresentativePrefix => "CompositeAlgorithmSignatures2025"u8;

        private MLDsa _mldsa;
        private ComponentAlgorithm _componentAlgorithm;

        private AlgorithmMetadata AlgorithmDetails => field ??= s_algorithmMetadata[Algorithm];

        private CompositeMLDsaManaged(CompositeMLDsaAlgorithm algorithm, MLDsa mldsa, ComponentAlgorithm componentAlgorithm)
            : base(algorithm)
        {
            _mldsa = mldsa;
            _componentAlgorithm = componentAlgorithm;
        }

        internal static bool SupportsAny() => MLDsaImplementation.SupportsAny();

        internal static bool IsAlgorithmSupportedImpl(CompositeMLDsaAlgorithm algorithm)
        {
            AlgorithmMetadata metadata = s_algorithmMetadata[algorithm];

            return s_algorithmSupport.GetOrAdd(
                algorithm,
                alg => MLDsaImplementation.IsAlgorithmSupported(metadata.MLDsaAlgorithm) && metadata.TraditionalAlgorithm switch
                {
                    RsaAlgorithm rsaAlgorithm => RsaComponent.IsAlgorithmSupported(rsaAlgorithm),
                    ECDsaAlgorithm ecdsaAlgorithm => ECDsaComponent.IsAlgorithmSupported(ecdsaAlgorithm),
                    _ => false,
                });
        }

        internal static CompositeMLDsa GenerateKeyImpl(CompositeMLDsaAlgorithm algorithm)
        {
            Debug.Assert(IsAlgorithmSupportedImpl(algorithm));

            AlgorithmMetadata metadata = s_algorithmMetadata[algorithm];

            // draft-ietf-lamps-pq-composite-sigs-latest (July 7, 2025), 4.1
            //  1.  Generate component keys
            //
            //      mldsaSeed = Random(32)
            //      (mldsaPK, _) = ML-DSA.KeyGen(mldsaSeed)
            //      (tradPK, tradSK) = Trad.KeyGen()

            MLDsa? mldsaKey = null;
            ComponentAlgorithm? tradKey = null;

            try
            {
                mldsaKey = MLDsaImplementation.GenerateKey(metadata.MLDsaAlgorithm);
            }
            catch (CryptographicException)
            {
            }

            try
            {
                tradKey = metadata.TraditionalAlgorithm switch
                {
                    RsaAlgorithm rsaAlgorithm => RsaComponent.GenerateKey(rsaAlgorithm),
                    ECDsaAlgorithm ecdsaAlgorithm => ECDsaComponent.GenerateKey(ecdsaAlgorithm),
                    _ => FailAndGetNull(),
                };

                static ComponentAlgorithm? FailAndGetNull()
                {
                    Debug.Fail("Only supported algorithms should reach here.");
                    return null;
                }
            }
            catch (CryptographicException)
            {
            }

            //  2.  Check for component key gen failure
            //
            //      if NOT (mldsaPK, mldsaSK) or NOT (tradPK, tradSK):
            //          output "Key generation error"

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            static bool KeyGenFailed([NotNullWhen(false)] MLDsa? mldsaKey, [NotNullWhen(false)] ComponentAlgorithm? tradKey) =>
                (mldsaKey is null) | (tradKey is null);

            if (KeyGenFailed(mldsaKey, tradKey))
            {
                try
                {
                    Debug.Assert(mldsaKey is null || tradKey is null);

                    mldsaKey?.Dispose();
                    tradKey?.Dispose();
                }
                catch (CryptographicException)
                {
                }

                throw new CryptographicException();
            }

            //  3.  Output the composite public and private keys
            //
            //      pk = SerializePublicKey(mldsaPK, tradPK)
            //      sk = SerializePrivateKey(mldsaSeed, tradSK)
            //      return (pk, sk)

            return new CompositeMLDsaManaged(algorithm, mldsaKey, tradKey);
        }

        internal static CompositeMLDsa ImportCompositeMLDsaPublicKeyImpl(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(IsAlgorithmSupportedImpl(algorithm));

            AlgorithmMetadata metadata = s_algorithmMetadata[algorithm];

            // draft-ietf-lamps-pq-composite-sigs-latest (June 20, 2025), 5.1
            //  1.  Parse each constituent encoded public key.
            //        The length of the mldsaKey is known based on the size of
            //        the ML-DSA component key length specified by the Object ID.
            //
            //      switch ML-DSA do
            //          case ML-DSA-44:
            //              mldsaPK = bytes[:1312]
            //              tradPK = bytes[1312:]
            //          case ML-DSA-65:
            //              mldsaPK = bytes[:1952]
            //              tradPK = bytes[1952:]
            //          case ML-DSA-87:
            //              mldsaPK = bytes[:2592]
            //              tradPK = bytes[2592:]
            //
            //      Note that while ML-DSA has fixed-length keys, RSA and ECDSA
            //      may not, depending on encoding, so rigorous length - checking
            //      of the overall composite key is not always possible.
            //
            //  2.  Output the component public keys
            //
            //      output(mldsaPK, tradPK)

            ReadOnlySpan<byte> mldsaKey = source.Slice(0, metadata.MLDsaAlgorithm.PublicKeySizeInBytes);
            ReadOnlySpan<byte> tradKey = source.Slice(metadata.MLDsaAlgorithm.PublicKeySizeInBytes);

            MLDsaImplementation mldsa = MLDsaImplementation.ImportPublicKey(metadata.MLDsaAlgorithm, mldsaKey);
            ComponentAlgorithm componentAlgorithm = metadata.TraditionalAlgorithm switch
            {
                RsaAlgorithm rsaAlgorithm => RsaComponent.ImportPublicKey(rsaAlgorithm, tradKey),
                ECDsaAlgorithm ecdsaAlgorithm => ECDsaComponent.ImportPublicKey(ecdsaAlgorithm, tradKey),
                _ => throw FailAndGetException(),
            };

            static CryptographicException FailAndGetException()
            {
                Debug.Fail("Only supported algorithms should reach here.");
                return new CryptographicException();
            }

            return new CompositeMLDsaManaged(algorithm, mldsa, componentAlgorithm);
        }

        internal static CompositeMLDsa ImportCompositeMLDsaPrivateKeyImpl(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(IsAlgorithmSupportedImpl(algorithm));

            AlgorithmMetadata metadata = s_algorithmMetadata[algorithm];

            // draft-ietf-lamps-pq-composite-sigs-latest (June 20, 2025), 5.2
            //  1.  Parse each constituent encoded key.
            //      The length of an ML-DSA private key is always a 32 byte seed
            //      for all parameter sets.
            //
            //      mldsaSeed = bytes[:32]
            //      tradSK  = bytes[32:]
            //
            //      Note that while ML-DSA has fixed-length keys, RSA and ECDSA
            //      may not, depending on encoding, so rigorous length-checking
            //      of the overall composite key is not always possible.
            //
            //  2.  Output the component private keys
            //
            //      output (mldsaSeed, tradSK)

            ReadOnlySpan<byte> mldsaKey = source.Slice(0, metadata.MLDsaAlgorithm.PrivateSeedSizeInBytes);
            ReadOnlySpan<byte> tradKey = source.Slice(metadata.MLDsaAlgorithm.PrivateSeedSizeInBytes);

            MLDsaImplementation mldsa = MLDsaImplementation.ImportSeed(metadata.MLDsaAlgorithm, mldsaKey);
            ComponentAlgorithm componentAlgorithm = metadata.TraditionalAlgorithm switch
            {
                RsaAlgorithm rsaAlgorithm => RsaComponent.ImportPrivateKey(rsaAlgorithm, tradKey),
                ECDsaAlgorithm ecdsaAlgorithm => ECDsaComponent.ImportPrivateKey(ecdsaAlgorithm, tradKey),
                _ => throw FailAndGetException(),
            };

            static CryptographicException FailAndGetException()
            {
                Debug.Fail("Only supported algorithms should reach here.");
                return new CryptographicException();
            }

            return new CompositeMLDsaManaged(algorithm, mldsa, componentAlgorithm);
        }

        protected override int SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination)
        {
            // draft-ietf-lamps-pq-composite-sigs-latest (June 20, 2025), 4.2
            //  1.  If len(ctx) > 255:
            //      return error

            Debug.Assert(context.Length <= 255, $"Caller should have checked context.Length, got {context.Length}");

            //  2.  Compute the Message representative M'.
            //      As in FIPS 204, len(ctx) is encoded as a single unsigned byte.
            //      Randomize the message representative
            //
            //          r = Random(32)
            //          M' :=  Prefix || Domain || len(ctx) || ctx || r
            //                                              || PH( M )

            Span<byte> r = stackalloc byte[CompositeMLDsaAlgorithm.RandomizerSizeInBytes];
            RandomNumberGenerator.Fill(r);

            byte[] M_prime = GetMessageRepresentative(AlgorithmDetails, context, r, data);

            //  3.  Separate the private key into component keys
            //      and re-generate the ML-DSA key from seed.
            //
            //          (mldsaSeed, tradSK) = DeserializePrivateKey(sk)
            //          (_, mldsaSK) = ML-DSA.KeyGen(mldsaSeed)

            /* no-op */

            //  4.  Generate the two component signatures independently by calculating
            //      the signature over M' according to their algorithm specifications.
            //
            //          mldsaSig = ML-DSA.Sign( mldsaSK, M', ctx=Domain )
            //          tradSig = Trad.Sign( tradSK, M' )

            //  Note that in step 4 above, both component signature processes are
            //  invoked, and no indication is given about which one failed.This
            //  SHOULD be done in a timing-invariant way to prevent side-channel
            //  attackers from learning which component algorithm failed.

            Span<byte> randomizer = destination.Slice(0, CompositeMLDsaAlgorithm.RandomizerSizeInBytes);
            Span<byte> mldsaSig = destination.Slice(CompositeMLDsaAlgorithm.RandomizerSizeInBytes, AlgorithmDetails.MLDsaAlgorithm.SignatureSizeInBytes);
            Span<byte> tradSig = destination.Slice(CompositeMLDsaAlgorithm.RandomizerSizeInBytes + AlgorithmDetails.MLDsaAlgorithm.SignatureSizeInBytes);

            bool mldsaSigned = false;
            bool tradSigned = false;

            try
            {
                _mldsa.SignData(M_prime, mldsaSig, AlgorithmDetails.DomainSeparator);
                mldsaSigned = true;
            }
            catch (CryptographicException)
            {
            }

            int tradBytesWritten = 0;

            try
            {
                tradBytesWritten = _componentAlgorithm.SignData(M_prime, tradSig);
                tradSigned = true;
            }
            catch (CryptographicException)
            {
            }

            //  5.  If either ML-DSA.Sign() or Trad.Sign() return an error, then this
            //      process MUST return an error.
            //
            //          if NOT mldsaSig or NOT tradSig:
            //              output "Signature generation error"

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            static bool Or(bool x, bool y) => x | y;

            if (Or(!mldsaSigned, !tradSigned))
            {
                CryptographicOperations.ZeroMemory(destination);
                throw new CryptographicException(SR.Cryptography_CompositeSignDataError);
            }

            //  6.  Output the encoded composite signature value.
            //
            //          s = SerializeSignatureValue(r, mldsaSig, tradSig)
            //          return s

            r.CopyTo(randomizer);
            return randomizer.Length + mldsaSig.Length + tradBytesWritten;
        }

        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature)
        {
            // draft-ietf-lamps-pq-composite-sigs-latest (June 20, 2025), 4.3
            //  1.  If len(ctx) > 255
            //          return error

            Debug.Assert(context.Length <= 255, $"Caller should have checked context.Length, got {context.Length}");

            //  2.  Separate the keys and signatures
            //
            //          (mldsaPK, tradPK)       = DeserializePublicKey(pk)
            //          (r, mldsaSig, tradSig)  = DeserializeSignatureValue(s)
            //
            //      If Error during deserialization, or if any of the component
            //      keys or signature values are not of the correct type or
            //      length for the given component algorithm then output
            //      "Invalid signature" and stop.

            ReadOnlySpan<byte> r = signature.Slice(0, CompositeMLDsaAlgorithm.RandomizerSizeInBytes);
            ReadOnlySpan<byte> mldsaSig = signature.Slice(CompositeMLDsaAlgorithm.RandomizerSizeInBytes, AlgorithmDetails.MLDsaAlgorithm.SignatureSizeInBytes);
            ReadOnlySpan<byte> tradSig = signature.Slice(CompositeMLDsaAlgorithm.RandomizerSizeInBytes + AlgorithmDetails.MLDsaAlgorithm.SignatureSizeInBytes);

            //  3.  Compute a Hash of the Message.
            //      As in FIPS 204, len(ctx) is encoded as a single unsigned byte.
            //
            //          M' = Prefix || Domain || len(ctx) || ctx || r
            //                                                   || PH( M )

            byte[] M_prime = GetMessageRepresentative(AlgorithmDetails, context, r, data);

            //  4.  Check each component signature individually, according to its
            //      algorithm specification.
            //      If any fail, then the entire signature validation fails.
            //
            //      if not ML-DSA.Verify( mldsaPK, M', mldsaSig, ctx=Domain ) then
            //          output "Invalid signature"
            //
            //      if not Trad.Verify( tradPK, M', tradSig ) then
            //          output "Invalid signature"
            //
            //      if all succeeded, then
            //          output "Valid signature"

            // We don't short circuit here because we want to avoid revealing which component signature failed.
            // This is not required in the spec, but it is a good practice to avoid timing attacks.

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            static bool And(bool x, bool y) => x & y;

            return And(_mldsa.VerifyData(M_prime, mldsaSig, AlgorithmDetails.DomainSeparator), _componentAlgorithm.VerifyData(M_prime, tradSig));
        }

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            AsnWriter? writer = null;

            try
            {
                using (CryptoPoolLease lease = CryptoPoolLease.Rent(Algorithm.MaxPrivateKeySizeInBytes))
                {
                    int privateKeySize = ExportCompositeMLDsaPrivateKeyCore(lease.Span);

                    // Add some overhead for the ASN.1 structure.
                    int initialCapacity = 32 + privateKeySize;

                    writer = new AsnWriter(AsnEncodingRules.DER, initialCapacity);

                    using (writer.PushSequence())
                    {
                        writer.WriteInteger(0); // Version

                        using (writer.PushSequence())
                        {
                            writer.WriteObjectIdentifier(Algorithm.Oid);
                        }

                        writer.WriteOctetString(lease.Span.Slice(0, privateKeySize));
                    }

                    Debug.Assert(writer.GetEncodedLength() <= initialCapacity);
                }

                return writer.TryEncode(destination, out bytesWritten);
            }
            finally
            {
                writer?.Reset();
            }
        }

        protected override int ExportCompositeMLDsaPublicKeyCore(Span<byte> destination)
        {
            // draft-ietf-lamps-pq-composite-sigs-latest (June 20, 2025), 5.1
            //  1.  Combine and output the encoded public key
            //
            //      output mldsaPK || tradPK

            int bytesWritten = 0;

            _mldsa.ExportMLDsaPublicKey(destination.Slice(0, AlgorithmDetails.MLDsaAlgorithm.PublicKeySizeInBytes));
            bytesWritten += AlgorithmDetails.MLDsaAlgorithm.PublicKeySizeInBytes;

            if (!_componentAlgorithm.TryExportPublicKey(destination.Slice(AlgorithmDetails.MLDsaAlgorithm.PublicKeySizeInBytes), out int componentBytesWritten))
            {
                throw new CryptographicException();
            }

            bytesWritten += componentBytesWritten;

            return bytesWritten;
        }

        protected override int ExportCompositeMLDsaPrivateKeyCore(Span<byte> destination)
        {
            // draft-ietf-lamps-pq-composite-sigs-latest (June 20, 2025), 5.2
            //  1.  Combine and output the encoded private key
            //
            //      output mldsaSeed || tradSK

            try
            {
                int bytesWritten = 0;

                _mldsa.ExportMLDsaPrivateSeed(destination.Slice(0, AlgorithmDetails.MLDsaAlgorithm.PrivateSeedSizeInBytes));
                bytesWritten += AlgorithmDetails.MLDsaAlgorithm.PrivateSeedSizeInBytes;

                if (!_componentAlgorithm.TryExportPrivateKey(destination.Slice(AlgorithmDetails.MLDsaAlgorithm.PrivateSeedSizeInBytes), out int componentBytesWritten))
                {
                    throw new CryptographicException();
                }

                bytesWritten += componentBytesWritten;

                return bytesWritten;
            }
            catch (CryptographicException)
            {
                CryptographicOperations.ZeroMemory(destination);
                throw;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _mldsa?.Dispose();
                _mldsa = null!;

                _componentAlgorithm?.Dispose();
                _componentAlgorithm = null!;
            }

            base.Dispose(disposing);
        }

        private static byte[] GetMessageRepresentative(
            AlgorithmMetadata metadata,
            ReadOnlySpan<byte> context,
            ReadOnlySpan<byte> r,
            ReadOnlySpan<byte> message)
        {
            checked
            {
                Debug.Assert(r.Length is CompositeMLDsaAlgorithm.RandomizerSizeInBytes);

                // M' = Prefix || Domain || len(ctx) || ctx || r || PH( M )

                using (IncrementalHash hash = IncrementalHash.CreateHash(metadata.HashAlgorithmName))
                {
#if NET
                    int hashLength = hash.HashLengthInBytes;
#else
                    int hashLength = hash.GetHashLengthInBytes();
#endif

                    int length =
                        MessageRepresentativePrefix.Length +    // Prefix
                        metadata.DomainSeparator.Length +       // Domain
                        1 +                                     // len(ctx)
                        context.Length +                        // ctx
                        r.Length +                              // r
                        hashLength;                             // PH( M )

                    // The representative message will often be < 256 bytes so we can stackalloc with a callback.
                    // That gets a little messy on .NET Framework where by-ref generics aren't supported, so we just allocate.
                    byte[] M_prime = new byte[length];

                    int offset = 0;

                    // Prefix
                    MessageRepresentativePrefix.CopyTo(M_prime.AsSpan(offset, MessageRepresentativePrefix.Length));
                    offset += MessageRepresentativePrefix.Length;

                    // Domain
                    metadata.DomainSeparator.AsSpan().CopyTo(M_prime.AsSpan(offset, metadata.DomainSeparator.Length));
                    offset += metadata.DomainSeparator.Length;

                    // len(ctx)
                    M_prime[offset] = (byte)context.Length;
                    offset++;

                    // ctx
                    context.CopyTo(M_prime.AsSpan(offset, context.Length));
                    offset += context.Length;

                    // r
                    r.CopyTo(M_prime.AsSpan(offset, r.Length));
                    offset += r.Length;

                    // PH( M )
                    hash.AppendData(message);
#if NET
                    hash.GetHashAndReset(M_prime.AsSpan(offset, hashLength));
#else
                    byte[] hashBytes = hash.GetHashAndReset();
                    hashBytes.CopyTo(M_prime.AsSpan(offset, hashLength));
#endif
                    offset += hashLength;

                    Debug.Assert(offset == M_prime.Length);

                    return M_prime;
                }
            }
        }

#if DESIGNTIMEINTERFACES
        private interface IComponentAlgorithmFactory<TComponentAlgorithm, TAlgorithmDescriptor>
            where TComponentAlgorithm : ComponentAlgorithm, IComponentAlgorithmFactory<TComponentAlgorithm, TAlgorithmDescriptor>
        {
            internal static abstract bool IsAlgorithmSupported(TAlgorithmDescriptor algorithm);
            internal static abstract TComponentAlgorithm GenerateKey(TAlgorithmDescriptor algorithm);
            internal static abstract TComponentAlgorithm ImportPrivateKey(TAlgorithmDescriptor algorithm, ReadOnlySpan<byte> source);
            internal static abstract TComponentAlgorithm ImportPublicKey(TAlgorithmDescriptor algorithm, ReadOnlySpan<byte> source);
        }
#endif

        private abstract class ComponentAlgorithm : IDisposable
        {
            private bool _disposed;

            internal abstract bool TryExportPublicKey(Span<byte> destination, out int bytesWritten);
            internal abstract bool TryExportPrivateKey(Span<byte> destination, out int bytesWritten);

            internal abstract int SignData(
#if NET
                ReadOnlySpan<byte> data,
#else
                byte[] data,
#endif
                Span<byte> destination);

            internal abstract bool VerifyData(
#if NET
                ReadOnlySpan<byte> data,
#else
                byte[] data,
#endif
                ReadOnlySpan<byte> signature);

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    Dispose(true);
                    GC.SuppressFinalize(this);
                }
            }

            protected virtual void Dispose(bool disposing)
            {
            }
        }

        private static Dictionary<CompositeMLDsaAlgorithm, AlgorithmMetadata> CreateAlgorithmMetadata()
        {
            const int count = 18;

            Dictionary<CompositeMLDsaAlgorithm, AlgorithmMetadata> algorithmMetadata = new(count)
            {
                {
                    CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss,
                    new AlgorithmMetadata(
                        MLDsaAlgorithm.MLDsa44,
                        new RsaAlgorithm(2048, HashAlgorithmName.SHA256, RSASignaturePadding.Pss),
                        [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x00],
                        HashAlgorithmName.SHA256)
                },
                {
                    CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15,
                    new AlgorithmMetadata(
                        MLDsaAlgorithm.MLDsa44,
                        new RsaAlgorithm(2048, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
                        [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x01],
                        HashAlgorithmName.SHA256)
                },
                {
                    CompositeMLDsaAlgorithm.MLDsa44WithEd25519,
                    new AlgorithmMetadata(
                        MLDsaAlgorithm.MLDsa44,
                        new EdDsaAlgorithm(),
                        [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x02],
                        HashAlgorithmName.SHA512)
                },
                {
                    CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256,
                    new AlgorithmMetadata(
                        MLDsaAlgorithm.MLDsa44,
                        ECDsaAlgorithm.CreateP256(HashAlgorithmName.SHA256),
                        [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x03],
                        HashAlgorithmName.SHA256)
                },
                {
                    CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss,
                    new AlgorithmMetadata(
                        MLDsaAlgorithm.MLDsa65,
                        new RsaAlgorithm(3072, HashAlgorithmName.SHA256, RSASignaturePadding.Pss),
                        [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x04],
                        HashAlgorithmName.SHA512)
                },
                {
                    CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pkcs15,
                    new AlgorithmMetadata(
                        MLDsaAlgorithm.MLDsa65,
                        new RsaAlgorithm(3072, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
                        [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x05],
                        HashAlgorithmName.SHA512)
                },
                {
                    CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss,
                    new AlgorithmMetadata(
                        MLDsaAlgorithm.MLDsa65,
                        new RsaAlgorithm(4096, HashAlgorithmName.SHA384, RSASignaturePadding.Pss),
                        [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x06],
                        HashAlgorithmName.SHA512)
                },
                {
                    CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pkcs15,
                    new AlgorithmMetadata(
                        MLDsaAlgorithm.MLDsa65,
                        new RsaAlgorithm(4096, HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1),
                        [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x07],
                        HashAlgorithmName.SHA512)
                },
                {
                    CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256,
                    new AlgorithmMetadata(
                        MLDsaAlgorithm.MLDsa65,
                        ECDsaAlgorithm.CreateP256(HashAlgorithmName.SHA256),
                        [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x08],
                        HashAlgorithmName.SHA512)
                },
                {
                    CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384,
                    new AlgorithmMetadata(
                        MLDsaAlgorithm.MLDsa65,
                        ECDsaAlgorithm.CreateP384(HashAlgorithmName.SHA384),
                        [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x09],
                        HashAlgorithmName.SHA512)
                },
                {
                    CompositeMLDsaAlgorithm.MLDsa65WithECDsaBrainpoolP256r1,
                    new AlgorithmMetadata(
                        MLDsaAlgorithm.MLDsa65,
                        ECDsaAlgorithm.CreateBrainpoolP256r1(HashAlgorithmName.SHA256),
                        [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x0A],
                        HashAlgorithmName.SHA512)
                },
                {
                    CompositeMLDsaAlgorithm.MLDsa65WithEd25519,
                    new AlgorithmMetadata(
                        MLDsaAlgorithm.MLDsa65,
                        new EdDsaAlgorithm(),
                        [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x0B],
                        HashAlgorithmName.SHA512)
                },
                {
                    CompositeMLDsaAlgorithm.MLDsa87WithECDsaP384,
                    new AlgorithmMetadata(
                        MLDsaAlgorithm.MLDsa87,
                        ECDsaAlgorithm.CreateP384(HashAlgorithmName.SHA384),
                        [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x0C],
                        HashAlgorithmName.SHA512)
                },
                {
                    CompositeMLDsaAlgorithm.MLDsa87WithECDsaBrainpoolP384r1,
                    new AlgorithmMetadata(
                        MLDsaAlgorithm.MLDsa87,
                        ECDsaAlgorithm.CreateBrainpoolP384r1(HashAlgorithmName.SHA384),
                        [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x0D],
                        HashAlgorithmName.SHA512)
                },
                {
                    CompositeMLDsaAlgorithm.MLDsa87WithEd448,
                    new AlgorithmMetadata(
                        MLDsaAlgorithm.MLDsa87,
                        new EdDsaAlgorithm(),
                        [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x0E],
                        new HashAlgorithmName("SHAKE256"))
                },
                {
                    CompositeMLDsaAlgorithm.MLDsa87WithRSA3072Pss,
                    new AlgorithmMetadata(
                        MLDsaAlgorithm.MLDsa87,
                        new RsaAlgorithm(3072, HashAlgorithmName.SHA256, RSASignaturePadding.Pss),
                        [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x0F],
                        HashAlgorithmName.SHA512)
                },
                {
                    CompositeMLDsaAlgorithm.MLDsa87WithRSA4096Pss,
                    new AlgorithmMetadata(
                        MLDsaAlgorithm.MLDsa87,
                        new RsaAlgorithm(4096, HashAlgorithmName.SHA384, RSASignaturePadding.Pss),
                        [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x10],
                        HashAlgorithmName.SHA512)
                },
                {
                    CompositeMLDsaAlgorithm.MLDsa87WithECDsaP521,
                    new AlgorithmMetadata(
                        MLDsaAlgorithm.MLDsa87,
                        ECDsaAlgorithm.CreateP521(HashAlgorithmName.SHA512),
                        [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x11],
                        HashAlgorithmName.SHA512)
                }
            };

            Debug.Assert(count == algorithmMetadata.Count);

            return algorithmMetadata;
        }

        private sealed class AlgorithmMetadata(
            MLDsaAlgorithm mldsaAlgorithm,
            object traditionalAlgorithm,
            byte[] domainSeparator,
            HashAlgorithmName hashAlgorithmName)
        {
            internal MLDsaAlgorithm MLDsaAlgorithm { get; } = mldsaAlgorithm;
            internal object TraditionalAlgorithm { get; } = traditionalAlgorithm;
            internal byte[] DomainSeparator { get; } = domainSeparator;
            internal HashAlgorithmName HashAlgorithmName { get; } = hashAlgorithmName;
        }

        private sealed class RsaAlgorithm(int keySizeInBits, HashAlgorithmName hashAlgorithmName, RSASignaturePadding padding)
        {
            internal int KeySizeInBits { get; } = keySizeInBits;
            internal HashAlgorithmName HashAlgorithmName { get; } = hashAlgorithmName;
            internal RSASignaturePadding Padding { get; } = padding;
        }

        private sealed class ECDsaAlgorithm
        {
            internal int KeySizeInBits { get; }
            internal HashAlgorithmName HashAlgorithmName { get; }

#if NET || NETSTANDARD
            internal ECCurve Curve { get; }
            internal Oid CurveOid => Curve.Oid;
#else
            internal Oid CurveOid { get; }
            internal KeyBlobMagicNumber PrivateKeyBlobMagicNumber { get; }
            internal KeyBlobMagicNumber PublicKeyBlobMagicNumber { get; }
#endif

            internal string CurveOidValue => CurveOid.Value!;

            internal int KeySizeInBytes => (KeySizeInBits + 7) / 8;

            private ECDsaAlgorithm(
                int keySizeInBits,
#if NET || NETSTANDARD
                ECCurve curve,
#else
                Oid curveOid,
                KeyBlobMagicNumber privateKeyBlobMagicNumber,
                KeyBlobMagicNumber publicKeyBlobMagicNumber,
#endif
                HashAlgorithmName hashAlgorithmName)
            {
                KeySizeInBits = keySizeInBits;
                HashAlgorithmName = hashAlgorithmName;

#if NET || NETSTANDARD
                Curve = curve;
#else
                CurveOid = curveOid;
                PrivateKeyBlobMagicNumber = privateKeyBlobMagicNumber;
                PublicKeyBlobMagicNumber = publicKeyBlobMagicNumber;
#endif

                Debug.Assert(CurveOid.Value is not null);
            }

            internal static ECDsaAlgorithm CreateP256(HashAlgorithmName hashAlgorithmName) =>
                new ECDsaAlgorithm(
                    256,
#if NET || NETSTANDARD
                    ECCurve.NamedCurves.nistP256,
#else
                    new Oid(Oids.secp256r1, "nistP256"),
                    KeyBlobMagicNumber.BCRYPT_ECDSA_PRIVATE_P256_MAGIC,
                    KeyBlobMagicNumber.BCRYPT_ECDSA_PUBLIC_P256_MAGIC,
#endif
                    hashAlgorithmName);

            internal static ECDsaAlgorithm CreateP384(HashAlgorithmName hashAlgorithmName) =>
                new ECDsaAlgorithm(
                    384,
#if NET || NETSTANDARD
                    ECCurve.NamedCurves.nistP384,
#else
                    new Oid(Oids.secp384r1, "nistP384"),
                    KeyBlobMagicNumber.BCRYPT_ECDSA_PRIVATE_P384_MAGIC,
                    KeyBlobMagicNumber.BCRYPT_ECDSA_PUBLIC_P384_MAGIC,
#endif
                    hashAlgorithmName);

            internal static ECDsaAlgorithm CreateP521(HashAlgorithmName hashAlgorithmName) =>
                new ECDsaAlgorithm(
                    521,
#if NET || NETSTANDARD
                    ECCurve.NamedCurves.nistP521,
#else
                    new Oid(Oids.secp521r1, "nistP521"),
                    KeyBlobMagicNumber.BCRYPT_ECDSA_PRIVATE_P521_MAGIC,
                    KeyBlobMagicNumber.BCRYPT_ECDSA_PUBLIC_P521_MAGIC,
#endif
                    hashAlgorithmName);

            internal static ECDsaAlgorithm CreateBrainpoolP256r1(HashAlgorithmName hashAlgorithmName) =>
                new ECDsaAlgorithm(
                    256,
#if NET || NETSTANDARD
                    ECCurve.NamedCurves.brainpoolP256r1,
#else
                    new Oid(Oids.brainpoolP256r1, "brainpoolP256r1"),
                    KeyBlobMagicNumber.BCRYPT_ECDSA_PRIVATE_GENERIC_MAGIC,
                    KeyBlobMagicNumber.BCRYPT_ECDSA_PUBLIC_GENERIC_MAGIC,
#endif
                    hashAlgorithmName);

            internal static ECDsaAlgorithm CreateBrainpoolP384r1(HashAlgorithmName hashAlgorithmName) =>
                new ECDsaAlgorithm(
                    384,
#if NET || NETSTANDARD
                    ECCurve.NamedCurves.brainpoolP384r1,
#else
                    new Oid(Oids.brainpoolP384r1, "brainpoolP384r1"),
                    KeyBlobMagicNumber.BCRYPT_ECDSA_PRIVATE_GENERIC_MAGIC,
                    KeyBlobMagicNumber.BCRYPT_ECDSA_PUBLIC_GENERIC_MAGIC,
#endif
                    hashAlgorithmName);
        }

        private sealed class EdDsaAlgorithm
        {
        }
    }
}
