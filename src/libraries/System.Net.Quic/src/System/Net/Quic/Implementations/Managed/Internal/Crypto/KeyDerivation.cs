// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace System.Net.Quic.Implementations.Managed.Internal.Crypto
{
    internal static class KeyDerivation
    {
        private static readonly HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA256;
        private const int InitialSecretLength = 256 / 8;

        public static readonly byte[] initialSalt =
        {
            0xc3, 0xee, 0xf7, 0x12, 0xc7, 0x2e, 0xbb, 0x5a, 0x11, 0xa7, 0xd2, 0x43, 0x2b, 0xb4, 0x63, 0x65, 0xbe,
            0xf9, 0xf5, 0x02
        };

        public static byte[] ExpandLabel(ReadOnlySpan<byte> prk, string label, int length)
        {
            // create the hkdfLabel structure locally, as defined by the RFC8446, Section 7.1.
            // both label and context fields are length prefixed
            //
            // struct {
            //     uint16 length = Length;
            //     opaque label<7..255> = "tls13 " + Label;
            //     opaque context<0..255> = Context;
            // } HkdfLabel;

            const string tls13Prefix = "tls13 ";
            int hkdfLabelSize = sizeof(short) +
                                1 + tls13Prefix.Length + label.Length +
                                1; // context is empty string in our case

            Debug.Assert(hkdfLabelSize < 32);
            Span<byte> hkdfLabel = stackalloc byte[hkdfLabelSize];
            // length is in big endian
            BinaryPrimitives.WriteUInt16BigEndian(hkdfLabel, (ushort) length);

            // write label
            hkdfLabel[2] = (byte)(label.Length + tls13Prefix.Length);
            Encoding.ASCII.GetBytes(tls13Prefix, hkdfLabel.Slice(3));
            Encoding.ASCII.GetBytes(label, hkdfLabel.Slice(3 + tls13Prefix.Length));

            // write context empty string
            hkdfLabel[hkdfLabelSize - 1] = 0;

            var result = new byte[length];
            HKDF.Expand(hashAlgorithm, prk, result, hkdfLabel);
            return result;
        }

        private static void DeriveInitialSecret(ReadOnlySpan<byte> dcid, Span<byte> secret)
        {
            HKDF.Extract(hashAlgorithm, dcid, initialSalt, secret);
        }

        public static byte[] DeriveClientInitialSecret(ReadOnlySpan<byte> dcid)
        {
            Span<byte> initial = stackalloc byte[InitialSecretLength];
            DeriveInitialSecret(dcid, initial);
            return ExpandLabel(initial, "client in", 32);
        }

        public static byte[] DeriveServerInitialSecret(ReadOnlySpan<byte> dcid)
        {
            Span<byte> initial = stackalloc byte[InitialSecretLength];
            DeriveInitialSecret(dcid, initial);
            return ExpandLabel(initial, "server in", 32);
        }

        public static byte[] DeriveKey(ReadOnlySpan<byte> prk)
        {
            return ExpandLabel(prk, "quic key", 16);
        }

        public static byte[] DeriveIv(ReadOnlySpan<byte> prk)
        {
            return ExpandLabel(prk, "quic iv", 12);
        }

        public static byte[] DeriveHp(ReadOnlySpan<byte> prk)
        {
            return ExpandLabel(prk, "quic hp", 16);
        }

        public static byte[] UpdateSecret(ReadOnlySpan<byte> oldSecret)
        {
            return ExpandLabel(oldSecret, "quic ku", oldSecret.Length);
        }
    }
}
