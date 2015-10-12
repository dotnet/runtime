// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class libcrypto
    {
        internal unsafe delegate int I2DFunc<in THandle>(THandle handle, byte** @out);

        internal static unsafe byte[] OpenSslI2D<THandle>(I2DFunc<THandle> i2d, THandle handle)
            where THandle : SafeHandle
        {
            int size = i2d(handle, null);

            if (size < 1)
            {
                throw Crypto.CreateOpenSslCryptographicException();
            }

            byte[] data = new byte[size];

            fixed (byte* pDataFixed = data)
            {
                byte* pData = pDataFixed;
                byte** ppData = &pData;

                int size2 = i2d(handle, ppData);

                Debug.Assert(size == size2);
            }

            return data;
        }
    }
}
