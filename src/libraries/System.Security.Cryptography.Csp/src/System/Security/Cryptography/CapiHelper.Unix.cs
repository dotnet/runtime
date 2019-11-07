// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography;

namespace Internal.NativeCrypto
{
    internal static partial class CapiHelper
    {
        public static CryptographicException GetBadDataException()
        {
            const int NTE_BAD_DATA = unchecked((int)CryptKeyError.NTE_BAD_DATA);
            return new CryptographicException(NTE_BAD_DATA);
        }

        public static CryptographicException GetEFailException()
        {
            return new CryptographicException(E_FAIL);
        }
    }
}
