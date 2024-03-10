// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed partial class AppleCertificatePal : ICertificatePal
    {
        private static SafePasswordHandle s_passwordExportHandle = new SafePasswordHandle("DotnetExportPassphrase", passwordProvided: true);

        private static AppleCertificatePal ImportPkcs12(
            ReadOnlySpan<byte> rawData,
            SafePasswordHandle password,
            bool ephemeralSpecified)
        {
            using (ApplePkcs12Reader reader = new ApplePkcs12Reader())
            {
                reader.ParsePkcs12(rawData);
                reader.Decrypt(password, ephemeralSpecified);
                return ImportPkcs12(reader.GetSingleCert());
            }
        }

        internal static AppleCertificatePal ImportPkcs12(UnixPkcs12Reader.CertAndKey certAndKey)
        {
            AppleCertificatePal pal = (AppleCertificatePal)certAndKey.Cert!;

            if (certAndKey.Key != null)
            {
                AppleCertificateExporter exporter = new AppleCertificateExporter(new TempExportPal(pal), certAndKey.Key);
                byte[] smallPfx = exporter.Export(X509ContentType.Pkcs12, s_passwordExportHandle)!;

                SafeSecIdentityHandle identityHandle;
                SafeSecCertificateHandle certHandle = Interop.AppleCrypto.X509ImportCertificate(
                    smallPfx,
                    X509ContentType.Pkcs12,
                    s_passwordExportHandle,
                    out identityHandle);

                if (identityHandle.IsInvalid)
                {
                    identityHandle.Dispose();
                    return new AppleCertificatePal(certHandle);
                }

                certHandle.Dispose();
                return new AppleCertificatePal(identityHandle);
            }

            return pal;
        }
    }
}
