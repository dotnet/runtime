// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    internal sealed class ManagedCompositeMLDsaComposer : CompositeMLDsa
    {
        private MLDsa _mldsa;
        private ComponentAlgorithm _componentAlgorithm;

        private ManagedCompositeMLDsaComposer(CompositeMLDsaAlgorithm algorithm, MLDsa mldsa, ComponentAlgorithm componentAlgorithm)
            : base(algorithm)
        {
            _mldsa = mldsa;
            _componentAlgorithm = componentAlgorithm;
        }

        internal static bool SupportsAny() => MLDsaImplementation.SupportsAny() && (RsaComponent.IsSupported || ECDsaComponent.IsSupported);

        internal static bool IsAlgorithmSupportedImpl(CompositeMLDsaAlgorithm algorithm)
        {
            return MLDsaImplementation.IsAlgorithmSupported(algorithm.MLDsaAlgorithm) && algorithm.TraditionalAlgorithm switch
            {
                CompositeMLDsaAlgorithm.RsaAlgorithm rsaAlgorithm => RsaComponent.IsAlgorithmSupported(rsaAlgorithm),
                _ => false,
            };
        }

        internal static CompositeMLDsa GenerateKeyImpl(CompositeMLDsaAlgorithm algorithm) =>
            throw new PlatformNotSupportedException();

        internal static CompositeMLDsa ImportCompositeMLDsaPublicKeyImpl(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(IsAlgorithmSupportedImpl(algorithm));

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

            ReadOnlySpan<byte> mldsaKey = source.Slice(0, algorithm.MLDsaAlgorithm.PublicKeySizeInBytes);
            ReadOnlySpan<byte> tradKey = source.Slice(algorithm.MLDsaAlgorithm.PublicKeySizeInBytes);

            MLDsaImplementation mldsa = MLDsaImplementation.ImportPublicKey(algorithm.MLDsaAlgorithm, mldsaKey);
            ComponentAlgorithm componentAlgorithm = algorithm.TraditionalAlgorithm switch
            {
                CompositeMLDsaAlgorithm.RsaAlgorithm rsaAlgorithm => RsaComponent.ImportPublicKey(rsaAlgorithm, tradKey),
                _ => throw FailAndGetException(),
            };

            static CryptographicException FailAndGetException()
            {
                Debug.Fail("Only supported algorithms should reach here.");
                return new CryptographicException();
            }

            return new ManagedCompositeMLDsaComposer(algorithm, mldsa, componentAlgorithm);
        }

        internal static CompositeMLDsa ImportCompositeMLDsaPrivateKeyImpl(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(IsAlgorithmSupportedImpl(algorithm));

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

            ReadOnlySpan<byte> mldsaKey = source.Slice(0, algorithm.MLDsaAlgorithm.PrivateSeedSizeInBytes);
            ReadOnlySpan<byte> tradKey = source.Slice(algorithm.MLDsaAlgorithm.PrivateSeedSizeInBytes);

            MLDsaImplementation mldsa = MLDsaImplementation.ImportSeed(algorithm.MLDsaAlgorithm, mldsaKey);
            ComponentAlgorithm componentAlgorithm = algorithm.TraditionalAlgorithm switch
            {
                CompositeMLDsaAlgorithm.RsaAlgorithm rsaAlgorithm => RsaComponent.ImportPrivateKey(rsaAlgorithm, tradKey),
                _ => throw FailAndGetException(),
            };

            static CryptographicException FailAndGetException()
            {
                Debug.Fail("Only supported algorithms should reach here.");
                return new CryptographicException();
            }

            return new ManagedCompositeMLDsaComposer(algorithm, mldsa, componentAlgorithm);
        }

        protected override bool TrySignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination, out int bytesWritten)
        {
            // draft-ietf-lamps-pq-composite-sigs-latest (June 20, 2025), 4.2
            //  1.  If len(ctx) > 255:
            //      return error

            if (context.Length > 255)
            {
                Debug.Fail("This should have been validated by caller.");
                throw new CryptographicException();
            }

            //  2.  Compute the Message representative M'.
            //      As in FIPS 204, len(ctx) is encoded as a single unsigned byte.
            //      Randomize the message representative
            //
            //          r = Random(32)
            //          M' :=  Prefix || Domain || len(ctx) || ctx || r
            //                                              || PH( M )

#if NET
            Span<byte> r = stackalloc byte[32];
            RandomNumberGenerator.Fill(r);
#else
            // TODO: add polyfill for RandomNumberGenerator.Fill
            byte[] rBytes = new byte[32];
            new Random().NextBytes(rBytes);
            Span<byte> r = rBytes;
#endif

            ReadOnlySpan<byte> M_prime;

            using (CompositeMLDsaMessageEncoder encoder = new(Algorithm, context, r))
            {
                encoder.AppendData(data);
                M_prime = encoder.GetMessageRepresentativeAndDispose();
            }

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

            bool mldsaSigned = false;
            bool tradSigned = false;

            try
            {
                // TODO should we use a temporary buffer for the signature? Currently if ML-DSA succeeds but the traditional fails,
                // then the ML-DSA signature will be observable briefly before we clear it.
                _mldsa.SignData(M_prime, destination.Slice(32, Algorithm.MLDsaAlgorithm.SignatureSizeInBytes), Algorithm.DomainSeparator);
                mldsaSigned = true;
            }
            catch (CryptographicException)
            {
            }

            int tradBytesWritten = 0;
            bool tradResult = false;

            try
            {
                tradResult = _componentAlgorithm.TrySignData(M_prime, destination.Slice(32 + Algorithm.MLDsaAlgorithm.SignatureSizeInBytes), out tradBytesWritten);
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

            if (mldsaSigned is false || tradSigned is false)
            {
                CryptographicOperations.ZeroMemory(destination);
                throw new CryptographicException(); // TODO resx
            }
            else if (tradResult is false)
            {
                CryptographicOperations.ZeroMemory(destination);
                bytesWritten = 0;
                return false;
            }

            //  6.  Output the encoded composite signature value.
            //
            //          s = SerializeSignatureValue(r, mldsaSig, tradSig)
            //          return s

            r.CopyTo(destination.Slice(0, 32));
            bytesWritten = 32 + Algorithm.MLDsaAlgorithm.SignatureSizeInBytes + tradBytesWritten;
            return true;
        }

        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature)
        {
            // draft-ietf-lamps-pq-composite-sigs-latest (June 20, 2025), 4.3
            //  1.  If len(ctx) > 255
            //          return error

            if (context.Length > 255)
            {
                Debug.Fail("This should have been validated by caller.");
                throw new CryptographicException();
            }

            //  2.  Separate the keys and signatures
            //
            //          (mldsaPK, tradPK)       = DeserializePublicKey(pk)
            //          (r, mldsaSig, tradSig)  = DeserializeSignatureValue(s)
            //
            //      If Error during deserialization, or if any of the component
            //      keys or signature values are not of the correct type or
            //      length for the given component algorithm then output
            //      "Invalid signature" and stop.

            ReadOnlySpan<byte> r = signature.Slice(0, 32);
            ReadOnlySpan<byte> mldsaSig = signature.Slice(32, Algorithm.MLDsaAlgorithm.SignatureSizeInBytes);
            ReadOnlySpan<byte> tradSig = signature.Slice(32 + Algorithm.MLDsaAlgorithm.SignatureSizeInBytes);

            //  3.  Compute a Hash of the Message.
            //      As in FIPS 204, len(ctx) is encoded as a single unsigned byte.
            //
            //          M' = Prefix || Domain || len(ctx) || ctx || r
            //                                                   || PH( M )

            ReadOnlySpan<byte> M_prime;

            using (CompositeMLDsaMessageEncoder encoder = new(Algorithm, context, r))
            {
                encoder.AppendData(data);
                M_prime = encoder.GetMessageRepresentativeAndDispose();
            }

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

            return _mldsa.VerifyData(M_prime, mldsaSig, Algorithm.DomainSeparator) && _componentAlgorithm.VerifyData(M_prime, tradSig);
        }

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten) =>
            throw new PlatformNotSupportedException();

        protected override bool TryExportCompositeMLDsaPublicKeyCore(Span<byte> destination, out int bytesWritten)
        {
            // draft-ietf-lamps-pq-composite-sigs-latest (June 20, 2025), 5.1
            //  1.  Combine and output the encoded public key
            //
            //      output mldsaPK || tradPK

            _mldsa.ExportMLDsaPublicKey(destination.Slice(0, Algorithm.MLDsaAlgorithm.PublicKeySizeInBytes));

            if (_componentAlgorithm.TryExportPublicKey(destination.Slice(Algorithm.MLDsaAlgorithm.PublicKeySizeInBytes), out int componentBytesWritten))
            {
                bytesWritten = Algorithm.MLDsaAlgorithm.PublicKeySizeInBytes + componentBytesWritten;
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        protected override bool TryExportCompositeMLDsaPrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            // draft-ietf-lamps-pq-composite-sigs-latest (June 20, 2025), 5.2
            //  1.  Combine and output the encoded private key
            //
            //      output mldsaSeed || tradSK

            try
            {
                _mldsa.ExportMLDsaPrivateSeed(destination.Slice(0, Algorithm.MLDsaAlgorithm.PrivateSeedSizeInBytes));

                if (_componentAlgorithm.TryExportPrivateKey(destination.Slice(Algorithm.MLDsaAlgorithm.PrivateSeedSizeInBytes), out int componentBytesWritten))
                {
                    bytesWritten = Algorithm.MLDsaAlgorithm.PrivateSeedSizeInBytes + componentBytesWritten;
                    return true;
                }

                bytesWritten = 0;
                return false;
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

#if DESIGNTIMEINTERFACES
        private interface IComponentAlgorithmFactory<TComponentAlgorithm, TAlgorithmDescriptor>
            where TComponentAlgorithm : ComponentAlgorithm, IComponentAlgorithmFactory<TComponentAlgorithm, TAlgorithmDescriptor>
        {
            internal static abstract bool IsSupported { get; }
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
            internal abstract bool TrySignData(ReadOnlySpan<byte> data, Span<byte> destination, out int bytesWritten);
            internal abstract bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature);

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

        private sealed class RsaComponent : ComponentAlgorithm
#if DESIGNTIMEINTERFACES
#pragma warning disable SA1001 // Commas should be spaced correctly
            , IComponentAlgorithmFactory<RsaComponent, CompositeMLDsaAlgorithm.RsaAlgorithm>
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

            public static bool IsSupported
            {
                get
                {
#if !NETFRAMEWORK
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER")))
                    {
                        return false;
                    }
#endif

                    return true;
                }
            }


            // TODO cache, use LegalKeySizes instead of try/finally
            public static bool IsAlgorithmSupported(CompositeMLDsaAlgorithm.RsaAlgorithm algorithm)
            {
#if !NETFRAMEWORK
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER")))
                {
                    return false;
                }
#endif

                try
                {
                    using RSA rsa = RSA.Create();
                    rsa.KeySize = algorithm.KeySizeInBits;
                    return true;
                }
                catch (CryptographicException)
                {
                    return false;
                }
            }

            internal override bool TrySignData(ReadOnlySpan<byte> data, Span<byte> destination, out int bytesWritten)
            {
#if NET
                return _rsa.TrySignData(data, destination, _hashAlgorithmName, _padding, out bytesWritten);
#else
                // TODO data (msg representative) can be passed as an array (caller of TrySignData can allocate instead of rent)

                // Composite ML-DSA virtual methods only accept ROS<byte> so we need to use ToArray() for signature
                byte[] signature = _rsa.SignData(data.ToArray(), _hashAlgorithmName, _padding);

                if (signature.AsSpan().TryCopyTo(destination))
                {
                    bytesWritten = signature.Length;
                    return true;
                }

                bytesWritten = 0;
                CryptographicOperations.ZeroMemory(destination);
                return false;
#endif
            }

            internal override bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature)
            {
#if NET
                return _rsa.VerifyData(data, signature, _hashAlgorithmName, _padding);
#else
                // TODO data (msg representative) can be passed as an array (caller of VerifyData can allocate instead of rent)

                // Composite ML-DSA virtual methods only accept ROS<byte> so we need to use ToArray() for signature
                return _rsa.VerifyData(data.ToArray(), signature.ToArray(), _hashAlgorithmName, _padding);
#endif
            }

            public static RsaComponent GenerateKey(CompositeMLDsaAlgorithm.RsaAlgorithm algorithm) => throw new NotImplementedException();
            public static RsaComponent ImportPrivateKey(CompositeMLDsaAlgorithm.RsaAlgorithm algorithm, ReadOnlySpan<byte> source)
            {
                Debug.Assert(IsAlgorithmSupported(algorithm));
#if NETFRAMEWORK || NETSTANDARD2_0
                // TODO netfx only has ImportParameters, so we need to implement the conversion from raw key to parameters
                throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(RSA)));
#else
                Debug.Assert(!RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER")));

                RSA? rsa = null;

                try
                {
                    rsa = RSA.Create();
                    rsa.ImportRSAPrivateKey(source, out int bytesRead);

                    if (bytesRead != source.Length)
                    {
                        // TODO resx
                        throw new CryptographicException();
                    }
                }
                catch (CryptographicException)
                {
                    rsa?.Dispose();
                    throw;
                }

                return new RsaComponent(rsa, algorithm.HashAlgorithmName, algorithm.Padding);
#endif
            }

            public static RsaComponent ImportPublicKey(CompositeMLDsaAlgorithm.RsaAlgorithm algorithm, ReadOnlySpan<byte> source)
            {
                Debug.Assert(IsAlgorithmSupported(algorithm));

#if NETFRAMEWORK || NETSTANDARD2_0
                // TODO netfx only has ImportParameters, so we need to implement the conversion from raw key to parameters
                throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(RSA)));
