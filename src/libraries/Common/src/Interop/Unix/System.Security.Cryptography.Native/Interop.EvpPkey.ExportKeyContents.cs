// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        internal static void ExportKeyContents(
            SafeEvpPKeyHandle key,
            Span<byte> destination,
            Func<SafeEvpPKeyHandle, Span<byte>, int, int> action)
        {
            const int Success = 1;
            const int Fail = 0;
            const int NotRetrievable = -1;

            int ret = action(key, destination, destination.Length);

            switch (ret)
            {
                case Success:
                    return;
                case NotRetrievable:
                    destination.Clear();
                    throw new CryptographicException(SR.Cryptography_NotRetrievable);
                case Fail:
                    destination.Clear();
                    throw CreateOpenSslCryptographicException();
                default:
                    destination.Clear();
                    Debug.Fail($"Unexpected return value {ret}.");
                    throw new CryptographicException();
            }
        }
    }
}
