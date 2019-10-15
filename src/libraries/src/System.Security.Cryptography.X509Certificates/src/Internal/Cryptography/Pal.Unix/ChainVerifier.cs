// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal static class ChainVerifier
    {
        public static bool Verify(X509ChainElement[] chainElements, X509VerificationFlags flags)
        {
            bool isEndEntity = true;

            foreach (X509ChainElement element in chainElements)
            {
                if (HasUnsuppressedError(flags, element, isEndEntity))
                {
                    return false;
                }

                isEndEntity = false;
            }

            return true;
        }

        private static bool HasUnsuppressedError(X509VerificationFlags flags, X509ChainElement element, bool isEndEntity)
        {
            foreach (X509ChainStatus status in element.ChainElementStatus)
            {
                if (status.Status == X509ChainStatusFlags.NoError)
                {
                    return false;
                }

                Debug.Assert(
                    (status.Status & (status.Status - 1)) == 0,
                    $"Only one bit should be set in status.Status ({status})");

                // The Windows certificate store API only checks the time error for a "peer trust" certificate,
                // but we don't have a concept for that in Unix.  If we did, we'd need to do that logic that here.
                // Note also that the logic is skipped if CERT_CHAIN_POLICY_IGNORE_PEER_TRUST_FLAG is set.

                X509VerificationFlags? suppressionFlag;

                if (status.Status == X509ChainStatusFlags.RevocationStatusUnknown)
                {
                    if (isEndEntity)
                    {
                        suppressionFlag = X509VerificationFlags.IgnoreEndRevocationUnknown;
                    }
                    else if (IsSelfSigned(element.Certificate))
                    {
                        suppressionFlag = X509VerificationFlags.IgnoreRootRevocationUnknown;
                    }
                    else
                    {
                        suppressionFlag = X509VerificationFlags.IgnoreCertificateAuthorityRevocationUnknown;
                    }
                }
                else
                {
                    suppressionFlag = GetSuppressionFlag(status.Status);
                }

                // If an error was found, and we do NOT have the suppression flag for it enabled,
                // we have an unsuppressed error, so return true. (If there's no suppression for a given code,
                // we (by definition) don't have that flag set.
                if (!suppressionFlag.HasValue ||
                    (flags & suppressionFlag) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSelfSigned(X509Certificate2 cert)
        {
            return cert.SubjectName.RawData.ContentsEqual(cert.IssuerName.RawData);
        }

        private static X509VerificationFlags? GetSuppressionFlag(X509ChainStatusFlags status)
        {
            switch (status)
            {
                case X509ChainStatusFlags.UntrustedRoot:
                case X509ChainStatusFlags.PartialChain:
                    return X509VerificationFlags.AllowUnknownCertificateAuthority;

                case X509ChainStatusFlags.NotValidForUsage:
                case X509ChainStatusFlags.CtlNotValidForUsage:
                    return X509VerificationFlags.IgnoreWrongUsage;

                case X509ChainStatusFlags.NotTimeValid:
                    return X509VerificationFlags.IgnoreNotTimeValid;

                case X509ChainStatusFlags.CtlNotTimeValid:
                    return X509VerificationFlags.IgnoreCtlNotTimeValid;

                case X509ChainStatusFlags.InvalidNameConstraints:
                case X509ChainStatusFlags.HasNotSupportedNameConstraint:
                case X509ChainStatusFlags.HasNotDefinedNameConstraint:
                case X509ChainStatusFlags.HasNotPermittedNameConstraint:
                case X509ChainStatusFlags.HasExcludedNameConstraint:
                    return X509VerificationFlags.IgnoreInvalidName;

                case X509ChainStatusFlags.InvalidPolicyConstraints:
                case X509ChainStatusFlags.NoIssuanceChainPolicy:
                    return X509VerificationFlags.IgnoreInvalidPolicy;

                case X509ChainStatusFlags.InvalidBasicConstraints:
                    return X509VerificationFlags.IgnoreInvalidBasicConstraints;

                case X509ChainStatusFlags.HasNotSupportedCriticalExtension:
                    // This field would be mapped in by AllFlags, but we don't have a name for it currently.
                    return (X509VerificationFlags)0x00002000;

                case X509ChainStatusFlags.NotTimeNested:
                    return X509VerificationFlags.IgnoreNotTimeNested;
            }

            return null;
        }
    }
}
