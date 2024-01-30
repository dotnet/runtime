// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Asn1.Pkcs12;
using System.Security.Cryptography.Asn1.Pkcs7;
using System.Threading;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal abstract class UnixPkcs12Reader : IDisposable
    {
        private const string DecryptedSentinel = nameof(UnixPkcs12Reader);
        private const int ErrorInvalidPasswordHResult = unchecked((int)0x80070056);

        private PfxAsn _pfxAsn;
        private ContentInfoAsn[]? _safeContentsValues;
        private CertAndKey[]? _certs;
        private int _certCount;
        private PointerMemoryManager<byte>? _tmpManager;
        private bool _allowDoubleBind;

        protected abstract ICertificatePalCore ReadX509Der(ReadOnlyMemory<byte> data);
        protected abstract AsymmetricAlgorithm LoadKey(ReadOnlyMemory<byte> safeBagBagValue);

        protected void ParsePkcs12(ReadOnlySpan<byte> data)
        {
            try
            {
                // RFC7292 specifies BER instead of DER
                AsnValueReader reader = new AsnValueReader(data, AsnEncodingRules.BER);

                // Windows compatibility: Ignore trailing data.
                ReadOnlySpan<byte> encodedData = reader.PeekEncodedValue();

                unsafe
                {
                    IntPtr tmpPtr = Marshal.AllocHGlobal(encodedData.Length);
                    Span<byte> tmpSpan = new Span<byte>((byte*)tmpPtr, encodedData.Length);
                    encodedData.CopyTo(tmpSpan);
                    _tmpManager = new PointerMemoryManager<byte>((void*)tmpPtr, encodedData.Length);
                }

                ReadOnlyMemory<byte> tmpMemory = _tmpManager.Memory;
                reader = new AsnValueReader(tmpMemory.Span, AsnEncodingRules.BER);

                PfxAsn.Decode(ref reader, tmpMemory, out PfxAsn pfxAsn);

                if (pfxAsn.AuthSafe.ContentType != Oids.Pkcs7Data)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                _pfxAsn = pfxAsn;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal CertAndKey GetSingleCert()
        {
            CertAndKey[]? certs = _certs;
            Debug.Assert(certs != null);

            if (_certCount < 1)
            {
                throw new CryptographicException(SR.Cryptography_Pfx_NoCertificates);
            }

            CertAndKey ret;

            for (int i = _certCount - 1; i >= 0; --i)
            {
                if (certs[i].Key != null)
                {
                    ret = certs[i];
                    certs[i] = default;
                    return ret;
                }
            }

            ret = certs[_certCount - 1];
            certs[_certCount - 1] = default;
            return ret;
        }

        internal int GetCertCount()
        {
            return _certCount;
        }

        internal IEnumerable<CertAndKey> EnumerateAll()
        {
            while (_certCount > 0)
            {
                int idx = _certCount - 1;
                CertAndKey ret = _certs![idx];
                _certs[idx] = default;
                _certCount--;
                yield return ret;
            }
        }

        public void Dispose()
        {
            // Generally, having a MemoryManager cleaned up in a Dispose is a bad practice.
            // In this case, the UnixPkcs12Reader is only ever created in a using statement,
            // never accessed by a second thread, and there isn't a manual call to Dispose
            // mixed in anywhere.
            if (_tmpManager != null)
            {
                unsafe
                {
                    Span<byte> tmp = _tmpManager.GetSpan();
                    CryptographicOperations.ZeroMemory(tmp);

                    fixed (byte* ptr = tmp)
                    {
                        Marshal.FreeHGlobal((IntPtr)ptr);
                    }
                }

                ((IDisposable)_tmpManager).Dispose();
                _tmpManager = null;
            }

            ContentInfoAsn[]? rentedContents = Interlocked.Exchange(ref _safeContentsValues, null);
            CertAndKey[]? rentedCerts = Interlocked.Exchange(ref _certs, null);

            if (rentedContents != null)
            {
                ReturnRentedContentInfos(rentedContents);
            }

            if (rentedCerts != null)
            {
                for (int i = _certCount - 1; i >= 0; --i)
                {
                    rentedCerts[i].Dispose();
                }

                ArrayPool<CertAndKey>.Shared.Return(rentedCerts, clearArray: true);
            }
        }

        private static void ReturnRentedContentInfos(ContentInfoAsn[] rentedContents)
        {
            for (int i = 0; i < rentedContents.Length; i++)
            {
                string contentType = rentedContents[i].ContentType;

                if (contentType == null)
                {
                    break;
                }

                if (contentType == DecryptedSentinel)
                {
                    ReadOnlyMemory<byte> content = rentedContents[i].Content;
                    rentedContents[i].Content = default;

                    if (!MemoryMarshal.TryGetArray(content, out ArraySegment<byte> segment))
                    {
                        Debug.Fail("Couldn't unpack decrypted buffer.");
                    }

                    CryptoPool.Return(segment);
                }
            }

            ArrayPool<ContentInfoAsn>.Shared.Return(rentedContents, clearArray: true);
        }

        public void Decrypt(SafePasswordHandle password, bool ephemeralSpecified)
        {
            ReadOnlyMemory<byte> authSafeContents =
                Helpers.DecodeOctetStringAsMemory(_pfxAsn.AuthSafe.Content);

            _allowDoubleBind = !ephemeralSpecified;

            bool hasRef = false;

            try
            {
                password.DangerousAddRef(ref hasRef);
                ReadOnlySpan<char> passwordChars = password.DangerousGetSpan();

                if (_pfxAsn.MacData.HasValue)
                {
                    VerifyAndDecrypt(passwordChars, authSafeContents);
                }
                else if (passwordChars.IsEmpty)
                {
                    try
                    {
                        // Try the empty password first.
                        // If anything goes wrong, try the null password.
                        //
                        // The same password has to work for the entirety of the file,
                        // null and empty aren't interchangeable between parts.
                        Decrypt("", authSafeContents);
                    }
                    catch (CryptographicException)
                    {
                        ContentInfoAsn[]? partialSuccess = _safeContentsValues;
                        _safeContentsValues = null;

                        if (partialSuccess != null)
                        {
                            ReturnRentedContentInfos(partialSuccess);
                        }

                        Decrypt(null, authSafeContents);
                    }
                }
                else
                {
                    Decrypt(passwordChars, authSafeContents);
                }
            }
            catch (Exception e)
            {
                throw new CryptographicException(SR.Cryptography_Pfx_BadPassword, e)
                {
                    HResult = ErrorInvalidPasswordHResult
                };
            }
            finally
            {
                if (hasRef)
                {
                    password.DangerousRelease();
                }
            }
        }

        private void VerifyAndDecrypt(ReadOnlySpan<char> password, ReadOnlyMemory<byte> authSafeContents)
        {
            Debug.Assert(_pfxAsn.MacData.HasValue);
            ReadOnlySpan<byte> authSafeSpan = authSafeContents.Span;

            if (password.Length == 0)
            {
                // VerifyMac produces different answers for the empty string and the null string,
                // when the length is 0 try empty first (more common), then null.
                if (_pfxAsn.VerifyMac("", authSafeSpan))
                {
                    Decrypt("", authSafeContents);
                    return;
                }

                if (_pfxAsn.VerifyMac(default, authSafeSpan))
                {
                    Decrypt(default, authSafeContents);
                    return;
                }
            }
            else if (_pfxAsn.VerifyMac(password, authSafeSpan))
            {
                Decrypt(password, authSafeContents);
                return;
            }

            throw new CryptographicException(SR.Cryptography_Pfx_BadPassword)
            {
                HResult = ErrorInvalidPasswordHResult
            };
        }

        private void Decrypt(ReadOnlySpan<char> password, ReadOnlyMemory<byte> authSafeContents)
        {
            _safeContentsValues ??= DecodeSafeContents(authSafeContents);

            // The average PFX contains one cert, and one key.
            // The next most common PFX contains 3 certs, and one key.
            //
            // Nothing requires that there be fewer keys than certs,
            // but it's sort of nonsensical when loading this way.
            CertBagAsn[] certBags = ArrayPool<CertBagAsn>.Shared.Rent(10);
            AttributeAsn[]?[] certBagAttrs = ArrayPool<AttributeAsn[]?>.Shared.Rent(10);
            SafeBagAsn[] keyBags = ArrayPool<SafeBagAsn>.Shared.Rent(10);
            RentedSubjectPublicKeyInfo[]? publicKeyInfos = null;
            AsymmetricAlgorithm[]? keys = null;
            CertAndKey[]? certs = null;
            int certBagIdx = 0;
            int keyBagIdx = 0;

            try
            {
                DecryptAndProcessSafeContents(
                    password,
                    ref certBags,
                    ref certBagAttrs,
                    ref certBagIdx,
                    ref keyBags,
                    ref keyBagIdx);

                certs = ArrayPool<CertAndKey>.Shared.Rent(certBagIdx);
                certs.AsSpan().Clear();

                keys = ArrayPool<AsymmetricAlgorithm>.Shared.Rent(keyBagIdx);
                keys.AsSpan().Clear();

                publicKeyInfos = ArrayPool<RentedSubjectPublicKeyInfo>.Shared.Rent(keyBagIdx);
                publicKeyInfos.AsSpan().Clear();

                ExtractPrivateKeys(password, keyBags, keyBagIdx, keys, publicKeyInfos);

                BuildCertsWithKeys(
                    password,
                    certBags,
                    certBagAttrs,
                    certs,
                    certBagIdx,
                    keyBags,
                    publicKeyInfos,
                    keys,
                    keyBagIdx);

                _certCount = certBagIdx;
                _certs = certs;
            }
            catch
            {
                if (certs != null)
                {
                    for (int i = 0; i < certBagIdx; i++)
                    {
                        CertAndKey certAndKey = certs[i];
                        certAndKey.Dispose();
                    }
                }

                throw;
            }
            finally
            {
                if (keys != null)
                {
                    foreach (AsymmetricAlgorithm key in keys)
                    {
                        key?.Dispose();
                    }

                    ArrayPool<AsymmetricAlgorithm>.Shared.Return(keys);
                }

                if (publicKeyInfos != null)
                {
                    for (int i = 0; i < keyBagIdx; i++)
                    {
                        publicKeyInfos[i].Dispose();
                    }

                    ArrayPool<RentedSubjectPublicKeyInfo>.Shared.Return(publicKeyInfos, clearArray: true);
                }

                ArrayPool<CertBagAsn>.Shared.Return(certBags, clearArray: true);
                ArrayPool<AttributeAsn[]?>.Shared.Return(certBagAttrs, clearArray: true);
                ArrayPool<SafeBagAsn>.Shared.Return(keyBags, clearArray: true);
            }
        }

        private static ContentInfoAsn[] DecodeSafeContents(ReadOnlyMemory<byte> authSafeContents)
        {
            // The expected number of ContentInfoAsns to read is 2, one encrypted (contains certs),
            // and one plain (contains encrypted keys)
            ContentInfoAsn[] rented = ArrayPool<ContentInfoAsn>.Shared.Rent(10);

            try
            {
                AsnValueReader outer = new AsnValueReader(authSafeContents.Span, AsnEncodingRules.BER);
                AsnValueReader reader = outer.ReadSequence();
                outer.ThrowIfNotEmpty();
                int i = 0;

                while (reader.HasData)
                {
                    GrowIfNeeded(ref rented, i);
                    ContentInfoAsn.Decode(ref reader, authSafeContents, out rented[i]);
                    i++;
                }

                rented.AsSpan(i).Clear();
                return rented;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        private void DecryptAndProcessSafeContents(
            ReadOnlySpan<char> password,
            ref CertBagAsn[] certBags,
            ref AttributeAsn[]?[] certBagAttrs,
            ref int certBagIdx,
            ref SafeBagAsn[] keyBags,
            ref int keyBagIdx)
        {
            for (int i = 0; i < _safeContentsValues!.Length; i++)
            {
                string contentType = _safeContentsValues[i].ContentType;
                bool process = false;

                if (contentType == null)
                {
                    break;
                }

                // Should enveloped throw here?
                if (contentType == Oids.Pkcs7Data)
                {
                    process = true;
                }
                else if (contentType == Oids.Pkcs7Encrypted)
                {
                    DecryptSafeContents(password, ref _safeContentsValues[i]);
                    process = true;
                }

                if (process)
                {
                    ProcessSafeContents(
                        _safeContentsValues[i],
                        ref certBags,
                        ref certBagAttrs,
                        ref certBagIdx,
                        ref keyBags,
                        ref keyBagIdx);
                }
            }
        }

        private void ExtractPrivateKeys(
            ReadOnlySpan<char> password,
            SafeBagAsn[] keyBags,
            int keyBagIdx,
            AsymmetricAlgorithm[] keys,
            RentedSubjectPublicKeyInfo[] publicKeyInfos)
        {
            byte[]? spkiBuf = null;

            for (int i = keyBagIdx - 1; i >= 0; i--)
            {
                ref RentedSubjectPublicKeyInfo cur = ref publicKeyInfos[i];

                try
                {
                    SafeBagAsn keyBag = keyBags[i];
                    AsymmetricAlgorithm key = LoadKey(keyBag, password);

                    int pubLength;

                    while (!key.TryExportSubjectPublicKeyInfo(spkiBuf, out pubLength))
                    {
                        byte[]? toReturn = spkiBuf;
                        spkiBuf = CryptoPool.Rent((toReturn?.Length ?? 128) * 2);

                        if (toReturn != null)
                        {
                            // public key info doesn't need to be cleared
                            CryptoPool.Return(toReturn, clearSize: 0);
                        }
                    }

                    cur.Value = SubjectPublicKeyInfoAsn.Decode(
                        spkiBuf.AsMemory(0, pubLength),
                        AsnEncodingRules.DER);

                    keys[i] = key;
                    cur.TrackArray(spkiBuf, clearSize: 0);
                    spkiBuf = null;
                }
                catch (CryptographicException)
                {
                    // Windows 10 compatibility:
                    // If anything goes wrong loading this key, just ignore it.
                    // If no one ended up needing it, no harm/no foul.
                    // If this has a LocalKeyId and something references it, then it'll fail.
                }
                finally
                {
                    if (spkiBuf != null)
                    {
                        // Public key data doesn't need to be cleared.
                        CryptoPool.Return(spkiBuf, clearSize: 0);
                    }
                }
            }
        }

        private void BuildCertsWithKeys(
            ReadOnlySpan<char> password,
            CertBagAsn[] certBags,
            AttributeAsn[]?[] certBagAttrs,
            CertAndKey[] certs,
            int certBagIdx,
            SafeBagAsn[] keyBags,
            RentedSubjectPublicKeyInfo[] publicKeyInfos,
            AsymmetricAlgorithm?[] keys,
            int keyBagIdx)
        {
            for (certBagIdx--; certBagIdx >= 0; certBagIdx--)
            {
                int matchingKeyIdx = -1;

                foreach (AttributeAsn attr in certBagAttrs[certBagIdx] ?? Array.Empty<AttributeAsn>())
                {
                    if (attr.AttrType == Oids.LocalKeyId && attr.AttrValues.Length > 0)
                    {
                        matchingKeyIdx = FindMatchingKey(
                            keyBags,
                            keyBagIdx,
                            Helpers.DecodeOctetStringAsMemory(attr.AttrValues[0]).Span);

                        // Only try the first one.
                        break;
                    }
                }

                ReadOnlyMemory<byte> x509Data =
                    Helpers.DecodeOctetStringAsMemory(certBags[certBagIdx].CertValue);

                certs[certBagIdx].Cert = ReadX509Der(x509Data);

                // If no matching key was found, but there are keys,
                // compare SubjectPublicKeyInfo values
                if (matchingKeyIdx == -1 && keyBagIdx > 0)
                {
                    ICertificatePalCore cert = certs[certBagIdx].Cert!;
                    string algorithm = cert.KeyAlgorithm;
                    byte[] keyParams = cert.KeyAlgorithmParameters;
                    byte[] keyValue = cert.PublicKeyValue;

                    for (int i = 0; i < keyBagIdx; i++)
                    {
                        if (PublicKeyMatches(algorithm, keyParams, keyValue, ref publicKeyInfos[i].Value))
                        {
                            matchingKeyIdx = i;
                            break;
                        }
                    }
                }

                if (matchingKeyIdx != -1)
                {
                    // Windows compat:
                    // If the PFX is loaded with EphemeralKeySet, don't allow double-bind.
                    // Otherwise, reload the key so a second instance is bound (avoiding one
                    // cert Dispose removing the key of another).
                    if (keys[matchingKeyIdx] == null)
                    {
                        if (_allowDoubleBind)
                        {
                            certs[certBagIdx].Key = LoadKey(keyBags[matchingKeyIdx], password);
                        }
                        else
                        {
                            throw new CryptographicException(SR.Cryptography_Pfx_BadKeyReference);
                        }
                    }
                    else
                    {
                        certs[certBagIdx].Key = keys[matchingKeyIdx];
                        keys[matchingKeyIdx] = null;
                    }
                }
            }
        }

        private static bool PublicKeyMatches(
            string algorithm,
            byte[] keyParams,
            byte[] keyValue,
            ref SubjectPublicKeyInfoAsn publicKeyInfo)
        {
            if (!publicKeyInfo.SubjectPublicKey.Span.SequenceEqual(keyValue))
            {
                return false;
            }

            switch (algorithm)
            {
                case Oids.Rsa:
                case Oids.RsaPss:
                    switch (publicKeyInfo.Algorithm.Algorithm)
                    {
                        case Oids.Rsa:
                        case Oids.RsaPss:
                            break;
                        default:
                            return false;
                    }

                    return
                        publicKeyInfo.Algorithm.HasNullEquivalentParameters() &&
                        AlgorithmIdentifierAsn.RepresentsNull(keyParams);
                case Oids.EcPublicKey:
                case Oids.EcDiffieHellman:
                    switch (publicKeyInfo.Algorithm.Algorithm)
                    {
                        case Oids.EcPublicKey:
                        case Oids.EcDiffieHellman:
                            break;
                        default:
                            return false;
                    }

                    return
                        publicKeyInfo.Algorithm.Parameters.HasValue &&
                        publicKeyInfo.Algorithm.Parameters.Value.Span.SequenceEqual(keyParams);
            }

            if (algorithm != publicKeyInfo.Algorithm.Algorithm)
            {
                return false;
            }

            if (!publicKeyInfo.Algorithm.Parameters.HasValue)
            {
                return (keyParams?.Length ?? 0) == 0;
            }

            return publicKeyInfo.Algorithm.Parameters.Value.Span.SequenceEqual(keyParams);
        }

        private static int FindMatchingKey(
            SafeBagAsn[] keyBags,
            int keyBagCount,
            ReadOnlySpan<byte> localKeyId)
        {
            for (int i = 0; i < keyBagCount; i++)
            {
                foreach (AttributeAsn attr in keyBags[i].BagAttributes ?? Array.Empty<AttributeAsn>())
                {
                    if (attr.AttrType == Oids.LocalKeyId && attr.AttrValues.Length > 0)
                    {
                        ReadOnlyMemory<byte> curKeyId =
                            Helpers.DecodeOctetStringAsMemory(attr.AttrValues[0]);

                        if (curKeyId.Span.SequenceEqual(localKeyId))
                        {
                            return i;
                        }

                        break;
                    }
                }
            }

            return -1;
        }

        private static void DecryptSafeContents(
            ReadOnlySpan<char> password,
            ref ContentInfoAsn safeContentsAsn)
        {
            EncryptedDataAsn encryptedData =
                EncryptedDataAsn.Decode(safeContentsAsn.Content, AsnEncodingRules.BER);

            // https://tools.ietf.org/html/rfc5652#section-8
            if (encryptedData.Version != 0 && encryptedData.Version != 2)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            // Since the contents are supposed to be the BER-encoding of an instance of
            // SafeContents (https://tools.ietf.org/html/rfc7292#section-4.1) that implies the
            // content type is simply "data", and that content is present.
            if (encryptedData.EncryptedContentInfo.ContentType != Oids.Pkcs7Data)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            if (!encryptedData.EncryptedContentInfo.EncryptedContent.HasValue)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            int encryptedValueLength = encryptedData.EncryptedContentInfo.EncryptedContent.Value.Length;
            byte[] destination = CryptoPool.Rent(encryptedValueLength);
            int written;

            try
            {
                written = PasswordBasedEncryption.Decrypt(
                    encryptedData.EncryptedContentInfo.ContentEncryptionAlgorithm,
                    password,
                    default,
                    encryptedData.EncryptedContentInfo.EncryptedContent.Value.Span,
                    destination);
            }
            catch
            {
                // Clear the whole thing, since we don't know what state we're in.
                CryptoPool.Return(destination);
                throw;
            }

            // The DecryptedSentiel content type value will cause Dispose to return
            // `destination` to the pool.
            safeContentsAsn.Content = destination.AsMemory(0, written);
            safeContentsAsn.ContentType = DecryptedSentinel;
        }

        private static void ProcessSafeContents(
            in ContentInfoAsn safeContentsAsn,
            ref CertBagAsn[] certBags,
            ref AttributeAsn[]?[] certBagAttrs,
            ref int certBagIdx,
            ref SafeBagAsn[] keyBags,
            ref int keyBagIdx)
        {
            ReadOnlyMemory<byte> contentData = safeContentsAsn.Content;

            if (safeContentsAsn.ContentType == Oids.Pkcs7Data)
            {
                contentData = Helpers.DecodeOctetStringAsMemory(contentData);
            }

            try
            {
                AsnValueReader outer = new AsnValueReader(contentData.Span, AsnEncodingRules.BER);
                AsnValueReader reader = outer.ReadSequence();
                outer.ThrowIfNotEmpty();

                while (reader.HasData)
                {
                    SafeBagAsn.Decode(ref reader, contentData, out SafeBagAsn bag);

                    if (bag.BagId == Oids.Pkcs12CertBag)
                    {
                        CertBagAsn certBag = CertBagAsn.Decode(bag.BagValue, AsnEncodingRules.BER);

                        if (certBag.CertId == Oids.Pkcs12X509CertBagType)
                        {
                            GrowIfNeeded(ref certBags, certBagIdx);
                            GrowIfNeeded(ref certBagAttrs, certBagIdx);
                            certBags[certBagIdx] = certBag;
                            certBagAttrs[certBagIdx] = bag.BagAttributes;
                            certBagIdx++;
                        }
                    }
                    else if (bag.BagId == Oids.Pkcs12KeyBag || bag.BagId == Oids.Pkcs12ShroudedKeyBag)
                    {
                        GrowIfNeeded(ref keyBags, keyBagIdx);
                        keyBags[keyBagIdx] = bag;
                        keyBagIdx++;
                    }
                }
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        private AsymmetricAlgorithm LoadKey(SafeBagAsn safeBag, ReadOnlySpan<char> password)
        {
            if (safeBag.BagId == Oids.Pkcs12ShroudedKeyBag)
            {
                ArraySegment<byte> decrypted = KeyFormatHelper.DecryptPkcs8(
                    password,
                    safeBag.BagValue,
                    out int localRead);

                try
                {
                    if (localRead != safeBag.BagValue.Length)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    return LoadKey(decrypted.AsMemory());
                }
                finally
                {
                    CryptoPool.Return(decrypted);
                }
            }

            Debug.Assert(safeBag.BagId == Oids.Pkcs12KeyBag);
            return LoadKey(safeBag.BagValue);
        }

        private static void GrowIfNeeded<T>(ref T[] array, int idx)
        {
            T[] oldRent = array;

            if (idx >= oldRent.Length)
            {
                T[] newRent = ArrayPool<T>.Shared.Rent(oldRent.Length * 2);
                Array.Copy(oldRent, 0, newRent, 0, idx);
                array = newRent;
                ArrayPool<T>.Shared.Return(oldRent, clearArray: true);
            }
        }

        internal struct CertAndKey
        {
            internal ICertificatePalCore? Cert;
            internal AsymmetricAlgorithm? Key;

            internal void Dispose()
            {
                Cert?.Dispose();
                Key?.Dispose();
            }
        }

        private struct RentedSubjectPublicKeyInfo
        {
            private byte[]? _rented;
            private int _clearSize;
            internal SubjectPublicKeyInfoAsn Value;

            internal void TrackArray(byte[]? rented, int clearSize = CryptoPool.ClearAll)
            {
                Debug.Assert(_rented == null);

                _rented = rented;
                _clearSize = clearSize;
            }

            public void Dispose()
            {
                byte[]? rented = Interlocked.Exchange(ref _rented, null);

                if (rented != null)
                {
                    CryptoPool.Return(rented, _clearSize);
                }
            }
        }
    }
}
