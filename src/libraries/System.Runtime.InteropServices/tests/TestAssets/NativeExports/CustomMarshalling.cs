// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using SharedTypes;
using static SharedTypes.BoolStructMarshaller;
using static SharedTypes.StringContainerMarshaller;

namespace NativeExports
{
    public static unsafe class CustomMarshalling
    {
        [UnmanagedCallersOnly(EntryPoint = "stringcontainer_deepduplicate")]
        [DNNE.C99DeclCode("struct string_container { char* str1; char* str2; };")]
        public static void DeepDuplicateStrings(
            [DNNE.C99Type("struct string_container")] StringContainerNative strings,
            [DNNE.C99Type("struct string_container*")] StringContainerNative* pStringsOut)
        {
            // Round trip through the managed view to allocate a new native instance.
            *pStringsOut = StringContainerMarshaller.In.ConvertToUnmanaged(StringContainerMarshaller.Out.ConvertToManaged(strings));
        }

        [UnmanagedCallersOnly(EntryPoint = "stringcontainer_reverse_strings")]
        public static void ReverseStrings(
            [DNNE.C99Type("struct string_container*")] StringContainerNative* strings)
        {
            strings->str1 = (IntPtr)Strings.Reverse((byte*)strings->str1);
            strings->str2 = (IntPtr)Strings.Reverse((byte*)strings->str2);
        }

        [UnmanagedCallersOnly(EntryPoint = "get_long_bytes_as_double")]
        public static double GetLongBytesAsDouble(long l)
        {
            return *(double*)&l;
        }

        [UnmanagedCallersOnly(EntryPoint = "get_bytes_as_double_big_endian")]
        public static double GetBytesAsDoubleBigEndian(byte* b)
        {
            return BinaryPrimitives.ReadDoubleBigEndian(new ReadOnlySpan<byte>(b, 8));
        }

        [UnmanagedCallersOnly(EntryPoint = "return_zero")]
        public static int ReturnZero(int* ret)
        {
            *ret = 0;
            return 0;
        }

        [UnmanagedCallersOnly(EntryPoint = "negate_bools")]
        [DNNE.C99DeclCode("struct bool_struct { int8_t b1; int8_t b2; int8_t b3; };")]
        public static void NegateBools(
            [DNNE.C99Type("struct bool_struct")] BoolStructNative boolStruct,
            [DNNE.C99Type("struct bool_struct*")] BoolStructNative* pBoolStructOut)
        {
            *pBoolStructOut = new BoolStructNative
            {
                b1 = (byte)(boolStruct.b1 != 0 ? 0 : 1),
                b2 = (byte)(boolStruct.b2 != 0 ? 0 : 1),
                b3 = (byte)(boolStruct.b3 != 0 ? 0 : 1),
            };
        }

        [UnmanagedCallersOnly(EntryPoint = "and_bools_ref")]
        public static byte AndBoolsRef(
            [DNNE.C99Type("struct bool_struct*")] BoolStructNative* boolStruct)
        {
            return (byte)(boolStruct->b1 != 0 && boolStruct->b2 != 0 && boolStruct->b3 != 0 ? 1 : 0);
        }

        [UnmanagedCallersOnly(EntryPoint = "double_int_ref")]
        public static int* DoubleIntRef(int* pInt)
        {
            *pInt *= 2;
            int* retVal = (int*)Marshal.AllocCoTaskMem(sizeof(int));
            *retVal = *pInt;
            return retVal;
        }
    }
}