#else
                // TODO move browser to different file
                Debug.Assert(!RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER")));

                RSA? rsa = null;

                try
                {
                    rsa = RSA.Create();
                    rsa.ImportRSAPublicKey(source, out int bytesRead);

                    if (bytesRead != source.Length)
                    {
                        // TODO resx
                        throw new CryptographicException();
                    }
                }
                catch (CryptographicException)
                {
                    rsa?.Dispose();
                    throw;
                }

                return new RsaComponent(rsa, algorithm.HashAlgorithmName, algorithm.Padding);
#endif
            }

            internal override bool TryExportPublicKey(Span<byte> destination, out int bytesWritten)
            {
#if NETFRAMEWORK || NETSTANDARD2_0
                // TODO netfx only has ImportParameters, so we need to implement the conversion from raw key to parameters
                throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(RSA)));
#else
                return _rsa.TryExportRSAPublicKey(destination, out bytesWritten);
#endif
            }

            internal override bool TryExportPrivateKey(Span<byte> destination, out int bytesWritten)
            {
#if NETFRAMEWORK || NETSTANDARD2_0
                // TODO netfx only has ImportParameters, so we need to implement the conversion from raw key to parameters
                throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(RSA)));
#else
                return _rsa.TryExportRSAPrivateKey(destination, out bytesWritten);
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

        private sealed class ECDsaComponent : ComponentAlgorithm
