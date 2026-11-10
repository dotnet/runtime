// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Security.Cryptography.X509Certificates
{
    internal static class UnixChainVerifier
    {
        public static bool Verify(X509ChainElement[] chainElements, X509VerificationFlags flags)
        {
            int rootIndex = HasPartialChain(chainElements) ? -1 : chainElements.Length - 1;

            for (int i = 0; i < chainElements.Length; i++)
            {
                // Element 0 is the end-entity.
                // If the chain is complete, then match the last element under "root".
                // Otherwise, the element must be part of the middle of a chain.
                //
                // Two fuzzy pieces in this logic:
                // 1. Non-self-issued trust anchors.  We generally don't support them,
                //    but they're possible on macOS because of system trust rules.
                //    This logic will treat them as roots, which is more correct than not.
                //    (As the trust anchor, you're not expecting someone to say it was revoked,
                //    but instead you remove it from anchor status.)
                //
                // 2. Black-box testing suggests Windows has some special considerations for a
                //    CA cert that was self-issued by the root (but with a different key and not
                //    self-signed).  This sort of re-keying is exceptionally rare, so it's not a
                //    high-priority research effort.  Until then, calling the signed re-key an
                //    intermediate is more accurate than calling it a root, as it will be checked
                //    for revocation under the ExcludeRoot policy.
                X509VerificationFlags suppressionFlag =
                    i == 0 ? X509VerificationFlags.IgnoreEndRevocationUnknown :
                    i == rootIndex ? X509VerificationFlags.IgnoreRootRevocationUnknown :
                    X509VerificationFlags.IgnoreCertificateAuthorityRevocationUnknown;

                if (HasUnsuppressedError(flags, chainElements[i], suppressionFlag))
                {
                    return false;
                }
            }

            return true;

            static bool HasPartialChain(X509ChainElement[] chainElements)
            {
                foreach (X509ChainElement element in chainElements)
                {
                    foreach (X509ChainStatus status in element.ChainElementStatus)
                    {
                        if (status.Status == X509ChainStatusFlags.PartialChain)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        private static bool HasUnsuppressedError(X509VerificationFlags flags, X509ChainElement element, X509VerificationFlags revocationSuppressionFlag)
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
                    suppressionFlag = revocationSuppressionFlag;
                }
                else if (status.Status == X509ChainStatusFlags.OfflineRevocation)
                {
                    // This is mainly a warning code, it's redundant to RevocationStatusUnknown
                    continue;
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
