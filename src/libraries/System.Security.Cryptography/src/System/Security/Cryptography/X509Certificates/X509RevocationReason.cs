// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography.X509Certificates
{
    /// <summary>
    ///   Specifies the reason a certificate was revoked.
    /// </summary>
    /// <remarks>
    ///   This enum represents the <c>CRLReason</c> enum from IETF RFC 5280 and
    ///   ITU T-REC X.509.
    /// </remarks>
    public enum X509RevocationReason
    {
        /// <summary>
        ///   Revocation occurred for a reason that has no more specific value.
        /// </summary>
        Unspecified = 0,

        /// <summary>
        ///   The private key, or another validated portion of an end-entity certificate,
        ///   is suspected to have been compromised.
        /// </summary>
        KeyCompromise = 1,

        /// <summary>
        ///   The private key, or another validated portion of a Certificate Authority (CA) certificate,
        ///   is suspected to have been compromised.
        /// </summary>
        CACompromise = 2,

        /// <summary>
        ///   The subject's name, or other validated information in the certificate, has changed without
        ///   anything being compromised.
        /// </summary>
        AffiliationChanged = 3,

        /// <summary>
        ///   The certificate has been superseded, but without anything being compromised.
        /// </summary>
        Superseded = 4,

        /// <summary>
        ///   The certificate is no longer needed, but nothing is suspected to be compromised.
        /// </summary>
        CessationOfOperation = 5,

        /// <summary>
        ///   The certificate is temporarily suspended, and may either return to service or
        ///   become permanently revoked in the future.
        /// </summary>
        CertificateHold = 6,

        // There is no 7

        /// <summary>
        ///   The certificate was revoked with <see cref="CertificateHold"/> on a base
        ///   Certificate Revocation List (CRL) and is being returned to service on a delta CRL.
        /// </summary>
        RemoveFromCrl = 8,

        /// <summary>
        ///   A privilege contained within the certificate has been withdrawn.
        /// </summary>
        PrivilegeWithdrawn = 9,

        /// <summary>
        ///   It is known, or suspected, that aspects of the Attribute Authority (AA) validated in
        ///   the attribute certificate have been compromised.
        /// </summary>
        AACompromise = 10,

        /// <summary>
        ///   The certificate key uses a weak cryptographic algorithm, or the
        ///   key is too short, or the key was generated in an unsafe manner.
        /// </summary>
        WeakAlgorithmOrKey = 11,
    }
}
