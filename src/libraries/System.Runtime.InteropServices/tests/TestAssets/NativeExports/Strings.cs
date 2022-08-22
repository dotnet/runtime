// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace NativeExports
{
    public static unsafe class Strings
    {
        [UnmanagedCallersOnly(EntryPoint = "return_length_ushort")]
        public static int ReturnLengthUShort(ushort* input)
        {
            if (input == null)
                return -1;

            return GetLength(input);
        }

        [UnmanagedCallersOnly(EntryPoint = "return_length_byte")]
        public static int ReturnLengthByte(byte* input)
        {
            if (input == null)
                return -1;

            return GetLength(input);
        }

        [UnmanagedCallersOnly(EntryPoint = "return_length_bstr")]
        public static int ReturnLengthBStr(byte* input)
        {
            if (input == null)
                return -1;

            return GetLengthBStr(input);
        }

        [UnmanagedCallersOnly(EntryPoint = "reverse_return_ushort")]
        public static ushort* ReverseReturnUShort(ushort* input)
        {
            return Reverse(input);
        }

        [UnmanagedCallersOnly(EntryPoint = "reverse_return_byte")]
        public static byte* ReverseReturnByte(byte* input)
        {
            return Reverse(input);
        }

        [UnmanagedCallersOnly(EntryPoint = "reverse_return_bstr")]
        public static byte* ReverseReturnBStr(byte* input)
        {
            return ReverseBStr(input);
        }

        [UnmanagedCallersOnly(EntryPoint = "reverse_out_ushort")]
        public static void ReverseReturnAsOutUShort(ushort* input, ushort** ret)
        {
            *ret = Reverse(input);
        }

        [UnmanagedCallersOnly(EntryPoint = "reverse_out_byte")]
        public static void ReverseReturnAsOutByte(byte* input, byte** ret)
        {
            *ret = Reverse(input);
        }

        [UnmanagedCallersOnly(EntryPoint = "reverse_out_bstr")]
        public static void ReverseReturnAsOutBStr(byte* input, byte** ret)
        {
            *ret = ReverseBStr(input);
        }

        [UnmanagedCallersOnly(EntryPoint = "reverse_inplace_ref_ushort")]
        public static void ReverseInPlaceUShort(ushort** refInput)
        {
            if (*refInput == null)
                return;

            int len = GetLength(*refInput);
            var span = new Span<ushort>(*refInput, len);
            span.Reverse();
        }

        [UnmanagedCallersOnly(EntryPoint = "reverse_inplace_ref_byte")]
        public static void ReverseInPlaceByte(byte** refInput)
        {
            int len = GetLength(*refInput);
            var span = new Span<byte>(*refInput, len);
            span.Reverse();
        }

        [UnmanagedCallersOnly(EntryPoint = "reverse_inplace_ref_bstr")]
        public static void ReverseInPlaceBStr(byte** refInput)
        {
            int len = GetLengthBStr(*refInput);

            // Testing of BSTRs is done under the assumption the
            // test character input size is 16 bit.
            var span = new Span<ushort>(*refInput, len);
            span.Reverse();
        }

        [UnmanagedCallersOnly(EntryPoint = "reverse_replace_ref_ushort")]
        public static void ReverseReplaceRefUShort(ushort** s)
        {
            if (*s == null)
                return;

            ushort* ret = Reverse(*s);
            Marshal.FreeCoTaskMem((IntPtr)(*s));
            *s = ret;
        }

        [UnmanagedCallersOnly(EntryPoint = "reverse_replace_ref_byte")]
        public static void ReverseReplaceRefByte(byte** s)
        {
            if (*s == null)
                return;

            byte* ret = Reverse(*s);
            Marshal.FreeCoTaskMem((IntPtr)(*s));
            *s = ret;
        }

        [UnmanagedCallersOnly(EntryPoint = "reverse_replace_ref_bstr")]
        public static void ReverseReplaceRefBStr(byte** s)
        {
            if (*s == null)
                return;

            byte* ret = ReverseBStr(*s);
            Marshal.FreeBSTR((IntPtr)(*s));
            *s = ret;
        }

        internal static ushort* Reverse(ushort *s)
        {
            if (s == null)
                return null;

            int len = GetLength(s);
            ushort* ret = (ushort*)Marshal.AllocCoTaskMem((len + 1) * sizeof(ushort));
            var span = new Span<ushort>(ret, len);

            new Span<ushort>(s, len).CopyTo(span);
            span.Reverse();
            ret[len] = 0;
            return ret;
        }

        internal static byte* Reverse(byte* s)
        {
            if (s == null)
                return null;

            int len = GetLength(s);
            byte* ret = (byte*)Marshal.AllocCoTaskMem((len + 1) * sizeof(byte));
            var span = new Span<byte>(ret, len);

            new Span<byte>(s, len).CopyTo(span);
            span.Reverse();
            ret[len] = 0;
            return ret;
        }

        internal static byte* ReverseBStr(byte* s)
        {
            if (s == null)
                return null;

            var arr = Marshal.PtrToStringBSTR((IntPtr)s).ToCharArray();
            Array.Reverse(arr);
            var revStr = new string(arr);
            return (byte*)Marshal.StringToBSTR(revStr);
        }

        private static int GetLength(ushort* input)
        {
            if (input == null)
                return 0;

            int len = 0;
            while (*input != 0)
            {
                input++;
                len++;
            }

            return len;
        }

        private static int GetLength(byte* input)
        {
            if (input == null)
                return 0;

            int len = 0;
            while (*input != 0)
            {
                input++;
                len++;
            }

            return len;
        }

        private static int GetLengthBStr(byte* input)
        {
            if (input == null)
                return 0;

            return Marshal.PtrToStringBSTR((IntPtr)input).Length;
        }
    }
}
