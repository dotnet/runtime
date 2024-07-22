// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed partial class AppleCertificatePal : ICertificatePal
    {
        private static readonly SafePasswordHandle s_passwordExportHandle = new SafePasswordHandle("DotnetExportPassphrase", passwordProvided: true);

        internal static AppleCertificatePal ImportPkcs12(AppleCertificatePal pal, AsymmetricAlgorithm? key)
        {
            if (key is not null)
            {
                AppleCertificateExporter exporter = new AppleCertificateExporter(new TempExportPal(pal), key);
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
