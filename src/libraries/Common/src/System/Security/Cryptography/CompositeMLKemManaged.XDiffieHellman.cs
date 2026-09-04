// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal sealed partial class CompositeMLKemManaged
    {
        private sealed class XDiffieHellmanKem : TraditionalKem
        {
            private X25519DiffieHellman _key;

            private XDiffieHellmanKem(X25519DiffieHellman key)
            {
                _key = key;
            }

            internal static bool IsAlgorithmSupported(XDiffieHellmanKemAlgorithm algorithm) =>
                X25519DiffieHellman.IsSupported && algorithm.IsX25519;

            internal static XDiffieHellmanKem GenerateKey(XDiffieHellmanKemAlgorithm algorithm)
            {
                Debug.Assert(algorithm.IsX25519);

                return new XDiffieHellmanKem(X25519DiffieHellman.GenerateKey());
            }

            internal static XDiffieHellmanKem ImportPublicKey(
                XDiffieHellmanKemAlgorithm algorithm,
                ReadOnlySpan<byte> source)
            {
                Debug.Assert(algorithm.IsX25519);

                return new XDiffieHellmanKem(X25519DiffieHellman.ImportPublicKey(source));
            }

            internal static XDiffieHellmanKem ImportPrivateKey(
                XDiffieHellmanKemAlgorithm algorithm,
                ReadOnlySpan<byte> source)
            {
                Debug.Assert(algorithm.IsX25519);

                return new XDiffieHellmanKem(X25519DiffieHellman.ImportPrivateKey(source));
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

                using (X25519DiffieHellman ephemeral = X25519DiffieHellman.GenerateKey())
                {
                    ephemeral.ExportPublicKey(ciphertext);

                    // The caller clears sharedSecret, even when this method throws, so no cleanup is needed here.
                    ephemeral.DeriveRawSecretAgreement(_key, sharedSecret);
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

                // The caller clears sharedSecret, even when this method throws, so no cleanup is needed here.
                _key.DeriveRawSecretAgreement(ciphertext, sharedSecret);
            }

            internal override int ExportPublicKey(Span<byte> destination)
            {
                _key.ExportPublicKey(destination);
                return X25519DiffieHellman.PublicKeySizeInBytes;
            }

            internal override int ExportPrivateKey(Span<byte> destination)
            {
                _key.ExportPrivateKey(destination);
                return X25519DiffieHellman.PrivateKeySizeInBytes;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _key?.Dispose();
                    _key = null!;
                }

                base.Dispose(disposing);
            }
        }
    }
}
