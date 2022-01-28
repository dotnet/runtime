// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1.Pkcs7;
using System.Security.Cryptography.Asn1.Pkcs12;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed partial class AppleCertificatePal : ICertificatePal
    {
        private static bool IsPkcs12(ReadOnlySpan<byte> rawData)
        {
            try
            {
                unsafe
                {
                    fixed (byte* pin = rawData)
                    {
                        using (var manager = new PointerMemoryManager<byte>(pin, rawData.Length))
                        {
                            PfxAsn.Decode(manager.Memory, AsnEncodingRules.BER);
                        }

                        return true;
                    }
                }
            }
            catch (CryptographicException)
            {
            }

            return false;
        }

        private static bool IsPkcs7Signed(ReadOnlySpan<byte> rawData)
        {
            try
            {
                unsafe
                {
                    fixed (byte* pin = rawData)
                    {
                        using (var manager = new PointerMemoryManager<byte>(pin, rawData.Length))
                        {
                            AsnValueReader reader = new AsnValueReader(rawData, AsnEncodingRules.BER);

                            ContentInfoAsn.Decode(ref reader, manager.Memory, out ContentInfoAsn contentInfo);

                            switch (contentInfo.ContentType)
                            {
                                case Oids.Pkcs7Signed:
                                case Oids.Pkcs7SignedEnveloped:
                                    return true;
                            }
                        }
                    }
                }
            }
            catch (CryptographicException)
            {
            }

            return false;
        }

        internal static X509ContentType GetDerCertContentType(ReadOnlySpan<byte> rawData)
        {
            X509ContentType contentType = Interop.AppleCrypto.X509GetContentType(rawData);

            if (contentType == X509ContentType.Unknown)
            {
                if (IsPkcs12(rawData))
                {
                    return X509ContentType.Pkcs12;
                }

                if (IsPkcs7Signed(rawData))
                {
                    return X509ContentType.Pkcs7;
                }
            }

            return contentType;
        }

        internal static ICertificatePal FromDerBlob(
            ReadOnlySpan<byte> rawData,
            X509ContentType contentType,
            SafePasswordHandle password,
            X509KeyStorageFlags keyStorageFlags)
        {
            Debug.Assert(password != null);

            bool ephemeralSpecified = keyStorageFlags.HasFlag(X509KeyStorageFlags.EphemeralKeySet);

            if (contentType == X509ContentType.Pkcs7)
            {
                throw new CryptographicException(
                    SR.Cryptography_X509_PKCS7_Unsupported,
                    new PlatformNotSupportedException(SR.Cryptography_X509_PKCS7_Unsupported));
            }

            if (contentType == X509ContentType.Pkcs12)
            {
                if ((keyStorageFlags & X509KeyStorageFlags.Exportable) == X509KeyStorageFlags.Exportable)
                {
                    throw new PlatformNotSupportedException(SR.Cryptography_X509_PKCS12_ExportableNotSupported);
                }

                if ((keyStorageFlags & X509KeyStorageFlags.PersistKeySet) == X509KeyStorageFlags.PersistKeySet)
                {
                    throw new PlatformNotSupportedException(SR.Cryptography_X509_PKCS12_PersistKeySetNotSupported);
                }

                return ImportPkcs12(rawData, password, ephemeralSpecified);
            }

            SafeSecIdentityHandle identityHandle;
            SafeSecCertificateHandle certHandle = Interop.AppleCrypto.X509ImportCertificate(
                rawData,
                contentType,
                password,
                out identityHandle);

            if (identityHandle.IsInvalid)
            {
                identityHandle.Dispose();
                return new AppleCertificatePal(certHandle);
            }

            Debug.Fail("Non-PKCS12 import produced an identity handle");

            identityHandle.Dispose();
            certHandle.Dispose();
            throw new CryptographicException();
        }

        public static ICertificatePal FromBlob(
            ReadOnlySpan<byte> rawData,
            SafePasswordHandle password,
            X509KeyStorageFlags keyStorageFlags)
        {
            Debug.Assert(password != null);

            ICertificatePal? result = null;
            TryDecodePem(
                rawData,
                (derData, contentType) =>
                {
                    result = FromDerBlob(derData, contentType, password, keyStorageFlags);
                    return false;
                });

            return result ?? FromDerBlob(rawData, GetDerCertContentType(rawData), password, keyStorageFlags);
        }

        // No temporary keychain on iOS
        partial void DisposeTempKeychain();
    }
}
