// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography
{
    internal static partial class KeyFormatHelper
    {
        internal delegate void KeyReader<TRet>(ReadOnlyMemory<byte> key, in AlgorithmIdentifierAsn algId, out TRet ret);

        internal static unsafe void ReadSubjectPublicKeyInfo<TRet>(
            string[] validOids,
            ReadOnlySpan<byte> source,
            KeyReader<TRet> keyReader,
            out int bytesRead,
            out TRet ret)
        {
            fixed (byte* ptr = &MemoryMarshal.GetReference(source))
            {
                using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, source.Length))
                {
                    ReadSubjectPublicKeyInfo(validOids, manager.Memory, keyReader, out bytesRead, out ret);
                }
            }
        }

        internal static ReadOnlyMemory<byte> ReadSubjectPublicKeyInfo(
            string[] validOids,
            ReadOnlyMemory<byte> source,
            out int bytesRead)
        {
            SubjectPublicKeyInfoAsn spki;
            int read;

            try
            {
                // X.509 SubjectPublicKeyInfo is described as DER.
                AsnValueReader reader = new AsnValueReader(source.Span, AsnEncodingRules.DER);
                read = reader.PeekEncodedValue().Length;
                SubjectPublicKeyInfoAsn.Decode(ref reader, source, out spki);
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }

            if (Array.IndexOf(validOids, spki.Algorithm.Algorithm) < 0)
            {
                throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
            }

            bytesRead = read;
            return spki.SubjectPublicKey;
        }

        private static void ReadSubjectPublicKeyInfo<TRet>(
            string[] validOids,
            ReadOnlyMemory<byte> source,
            KeyReader<TRet> keyReader,
            out int bytesRead,
            out TRet ret)
        {
            SubjectPublicKeyInfoAsn spki;
            int read;

            try
            {
                // X.509 SubjectPublicKeyInfo is described as DER.
                AsnValueReader reader = new AsnValueReader(source.Span, AsnEncodingRules.DER);
                read = reader.PeekEncodedValue().Length;
                SubjectPublicKeyInfoAsn.Decode(ref reader, source, out spki);
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }

            if (Array.IndexOf(validOids, spki.Algorithm.Algorithm) < 0)
            {
                throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
            }

            keyReader(spki.SubjectPublicKey, spki.Algorithm, out ret);
            bytesRead = read;
        }

        internal static unsafe void ReadPkcs8<TRet>(
            string[] validOids,
            ReadOnlySpan<byte> source,
            KeyReader<TRet> keyReader,
            out int bytesRead,
            out TRet ret)
        {
            fixed (byte* ptr = &MemoryMarshal.GetReference(source))
            {
                using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, source.Length))
                {
                    ReadPkcs8(validOids, manager.Memory, keyReader, out bytesRead, out ret);
                }
            }
        }

        internal static ReadOnlyMemory<byte> ReadPkcs8(
            string[] validOids,
            ReadOnlyMemory<byte> source,
            out int bytesRead)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(source.Span, AsnEncodingRules.BER);
                int read = reader.PeekEncodedValue().Length;
                PrivateKeyInfoAsn.Decode(ref reader, source, out PrivateKeyInfoAsn privateKeyInfo);

                if (Array.IndexOf(validOids, privateKeyInfo.PrivateKeyAlgorithm.Algorithm) < 0)
                {
                    throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
                }

                bytesRead = read;
                return privateKeyInfo.PrivateKey;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        private static void ReadPkcs8<TRet>(
            string[] validOids,
            ReadOnlyMemory<byte> source,
            KeyReader<TRet> keyReader,
            out int bytesRead,
            out TRet ret)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(source.Span, AsnEncodingRules.BER);
                int read = reader.PeekEncodedValue().Length;
                PrivateKeyInfoAsn.Decode(ref reader, source, out PrivateKeyInfoAsn privateKeyInfo);

                if (Array.IndexOf(validOids, privateKeyInfo.PrivateKeyAlgorithm.Algorithm) < 0)
                {
                    throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
                }

                // Fails if there are unconsumed bytes.
                keyReader(privateKeyInfo.PrivateKey, privateKeyInfo.PrivateKeyAlgorithm, out ret);
                bytesRead = read;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static AsnWriter WritePkcs8(
            AsnWriter algorithmIdentifierWriter,
            AsnWriter privateKeyWriter,
            AsnWriter? attributesWriter = null)
        {
            // Ensure both algorithm identifier and key writers are balanced.
            int algorithmIdentifierLength = algorithmIdentifierWriter.GetEncodedLength();
            int privateKeyLength = privateKeyWriter.GetEncodedLength();

            Debug.Assert(algorithmIdentifierLength > 0, "algorithmIdentifier was empty");
            Debug.Assert(privateKeyLength > 0, "privateKey was empty");

            // https://tools.ietf.org/html/rfc5208#section-5
            //
            // PrivateKeyInfo ::= SEQUENCE {
            //   version                   Version,
            //   privateKeyAlgorithm       PrivateKeyAlgorithmIdentifier,
            //   privateKey                PrivateKey,
            //   attributes           [0]  IMPLICIT Attributes OPTIONAL }
            //
            // Version ::= INTEGER
            // PrivateKeyAlgorithmIdentifier ::= AlgorithmIdentifier
            // PrivateKey ::= OCTET STRING
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            // PrivateKeyInfo
            writer.PushSequence();

            // https://tools.ietf.org/html/rfc5208#section-5 says the current version is 0.
            writer.WriteInteger(0);

            // PKI.Algorithm (AlgorithmIdentifier)
            algorithmIdentifierWriter.CopyTo(writer);

            // PKI.privateKey
            using (writer.PushOctetString())
            {
                privateKeyWriter.CopyTo(writer);
            }

            // PKI.Attributes
            attributesWriter?.CopyTo(writer);

            writer.PopSequence();
            return writer;
        }
    }
}
