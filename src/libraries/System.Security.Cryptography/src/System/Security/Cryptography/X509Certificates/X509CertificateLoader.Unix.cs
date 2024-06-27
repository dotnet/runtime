// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Asn1.Pkcs12;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    public static partial class X509CertificateLoader
    {
        private static partial Pkcs12Return FromCertAndKey(CertAndKey certAndKey, ImportState importState);

        private static partial AsymmetricAlgorithm? CreateKey(string algorithm);

        private static partial ICertificatePalCore LoadX509Der(ReadOnlyMemory<byte> data);

        static partial void InitializeImportState(ref ImportState importState, X509KeyStorageFlags keyStorageFlags);

        private static partial Pkcs12Return LoadPkcs12(
            ref BagState bagState,
            ReadOnlySpan<char> password,
            X509KeyStorageFlags keyStorageFlags)
        {
            bool ephemeral = (keyStorageFlags & X509KeyStorageFlags.EphemeralKeySet) != 0;

            CertKeyMatcher matcher = default;
            CertAndKey[]? certsAndKeys = null;
            ImportState importState = default;

            try
            {
                matcher.LoadCerts(ref bagState);
                matcher.LoadKeys(ref bagState);

                // Windows compat: Don't allow double-bind for EphemeralKeySet loads.
                certsAndKeys = matcher.MatchCertAndKeys(ref bagState, !ephemeral);

                int matchIndex;

                for (matchIndex = bagState.CertCount - 1; matchIndex >= 0; matchIndex--)
                {
                    if (certsAndKeys[matchIndex].Key is not null)
                    {
                        break;
                    }
                }

                if (matchIndex < 0)
                {
                    matchIndex = bagState.CertCount - 1;
                }

                Debug.Assert(matchIndex >= 0);

                InitializeImportState(ref importState, keyStorageFlags);
                Pkcs12Return ret = FromCertAndKey(certsAndKeys[matchIndex], importState);
                certsAndKeys[matchIndex] = default;

                return ret;
            }
            finally
            {
                if (certsAndKeys is not null)
                {
                    CertKeyMatcher.Free(certsAndKeys, bagState.CertCount);
                }

                importState.Dispose();
                matcher.Dispose();
            }
        }

        private static partial X509Certificate2Collection LoadPkcs12Collection(
            ref BagState bagState,
            ReadOnlySpan<char> password,
            X509KeyStorageFlags keyStorageFlags)
        {
            bool ephemeral = (keyStorageFlags & X509KeyStorageFlags.EphemeralKeySet) != 0;

            CertKeyMatcher matcher = default;
            CertAndKey[]? certsAndKeys = null;
            ImportState importState = default;

            try
            {
                matcher.LoadCerts(ref bagState);
                matcher.LoadKeys(ref bagState);

                // Windows compat: Don't allow double-bind for EphemeralKeySet loads.
                certsAndKeys = matcher.MatchCertAndKeys(ref bagState, !ephemeral);

                InitializeImportState(ref importState, keyStorageFlags);

                X509Certificate2Collection coll = new X509Certificate2Collection();

                for (int i = bagState.CertCount - 1; i >= 0; i--)
                {
                    coll.Add(FromCertAndKey(certsAndKeys[i], importState).ToCertificate());
                    certsAndKeys[i] = default;
                }

                return coll;
            }
            finally
            {
                if (certsAndKeys is not null)
                {
                    CertKeyMatcher.Free(certsAndKeys, bagState.CertCount);
                }

                importState.Dispose();
                matcher.Dispose();
            }
        }

        internal static unsafe bool IsPkcs12(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
            {
                return false;
            }

            fixed (byte* ptr = data)
            {
                using (PointerMemoryManager<byte> manager = new(ptr, data.Length))
                {
                    try
                    {
                        ReadOnlyMemory<byte> memory = manager.Memory;
                        AsnValueReader reader = new AsnValueReader(memory.Span, AsnEncodingRules.BER);
                        PfxAsn.Decode(ref reader, memory, out _);
                        return true;
                    }
                    catch (AsnContentException)
                    {
                    }
                    catch (CryptographicException)
                    {
                    }

                    return false;
                }
            }
        }

        internal static bool IsPkcs12(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            (byte[]? rented, int length, MemoryManager<byte>? manager) = ReadAllBytesIfBerSequence(path);

            try
            {
                ReadOnlyMemory<byte> memory = manager?.Memory ?? new ReadOnlyMemory<byte>(rented, 0, length);

                AsnValueReader reader = new AsnValueReader(memory.Span, AsnEncodingRules.BER);
                PfxAsn.Decode(ref reader, memory, out _);
                return true;
            }
            catch (AsnContentException)
            {
            }
            catch (CryptographicException)
            {
            }
            finally
            {
                (manager as IDisposable)?.Dispose();

                if (rented is not null)
                {
                    CryptoPool.Return(rented, length);
                }
            }

            return false;
        }

        private partial struct BagState
        {
            internal ReadOnlySpan<SafeBagAsn> GetCertsSpan()
            {
                return new ReadOnlySpan<SafeBagAsn>(_certBags, 0, _certCount);
            }

            internal ReadOnlySpan<SafeBagAsn> GetKeysSpan()
            {
                return new ReadOnlySpan<SafeBagAsn>(_keyBags, 0, _keyCount);
            }
        }

        private struct CertAndKey
        {
            internal ICertificatePalCore? Cert;
            internal AsymmetricAlgorithm? Key;

            internal void Dispose()
            {
                Cert?.Dispose();
                Key?.Dispose();
            }
        }

        private struct CertKeyMatcher
        {
            private CertAndKey[] _certAndKeys;
            private int _certCount;
            private AsymmetricAlgorithm?[] _keys;
            private RentedSubjectPublicKeyInfo[] _rentedSpki;
            private int _keyCount;

            internal void LoadCerts(ref BagState bagState)
            {
                if (bagState.CertCount == 0)
                {
                    return;
                }

                _certAndKeys = ArrayPool<CertAndKey>.Shared.Rent(bagState.CertCount);

                foreach (SafeBagAsn safeBag in bagState.GetCertsSpan())
                {
                    Debug.Assert(safeBag.BagId == Oids.Pkcs12CertBag);

                    CertBagAsn certBag = CertBagAsn.Decode(safeBag.BagValue, AsnEncodingRules.BER);

                    // Non-X.509 cert-type bags should have already been removed.
                    Debug.Assert(certBag.CertId == Oids.Pkcs12X509CertBagType);
                    ReadOnlyMemory<byte> certData = Helpers.DecodeOctetStringAsMemory(certBag.CertValue);

                    ICertificatePalCore pal = LoadX509Der(certData);
                    Debug.Assert(pal is not null);

                    _certAndKeys[_certCount].Cert = pal;
                    _certCount++;
                }
            }

            internal void LoadKeys(ref BagState bagState)
            {
                if (bagState.KeyCount == 0)
                {
                    return;
                }

                _keys = ArrayPool<AsymmetricAlgorithm?>.Shared.Rent(bagState.KeyCount);

                foreach (SafeBagAsn safeBag in bagState.GetKeysSpan())
                {
                    AsymmetricAlgorithm? key = null;

                    try
                    {
                        if (safeBag.BagId == Oids.Pkcs12KeyBag)
                        {
                            PrivateKeyInfoAsn privateKeyInfo =
                                PrivateKeyInfoAsn.Decode(safeBag.BagValue, AsnEncodingRules.BER);

                            key = CreateKey(privateKeyInfo.PrivateKeyAlgorithm.Algorithm);

                            if (key is not null)
                            {
                                ImportPrivateKey(key, safeBag.BagValue.Span);

                                if (_rentedSpki is null)
                                {
                                    _rentedSpki =
                                        ArrayPool<RentedSubjectPublicKeyInfo>.Shared.Rent(bagState.KeyCount);
                                    _rentedSpki.AsSpan().Clear();
                                }

                                ExtractPublicKey(ref _rentedSpki[_keyCount], key, safeBag.BagValue.Length);
                            }
                        }
                        else
                        {
                            // There may still be shrouded bags in the state, signifying that
                            // decryption failed for them.  They get ignored, unless matched
                            // by keyId, which produces a failure.
                            //
                            // If there's any other kind of bag here, there's a mismatch between
                            // this code and the main extractor.
                            Debug.Assert(safeBag.BagId == Oids.Pkcs12ShroudedKeyBag);
                        }
                    }
                    catch (AsnContentException)
                    {
                        key?.Dispose();
                        key = null;
                    }
                    catch (CryptographicException)
                    {
                        key?.Dispose();
                        key = null;
                    }

                    if (key is not null)
                    {
                        _keys[_keyCount] = key;
                    }

                    _keyCount++;
                }
            }

            internal CertAndKey[] MatchCertAndKeys(ref BagState bagState, bool allowDoubleBind)
            {
                ReadOnlySpan<SafeBagAsn> certBags = bagState.GetCertsSpan();
                ReadOnlySpan<SafeBagAsn> keyBags = bagState.GetKeysSpan();

                for (int certBagIdx = certBags.Length - 1; certBagIdx >= 0; certBagIdx--)
                {
                    int matchingKeyIdx = -1;

                    foreach (AttributeAsn attr in certBags[certBagIdx].BagAttributes ?? Array.Empty<AttributeAsn>())
                    {
                        if (attr.AttrType == Oids.LocalKeyId && attr.AttrValues.Length > 0)
                        {
                            matchingKeyIdx = FindMatchingKey(
                                keyBags,
                                Helpers.DecodeOctetStringAsMemory(attr.AttrValues[0]).Span);

                            // Only try the first one.
                            break;
                        }
                    }

                    ICertificatePalCore cert = _certAndKeys[certBagIdx].Cert!;

                    // If no matching key was found, but there are keys,
                    // compare SubjectPublicKeyInfo values
                    if (matchingKeyIdx == -1 && _rentedSpki is not null)
                    {
                        for (int i = 0; i < keyBags.Length; i++)
                        {
                            if (PublicKeyMatches(cert, ref _rentedSpki[i].Value))
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
                        if (_keys[matchingKeyIdx] is null)
                        {
                            // The key could be null because we already matched it (and made it null),
                            // or because it never loaded.
                            SafeBagAsn keyBag = keyBags[matchingKeyIdx];

                            if (keyBag.BagId != Oids.Pkcs12KeyBag)
                            {
                                // The key bag didn't get transformed.
                                // That means the password didn't decrypt it.

                                if (bagState.ConfirmedPassword)
                                {
                                    throw new CryptographicException(SR.Cryptography_Pfx_BadKeyReference);
                                }

                                throw new CryptographicException(SR.Cryptography_Pfx_BadPassword)
                                {
                                    HResult = ERROR_INVALID_PASSWORD,
                                };
                            }

                            if (allowDoubleBind)
                            {

                                AsymmetricAlgorithm? key = CreateKey(cert.KeyAlgorithm);

                                if (key is null)
                                {
                                    // The key is actually an algorithm that isn't supported...

                                    throw new CryptographicException(
                                        SR.Cryptography_UnknownAlgorithmIdentifier,
                                        cert.KeyAlgorithm);
                                }

                                _certAndKeys[certBagIdx].Key = key;
                                ImportPrivateKey(key, keyBag.BagValue.Span);
                            }
                            else
                            {
                                throw new CryptographicException(SR.Cryptography_Pfx_BadKeyReference);
                            }
                        }
                        else
                        {
                            _certAndKeys[certBagIdx].Key = _keys[matchingKeyIdx];
                            _keys[matchingKeyIdx] = null;
                        }
                    }
                }

                CertAndKey[] ret = _certAndKeys;
                _certAndKeys = null!;
                return ret;
            }

            private static int FindMatchingKey(
                ReadOnlySpan<SafeBagAsn> keyBags,
                ReadOnlySpan<byte> localKeyId)
            {
                for (int i = 0; i < keyBags.Length; i++)
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

            private static bool PublicKeyMatches(
                ICertificatePalCore cert,
                ref SubjectPublicKeyInfoAsn publicKeyInfo)
            {
                string certAlgorithm = cert.KeyAlgorithm;
                string keyAlgorithm = publicKeyInfo.Algorithm.Algorithm;

                bool algorithmMatches = certAlgorithm switch
                {
                    // RSA/RSA-PSS are interchangeable
                    Oids.Rsa or Oids.Rsa =>
                        keyAlgorithm is Oids.Rsa or Oids.RsaPss,

                    // id-ecPublicKey and id-ecDH are interchangeable
                    Oids.EcPublicKey or Oids.EcDiffieHellman =>
                        keyAlgorithm is Oids.EcPublicKey or Oids.EcDiffieHellman,

                    // Everything else is an exact match.
                    _ => certAlgorithm.Equals(keyAlgorithm, StringComparison.Ordinal),
                };

                if (!algorithmMatches)
                {
                    return false;
                }

                // Both cert.PublicKeyValue and cert.KeyAlgorithmParameters use memoization
                // on all applicable platforms, but they are still worth deferring past the
                // algorithm family check.  Once they need to be queried at all,
                // PublicKeyValue is more likely to be distinct, so query it first.

                byte[] certEncodedKeyValue = cert.PublicKeyValue;

                if (!publicKeyInfo.SubjectPublicKey.Span.SequenceEqual(certEncodedKeyValue))
                {
                    return false;
                }

                byte[] certKeyParameters = cert.KeyAlgorithmParameters;

                switch (certAlgorithm)
                {
                    // Accept either DER-NULL or missing for RSA algorithm parameters
                    case Oids.Rsa:
                    case Oids.RsaPss:
                        return
                            publicKeyInfo.Algorithm.HasNullEquivalentParameters() &&
                            AlgorithmIdentifierAsn.RepresentsNull(certKeyParameters);

                    // For ECC the parameters are required, and must match exactly.
                    case Oids.EcPublicKey:
                    case Oids.EcDiffieHellman:
                        return
                            publicKeyInfo.Algorithm.Parameters.HasValue &&
                            publicKeyInfo.Algorithm.Parameters.Value.Span.SequenceEqual(certKeyParameters);
                }

                // Any other algorithm matches null/empty parameters as equivalent
                if (!publicKeyInfo.Algorithm.Parameters.HasValue)
                {
                    return (certKeyParameters?.Length ?? 0) == 0;
                }

                return publicKeyInfo.Algorithm.Parameters.Value.Span.SequenceEqual(certKeyParameters);
            }

            private static void ExtractPublicKey(
                ref RentedSubjectPublicKeyInfo spki,
                AsymmetricAlgorithm key,
                int sizeHint)
            {
                Debug.Assert(sizeHint > 0);

                byte[] buf = CryptoPool.Rent(sizeHint);
                int written;

                while (!key.TryExportSubjectPublicKeyInfo(buf, out written))
                {
                    sizeHint = checked(buf.Length * 2);
                    CryptoPool.Return(buf);
                    buf = CryptoPool.Rent(sizeHint);
                }

                spki.TrackArray(buf, written);

                spki.Value = SubjectPublicKeyInfoAsn.Decode(
                    buf.AsMemory(0, written),
                    AsnEncodingRules.BER);
            }

            internal static void Free(CertAndKey[] certAndKeys, int count)
            {
                for (int i = count - 1; i >= 0; i--)
                {
                    certAndKeys[i].Dispose();
                }

                ArrayPool<CertAndKey>.Shared.Return(certAndKeys, clearArray: true);
            }

            internal void Dispose()
            {
                if (_certAndKeys is not null)
                {
                    Free(_certAndKeys, _certCount);
                }

                if (_keys is not null)
                {
                    for (int i = _keyCount - 1; i >= 0; i--)
                    {
                        _keys[i]?.Dispose();
                    }

                    ArrayPool<AsymmetricAlgorithm?>.Shared.Return(_keys, clearArray: true);
                }

                if (_rentedSpki is not null)
                {
                    for (int i = _keyCount - 1; i >= 0; i--)
                    {
                        _rentedSpki[i].Dispose();
                    }
                }

                this = default;
            }

            private static void ImportPrivateKey(AsymmetricAlgorithm key, ReadOnlySpan<byte> pkcs8)
            {
                try
                {
                    key.ImportPkcs8PrivateKey(pkcs8, out int bytesRead);

                    // The key should have already been run through PrivateKeyInfoAsn.Decode,
                    // verifying no trailing data.
                    Debug.Assert(bytesRead == pkcs8.Length);
                }
                catch (PlatformNotSupportedException nse)
                {
                    // Turn a "curve not supported" PNSE (or other PNSE)
                    // into a standardized CryptographicException.
                    throw new CryptographicException(SR.Cryptography_NotValidPrivateKey, nse);
                }
            }
        }

        private struct RentedSubjectPublicKeyInfo
        {
            private byte[]? _rented;
            private int _clearSize;
            internal SubjectPublicKeyInfoAsn Value;

            internal void TrackArray(byte[]? rented, int clearSize = CryptoPool.ClearAll)
            {
                Debug.Assert(_rented is null);

                _rented = rented;
                _clearSize = clearSize;
            }

            internal void Dispose()
            {
                byte[]? rented = _rented;
                _rented = null;

                if (rented != null)
                {
                    CryptoPool.Return(rented, _clearSize);
                }
            }
        }

        private partial struct ImportState
        {
            partial void DisposeCore();

            internal void Dispose()
            {
                DisposeCore();
            }
        }
    }
}
