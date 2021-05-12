// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Mono
{
    internal static class RuntimeMarshal
    {
        internal static string PtrToUtf8String(IntPtr ptr)
        {
            unsafe
            {
                if (ptr == IntPtr.Zero)
                    return string.Empty;

                byte* bytes = (byte*)ptr;
                int length = 0;

                try
                {
                    while (bytes++[0] != 0)
                        length++;
                }
                catch (NullReferenceException)
                {
                    throw new ArgumentOutOfRangeException(nameof(ptr), "Value does not refer to a valid string.");
                }

                return new string((sbyte*)ptr, 0, length, System.Text.Encoding.UTF8);
            }
        }

        internal static SafeStringMarshal MarshalString(string? str)
        {
            return new SafeStringMarshal(str);
        }

        private static int DecodeBlobSize(IntPtr in_ptr, out IntPtr out_ptr)
        {
            uint size;
            unsafe
            {
                byte* ptr = (byte*)in_ptr;

                if ((*ptr & 0x80) == 0)
                {
                    size = (uint)(ptr[0] & 0x7f);
                    ptr++;
                }
                else if ((*ptr & 0x40) == 0)
                {
                    size = (uint)(((ptr[0] & 0x3f) << 8) + ptr[1]);
                    ptr += 2;
                }
                else
                {
                    size = (uint)(((ptr[0] & 0x1f) << 24) +
                        (ptr[1] << 16) +
                        (ptr[2] << 8) +
                        ptr[3]);
                    ptr += 4;
                }
                out_ptr = (IntPtr)ptr;
            }

            return (int)size;
        }

        internal static byte[] DecodeBlobArray(IntPtr ptr)
        {
            IntPtr out_ptr;
            int size = DecodeBlobSize(ptr, out out_ptr);
            byte[] res = new byte[size];
            Marshal.Copy(out_ptr, res, 0, size);
            return res;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void FreeAssemblyName(ref MonoAssemblyName name, bool freeStruct);
    }
}
