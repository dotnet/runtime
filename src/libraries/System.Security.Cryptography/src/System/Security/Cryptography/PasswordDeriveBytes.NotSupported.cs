// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    public partial class PasswordDeriveBytes : DeriveBytes
    {
        [SupportedOSPlatform("windows")]
        public byte[] CryptDeriveKey(string? algname, string? alghashname, int keySize, byte[] rgbIV)
        {
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CAPI_Required, nameof(CryptDeriveKey)));
        }
    }
}
