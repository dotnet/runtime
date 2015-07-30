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
        internal unsafe delegate THandle D2IFunc<out THandle>(IntPtr zero, byte** ppin, int len);

        internal unsafe delegate int I2DFunc<in THandle>(THandle handle, byte** @out);

        internal static unsafe THandle OpenSslD2I<THandle>(D2IFunc<THandle> d2i, byte[] data, bool checkHandle=true)
            where THandle : SafeHandle
        {
            // The OpenSSL d2i_* functions are set up for cascaded calls, so they increment *ppData while reading.
            // Since only the outermost caller (that'd be this method) knows who the outermost caller is, it's their
            // job to keep a pointer to the original data.
            fixed (byte* pDataFixed = data)
            {
                byte* pData = pDataFixed;
                byte** ppData = &pData;

                THandle handle = d2i(IntPtr.Zero, ppData, data.Length);

                if (checkHandle)
                {
                    CheckValidOpenSslHandle(handle);
                }

                return handle;
            }
        }

        internal static unsafe byte[] OpenSslI2D<THandle>(I2DFunc<THandle> i2d, THandle handle)
            where THandle : SafeHandle
        {
            int size = i2d(handle, null);

            if (size < 1)
            {
                throw CreateOpenSslCryptographicException();
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
