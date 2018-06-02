// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal static unsafe void GetRandomBytes(byte* buffer, int length)
    {
        Debug.Assert(buffer != null);
        Debug.Assert(length >= 0);

        BCrypt.NTSTATUS status = BCrypt.BCryptGenRandom(IntPtr.Zero, buffer, length, BCrypt.BCRYPT_USE_SYSTEM_PREFERRED_RNG);
        if (status != BCrypt.NTSTATUS.STATUS_SUCCESS)
        {
            if (status == BCrypt.NTSTATUS.STATUS_NO_MEMORY)
            {
                throw new OutOfMemoryException();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
