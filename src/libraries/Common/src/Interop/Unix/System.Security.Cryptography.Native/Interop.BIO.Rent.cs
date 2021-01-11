// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        internal static ArraySegment<byte> RentReadMemoryBio(SafeBioHandle bio)
        {
            bool addedRef = false;

            try
            {
                bio.DangerousAddRef(ref addedRef);
                IntPtr bioPtr = bio.DangerousGetHandle();

                int size = GetMemoryBioSize(bioPtr);
                byte[] rented = CryptoPool.Rent(size);

                int read = CryptoNative_BioRead(
                    bioPtr,
                    ref MemoryMarshal.GetReference(rented.AsSpan()),
                    rented.Length);

                Debug.Assert(read == size);
                return new ArraySegment<byte>(rented, 0, read);
            }
            finally
            {
                if (addedRef)
                {
                    bio.DangerousRelease();
                }
            }
        }

        internal static byte[] ReadMemoryBio(SafeBioHandle bio)
        {
            bool addedRef = false;

            try
            {
                bio.DangerousAddRef(ref addedRef);
                IntPtr bioPtr = bio.DangerousGetHandle();

                int size = GetMemoryBioSize(bioPtr);
                byte[] ret = new byte[size];

                int read = CryptoNative_BioRead(
                    bioPtr,
                    ref MemoryMarshal.GetReference(ret.AsSpan()),
                    ret.Length);

                Debug.Assert(read == size);
                return ret;
            }
            finally
            {
                if (addedRef)
                {
                    bio.DangerousRelease();
                }
            }
        }
    }
}
