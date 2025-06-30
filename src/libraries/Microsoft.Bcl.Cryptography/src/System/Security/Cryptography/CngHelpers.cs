// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    internal static partial class CngHelpers
    {
        internal static unsafe void GetRandomBytes(Span<byte> buffer)
        {
            if (buffer.Length > 0)
            {
                fixed (byte* pbBuffer = buffer)
                {
                    Interop.BCrypt.NTSTATUS status = Interop.BCrypt.BCryptGenRandom(IntPtr.Zero, pbBuffer, buffer.Length, Interop.BCrypt.BCRYPT_USE_SYSTEM_PREFERRED_RNG);
                    if (status != Interop.BCrypt.NTSTATUS.STATUS_SUCCESS)
                        throw Interop.BCrypt.CreateCryptographicException(status);
                }
            }
        }
    }
}