#if DESIGNTIMEINTERFACES
#pragma warning disable SA1001 // Commas should be spaced correctly
            , IComponentAlgorithmFactory<ECDsaComponent, CompositeMLDsaAlgorithm.ECDsaAlgorithm>
#pragma warning restore SA1001 // Commas should be spaced correctly
#endif
        {
            public static bool IsSupported => false;
            public static bool IsAlgorithmSupported(CompositeMLDsaAlgorithm.ECDsaAlgorithm _) => false;
            public static ECDsaComponent GenerateKey(CompositeMLDsaAlgorithm.ECDsaAlgorithm algorithm) => throw new NotImplementedException();
            public static ECDsaComponent ImportPrivateKey(CompositeMLDsaAlgorithm.ECDsaAlgorithm algorithm, ReadOnlySpan<byte> source) => throw new NotImplementedException();
            public static ECDsaComponent ImportPublicKey(CompositeMLDsaAlgorithm.ECDsaAlgorithm algorithm, ReadOnlySpan<byte> source) => throw new NotImplementedException();

            internal override bool TryExportPrivateKey(Span<byte> destination, out int bytesWritten) => throw new NotImplementedException();
            internal override bool TryExportPublicKey(Span<byte> destination, out int bytesWritten) => throw new NotImplementedException();
            internal override bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature) => throw new NotImplementedException();
            internal override bool TrySignData(ReadOnlySpan<byte> data, Span<byte> destination, out int bytesWritten) => throw new NotImplementedException();
        }
    }
}
