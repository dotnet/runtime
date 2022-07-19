// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Managed representation of an NCrypt key
    /// </summary>
    public sealed partial class CngKey : IDisposable
    {
        private readonly SafeNCryptKeyHandle _keyHandle;
        private readonly SafeNCryptProviderHandle _providerHandle;

        private CngKey(SafeNCryptProviderHandle providerHandle, SafeNCryptKeyHandle keyHandle)
        {
            Debug.Assert(keyHandle != null && !keyHandle.IsInvalid && !keyHandle.IsClosed);
            Debug.Assert(providerHandle != null && !providerHandle.IsInvalid && !providerHandle.IsClosed);

            _providerHandle = providerHandle;
            _keyHandle = keyHandle;
        }

        public void Dispose()
        {
            _providerHandle?.Dispose();
            _keyHandle?.Dispose();
        }

        //
        // The C# construct
        //
        //    fixed (byte* p = new byte[0])
        //
        // sets "p" to 0 rather than a valid address. Sometimes, we actually want a non-NULL pointer instead.
        // (Some CNG apis actually care whether the buffer pointer is NULL or not, even if the accompanying
        // size argument is 0.)
        //
        // This helper enables the syntax:
        //
        //    fixed (byte* p = new byte[0].MapZeroLengthArrayToNonNullPointer())
        //
        // which always sets "p" to a non-NULL pointer for a non-null byte array.
        //
        private static byte[]? MapZeroLengthArrayToNonNullPointer(byte[]? src)
        {
            if (src != null && src.Length == 0)
            {
                return new byte[1];
            }

            return src;
        }
    }
}
