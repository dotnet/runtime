// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace SharedTypes
{
    [NativeMarshalling(typeof(StringContainerMarshaller))]
    public struct StringContainer
    {
        public string str1;
        public string str2;
    }

    [ManagedToUnmanagedMarshallers(typeof(StringContainer),
        InMarshaller = typeof(In),
        RefMarshaller = typeof(Ref),
        OutMarshaller = typeof(Out))]
    public static class StringContainerMarshaller
    {
        public struct StringContainerNative
        {
            public IntPtr str1;
            public IntPtr str2;
        }

        public static class In
        {
            public static StringContainerNative ConvertToUnmanaged(StringContainer managed)
                => Ref.ConvertToUnmanaged(managed);

            public static void Free(StringContainerNative unmanaged)
                => Ref.Free(unmanaged);
        }

        public static class Ref
        {
            public static StringContainerNative ConvertToUnmanaged(StringContainer managed)
            {
                return new StringContainerNative
                {
                    str1 = Marshal.StringToCoTaskMemUTF8(managed.str1),
                    str2 = Marshal.StringToCoTaskMemUTF8(managed.str2)
                };
            }

            public static StringContainer ConvertToManaged(StringContainerNative unmanaged)
            {
                return new StringContainer
                {
                    str1 = Marshal.PtrToStringUTF8(unmanaged.str1),
                    str2 = Marshal.PtrToStringUTF8(unmanaged.str2)
                };
            }

            public static void Free(StringContainerNative unmanaged)
            {
                Marshal.FreeCoTaskMem(unmanaged.str1);
                Marshal.FreeCoTaskMem(unmanaged.str2);
            }
        }

        public static class Out
        {
            public static StringContainer ConvertToManaged(StringContainerNative unmanaged)
                => Ref.ConvertToManaged(unmanaged);

            public static void Free(StringContainerNative unmanaged)
                => Ref.Free(unmanaged);
        }
    }

    [ManagedToUnmanagedMarshallers(typeof(double))]
    public static unsafe class DoubleToBytesBigEndianMarshaller
    {
        public const int BufferSize = 8;

        public static byte* ConvertToUnmanaged(double managed, Span<byte> buffer)
        {
            BinaryPrimitives.WriteDoubleBigEndian(buffer, managed);
            return (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
        }
    }

    [ManagedToUnmanagedMarshallers(typeof(double))]
    public static class DoubleToLongMarshaller
    {
        public static long ConvertToUnmanaged(double managed)
        {
            return MemoryMarshal.Cast<double, long>(MemoryMarshal.CreateSpan(ref managed, 1))[0];
        }
    }

    [NativeMarshalling(typeof(BoolStructMarshaller))]
    public struct BoolStruct
    {
        public bool b1;
        public bool b2;
        public bool b3;
    }

    [ManagedToUnmanagedMarshallers(typeof(BoolStruct))]
    public static class BoolStructMarshaller
    {
        public struct BoolStructNative
        {
            public byte b1;
            public byte b2;
            public byte b3;
        }

        public static BoolStructNative ConvertToUnmanaged(BoolStruct managed)
        {
            return new BoolStructNative
            {
                b1 = (byte)(managed.b1 ? 1 : 0),
                b2 = (byte)(managed.b2 ? 1 : 0),
                b3 = (byte)(managed.b3 ? 1 : 0)
            };
        }

        public static BoolStruct ConvertToManaged(BoolStructNative unmanaged)
        {
            return new BoolStruct
            {
                b1 = unmanaged.b1 != 0,
                b2 = unmanaged.b2 != 0,
                b3 = unmanaged.b3 != 0
            };
        }
    }

    [NativeMarshalling(typeof(IntWrapperMarshaller))]
    public class IntWrapper
    {
        public int i;

        public ref int GetPinnableReference() => ref i;
    }

    [ManagedToUnmanagedMarshallers(typeof(IntWrapper))]
    public static unsafe class IntWrapperMarshaller
    {
        public static int* ConvertToUnmanaged(IntWrapper managed)
        {
            int* ret = (int*)Marshal.AllocCoTaskMem(sizeof(int));
            *ret = managed.i;
            return ret;
        }

        public static IntWrapper ConvertToManaged(int* unmanaged)
        {
            return new IntWrapper { i = *unmanaged };
        }

        public static void Free(int* unmanaged)
        {
            Marshal.FreeCoTaskMem((IntPtr)unmanaged);
        }
    }
}
