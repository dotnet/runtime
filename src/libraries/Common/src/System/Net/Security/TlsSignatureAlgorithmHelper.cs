// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;

namespace System.Net.Security
{
    internal static class TlsSignatureAlgorithmHelper
    {
        private const ushort SignatureAlgorithmsExtension = 13;

        internal static bool TryGetFamiliesFromClientHello(
            ReadOnlySpan<byte> clientHello,
            out TlsSignatureAlgorithmFamilies signatureAlgorithmFamilies)
        {
            // https://www.rfc-editor.org/rfc/rfc8446.html#section-4.1.2
            const int HandshakeHeaderLength = 4;
            const int ClientHelloFixedLength = 2 + 32;

            signatureAlgorithmFamilies = TlsSignatureAlgorithmFamilies.None;

            if (clientHello.Length < HandshakeHeaderLength + ClientHelloFixedLength ||
                clientHello[0] != 1)
            {
                return false;
            }

            int helloLength = (clientHello[1] << 16) | (clientHello[2] << 8) | clientHello[3];
            if (helloLength != clientHello.Length - HandshakeHeaderLength)
            {
                return false;
            }

            ReadOnlySpan<byte> hello = clientHello.Slice(HandshakeHeaderLength + ClientHelloFixedLength);
            if (!TrySkipOpaque1(ref hello) ||
                !TrySkipOpaque2(ref hello) ||
                !TrySkipOpaque1(ref hello))
            {
                return false;
            }

            if (hello.IsEmpty)
            {
                return true;
            }

            if (hello.Length < sizeof(ushort))
            {
                return false;
            }

            int extensionsLength = BinaryPrimitives.ReadUInt16BigEndian(hello);
            hello = hello.Slice(sizeof(ushort));
            if (extensionsLength != hello.Length)
            {
                return false;
            }

            while (hello.Length >= 2 * sizeof(ushort))
            {
                ushort extensionType = BinaryPrimitives.ReadUInt16BigEndian(hello);
                int extensionLength = BinaryPrimitives.ReadUInt16BigEndian(hello.Slice(sizeof(ushort)));
                hello = hello.Slice(2 * sizeof(ushort));

                if (extensionLength > hello.Length)
                {
                    return false;
                }

                if (extensionType == SignatureAlgorithmsExtension)
                {
                    return TryGetFamiliesFromExtension(
                        hello.Slice(0, extensionLength),
                        out signatureAlgorithmFamilies);
                }

                hello = hello.Slice(extensionLength);
            }

            return hello.IsEmpty;
        }

        internal static bool TryGetFamiliesFromExtension(
            ReadOnlySpan<byte> extensionData,
            out TlsSignatureAlgorithmFamilies signatureAlgorithmFamilies)
        {
            // https://www.rfc-editor.org/rfc/rfc8446.html#section-4.2.3
            signatureAlgorithmFamilies = TlsSignatureAlgorithmFamilies.None;

            if (extensionData.Length < sizeof(ushort))
            {
                return false;
            }

            int signatureAlgorithmsLength = BinaryPrimitives.ReadUInt16BigEndian(extensionData);
            ReadOnlySpan<byte> signatureAlgorithms = extensionData.Slice(sizeof(ushort));

            if (signatureAlgorithmsLength == 0 ||
                signatureAlgorithmsLength != signatureAlgorithms.Length ||
                (signatureAlgorithmsLength & 1) != 0)
            {
                return false;
            }

            while (!signatureAlgorithms.IsEmpty)
            {
                ushort signatureScheme = BinaryPrimitives.ReadUInt16BigEndian(signatureAlgorithms);
                signatureAlgorithms = signatureAlgorithms.Slice(sizeof(ushort));

                signatureAlgorithmFamilies |= signatureScheme switch
                {
                    0x0201 or // rsa_pkcs1_sha1
                    0x0401 or // rsa_pkcs1_sha256
                    0x0501 or // rsa_pkcs1_sha384
                    0x0601 or // rsa_pkcs1_sha512
                    0x0804 or // rsa_pss_rsae_sha256
                    0x0805 or // rsa_pss_rsae_sha384
                    0x0806 or // rsa_pss_rsae_sha512
                    0x0809 or // rsa_pss_pss_sha256
                    0x080A or // rsa_pss_pss_sha384
                    0x080B => // rsa_pss_pss_sha512
                        TlsSignatureAlgorithmFamilies.Rsa,

                    0x0203 or // ecdsa_sha1
                    0x0403 or // ecdsa_secp256r1_sha256
                    0x0503 or // ecdsa_secp384r1_sha384
                    0x0603 or // ecdsa_secp521r1_sha512
                    0x081A or // ecdsa_brainpoolP256r1tls13_sha256
                    0x081B or // ecdsa_brainpoolP384r1tls13_sha384
                    0x081C => // ecdsa_brainpoolP512r1tls13_sha512
                        TlsSignatureAlgorithmFamilies.ECDsa,

                    0x0807 or // ed25519
                    0x0808 => // ed448
                        TlsSignatureAlgorithmFamilies.EdDsa,

                    0x0904 or // mldsa44
                    0x0905 or // mldsa65
                    0x0906 => // mldsa87
                        TlsSignatureAlgorithmFamilies.MLDsa,

                    >= 0x0911 and <= 0x091C =>
                        TlsSignatureAlgorithmFamilies.SlhDsa,

                    _ => TlsSignatureAlgorithmFamilies.None,
                };
            }

            return true;
        }

        private static bool TrySkipOpaque1(ref ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty || data.Length < data[0] + 1)
            {
                return false;
            }

            data = data.Slice(data[0] + 1);
            return true;
        }

        private static bool TrySkipOpaque2(ref ReadOnlySpan<byte> data)
        {
            if (data.Length < sizeof(ushort))
            {
                return false;
            }

            int length = BinaryPrimitives.ReadUInt16BigEndian(data);
            if (data.Length < sizeof(ushort) + length)
            {
                return false;
            }

            data = data.Slice(sizeof(ushort) + length);
            return true;
        }
    }
}
