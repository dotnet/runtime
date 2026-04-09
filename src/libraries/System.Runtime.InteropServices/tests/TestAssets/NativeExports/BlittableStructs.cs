// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

using SharedTypes;

namespace NativeExports
{
    public static unsafe class BlittableStructs
    {
        [UnmanagedCallersOnly(EntryPoint = "blittablestructs_return_instance")]
        [DNNE.C99DeclCode("struct int_fields { int a; int b; int c; };")]
        [return: DNNE.C99Type("struct int_fields")]
        public static IntFields DoubleIntFields([DNNE.C99Type("struct int_fields")] IntFields input)
        {
            return new IntFields()
            {
                a = input.a * 2,
                b = input.b * 2,
                c = input.c * 2,
            };
        }

        [UnmanagedCallersOnly(EntryPoint = "blittablestructs_double_intfields_byref")]
        public static void DoubleIntFieldsByRef(
            [DNNE.C99Type("struct int_fields*")] IntFields* result)
        {
            result->a *= 2;
            result->b *= 2;
            result->c *= 2;
        }

        [UnmanagedCallersOnly(EntryPoint = "blittablestructs_double_intfields_refreturn")]
        public static void DoubleIntFieldsRefReturn(
            [DNNE.C99Type("struct int_fields")] IntFields input,
            [DNNE.C99Type("struct int_fields*")] IntFields* result)
        {
            result->a = input.a * 2;
            result->b = input.b * 2;
            result->c = input.c * 2;
        }

        [UnmanagedCallersOnly(EntryPoint = "blittablestructs_increment_invert_ptrfields_byref")]
        [DNNE.C99DeclCode("struct ptr_fields { int* i; int* b; uint16_t* c; };")]
        public static void IncrementInvertPointerFieldsByRef(
            [DNNE.C99Type("struct ptr_fields*")] PointerFields* result)
        {
            *(result->i) += 1;
            *(result->b) = !*(result->b);
            *(result->c) += (char)1;
        }

        [UnmanagedCallersOnly(EntryPoint = "blittablestructs_increment_invert_ptrfields_refreturn")]
        public static void IncrementInvertPointerFieldsRefReturn(
            [DNNE.C99Type("struct ptr_fields")] PointerFields input,
            [DNNE.C99Type("struct ptr_fields*")] PointerFields* result)
        {
            *(result->i) = *(input.i) + 1;
            *(result->b) = !(*input.b);
            *(result->c) = (char)(*(input.c) + 1);
        }
    }
}
