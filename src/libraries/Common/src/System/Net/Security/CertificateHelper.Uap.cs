// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using RTCertificate = Windows.Security.Cryptography.Certificates.Certificate;
using RTCertificateQuery = Windows.Security.Cryptography.Certificates.CertificateQuery;
using RTCertificateStores = Windows.Security.Cryptography.Certificates.CertificateStores;
using RTIBuffer = Windows.Storage.Streams.IBuffer;

namespace System.Net.Security
{
    internal static partial class CertificateHelper
    {
        // There are currently only two ways to convert a .NET X509Certificate2 object into a WinRT Certificate without
        // losing its private keys, each with its own limitations:
        //
        // (1) Using the X509Certificate2.Export method with PKCS12/PFX to obtain a byte[] representation (including private
        //     keys) that can then be passed into the IBuffer-based WinRT Certificate constructor. Unfortunately, the
        //     X509Certificate2.Export operation will only succeed if the app-provided X509Certificate2 object was created
        //     with the non-default X509KeyStorageFlags.Exportable flag.
        //
        // (2) Going through the certificate store. That is, retrieving the certificate represented by the X509Certificate2
        //     object as a WinRT Certificate via WinRT CertificateStores APIs. Of course, this requires the certificate to
        //     have been added to a certificate store in the first place.
        //
        // Furthermore, WinRT WebSockets only support certificates that have been added to the personal certificate store
        // (i.e., "MY" store) due to other WinRT-specific private key limitations. With that in mind, approach (2) is the
        // most appropriate for our needs, as it guarantees that WinRT WebSockets will be able to handle the resulting
        // WinRT Certificate during ConnectAsync.
        internal static async Task<RTCertificate> ConvertDotNetClientCertToWinRtClientCertAsync(X509Certificate2 dotNetCertificate)
        {
            var query = new RTCertificateQuery
            {
                Thumbprint = dotNetCertificate.GetCertHash(),
                IncludeDuplicates = false,
                StoreName = "MY"
            };

            IReadOnlyList<RTCertificate> certificates = await RTCertificateStores.FindAllAsync(query).AsTask().ConfigureAwait(false);
            if (certificates.Count > 0)
            {
                return certificates[0];
            }

            return null;
        }

        internal static X509Certificate2 ConvertPublicKeyCertificate(RTCertificate cert)
        {
            // Convert Windows X509v2 cert to .NET X509v2 cert.
            RTIBuffer blob = cert.GetCertificateBlob();
            return new X509Certificate2(blob.ToArray());
        }
    }
}
