// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        internal delegate int GetI2DSizeFunc<in THandle>(THandle handle);

        internal delegate int I2DFunc<in THandle>(THandle handle, byte[] buf);

        internal static byte[] OpenSslI2D<THandle>(GetI2DSizeFunc<THandle> getSize, I2DFunc<THandle> i2d, THandle handle)
            where THandle : SafeHandle
        {
            int size = getSize(handle);

            if (size < 1)
            {
                throw Crypto.CreateOpenSslCryptographicException();
            }

            byte[] data = new byte[size];

            int size2 = i2d(handle, data);
            Debug.Assert(size == size2);

            return data;
        }
    }
}
