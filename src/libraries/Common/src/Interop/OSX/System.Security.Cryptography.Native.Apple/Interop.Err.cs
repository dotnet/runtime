// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        internal const string CCCryptorStatus = "CCCryptorStatus";
        internal const string CCRNGStatus = "CCRNGStatus";

        internal static Exception CreateExceptionForCCError(int errorCode, string errorType)
        {
            return new AppleCommonCryptoCryptographicException(
                errorCode,
                SR.Format(
                    SR.Cryptography_Unmapped_System_Typed_Error,
                    errorCode,
                    errorType));
        }

        internal static Exception CreateExceptionForCFError(SafeCFErrorHandle cfError)
        {
            Debug.Assert(cfError != null);

            if (cfError.IsInvalid)
            {
                return new CryptographicException();
            }

            return new AppleCFErrorCryptographicException(cfError);
        }

        private sealed class AppleCFErrorCryptographicException : CryptographicException
        {
            internal AppleCFErrorCryptographicException(SafeCFErrorHandle cfError)
                : base(Interop.CoreFoundation.GetErrorDescription(cfError))
            {
                HResult = Interop.CoreFoundation.GetErrorCode(cfError);
            }
        }

        private sealed class AppleCommonCryptoCryptographicException : CryptographicException
        {
            internal AppleCommonCryptoCryptographicException(int errorCode, string message)
                : base(message)
            {
                HResult = errorCode;
            }
        }
    }
}
