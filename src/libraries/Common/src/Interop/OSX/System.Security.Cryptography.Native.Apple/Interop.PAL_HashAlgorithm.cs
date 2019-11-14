// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        internal enum PAL_HashAlgorithm
        {
            Unknown = 0,
            Md5,
            Sha1,
            Sha256,
            Sha384,
            Sha512,
        }
    }
}
