// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

using Internal.Cryptography.Pal.Native;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class ChainPal : IDisposable, IChainPal
    {
        private static X509ChainStatus[] GetChainStatusInformation(CertTrustErrorStatus dwStatus)
        {
            if (dwStatus == CertTrustErrorStatus.CERT_TRUST_NO_ERROR)
                return Array.Empty<X509ChainStatus>();

            int count = 0;
            for (uint bits = (uint)dwStatus; bits != 0; bits = bits >> 1)
            {
                if ((bits & 0x1) != 0)
                    count++;
            }

            X509ChainStatus[] chainStatus = new X509ChainStatus[count];
            int index = 0;

            foreach (X509ChainErrorMapping mapping in s_x509ChainErrorMappings)
            {
                if ((dwStatus & mapping.Win32Flag) != 0)
                {
                    Debug.Assert(index < chainStatus.Length);

                    chainStatus[index].StatusInformation = mapping.Message;
                    chainStatus[index].Status = mapping.ChainStatusFlag;
                    index++;
                    dwStatus &= ~mapping.Win32Flag;
                }
            }

            int shiftCount = 0;
            for (uint bits = (uint)dwStatus; bits != 0; bits = bits >> 1)
            {
                if ((bits & 0x1) != 0)
                {
                    Debug.Assert(index < chainStatus.Length);

                    chainStatus[index].Status = (X509ChainStatusFlags)(1 << shiftCount);
                    chainStatus[index].StatusInformation = SR.Unknown_Error;
                    index++;
                }
                shiftCount++;
            }

            Debug.Assert(index == chainStatus.Length);

            return chainStatus;
        }

        private readonly struct X509ChainErrorMapping
        {
            public readonly CertTrustErrorStatus Win32Flag;
            public readonly int Win32ErrorCode;
            public readonly X509ChainStatusFlags ChainStatusFlag;
            public readonly string Message;

            public X509ChainErrorMapping(CertTrustErrorStatus win32Flag, int win32ErrorCode, X509ChainStatusFlags chainStatusFlag)
            {
                Win32Flag = win32Flag;
                Win32ErrorCode = win32ErrorCode;
                ChainStatusFlag = chainStatusFlag;
                Message = Interop.Kernel32.GetMessage(win32ErrorCode);
            }
        }

        private static readonly X509ChainErrorMapping[] s_x509ChainErrorMappings = new[]
        {
            new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_IS_NOT_SIGNATURE_VALID, ErrorCode.TRUST_E_CERT_SIGNATURE, X509ChainStatusFlags.NotSignatureValid),
            new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_CTL_IS_NOT_SIGNATURE_VALID, ErrorCode.TRUST_E_CERT_SIGNATURE, X509ChainStatusFlags.CtlNotSignatureValid),
            new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_IS_UNTRUSTED_ROOT, ErrorCode.CERT_E_UNTRUSTEDROOT, X509ChainStatusFlags.UntrustedRoot),
            new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_IS_PARTIAL_CHAIN, ErrorCode.CERT_E_CHAINING, X509ChainStatusFlags.PartialChain),
            new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_IS_REVOKED, ErrorCode.CRYPT_E_REVOKED, X509ChainStatusFlags.Revoked),
            new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_IS_NOT_VALID_FOR_USAGE, ErrorCode.CERT_E_WRONG_USAGE, X509ChainStatusFlags.NotValidForUsage),
            new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_CTL_IS_NOT_VALID_FOR_USAGE, ErrorCode.CERT_E_WRONG_USAGE, X509ChainStatusFlags.CtlNotValidForUsage),
            new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_IS_NOT_TIME_VALID, ErrorCode.CERT_E_EXPIRED, X509ChainStatusFlags.NotTimeValid),
            new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_CTL_IS_NOT_TIME_VALID, ErrorCode.CERT_E_EXPIRED, X509ChainStatusFlags.CtlNotTimeValid),
            new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_INVALID_NAME_CONSTRAINTS, ErrorCode.CERT_E_INVALID_NAME, X509ChainStatusFlags.InvalidNameConstraints),
            new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_HAS_NOT_SUPPORTED_NAME_CONSTRAINT, ErrorCode.CERT_E_INVALID_NAME, X509ChainStatusFlags.HasNotSupportedNameConstraint),
            new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_HAS_NOT_DEFINED_NAME_CONSTRAINT, ErrorCode.CERT_E_INVALID_NAME, X509ChainStatusFlags.HasNotDefinedNameConstraint),
            new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_HAS_NOT_PERMITTED_NAME_CONSTRAINT, ErrorCode.CERT_E_INVALID_NAME, X509ChainStatusFlags.HasNotPermittedNameConstraint),
            new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_HAS_EXCLUDED_NAME_CONSTRAINT, ErrorCode.CERT_E_INVALID_NAME, X509ChainStatusFlags.HasExcludedNameConstraint),
            new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_INVALID_POLICY_CONSTRAINTS, ErrorCode.CERT_E_INVALID_POLICY, X509ChainStatusFlags.InvalidPolicyConstraints),
            new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_NO_ISSUANCE_CHAIN_POLICY, ErrorCode.CERT_E_INVALID_POLICY, X509ChainStatusFlags.NoIssuanceChainPolicy),
            new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_INVALID_BASIC_CONSTRAINTS, ErrorCode.TRUST_E_BASIC_CONSTRAINTS, X509ChainStatusFlags.InvalidBasicConstraints),
            new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_IS_NOT_TIME_NESTED, ErrorCode.CERT_E_VALIDITYPERIODNESTING, X509ChainStatusFlags.NotTimeNested),
            new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_REVOCATION_STATUS_UNKNOWN, ErrorCode.CRYPT_E_NO_REVOCATION_CHECK, X509ChainStatusFlags.RevocationStatusUnknown),
            new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_IS_OFFLINE_REVOCATION, ErrorCode.CRYPT_E_REVOCATION_OFFLINE, X509ChainStatusFlags.OfflineRevocation),
            new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_IS_EXPLICIT_DISTRUST, ErrorCode.TRUST_E_EXPLICIT_DISTRUST, X509ChainStatusFlags.ExplicitDistrust),
            new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_HAS_NOT_SUPPORTED_CRITICAL_EXT, ErrorCode.CERT_E_CRITICAL, X509ChainStatusFlags.HasNotSupportedCriticalExtension),
            new X509ChainErrorMapping(CertTrustErrorStatus.CERT_TRUST_HAS_WEAK_SIGNATURE, ErrorCode.CERTSRV_E_WEAK_SIGNATURE_OR_KEY, X509ChainStatusFlags.HasWeakSignature),
        };
    }
}
