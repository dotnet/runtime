// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Asn1.Pkcs12;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class AppleCertificatePal : ICertificatePal
    {
        private static SafePasswordHandle s_passwordExportHandle = new SafePasswordHandle("DotnetExportPassphrase");

        private static AppleCertificatePal ImportPkcs12(
            ReadOnlySpan<byte> rawData,
            SafePasswordHandle password)
        {
            using (ApplePkcs12Reader reader = new ApplePkcs12Reader(rawData))
            {
                reader.Decrypt(password);
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
