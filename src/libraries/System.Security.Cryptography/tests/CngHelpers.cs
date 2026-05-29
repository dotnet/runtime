// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal static class CngHelpers
    {
        public static CryptographicException ToCryptographicException(this Interop.NCrypt.ErrorCode errorCode)
        {
            return ((int)errorCode).ToCryptographicException();
        }
    }
}
