// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.IO
{
    public static partial class Path
    {
        private static byte[] CreateCryptoRandomByteArray(int byteLength)
        {
            // We need to fill a byte array with cryptographically-strong random bytes, but we can't reference
            // System.Security.Cryptography.RandomNumberGenerator.dll due to layering.  Instead, we just
            // call to BCryptGenRandom directly, which is all that RandomNumberGenerator does.

            var arr = new byte[byteLength];
            Interop.BCrypt.NTSTATUS status = Interop.BCrypt.BCryptGenRandom(arr, arr.Length);
            if (status == Interop.BCrypt.NTSTATUS.STATUS_SUCCESS)
            {
                return arr;
            }
            else if (status == Interop.BCrypt.NTSTATUS.STATUS_NO_MEMORY)
            {
                throw new OutOfMemoryException();
            }
            else
            {
                Debug.Fail("BCryptGenRandom should only fail due to OOM or invalid args / handle inputs.");
                throw new InvalidOperationException();
            }
        }
    }
}
