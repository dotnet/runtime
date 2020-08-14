// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1.Pkcs12;
using System.Security.Cryptography.Asn1.Pkcs7;
using System.Security.Cryptography.Pkcs.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class Pkcs12Info
    {
        private PfxAsn _decoded;
        private ReadOnlyMemory<byte> _authSafeContents;

        public ReadOnlyCollection<Pkcs12SafeContents> AuthenticatedSafe { get; private set; } = null!; // Initialized using object initializer
        public Pkcs12IntegrityMode IntegrityMode { get; private set; }

        private Pkcs12Info()
        {
        }

        public bool VerifyMac(string? password)
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

            return _decoded.VerifyMac(password, _authSafeContents.Span);
        }

        public static Pkcs12Info Decode(
            ReadOnlyMemory<byte> encodedBytes,
            out int bytesConsumed,
            bool skipCopy = false)
        {
            // Trim it to the first value
            int firstValueLength = PkcsHelpers.FirstBerValueLength(encodedBytes.Span);
            ReadOnlyMemory<byte> firstValue = encodedBytes.Slice(0, firstValueLength);

            ReadOnlyMemory<byte> maybeCopy = skipCopy ? firstValue : firstValue.ToArray();
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
                authSafeBytes = PkcsHelpers.DecodeOctetStringAsMemory(pfx.AuthSafe.Content);

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
            try
            {
                AsnValueReader authSafeReader = new AsnValueReader(authSafeBytes.Span, AsnEncodingRules.BER);
                AsnValueReader sequenceReader = authSafeReader.ReadSequence();

                authSafeReader.ThrowIfNotEmpty();
                while (sequenceReader.HasData)
                {
                    ContentInfoAsn.Decode(ref sequenceReader, authSafeBytes, out ContentInfoAsn contentInfo);
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

                bytesConsumed = firstValueLength;

                return new Pkcs12Info
                {
                    AuthenticatedSafe = authSafe,
                    IntegrityMode = mode,
                    _decoded = pfx,
                    _authSafeContents = authSafeBytes,
                };
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }
    }
}
