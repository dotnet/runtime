// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography.Apple;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed partial class AppleCertificatePal : ICertificatePal
    {
        private static AppleCertificatePal ImportPkcs12(
            ReadOnlySpan<byte> rawData,
            SafePasswordHandle password,
            bool exportable,
            SafeKeychainHandle keychain)
        {
            using (ApplePkcs12Reader reader = new ApplePkcs12Reader())
            {
                reader.ParsePkcs12(rawData);
                reader.Decrypt(password, ephemeralSpecified: false);

                UnixPkcs12Reader.CertAndKey certAndKey = reader.GetSingleCert();
                AppleCertificatePal pal = (AppleCertificatePal)certAndKey.Cert!;

                SafeSecKeyRefHandle? safeSecKeyRefHandle =
                    ApplePkcs12Reader.GetPrivateKey(certAndKey.Key);

                AppleCertificatePal? newPal;

                using (safeSecKeyRefHandle)
                {
                    // SecItemImport doesn't seem to respect non-exportable import for PKCS#8,
                    // only PKCS#12.
                    //
                    // So, as part of reading this PKCS#12 we now need to write the minimum
                    // PKCS#12 in a normalized form, and ask the OS to import it.
                    if (!exportable && safeSecKeyRefHandle != null)
                    {
                        using (pal)
                        {
                            return ImportPkcs12NonExportable(pal, safeSecKeyRefHandle, password, keychain);
                        }
                    }

                    newPal = pal.MoveToKeychain(keychain, safeSecKeyRefHandle);

                    if (newPal != null)
                    {
                        pal.Dispose();
                    }
                }

                // If no new PAL came back, it means we moved the cert, but had no private key.
                return newPal ?? pal;
            }
        }

        internal static AppleCertificatePal ImportPkcs12NonExportable(
            AppleCertificatePal cert,
            SafeSecKeyRefHandle privateKey,
            SafePasswordHandle password,
            SafeKeychainHandle keychain)
        {
            Pkcs12SmallExport exporter = new Pkcs12SmallExport(new TempExportPal(cert), privateKey);
            byte[] smallPfx = exporter.Export(X509ContentType.Pkcs12, password)!;

            SafeSecIdentityHandle identityHandle;
            SafeSecCertificateHandle certHandle = Interop.AppleCrypto.X509ImportCertificate(
                smallPfx,
                X509ContentType.Pkcs12,
                password,
                keychain,
                exportable: false,
                out identityHandle);

            // On Windows and Linux if a PFX uses a LocalKeyId to bind the wrong key to a cert, the
            // nonsensical object of "this cert, that key" is returned.
            //
            // On macOS, because we can't forge CFIdentityRefs without the keychain, we're subject to
            // Apple's more stringent matching of a consistent keypair.
            if (identityHandle.IsInvalid)
            {
                identityHandle.Dispose();
                return new AppleCertificatePal(certHandle);
            }

            certHandle.Dispose();
            return new AppleCertificatePal(identityHandle);
        }

        private sealed class Pkcs12SmallExport : UnixExportProvider
        {
            private readonly SafeSecKeyRefHandle _privateKey;

            internal Pkcs12SmallExport(ICertificatePalCore cert, SafeSecKeyRefHandle privateKey)
                : base(cert)
            {
                Debug.Assert(!privateKey.IsInvalid);
                _privateKey = privateKey;
            }

            protected override byte[] ExportPkcs7() => throw new NotImplementedException();

            protected override byte[] ExportPkcs8(ICertificatePalCore certificatePal, ReadOnlySpan<char> password)
            {
                return AppleCertificatePal.ExportPkcs8(_privateKey, password);
            }
        }
    }
}
