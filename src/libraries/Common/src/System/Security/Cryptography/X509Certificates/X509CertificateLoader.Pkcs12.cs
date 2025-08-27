// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Asn1.Pkcs7;
using System.Security.Cryptography.Asn1.Pkcs12;
using System.Security.Cryptography.Pkcs;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    public static partial class X509CertificateLoader
    {
        private const int CRYPT_E_BAD_DECODE = unchecked((int)0x80092002);
        private const int ERROR_INVALID_PASSWORD = unchecked((int)0x80070056);

#if NET
        private const int NTE_FAIL = unchecked((int)0x80090020);
#endif

        private static readonly AttributeAsn s_syntheticKspAttribute = BuildSyntheticKspAttribute();
        private static readonly AttributeAsn s_syntheticCapiCspAttribute = BuildSyntheticCapiAttribute();

        static partial void LoadPkcs12NoLimits(
            ReadOnlyMemory<byte> data,
            ReadOnlySpan<char> password,
            X509KeyStorageFlags keyStorageFlags,
            ref Pkcs12Return earlyReturn);

        static partial void LoadPkcs12NoLimits(
            ReadOnlyMemory<byte> data,
            ReadOnlySpan<char> password,
            X509KeyStorageFlags keyStorageFlags,
            ref X509Certificate2Collection? earlyReturn);

        private static partial Pkcs12Return LoadPkcs12(
            ref BagState bagState,
            ReadOnlySpan<char> password,
            X509KeyStorageFlags keyStorageFlags);

        private static partial X509Certificate2Collection LoadPkcs12Collection(
            ref BagState bagState,
            ReadOnlySpan<char> password,
            X509KeyStorageFlags keyStorageFlags);

        private static Pkcs12Return LoadPkcs12(
            ReadOnlyMemory<byte> data,
            ReadOnlySpan<char> password,
            X509KeyStorageFlags keyStorageFlags,
            Pkcs12LoaderLimits loaderLimits)
        {
            if (ReferenceEquals(loaderLimits, Pkcs12LoaderLimits.DangerousNoLimits))
            {
                Pkcs12Return earlyReturn = default;
                LoadPkcs12NoLimits(data, password, keyStorageFlags, ref earlyReturn);

                if (earlyReturn.HasValue())
                {
                    return earlyReturn;
                }
            }

            BagState bagState = default;
            bagState.SetStorageFlags(keyStorageFlags);

            try
            {
                ReadCertsAndKeys(ref bagState, data, ref password, loaderLimits);

                if (bagState.CertCount == 0)
                {
                    throw new CryptographicException(SR.Cryptography_Pfx_NoCertificates);
                }

                bagState.UnshroudKeys(ref password);

                return LoadPkcs12(ref bagState, password, keyStorageFlags);
            }
            finally
            {
                bagState.Dispose();
            }
        }

        private static X509Certificate2Collection LoadPkcs12Collection(
            ReadOnlyMemory<byte> data,
            ReadOnlySpan<char> password,
            X509KeyStorageFlags keyStorageFlags,
            Pkcs12LoaderLimits loaderLimits)
        {
            if (ReferenceEquals(loaderLimits, Pkcs12LoaderLimits.DangerousNoLimits))
            {
                X509Certificate2Collection? earlyReturn = null;
                LoadPkcs12NoLimits(data, password, keyStorageFlags, ref earlyReturn);

                if (earlyReturn is not null)
                {
                    return earlyReturn;
                }
            }

            BagState bagState = default;

            try
            {
                ReadCertsAndKeys(ref bagState, data, ref password, loaderLimits);

                if (bagState.CertCount == 0)
                {
                    return new X509Certificate2Collection();
                }

                bagState.UnshroudKeys(ref password);

                return LoadPkcs12Collection(ref bagState, password, keyStorageFlags);
            }
            finally
            {
                bagState.Dispose();
            }
        }

        private static void ReadCertsAndKeys(
            ref BagState bagState,
            ReadOnlyMemory<byte> data,
            ref ReadOnlySpan<char> password,
            Pkcs12LoaderLimits loaderLimits)
        {
            try
            {
                AsnDecoder.ReadSequence(data.Span, AsnEncodingRules.BER, out _, out _, out int trimLength);
                data = data.Slice(0, trimLength);

                PfxAsn pfxAsn = PfxAsn.Decode(data, AsnEncodingRules.BER);

                if (pfxAsn.AuthSafe.ContentType != Oids.Pkcs7Data)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                ReadOnlyMemory<byte> authSafeMemory =
                    Helpers.DecodeOctetStringAsMemory(pfxAsn.AuthSafe.Content);
                ReadOnlySpan<byte> authSafeContents = authSafeMemory.Span;

                if (!password.IsEmpty)
                {
                    bagState.LockPassword();
                }

                if (pfxAsn.MacData.HasValue)
                {
                    if (pfxAsn.MacData.Value.IterationCount > loaderLimits.MacIterationLimit)
                    {
                        throw new Pkcs12LoadLimitExceededException(nameof(Pkcs12LoaderLimits.MacIterationLimit));
                    }

                    bool verified = false;

                    if (!bagState.LockedPassword)
                    {
                        if (!pfxAsn.VerifyMac(password, authSafeContents))
                        {
                            password = password.ContainsNull() ? "".AsSpan() : default;
                        }
                        else
                        {
                            verified = true;
                        }
                    }

                    if (!verified && !pfxAsn.VerifyMac(password, authSafeContents))
                    {
                        ThrowWithHResult(SR.Cryptography_Pfx_BadPassword, ERROR_INVALID_PASSWORD);
                    }

                    bagState.ConfirmPassword();
                }

                AsnValueReader outer = new AsnValueReader(authSafeContents, AsnEncodingRules.BER);
                AsnValueReader reader = outer.ReadSequence();
                outer.ThrowIfNotEmpty();

                ReadOnlyMemory<byte> rebind = pfxAsn.AuthSafe.Content;
                bagState.Init(loaderLimits);

                int? workRemaining = loaderLimits.TotalKdfIterationLimit;

                while (reader.HasData)
                {
                    ContentInfoAsn.Decode(ref reader, rebind, out ContentInfoAsn safeContentsAsn);

                    ReadOnlyMemory<byte> contentData;

                    if (safeContentsAsn.ContentType == Oids.Pkcs7Data)
                    {
                        contentData = Helpers.DecodeOctetStringAsMemory(safeContentsAsn.Content);
                    }
                    else if (safeContentsAsn.ContentType == Oids.Pkcs7Encrypted)
                    {
                        if (loaderLimits.IgnoreEncryptedAuthSafes)
                        {
                            continue;
                        }

                        bagState.PrepareDecryptBuffer(authSafeContents.Length);

                        if (!bagState.LockedPassword)
                        {
                            bagState.LockPassword();
                            int? workRemainingSave = workRemaining;

                            try
                            {
                                contentData = DecryptSafeContents(
                                    safeContentsAsn,
                                    loaderLimits,
                                    password,
                                    ref bagState,
                                    ref workRemaining);
                            }
                            catch (CryptographicException)
                            {
                                password = password.ContainsNull() ? "".AsSpan() : default;
                                workRemaining = workRemainingSave;

                                contentData = DecryptSafeContents(
                                    safeContentsAsn,
                                    loaderLimits,
                                    password,
                                    ref bagState,
                                    ref workRemaining);
                            }
                        }
                        else
                        {
                            contentData = DecryptSafeContents(
                                safeContentsAsn,
                                loaderLimits,
                                password,
                                ref bagState,
                                ref workRemaining);
                        }
                    }
                    else
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    ProcessSafeContents(
                        contentData,
                        loaderLimits,
                        ref workRemaining,
                        ref bagState);
                }
            }
            catch (AsnContentException e)
            {
                ThrowWithHResult(SR.Cryptography_Der_Invalid_Encoding, CRYPT_E_BAD_DECODE, e);
            }
        }

        private static void ProcessSafeContents(
            ReadOnlyMemory<byte> contentData,
            Pkcs12LoaderLimits loaderLimits,
            ref int? workRemaining,
            ref BagState bagState)
        {
            AsnValueReader outer = new AsnValueReader(contentData.Span, AsnEncodingRules.BER);
            AsnValueReader reader = outer.ReadSequence();
            outer.ThrowIfNotEmpty();

            HashSet<string> duplicateAttributeCheck = new();
            CngMachineKeyState machineKeyState = CngMachineKeyState.Unknown;

            while (reader.HasData)
            {
                SafeBagAsn.Decode(ref reader, contentData, out SafeBagAsn bag);

                if (bag.BagId == Oids.Pkcs12CertBag)
                {
                    if (bag.BagAttributes is not null && !loaderLimits.AllowDuplicateAttributes)
                    {
                        RejectDuplicateAttributes(bag.BagAttributes, duplicateAttributeCheck);
                    }

                    CertBagAsn certBag = CertBagAsn.Decode(bag.BagValue, AsnEncodingRules.BER);

                    if (certBag.CertId == Oids.Pkcs12X509CertBagType)
                    {
                        if (bagState.CertCount >= loaderLimits.MaxCertificates)
                        {
                            throw new Pkcs12LoadLimitExceededException(nameof(Pkcs12LoaderLimits.MaxCertificates));
                        }

                        if (bag.BagAttributes is not null)
                        {
                            FilterAttributes(
                                loaderLimits,
                                ref bag,
                                static (limits, oid) =>
                                    oid switch
                                    {
                                        Oids.LocalKeyId => true,
                                        Oids.Pkcs9FriendlyName => limits.PreserveCertificateAlias,
                                        _ => limits.PreserveUnknownAttributes,
                                    });
                        }

                        bagState.AddCert(bag);
                    }
                }
                else if (bag.BagId is Oids.Pkcs12KeyBag or Oids.Pkcs12ShroudedKeyBag)
                {
                    if (bag.BagAttributes is not null && !loaderLimits.AllowDuplicateAttributes)
                    {
                        RejectDuplicateAttributes(bag.BagAttributes, duplicateAttributeCheck);
                    }

                    if (loaderLimits.IgnorePrivateKeys)
                    {
                        continue;
                    }

                    if (bagState.KeyCount >= loaderLimits.MaxKeys)
                    {
                        throw new Pkcs12LoadLimitExceededException(nameof(Pkcs12LoaderLimits.MaxKeys));
                    }

                    if (bag.BagId == Oids.Pkcs12ShroudedKeyBag)
                    {
                        EncryptedPrivateKeyInfoAsn epki = EncryptedPrivateKeyInfoAsn.Decode(
                            bag.BagValue,
                            AsnEncodingRules.BER);

                        int kdfCount = GetKdfCount(epki.EncryptionAlgorithm);

                        if (kdfCount > loaderLimits.IndividualKdfIterationLimit || kdfCount > workRemaining)
                        {
                            string propertyName = kdfCount > loaderLimits.IndividualKdfIterationLimit ?
                                nameof(Pkcs12LoaderLimits.IndividualKdfIterationLimit) :
                                nameof(Pkcs12LoaderLimits.TotalKdfIterationLimit);

                            throw new Pkcs12LoadLimitExceededException(propertyName);
                        }

                        if (workRemaining.HasValue)
                        {
                            workRemaining = checked(workRemaining - kdfCount);
                        }
                    }

                    if (bag.BagAttributes is not null)
                    {
                        FilterAttributes(
                            loaderLimits,
                            ref bag,
                            static (limits, attrType) =>
                                attrType switch
                                {
                                    Oids.LocalKeyId => true,
                                    // MsPkcs12MachineKeySet can be forced off with the UserKeySet flag, or on with MachineKeySet,
                                    // so always preserve it.
                                    Oids.MsPkcs12MachineKeySet => true,
                                    Oids.Pkcs9FriendlyName => limits.PreserveKeyName,
                                    Oids.MsPkcs12KeyProviderName => ProviderNameIsRelevant,
                                    _ => limits.PreserveUnknownAttributes,
                                });
                    }

                    AttributeAsn? providerName = null;

                    if (ProviderNameIsRelevant)
                    {
                        const X509KeyStorageFlags EphemeralKeySet =
#if NET
                            X509KeyStorageFlags.EphemeralKeySet;
#else
                            (X509KeyStorageFlags)0x20;
#endif

                        if ((bagState.StorageFlags & EphemeralKeySet) != 0)
                        {
                            // Ephemeral loads have to go into CNG, but also won't get access denied.
                        }
                        else if (!loaderLimits.PreserveStorageProvider)
                        {
                            providerName = DetermineStorageProvider(
                                bag.BagAttributes,
                                bagState.StorageFlags,
                                ref machineKeyState);
                        }

                        // If DetermineStorageProvider returns null, leave an existing provider alone,
                        // but if there's no provider, add the synthetic KSP attribute.

                        // Otherwise, replace any existing provider with what it returned,
                        // or add what it said if there are no provider attributes.

                        bool hasAttribute = false;

                        foreach (AttributeAsn attr in bag.BagAttributes ?? Array.Empty<AttributeAsn>())
                        {
                            if (attr.AttrType == Oids.MsPkcs12KeyProviderName)
                            {
                                hasAttribute = true;

                                if (providerName.HasValue)
                                {
                                    if (attr.AttrValues?.Length > 0)
                                    {
                                        for (int i = 0; i < attr.AttrValues.Length; i++)
                                        {
                                            attr.AttrValues[i] = providerName.GetValueOrDefault().AttrValues[0];
                                        }
                                    }
                                }
                            }
                        }

                        if (!hasAttribute)
                        {
                            int oldCount = (bag.BagAttributes?.Length).GetValueOrDefault(0);
                            Array.Resize(ref bag.BagAttributes, oldCount + 1);
                            bag.BagAttributes[oldCount] = s_syntheticKspAttribute;
                        }
                    }

                    bagState.AddKey(bag);
                }
            }
        }

        private static void RejectDuplicateAttributes(AttributeAsn[] bagAttributes, HashSet<string> duplicateAttributeCheck)
        {
            duplicateAttributeCheck.Clear();

            foreach (AttributeAsn attrSet in bagAttributes)
            {
                // Use >1 instead of =1 to account for MsPkcs12MachineKeySet, which is a named set with no values.
                // An empty attribute set can't be followed by the same empty set, or a non-empty set.
                if (!duplicateAttributeCheck.Add(attrSet.AttrType) || attrSet.AttrValues.Length > 1)
                {
                    throw new Pkcs12LoadLimitExceededException(nameof(Pkcs12LoaderLimits.AllowDuplicateAttributes));
                }
            }
        }

        private static void FilterAttributes(
            Pkcs12LoaderLimits loaderLimits,
            ref SafeBagAsn bag,
            Func<Pkcs12LoaderLimits, string, bool> filter)
        {
            if (bag.BagAttributes is not null)
            {
                int attrIdx = -1;

                // Filter the attributes, per the loader limits.
                // Because duplicates might be permitted by the options, this filter
                // needs to be order preserving.

                for (int i = 0; i < bag.BagAttributes.Length; i++)
                {
                    string attrType = bag.BagAttributes[i].AttrType;

                    if (filter(loaderLimits, attrType))
                    {
                        attrIdx++;

                        if (attrIdx != i)
                        {
                            AttributeAsn attr = bag.BagAttributes[i];
#if DEBUG
                            bag.BagAttributes[i] = bag.BagAttributes[attrIdx];
#endif
                            bag.BagAttributes[attrIdx] = attr;
                        }
                    }
                }

                int attrLen = attrIdx + 1;

                if (attrLen < bag.BagAttributes.Length)
                {
                    if (attrLen == 0)
                    {
                        bag.BagAttributes = null;
                    }
                    else
                    {
                        Array.Resize(ref bag.BagAttributes, attrLen);
                    }
                }
            }
        }

        private static ReadOnlyMemory<byte> DecryptSafeContents(
            ContentInfoAsn safeContentsAsn,
            Pkcs12LoaderLimits loaderLimits,
            ReadOnlySpan<char> passwordSpan,
            ref BagState bagState,
            ref int? workRemaining)
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

            ReadOnlyMemory<byte> encryptedContent =
                encryptedData.EncryptedContentInfo.EncryptedContent.Value;

            int kdfCount = GetKdfCount(encryptedData.EncryptedContentInfo.ContentEncryptionAlgorithm);

            if (kdfCount > loaderLimits.IndividualKdfIterationLimit || kdfCount > workRemaining)
            {
                throw new Pkcs12LoadLimitExceededException(
                    kdfCount > loaderLimits.IndividualKdfIterationLimit ?
                        nameof(Pkcs12LoaderLimits.IndividualKdfIterationLimit) :
                        nameof(Pkcs12LoaderLimits.TotalKdfIterationLimit));
            }

            if (workRemaining.HasValue)
            {
                workRemaining = checked(workRemaining - kdfCount);
            }

            return bagState.DecryptSafeContents(
                encryptedData.EncryptedContentInfo.ContentEncryptionAlgorithm,
                passwordSpan,
                encryptedContent.Span);
        }

        private static int GetKdfCount(in AlgorithmIdentifierAsn algorithmIdentifier)
        {
            int rawCount = GetRawKdfCount(in algorithmIdentifier);

            if (rawCount < 0)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            return rawCount;

            static int GetRawKdfCount(in AlgorithmIdentifierAsn algorithmIdentifier)
            {
                if (!algorithmIdentifier.Parameters.HasValue)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                switch (algorithmIdentifier.Algorithm)
                {
                    case Oids.PbeWithMD5AndDESCBC:
                    case Oids.PbeWithMD5AndRC2CBC:
                    case Oids.PbeWithSha1AndDESCBC:
                    case Oids.PbeWithSha1AndRC2CBC:
                    case Oids.Pkcs12PbeWithShaAnd3Key3Des:
                    case Oids.Pkcs12PbeWithShaAnd2Key3Des:
                    case Oids.Pkcs12PbeWithShaAnd128BitRC2:
                    case Oids.Pkcs12PbeWithShaAnd40BitRC2:
                        PBEParameter pbeParameter = PBEParameter.Decode(
                            algorithmIdentifier.Parameters.Value,
                            AsnEncodingRules.BER);

                        return pbeParameter.IterationCount;
                    case Oids.PasswordBasedEncryptionScheme2:
                        PBES2Params pbes2Params = PBES2Params.Decode(
                            algorithmIdentifier.Parameters.Value,
                            AsnEncodingRules.BER);

                        if (pbes2Params.KeyDerivationFunc.Algorithm != Oids.Pbkdf2)
                        {
                            throw new CryptographicException(
                                SR.Format(
                                    SR.Cryptography_UnknownAlgorithmIdentifier,
                                    pbes2Params.EncryptionScheme.Algorithm));
                        }

                        if (!pbes2Params.KeyDerivationFunc.Parameters.HasValue)
                        {
                            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                        }

                        Pbkdf2Params pbkdf2Params = Pbkdf2Params.Decode(
                            pbes2Params.KeyDerivationFunc.Parameters.Value,
                            AsnEncodingRules.BER);

                        return pbkdf2Params.IterationCount;
                    default:
                        throw new CryptographicException(
                            SR.Format(
                                SR.Cryptography_UnknownAlgorithmIdentifier,
                                algorithmIdentifier.Algorithm));
                }
            }
        }

        private static bool ProviderNameIsRelevant =>
