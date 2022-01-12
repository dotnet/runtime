// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace NativeExports
{
    public static unsafe class HResult
    {
        [UnmanagedCallersOnly(EntryPoint = "hresult_return")]
        public static int Return(int hr)
        {
            return hr;
        }

        [UnmanagedCallersOnly(EntryPoint = "hresult_out_int")]
        public static int ReturnAsOutInt(int hr, int* ret)
        {
            *ret = hr;
            return hr;
        }

        [UnmanagedCallersOnly(EntryPoint = "hresult_out_ushort")]
        public static int ReturnAsOutShort(int hr, ushort* ret)
        {
            *ret = (ushort)hr;
            return hr;
        }

        [UnmanagedCallersOnly(EntryPoint = "hresult_out_ushort_string")]
        public static int ReturnAsOutString(int hr, ushort** ret)
        {
            string str = hr.ToString();
            *ret = (ushort*)Marshal.StringToCoTaskMemUni(str);
            return hr;
        }

        [UnmanagedCallersOnly(EntryPoint = "hresult_out_int_array")]
        public static int ReturnAsOutIntArray(int hr, int** ret)
        {
            const int NumBytesInInt = sizeof(int);
            *ret = (int*)Marshal.AllocCoTaskMem(sizeof(int) * NumBytesInInt);
            new Span<int>(*ret, NumBytesInInt).Fill(hr);
            return hr;
        }

        [UnmanagedCallersOnly(EntryPoint = "hresult_out_ushort_string_array")]
        public static int ReturnAsOutStringArray(int hr, ushort*** ret)
        {
            const int NumBytesInInt = sizeof(int);
            string str = hr.ToString();

            *ret = (ushort**)Marshal.AllocCoTaskMem(sizeof(ushort*) * NumBytesInInt);
            for (int i = 0; i < NumBytesInInt; i++)
            {
                (*ret)[i] = (ushort*)Marshal.StringToCoTaskMemUni(str);
            }

            return hr;
        }

        [UnmanagedCallersOnly(EntryPoint = "hresult_out_handle")]
        public static int ReturnAsOutHandle(int hr, nint* handle)
        {
            *handle = hr;
            return hr;
        }
    }
}
