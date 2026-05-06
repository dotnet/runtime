// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.X509Certificates
{
    /// <summary>
    ///   Specifies options when loading a <see cref="CertificateRequest" />.
    /// </summary>
    [Flags]
    public enum CertificateRequestLoadOptions
    {
        /// <summary>
        ///   Load the certificate request with default options.
        /// </summary>
        Default = 0,

        /// <summary>
        ///   When loading the request, do not check if the embedded public key validates the request
        ///   signature.
        /// </summary>
        SkipSignatureValidation = 0x01,

        /// <summary>
        ///   When loading the request, populate the
        ///   <see cref="CertificateRequest.CertificateExtensions" /> collection based on the PKCS#9
        ///   Extension Request attribute (1.2.840.113549.1.9.14).
        /// </summary>
        UnsafeLoadCertificateExtensions = 0x02,
    }
}
