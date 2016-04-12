// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    //
    // Error codes for specific throw sites. Defined outside of Internal.Crytography.Pal.Native as some non-Pal code uses these.
    // Since these error codes are publicly surfaced through the Exception class, these hresults are effectively managed exchange values despite
    // their Win32 origin.
    //
    internal static class ErrorCode
    {
        public const int CERT_E_CHAINING = unchecked((int)0x800B010A);
        public const int CERT_E_CRITICAL = unchecked((int)0x800B0105);
        public const int CERT_E_EXPIRED = unchecked((int)0x800B0101);
        public const int CERT_E_INVALID_NAME = unchecked((int)0x800B0114);
        public const int CERT_E_INVALID_POLICY = unchecked((int)0x800B0113);
        public const int CERT_E_UNTRUSTEDROOT = unchecked((int)0x800B0109);
        public const int CERT_E_VALIDITYPERIODNESTING = unchecked((int)0x800B0102);
        public const int CERT_E_WRONG_USAGE = unchecked((int)0x800B0110);
        public const int CERTSRV_E_WEAK_SIGNATURE_OR_KEY = unchecked((int)0x80094016);
        public const int CRYPT_E_NO_REVOCATION_CHECK = unchecked((int)0x80092012);
        public const int CRYPT_E_NOT_FOUND = unchecked((int)0x80092004);
        public const int CRYPT_E_REVOCATION_OFFLINE = unchecked((int)0x80092013);
        public const int CRYPT_E_REVOKED = unchecked((int)0x80092010);
        public const int CRYPT_E_SIGNER_NOT_FOUND = unchecked((int)0x8009100e);
        public const int E_POINTER = unchecked((int)0x80004003);
        public const int ERROR_INVALID_PARAMETER = 0x00000057;
        public const int HRESULT_INVALID_HANDLE = unchecked((int)0x80070006);
        public const int NTE_BAD_PUBLIC_KEY = unchecked((int)0x80090015);
        public const int TRUST_E_BASIC_CONSTRAINTS = unchecked((int)0x80096019);
        public const int TRUST_E_CERT_SIGNATURE = unchecked((int)0x80096004);
        public const int TRUST_E_EXPLICIT_DISTRUST = unchecked((int)0x800B0111);
    }
}
