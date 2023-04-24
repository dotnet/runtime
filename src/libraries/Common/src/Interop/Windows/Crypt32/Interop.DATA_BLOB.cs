// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct DATA_BLOB
        {
            internal uint cbData;
            internal IntPtr pbData;

            internal DATA_BLOB(IntPtr handle, uint size)
            {
                cbData = size;
                pbData = handle;
            }

            internal byte[] ToByteArray()
            {
                if (cbData == 0)
                {
                    return Array.Empty<byte>();
                }

                byte[] array = new byte[cbData];
                Marshal.Copy(pbData, array, 0, (int)cbData);
                return array;
            }

            internal unsafe ReadOnlySpan<byte> DangerousAsSpan() => new ReadOnlySpan<byte>((void*)pbData, (int)cbData);
        }
    }
}