#if NET
            OperatingSystem.IsWindows();
#else
            // The netstandard builds could use RuntimeInformation.IsOSPlatform,
            // but we'd still need a #else for .NET Framework, so netstandard
            // and netfx will both say provider name is relevant and process
            // the attributes accordingly.
            true;
#endif

        private static AttributeAsn BuildSyntheticKspAttribute()
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            writer.WriteCharacterString(UniversalTagNumber.BMPString, "Microsoft Software Key Storage Provider");

            return new AttributeAsn
            {
                AttrType = Oids.MsPkcs12KeyProviderName,
                AttrValues = new[] { new ReadOnlyMemory<byte>(writer.Encode()) }
            };
        }

        private static AttributeAsn BuildSyntheticCapiAttribute()
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            writer.WriteCharacterString(UniversalTagNumber.BMPString, "Microsoft Enhanced RSA and AES Cryptographic Provider");

            return new AttributeAsn
            {
                AttrType = Oids.MsPkcs12KeyProviderName,
                AttrValues = new[] { new ReadOnlyMemory<byte>(writer.Encode()) }
            };
        }

        private static AttributeAsn? DetermineStorageProvider(
            AttributeAsn[]? bagAttributes,
            X509KeyStorageFlags storageFlags,
            ref CngMachineKeyState machineKeyState)
        {
            // Windows CNG and Windows CAPI have different default permissions models for machine keys.
            //
            // CAPI allows anyone to create a machine key, CNG only allows administrators.
            //
            // But if a system has changed their permissions to allow non-admins to create CNG machine keys,
            // then, by all means, we should use CNG.

            // This all breaks down to:
            //
            // if (machine key && can't make machine keys && known CAPI provider)
            // {
            //     if (RSA)
            //     {
            //         return CAPI PROV_RSA_AES;
            //     }
            //     else
            //     {
            //         pretend PreserveStorageProvider was set.
            //     }
            // }
            //
            // return CNG KSP;

            if (HasMachineKey(bagAttributes, storageFlags) && HasCapiCsp(bagAttributes, out bool isRsa))
            {
                if (machineKeyState == CngMachineKeyState.Unknown)
                {
                    machineKeyState = CheckMachineKeyPermissions();
                }

                if (machineKeyState == CngMachineKeyState.Denied)
                {
                    if (isRsa)
                    {
                        return s_syntheticCapiCspAttribute;
                    }

                    // A DSS or DH CAPI provider, preserve it.
                    return null;
                }
            }

            // Not a machine key, or wasn't known to be a CAPI key, so use CNG.
            return s_syntheticKspAttribute;

            static bool HasMachineKey(AttributeAsn[]? bagAttributes, X509KeyStorageFlags storageKind)
            {
                // This needs to be kept in sync with the Windows MapStorageFlags behavior.
                // We say that both UserKeySet & MachineKeySet maps to a user key.

                if ((storageKind & X509KeyStorageFlags.UserKeySet) != 0)
                {
                    return false;
                }

                if ((storageKind & X509KeyStorageFlags.MachineKeySet) != 0)
                {
                    return true;
                }

                // The Machine Key Set attribute OID is a sentinel, the attribute has no interpreted value.
                // So, any attribute that specifies it means it's a machine key.
                if (bagAttributes is not null)
                {
                    foreach (AttributeAsn attr in bagAttributes)
                    {
                        if (attr.AttrType == Oids.MsPkcs12MachineKeySet)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            static CngMachineKeyState CheckMachineKeyPermissions()
            {
#if NET || NETFRAMEWORK
                CngKey? cngKey = null;

                try
                {
#if NET
                    Debug.Assert(OperatingSystem.IsWindows());
#endif

                    cngKey = CngKey.Create(
                        CngAlgorithm.Rsa,
                        "netperm-" + Guid.NewGuid().ToString("B"),
                        new CngKeyCreationParameters
                        {
                            Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider,
                            KeyCreationOptions = CngKeyCreationOptions.MachineKey,
                        });

                    return CngMachineKeyState.Permitted;
                }
                catch (CryptographicException)
                {
                    return CngMachineKeyState.Denied;
                }
                finally
                {
                    cngKey?.Delete();
                }
#else
                // The netstandard builds shouldn't ever execute on a relevant configuration,
                // so it's not worth adding a reference to the Cng package for this method
                // to compile.
                return CngMachineKeyState.Permitted;
#endif
            }

            static bool HasCapiCsp(AttributeAsn[]? bagAttributes, out bool isRsa)
            {
                isRsa = false;

                if (bagAttributes is not null)
                {
                    foreach (AttributeAsn attr in bagAttributes)
                    {
                        if (attr.AttrType == Oids.MsPkcs12KeyProviderName && attr.AttrValues?.Length > 0)
                        {
                            return HasCapiValue(attr.AttrValues[0].Span, out isRsa);
                        }
                    }
                }

                // If there is no provider name at all, then PFXImportCertStore will use the CAPI
                // MS_BASE_PROV provider, so it's a CAPI key.
                return true;

                static bool HasCapiValue(ReadOnlySpan<byte> encodedAttribute, out bool isRsa)
                {
                    // All of the MS_*_PROV_W values from wincrypt.h, except MS_SCARD_PROV(_W).
                    const string MS_DEF_PROV = "Microsoft Base Cryptographic Provider v1.0";
                    const string MS_ENHANCED_PROV = "Microsoft Enhanced Cryptographic Provider v1.0";
                    const string MS_STRONG_PROV = "Microsoft Strong Cryptographic Provider";
                    const string MS_DEF_RSA_SIG_PROV = "Microsoft RSA Signature Cryptographic Provider";
                    const string MS_DEF_RSA_SCHANNEL_PROV = "Microsoft RSA SChannel Cryptographic Provider";
                    const string MS_DEF_DSS_PROV = "Microsoft Base DSS Cryptographic Provider";
                    const string MS_DEF_DSS_DH_PROV = "Microsoft Base DSS and Diffie-Hellman Cryptographic Provider";
                    const string MS_ENH_DSS_DH_PROV = "Microsoft Enhanced DSS and Diffie-Hellman Cryptographic Provider";
                    const string MS_DEF_DH_SCHANNEL_PROV = "Microsoft DH SChannel Cryptographic Provider";
                    const string MS_ENH_RSA_AES_PROV = "Microsoft Enhanced RSA and AES Cryptographic Provider";
                    const string MS_ENH_RSA_AES_PROV_XP = "Microsoft Enhanced RSA and AES Cryptographic Provider (Prototype)";

                    isRsa = false;

                    if (!Asn1Tag.TryDecode(encodedAttribute, out Asn1Tag tag, out _))
                    {
                        return false;
                    }

                    if (tag.TagClass != TagClass.Universal)
                    {
                        return false;
                    }

                    string? providerName;

                    try
                    {
                        UniversalTagNumber encodingType = (UniversalTagNumber)tag.TagValue;
                        int bytesRead = 0;

                        // Windows writes the provider using BMPString, but reads it from any type.
                        // Technically, Windows will read it as a NumericString, because they don't
                        // validate characters being outside the character set.
                        // Since we do validation, NumericString is omitted.
                        providerName = encodingType switch
                        {
                            UniversalTagNumber.BMPString or
                            UniversalTagNumber.UTF8String or
                            UniversalTagNumber.IA5String or
                            UniversalTagNumber.PrintableString or
                            UniversalTagNumber.VisibleString or
                            UniversalTagNumber.T61String => AsnDecoder.ReadCharacterString(
                                encodedAttribute,
                                AsnEncodingRules.BER,
                                encodingType,
                                out bytesRead),
                            _ => null,
                        };

                        if (bytesRead != encodedAttribute.Length)
                        {
                            return false;
                        }
                    }
                    catch (AsnContentException)
                    {
                        return false;
                    }

                    isRsa = providerName switch
                    {
                        MS_DEF_PROV or
                        MS_ENHANCED_PROV or
                        MS_STRONG_PROV or
                        MS_DEF_RSA_SIG_PROV or
                        MS_DEF_RSA_SCHANNEL_PROV or
                        MS_ENH_RSA_AES_PROV or
                        MS_ENH_RSA_AES_PROV_XP => true,
                        _ => false,
                    };

                    return isRsa || providerName switch
                    {
                        MS_DEF_DSS_PROV or
                        MS_DEF_DSS_DH_PROV or
                        MS_ENH_DSS_DH_PROV or
                        MS_DEF_DH_SCHANNEL_PROV => true,
                        _ => false,
                    };
                }
            }
        }

        private readonly partial struct Pkcs12Return
        {
            internal partial bool HasValue();
            internal partial X509Certificate2 ToCertificate();
        }

        private enum CngMachineKeyState
        {
            Unknown,
            Permitted,
            Denied,
        };

        private partial struct BagState
        {
            private SafeBagAsn[]? _certBags;
            private SafeBagAsn[]? _keyBags;
            private byte[]? _decryptBuffer;
            private byte[]? _keyDecryptBuffer;
            private int _certCount;
            private int _keyCount;
            private int _decryptBufferOffset;
            private int _keyDecryptBufferOffset;
            private byte _passwordState;
            private X509KeyStorageFlags _storageFlags;

            internal void SetStorageFlags(X509KeyStorageFlags storageFlags)
            {
                _storageFlags = storageFlags & (X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.UserKeySet);
            }

            internal void Init(Pkcs12LoaderLimits loaderLimits)
            {
                _certBags = ArrayPool<SafeBagAsn>.Shared.Rent(loaderLimits.MaxCertificates.GetValueOrDefault(10));
                _keyBags = ArrayPool<SafeBagAsn>.Shared.Rent(loaderLimits.MaxKeys.GetValueOrDefault(10));
                _certCount = 0;
                _keyCount = 0;
                _decryptBufferOffset = 0;
            }

            public void Dispose()
            {
                if (_certBags is not null)
                {
                    ArrayPool<SafeBagAsn>.Shared.Return(_certBags, clearArray: true);
                }

                if (_keyBags is not null)
                {
                    ArrayPool<SafeBagAsn>.Shared.Return(_keyBags, clearArray: true);
                }

                if (_decryptBuffer is not null)
                {
                    CryptoPool.Return(_decryptBuffer, _decryptBufferOffset);
                }

                if (_keyDecryptBuffer is not null)
                {
                    CryptoPool.Return(_keyDecryptBuffer, _keyDecryptBufferOffset);
                }

                this = default;
            }

            internal readonly X509KeyStorageFlags StorageFlags => _storageFlags;

            public readonly int CertCount => _certCount;

            public readonly int KeyCount => _keyCount;

            internal bool LockedPassword => (_passwordState & 1) != 0;

            internal void LockPassword()
            {
                _passwordState |= 1;
            }

            internal bool ConfirmedPassword => (_passwordState & 2) != 0;

            internal void ConfirmPassword()
            {
                // Confirming it (verifying it was correct), also locks it.
                _passwordState |= 3;
            }

            internal void PrepareDecryptBuffer(int upperBound)
            {
                if (_decryptBuffer is null)
                {
                    _decryptBuffer = CryptoPool.Rent(upperBound);
                }
                else
                {
                    Debug.Assert(_decryptBuffer.Length >= upperBound);
                }
            }

            internal ReadOnlyMemory<byte> DecryptSafeContents(
                in AlgorithmIdentifierAsn algorithmIdentifier,
                ReadOnlySpan<char> passwordSpan,
                ReadOnlySpan<byte> encryptedContent)
            {
                Debug.Assert(_decryptBuffer is not null);
                Debug.Assert(_decryptBuffer.Length - _decryptBufferOffset >= encryptedContent.Length);

                // In case anything goes wrong decrypting, clear the whole buffer in the cleanup
                int saveOffset = _decryptBufferOffset;
                _decryptBufferOffset = _decryptBuffer.Length;

                try
                {
                    int written = PasswordBasedEncryption.Decrypt(
                        algorithmIdentifier,
                        passwordSpan,
                        default,
                        encryptedContent,
                        _decryptBuffer.AsSpan(saveOffset));

                    _decryptBufferOffset = saveOffset + written;

                    try
                    {
                        AsnValueReader reader = new AsnValueReader(
                            _decryptBuffer.AsSpan(saveOffset, written),
                            AsnEncodingRules.BER);

                        reader.ReadSequence();
                        reader.ThrowIfNotEmpty();
                    }
                    catch (AsnContentException)
                    {
                        ThrowWithHResult(SR.Cryptography_Der_Invalid_Encoding, CRYPT_E_BAD_DECODE);
                    }

                    ConfirmPassword();

                    return new ReadOnlyMemory<byte>(
                        _decryptBuffer,
                        saveOffset,
                        written);
                }
                catch (PlatformNotSupportedException pnse)
                {
                    // May be thrown by PBE decryption if the platform does not support the algorithm.
                    ThrowWithHResult(SR.Cryptography_Pfx_BadPassword, ERROR_INVALID_PASSWORD, pnse);
                    throw; // This is unreachable because of the throw helper, but the compiler does not know that.
                }
                catch (CryptographicException e)
                {
                    CryptographicOperations.ZeroMemory(
                        _decryptBuffer.AsSpan(saveOffset, _decryptBufferOffset - saveOffset));

                    _decryptBufferOffset = saveOffset;

#if NET
                    if (e.HResult != CRYPT_E_BAD_DECODE)
                    {
                        e.HResult = ConfirmedPassword ? NTE_FAIL : ERROR_INVALID_PASSWORD;
                    }
#else
                    Debug.Assert(e.HResult != 0);
#endif

                    throw;
                }
            }

            internal void AddCert(SafeBagAsn bag)
            {
                Debug.Assert(_certBags is not null);

                GrowIfNeeded(ref _certBags, _certCount);
                _certBags[_certCount] = bag;
                _certCount++;
            }

            internal void AddKey(SafeBagAsn bag)
            {
                Debug.Assert(_keyBags is not null);

                GrowIfNeeded(ref _keyBags, _keyCount);
                _keyBags[_keyCount] = bag;
                _keyCount++;
            }

            private static void GrowIfNeeded(ref SafeBagAsn[] array, int index)
            {
                if (array.Length <= index)
                {
                    SafeBagAsn[] next = ArrayPool<SafeBagAsn>.Shared.Rent(checked(index + 1));
                    array.AsSpan().CopyTo(next);
                    ArrayPool<SafeBagAsn>.Shared.Return(array, clearArray: true);
                    array = next;
                }
            }

            internal void UnshroudKeys(ref ReadOnlySpan<char> password)
            {
                Debug.Assert(_keyBags is not null);

                int spaceRequired = 0;

                for (int i = 0; i < _keyCount; i++)
                {
                    SafeBagAsn bag = _keyBags[i];

                    if (bag.BagId == Oids.Pkcs12ShroudedKeyBag)
                    {
                        spaceRequired += bag.BagValue.Length;
                    }
                }

                _keyDecryptBuffer = CryptoPool.Rent(spaceRequired);

                for (int i = 0; i < _keyCount; i++)
                {
                    ref SafeBagAsn bag = ref _keyBags[i];

                    if (bag.BagId == Oids.Pkcs12ShroudedKeyBag)
                    {
                        ArraySegment<byte> decrypted = default;
                        int contentRead = 0;

                        if (!LockedPassword)
                        {
                            try
                            {
                                decrypted = KeyFormatHelper.DecryptPkcs8(
                                    password,
                                    bag.BagValue,
                                    out contentRead);

                                try
                                {
                                    AsnValueReader reader = new AsnValueReader(decrypted, AsnEncodingRules.BER);
                                    reader.ReadSequence();
                                    reader.ThrowIfNotEmpty();
                                }
                                catch (AsnContentException)
                                {
                                    CryptoPool.Return(decrypted);
                                    decrypted = default;
                                    throw new CryptographicException();
                                }
                            }
                            catch (CryptographicException)
                            {
                                password = password.ContainsNull() ? "".AsSpan() : default;
                            }
                        }

                        if (decrypted.Array is null)
                        {
                            try
                            {
                                decrypted = KeyFormatHelper.DecryptPkcs8(
                                    password,
                                    bag.BagValue,
                                    out contentRead);

                                try
                                {
                                    AsnValueReader reader = new AsnValueReader(decrypted, AsnEncodingRules.BER);
                                    reader.ReadSequence();
                                    reader.ThrowIfNotEmpty();
                                }
                                catch (AsnContentException)
                                {
                                    CryptoPool.Return(decrypted);
                                    decrypted = default;
                                    throw new CryptographicException();
                                }
                            }
                            catch (CryptographicException)
                            {
                                // Windows 10 compatibility:
                                // If anything goes wrong loading this key, just ignore it.
                                // If no one ended up needing it, no harm/no foul.
                                // If this has a LocalKeyId and something references it, then it'll fail.

                                continue;
                            }
                        }

                        ConfirmPassword();

                        Debug.Assert(decrypted.Array is not null);
                        Debug.Assert(_keyDecryptBuffer.Length - _keyDecryptBufferOffset >= decrypted.Count);
                        decrypted.AsSpan().CopyTo(_keyDecryptBuffer.AsSpan(_keyDecryptBufferOffset));

                        ReadOnlyMemory<byte> newBagValue = new(
                            _keyDecryptBuffer,
                            _keyDecryptBufferOffset,
                            decrypted.Count);

                        CryptoPool.Return(decrypted);
                        _keyDecryptBufferOffset += newBagValue.Length;

                        if (contentRead != bag.BagValue.Length)
                        {
                            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                        }

                        bag.BagValue = newBagValue;
                        bag.BagId = Oids.Pkcs12KeyBag;
                    }
                }
            }

            internal
#if !NET
                unsafe
#endif
                ArraySegment<byte> ToPfx(ReadOnlySpan<char> password)
            {
                Debug.Assert(_certBags is not null);
                Debug.Assert(_keyBags is not null);

                ContentInfoAsn safeContents = new ContentInfoAsn
                {
                    ContentType = Oids.Pkcs7Data,
                };

                AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);

                using (writer.PushOctetString())
                using (writer.PushSequence())
                {
                    for (int i = 0; i < _certCount; i++)
                    {
                        SafeBagAsn bag = _certBags[i];
                        bag.Encode(writer);
                    }

                    for (int i = 0; i < _keyCount; i++)
                    {
                        SafeBagAsn bag = _keyBags[i];
                        bag.Encode(writer);
                    }
                }

                safeContents.Content = writer.Encode();
                writer.Reset();

                using (writer.PushSequence())
                {
                    safeContents.Encode(writer);
                }

                byte[] authSafe = writer.Encode();
                writer.Reset();

                const int Sha1MacSize = 20;
                HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA1;
                Span<byte> salt = stackalloc byte[Sha1MacSize];
                Helpers.RngFill(salt);

#if NET
                Span<byte> macKey = stackalloc byte[Sha1MacSize];
#else
                byte[] macKey = new byte[Sha1MacSize];
#endif

                // Pin macKey (if it's on the heap), derive the key into it, then overwrite it with the MAC output.
#if !NET
                fixed (byte* macKeyPin = macKey)
#endif
                {
                    Pkcs12Kdf.DeriveMacKey(
                        password,
                        hashAlgorithm,
                        1,
                        salt,
                        macKey);

                    using (IncrementalHash mac = IncrementalHash.CreateHMAC(hashAlgorithm, macKey))
                    {
                        mac.AppendData(authSafe);

                        if (!mac.TryGetHashAndReset(macKey, out int bytesWritten) || bytesWritten != macKey.Length)
                        {
                            Debug.Fail($"TryGetHashAndReset wrote {bytesWritten} of {macKey.Length} bytes");
                            throw new CryptographicException();
                        }
                    }
                }

                // https://tools.ietf.org/html/rfc7292#section-4
                //
                // PFX ::= SEQUENCE {
                //   version    INTEGER {v3(3)}(v3,...),
                //   authSafe   ContentInfo,
                //   macData    MacData OPTIONAL
                // }
                using (writer.PushSequence())
                {
                    writer.WriteInteger(3);

                    using (writer.PushSequence())
                    {
                        writer.WriteObjectIdentifierForCrypto(Oids.Pkcs7Data);

                        Asn1Tag contextSpecific0 = new Asn1Tag(TagClass.ContextSpecific, 0);

                        using (writer.PushSequence(contextSpecific0))
                        {
                            writer.WriteOctetString(authSafe);
                        }
                    }

                    // https://tools.ietf.org/html/rfc7292#section-4
                    //
                    // MacData ::= SEQUENCE {
                    //   mac        DigestInfo,
                    //   macSalt    OCTET STRING,
                    //   iterations INTEGER DEFAULT 1
                    //   -- Note: The default is for historical reasons and its use is
                    //   -- deprecated.
                    // }
                    using (writer.PushSequence())
                    {
                        using (writer.PushSequence())
                        {
                            using (writer.PushSequence())
                            {
                                writer.WriteObjectIdentifierForCrypto(Oids.Sha1);
                            }

                            writer.WriteOctetString(macKey);
                        }

                        writer.WriteOctetString(salt);
                    }
                }

                byte[] ret = CryptoPool.Rent(writer.GetEncodedLength());
                int written = writer.Encode(ret);
                return new ArraySegment<byte>(ret, 0, written);
            }
        }
    }
}
