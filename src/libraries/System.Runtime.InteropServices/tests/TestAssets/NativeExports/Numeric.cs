// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace NativeExports
{
    public static unsafe class Numeric
    {
        [UnmanagedCallersOnly(EntryPoint = "subtract_return_byte")]
        public static byte SubtractReturnByte(byte a, byte b)
        {
            return (byte)(a - b);
        }

        [UnmanagedCallersOnly(EntryPoint = "subtract_out_byte")]
        public static void SubtractReturnAsOutByte(byte a, byte b, byte* ret)
        {
            *ret = (byte)(a - b);
        }

        [UnmanagedCallersOnly(EntryPoint = "subtract_ref_byte")]
        public static void SubtractRefByte(byte a, byte* b)
        {
            *b = (byte)(a - *b);
        }

        [UnmanagedCallersOnly(EntryPoint = "subtract_return_int")]
        public static int SubtractReturnInt(int a, int b)
        {
            return a - b;
        }

        [UnmanagedCallersOnly(EntryPoint = "subtract_out_int")]
        public static void SubtractReturnAsOutInt(int a, int b, int* ret)
        {
            *ret = a - b;
        }

        [UnmanagedCallersOnly(EntryPoint = "subtract_ref_int")]
        public static void SubtractRefInt(int a, int* b)
        {
            *b = a - *b;
        }
    }
}
