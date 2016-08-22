// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography;

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
