// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Pkcs.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class Pkcs12Info
    {
        private PfxAsn _decoded;
        private ReadOnlyMemory<byte> _authSafeContents;

        public ReadOnlyCollection<Pkcs12SafeContents> AuthenticatedSafe { get; private set; }
        public Pkcs12IntegrityMode IntegrityMode { get; private set; }

        private Pkcs12Info()
        {
        }

        public bool VerifyMac(string password)
        {
            // This extension-method call allows null.
            return VerifyMac(password.AsSpan());
        }

        public bool VerifyMac(ReadOnlySpan<char> password)
        {
            if (IntegrityMode != Pkcs12IntegrityMode.Password)
            {
                throw new InvalidOperationException(
                    SR.Format(
                        SR.Cryptography_Pkcs12_WrongModeForVerify,
                        Pkcs12IntegrityMode.Password,
                        IntegrityMode));
            }

            Debug.Assert(_decoded.MacData.HasValue);

            HashAlgorithmName hashAlgorithm;
            int expectedOutputSize;

            string algorithmValue = _decoded.MacData.Value.Mac.DigestAlgorithm.Algorithm.Value;

            switch (algorithmValue)
            {
                case Oids.Md5:
                    expectedOutputSize = 128 >> 3;
                    hashAlgorithm = HashAlgorithmName.MD5;
                    break;
                case Oids.Sha1:
                    expectedOutputSize = 160 >> 3;
                    hashAlgorithm = HashAlgorithmName.SHA1;
                    break;
                case Oids.Sha256:
                    expectedOutputSize = 256 >> 3;
                    hashAlgorithm = HashAlgorithmName.SHA256;
                    break;
                case Oids.Sha384:
                    expectedOutputSize = 384 >> 3;
                    hashAlgorithm = HashAlgorithmName.SHA384;
                    break;
                case Oids.Sha512:
                    expectedOutputSize = 512 >> 3;
                    hashAlgorithm = HashAlgorithmName.SHA512;
                    break;
                default:
                    throw new CryptographicException(
                        SR.Format(SR.Cryptography_UnknownHashAlgorithm, algorithmValue));
            }

            if (_decoded.MacData.Value.Mac.Digest.Length != expectedOutputSize)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            // Cannot use the ArrayPool or stackalloc here because CreateHMAC needs a properly bounded array.
            byte[] derived = new byte[expectedOutputSize];

            int iterationCount =
                PasswordBasedEncryption.NormalizeIterationCount(_decoded.MacData.Value.IterationCount);

            Pkcs12Kdf.DeriveMacKey(
                password,
                hashAlgorithm,
                iterationCount,
                _decoded.MacData.Value.MacSalt.Span,
                derived);

            using (IncrementalHash hmac = IncrementalHash.CreateHMAC(hashAlgorithm, derived))
            {
                hmac.AppendData(_authSafeContents.Span);

                if (!hmac.TryGetHashAndReset(derived, out int bytesWritten) || bytesWritten != expectedOutputSize)
                {
                    Debug.Fail($"TryGetHashAndReset wrote {bytesWritten} bytes when {expectedOutputSize} was expected");
                    throw new CryptographicException();
                }

                return CryptographicOperations.FixedTimeEquals(
                    derived,
                    _decoded.MacData.Value.Mac.Digest.Span);
            }
        }

        public static Pkcs12Info Decode(
            ReadOnlyMemory<byte> encodedBytes,
            out int bytesConsumed,
            bool skipCopy = false)
        {
            AsnReader reader = new AsnReader(encodedBytes, AsnEncodingRules.BER);
            // Trim it to the first value
            encodedBytes = reader.PeekEncodedValue();

            ReadOnlyMemory<byte> maybeCopy = skipCopy ? encodedBytes : encodedBytes.ToArray();
            PfxAsn pfx = PfxAsn.Decode(maybeCopy, AsnEncodingRules.BER);

            // https://tools.ietf.org/html/rfc7292#section-4 only defines version 3.
            if (pfx.Version != 3)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            ReadOnlyMemory<byte> authSafeBytes = ReadOnlyMemory<byte>.Empty;
            Pkcs12IntegrityMode mode = Pkcs12IntegrityMode.Unknown;

            if (pfx.AuthSafe.ContentType == Oids.Pkcs7Data)
            {
                authSafeBytes = PkcsHelpers.DecodeOctetString(pfx.AuthSafe.Content);

                if (pfx.MacData.HasValue)
                {
                    mode = Pkcs12IntegrityMode.Password;
                }
                else
                {
                    mode = Pkcs12IntegrityMode.None;
                }
            }
            else if (pfx.AuthSafe.ContentType == Oids.Pkcs7Signed)
            {
                SignedDataAsn signedData = SignedDataAsn.Decode(pfx.AuthSafe.Content, AsnEncodingRules.BER);

                mode = Pkcs12IntegrityMode.PublicKey;

                if (signedData.EncapContentInfo.ContentType == Oids.Pkcs7Data)
                {
                    authSafeBytes = signedData.EncapContentInfo.Content.GetValueOrDefault();
                }

                if (pfx.MacData.HasValue)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }
            }

            if (mode == Pkcs12IntegrityMode.Unknown)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            List<ContentInfoAsn> authSafeData = new List<ContentInfoAsn>();
            AsnReader authSafeReader = new AsnReader(authSafeBytes, AsnEncodingRules.BER);
            AsnReader sequenceReader = authSafeReader.ReadSequence();

            authSafeReader.ThrowIfNotEmpty();
            while (sequenceReader.HasData)
            {
                ContentInfoAsn.Decode(sequenceReader, out ContentInfoAsn contentInfo);
                authSafeData.Add(contentInfo);
            }

            ReadOnlyCollection<Pkcs12SafeContents> authSafe;

            if (authSafeData.Count == 0)
            {
                authSafe = new ReadOnlyCollection<Pkcs12SafeContents>(Array.Empty<Pkcs12SafeContents>());
            }
            else
            {
                Pkcs12SafeContents[] contentsArray = new Pkcs12SafeContents[authSafeData.Count];

                for (int i = 0; i < contentsArray.Length; i++)
                {
                    contentsArray[i] = new Pkcs12SafeContents(authSafeData[i]);
                }

                authSafe = new ReadOnlyCollection<Pkcs12SafeContents>(contentsArray);
            }

            bytesConsumed = encodedBytes.Length;

            return new Pkcs12Info
            {
                AuthenticatedSafe = authSafe,
                IntegrityMode = mode,
                _decoded = pfx,
                _authSafeContents = authSafeBytes,
            };
        }
    }
}
