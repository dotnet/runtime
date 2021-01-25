// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal static class ErrorCodeHelper
    {
        public static CryptographicException ToCryptographicException(this Interop.NCrypt.ErrorCode errorCode)
        {
            return ((int)errorCode).ToCryptographicException();
        }
    }
}
