// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.IO
{
    public static partial class Path
    {
        private static unsafe void GetCryptoRandomBytes(byte* bytes, int byteCount)
        {
            // We need to fill a byte array with cryptographically-strong random bytes, but we can't reference
            // System.Security.Cryptography.RandomNumberGenerator.dll due to layering.  Instead, we just
            // call to BCryptGenRandom directly, which is all that RandomNumberGenerator does.

            Debug.Assert(bytes != null);
            Debug.Assert(byteCount >= 0);

            Interop.BCrypt.NTSTATUS status = Interop.BCrypt.BCryptGenRandom(bytes, byteCount);
            if (status == Interop.BCrypt.NTSTATUS.STATUS_SUCCESS)
            {
                return;
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
