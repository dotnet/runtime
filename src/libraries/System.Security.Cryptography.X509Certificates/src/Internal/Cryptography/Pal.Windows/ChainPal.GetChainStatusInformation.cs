// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Internal.NativeCrypto;
using Internal.Cryptography;
using Internal.Cryptography.Pal.Native;

using System.Security.Cryptography;

using FILETIME = Internal.Cryptography.Pal.Native.FILETIME;
using SafeX509ChainHandle = Microsoft.Win32.SafeHandles.SafeX509ChainHandle;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class ChainPal : IDisposable, IChainPal
    {
        private static X509ChainStatus[] GetChainStatusInformation(CertTrustErrorStatus dwStatus)
        {
            if (dwStatus == CertTrustErrorStatus.CERT_TRUST_NO_ERROR)
                return new X509ChainStatus[0];

            int count = 0;
            for (uint bits = (uint)dwStatus; bits != 0; bits = bits >> 1)
            {
                if ((bits & 0x1) != 0)
                    count++;
            }

            X509ChainStatus[] chainStatus = new X509ChainStatus[count];
            int index = 0;
            if ((dwStatus & CertTrustErrorStatus.CERT_TRUST_IS_NOT_SIGNATURE_VALID) != 0)
            {
                chainStatus[index].StatusInformation = GetSystemErrorString(ErrorCode.TRUST_E_CERT_SIGNATURE);
                chainStatus[index].Status = X509ChainStatusFlags.NotSignatureValid;
                index++;
                dwStatus &= ~CertTrustErrorStatus.CERT_TRUST_IS_NOT_SIGNATURE_VALID;
            }

            if ((dwStatus & CertTrustErrorStatus.CERT_TRUST_CTL_IS_NOT_SIGNATURE_VALID) != 0)
            {
                chainStatus[index].StatusInformation = GetSystemErrorString(ErrorCode.TRUST_E_CERT_SIGNATURE);
                chainStatus[index].Status = X509ChainStatusFlags.CtlNotSignatureValid;
                index++;
                dwStatus &= ~CertTrustErrorStatus.CERT_TRUST_CTL_IS_NOT_SIGNATURE_VALID;
            }

            if ((dwStatus & CertTrustErrorStatus.CERT_TRUST_IS_UNTRUSTED_ROOT) != 0)
            {
                chainStatus[index].StatusInformation = GetSystemErrorString(ErrorCode.CERT_E_UNTRUSTEDROOT);
                chainStatus[index].Status = X509ChainStatusFlags.UntrustedRoot;
                index++;
                dwStatus &= ~CertTrustErrorStatus.CERT_TRUST_IS_UNTRUSTED_ROOT;
            }

            if ((dwStatus & CertTrustErrorStatus.CERT_TRUST_IS_PARTIAL_CHAIN) != 0)
            {
                chainStatus[index].StatusInformation = GetSystemErrorString(ErrorCode.CERT_E_CHAINING);
                chainStatus[index].Status = X509ChainStatusFlags.PartialChain;
                index++;
                dwStatus &= ~CertTrustErrorStatus.CERT_TRUST_IS_PARTIAL_CHAIN;
            }

            if ((dwStatus & CertTrustErrorStatus.CERT_TRUST_IS_REVOKED) != 0)
            {
                chainStatus[index].StatusInformation = GetSystemErrorString(ErrorCode.CRYPT_E_REVOKED);
                chainStatus[index].Status = X509ChainStatusFlags.Revoked;
                index++;
                dwStatus &= ~CertTrustErrorStatus.CERT_TRUST_IS_REVOKED;
            }

            if ((dwStatus & CertTrustErrorStatus.CERT_TRUST_IS_NOT_VALID_FOR_USAGE) != 0)
            {
                chainStatus[index].StatusInformation = GetSystemErrorString(ErrorCode.CERT_E_WRONG_USAGE);
                chainStatus[index].Status = X509ChainStatusFlags.NotValidForUsage;
                index++;
                dwStatus &= ~CertTrustErrorStatus.CERT_TRUST_IS_NOT_VALID_FOR_USAGE;
            }

            if ((dwStatus & CertTrustErrorStatus.CERT_TRUST_CTL_IS_NOT_VALID_FOR_USAGE) != 0)
            {
                chainStatus[index].StatusInformation = GetSystemErrorString(ErrorCode.CERT_E_WRONG_USAGE);
                chainStatus[index].Status = X509ChainStatusFlags.CtlNotValidForUsage;
                index++;
                dwStatus &= ~CertTrustErrorStatus.CERT_TRUST_CTL_IS_NOT_VALID_FOR_USAGE;
            }

            if ((dwStatus & CertTrustErrorStatus.CERT_TRUST_IS_NOT_TIME_VALID) != 0)
            {
                chainStatus[index].StatusInformation = GetSystemErrorString(ErrorCode.CERT_E_EXPIRED);
                chainStatus[index].Status = X509ChainStatusFlags.NotTimeValid;
                index++;
                dwStatus &= ~CertTrustErrorStatus.CERT_TRUST_IS_NOT_TIME_VALID;
            }

            if ((dwStatus & CertTrustErrorStatus.CERT_TRUST_CTL_IS_NOT_TIME_VALID) != 0)
            {
                chainStatus[index].StatusInformation = GetSystemErrorString(ErrorCode.CERT_E_EXPIRED);
                chainStatus[index].Status = X509ChainStatusFlags.CtlNotTimeValid;
                index++;
                dwStatus &= ~CertTrustErrorStatus.CERT_TRUST_CTL_IS_NOT_TIME_VALID;
            }

            if ((dwStatus & CertTrustErrorStatus.CERT_TRUST_INVALID_NAME_CONSTRAINTS) != 0)
            {
                chainStatus[index].StatusInformation = GetSystemErrorString(ErrorCode.CERT_E_INVALID_NAME);
                chainStatus[index].Status = X509ChainStatusFlags.InvalidNameConstraints;
                index++;
                dwStatus &= ~CertTrustErrorStatus.CERT_TRUST_INVALID_NAME_CONSTRAINTS;
            }

            if ((dwStatus & CertTrustErrorStatus.CERT_TRUST_HAS_NOT_SUPPORTED_NAME_CONSTRAINT) != 0)
            {
                chainStatus[index].StatusInformation = GetSystemErrorString(ErrorCode.CERT_E_INVALID_NAME);
                chainStatus[index].Status = X509ChainStatusFlags.HasNotSupportedNameConstraint;
                index++;
                dwStatus &= ~CertTrustErrorStatus.CERT_TRUST_HAS_NOT_SUPPORTED_NAME_CONSTRAINT;
            }

            if ((dwStatus & CertTrustErrorStatus.CERT_TRUST_HAS_NOT_DEFINED_NAME_CONSTRAINT) != 0)
            {
                chainStatus[index].StatusInformation = GetSystemErrorString(ErrorCode.CERT_E_INVALID_NAME);
                chainStatus[index].Status = X509ChainStatusFlags.HasNotDefinedNameConstraint;
                index++;
                dwStatus &= ~CertTrustErrorStatus.CERT_TRUST_HAS_NOT_DEFINED_NAME_CONSTRAINT;
            }

            if ((dwStatus & CertTrustErrorStatus.CERT_TRUST_HAS_NOT_PERMITTED_NAME_CONSTRAINT) != 0)
            {
                chainStatus[index].StatusInformation = GetSystemErrorString(ErrorCode.CERT_E_INVALID_NAME);
                chainStatus[index].Status = X509ChainStatusFlags.HasNotPermittedNameConstraint;
                index++;
                dwStatus &= ~CertTrustErrorStatus.CERT_TRUST_HAS_NOT_PERMITTED_NAME_CONSTRAINT;
            }

            if ((dwStatus & CertTrustErrorStatus.CERT_TRUST_HAS_EXCLUDED_NAME_CONSTRAINT) != 0)
            {
                chainStatus[index].StatusInformation = GetSystemErrorString(ErrorCode.CERT_E_INVALID_NAME);
                chainStatus[index].Status = X509ChainStatusFlags.HasExcludedNameConstraint;
                index++;
                dwStatus &= ~CertTrustErrorStatus.CERT_TRUST_HAS_EXCLUDED_NAME_CONSTRAINT;
            }

            if ((dwStatus & CertTrustErrorStatus.CERT_TRUST_INVALID_POLICY_CONSTRAINTS) != 0)
            {
                chainStatus[index].StatusInformation = GetSystemErrorString(ErrorCode.CERT_E_INVALID_POLICY);
                chainStatus[index].Status = X509ChainStatusFlags.InvalidPolicyConstraints;
                index++;
                dwStatus &= ~CertTrustErrorStatus.CERT_TRUST_INVALID_POLICY_CONSTRAINTS;
            }

            if ((dwStatus & CertTrustErrorStatus.CERT_TRUST_NO_ISSUANCE_CHAIN_POLICY) != 0)
            {
                chainStatus[index].StatusInformation = GetSystemErrorString(ErrorCode.CERT_E_INVALID_POLICY);
                chainStatus[index].Status = X509ChainStatusFlags.NoIssuanceChainPolicy;
                index++;
                dwStatus &= ~CertTrustErrorStatus.CERT_TRUST_NO_ISSUANCE_CHAIN_POLICY;
            }

            if ((dwStatus & CertTrustErrorStatus.CERT_TRUST_INVALID_BASIC_CONSTRAINTS) != 0)
            {
                chainStatus[index].StatusInformation = GetSystemErrorString(ErrorCode.TRUST_E_BASIC_CONSTRAINTS);
                chainStatus[index].Status = X509ChainStatusFlags.InvalidBasicConstraints;
                index++;
                dwStatus &= ~CertTrustErrorStatus.CERT_TRUST_INVALID_BASIC_CONSTRAINTS;
            }

            if ((dwStatus & CertTrustErrorStatus.CERT_TRUST_IS_NOT_TIME_NESTED) != 0)
            {
                chainStatus[index].StatusInformation = GetSystemErrorString(ErrorCode.CERT_E_VALIDITYPERIODNESTING);
                chainStatus[index].Status = X509ChainStatusFlags.NotTimeNested;
                index++;
                dwStatus &= ~CertTrustErrorStatus.CERT_TRUST_IS_NOT_TIME_NESTED;
            }

            if ((dwStatus & CertTrustErrorStatus.CERT_TRUST_REVOCATION_STATUS_UNKNOWN) != 0)
            {
                chainStatus[index].StatusInformation = GetSystemErrorString(ErrorCode.CRYPT_E_NO_REVOCATION_CHECK);
                chainStatus[index].Status = X509ChainStatusFlags.RevocationStatusUnknown;
                index++;
                dwStatus &= ~CertTrustErrorStatus.CERT_TRUST_REVOCATION_STATUS_UNKNOWN;
            }

            if ((dwStatus & CertTrustErrorStatus.CERT_TRUST_IS_OFFLINE_REVOCATION) != 0)
            {
                chainStatus[index].StatusInformation = GetSystemErrorString(ErrorCode.CRYPT_E_REVOCATION_OFFLINE);
                chainStatus[index].Status = X509ChainStatusFlags.OfflineRevocation;
                index++;
                dwStatus &= ~CertTrustErrorStatus.CERT_TRUST_IS_OFFLINE_REVOCATION;
            }

            int shiftCount = 0;
            for (uint bits = (uint)dwStatus; bits != 0; bits = bits >> 1)
            {
                if ((bits & 0x1) != 0)
                {
                    chainStatus[index].Status = (X509ChainStatusFlags)(1 << shiftCount);
                    chainStatus[index].StatusInformation = SR.Unknown_Error;
                    index++;
                }
                shiftCount++;
            }

            return chainStatus;
        }

        private static String GetSystemErrorString(int errorCode)
        {
            StringBuilder strMessage = new StringBuilder(512);
            int dwErrorCode = Interop.localization.FormatMessage(
                FormatMessageFlags.FORMAT_MESSAGE_FROM_SYSTEM | FormatMessageFlags.FORMAT_MESSAGE_IGNORE_INSERTS,
                IntPtr.Zero,
                errorCode,
                0,
                strMessage,
                strMessage.Capacity,
                IntPtr.Zero);
            if (dwErrorCode != 0)
                return strMessage.ToString();
            else
                return SR.Unknown_Error;
        }
    }
}
