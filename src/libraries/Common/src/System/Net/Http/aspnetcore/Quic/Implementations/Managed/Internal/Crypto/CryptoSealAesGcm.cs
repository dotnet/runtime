using System.Diagnostics;
using System.Security.Cryptography;

namespace System.Net.Quic.Implementations.Managed.Internal.Crypto
{
    internal class CryptoSealAesGcm
    {
        private const int TagLength = 16;
        private const int SampleLength = 16;

        public byte[] IV { get; }
        public byte[] Key { get; }
        public byte[] HeaderKey { get; }

        private AesGcm _aes;

        public CryptoSealAesGcm(byte[] iv, byte[] key, byte[] headerKey)
        {
            IV = iv;
            Key = key;
            HeaderKey = headerKey;
            _aes = new AesGcm(key);
        }

        public void EncryptPacket(Span<byte> buffer, int payloadOffset, int payloadLength, ulong packetNumber)
        {
            var nonce = new byte[IV.Length];
            MakeNonce(IV, packetNumber, nonce);

            // split packet buffer into spans
            var header = buffer.Slice(0, payloadOffset);
            var payload = buffer.Slice(payloadOffset, payloadLength);
            var tag = buffer.Slice(payloadOffset + payloadLength, TagLength);

            // encrypt payload in-place
            _aes.Encrypt(nonce, payload, payload, tag, header);

            // apply header protection
            var payloadSample = payload.Slice(0, SampleLength);
            var protectionMask = Encryption.GetHeaderProtectionMask(Algorithm.AEAD_AES_128_GCM, HeaderKey, payloadSample);

            Encryption.ProtectHeader(header, protectionMask);
        }

        public void DecryptPacket(Span<byte> buffer)
        {
            // remove header protection
            // TODO-RZ: calculate these properly by parsing the header
            int pnOffset = 18;
            int payloadLength = 1162;

            // this works on unprotected header only
            // int pnLength = (buffer[0] & 0x03) + 1;

            var sample = buffer.Slice(pnOffset + 4, SampleLength);
            var mask = Encryption.GetHeaderProtectionMask(Algorithm.AEAD_AES_128_GCM, HeaderKey, sample);
            var header = buffer.Slice(0, pnOffset + 4);

            int pnLength = Encryption.UnprotectHeader(header, mask, pnOffset);
            // correct the span size after establishing the packet number length
            header = header.Slice(0, pnOffset + pnLength);

            ulong packetNumber = 0;
            for (int i = 0; i < pnLength; ++i)
            {
                packetNumber = (packetNumber << 8) | buffer[pnOffset + i];
            }
            packetNumber = Encoder.DecodePacketNumber(0, packetNumber, pnLength);

            var nonce = new byte[IV.Length];
            MakeNonce(IV, packetNumber, nonce);

            var ciphertext = buffer.Slice(header.Length, payloadLength);
            var tag = buffer.Slice(header.Length + ciphertext.Length, TagLength);

            // decrypt in-place
            _aes.Decrypt(nonce, ciphertext, tag, ciphertext, header);
        }

        private static void MakeNonce(ReadOnlySpan<byte> iv, ulong packetNumber, Span<byte> nonce)
        {
            Debug.Assert(iv.Length == 12);
            Debug.Assert(iv.Length >= 12);

            // From RFC: The nonce, N, is formed by combining the packet
            // protection IV with the packet number.  The 62 bits of the
            // reconstructed QUIC packet number in network byte order are left-
            // padded with zeros to the size of the IV.  The exclusive OR of the
            // padded packet number and the IV forms the AEAD nonce.

            iv.CopyTo(nonce);
            for (int i = 0; i < 8; i++)
            {
                nonce[nonce.Length - 1 - i] ^= (byte) (packetNumber >> (i * 8));
            }
        }

    }
}
