// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Demo
{
    internal static partial class NativeExportsNE
    {
        public const string NativeExportsNE_Binary = "Microsoft.Interop.Tests." + nameof(NativeExportsNE);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "return_zero")]
        [return: MarshalUsing(typeof(ExceptionOnUnmarshal))]
        public static partial int GuaranteedUnmarshal([MarshalUsing(typeof(ListGuaranteedUnmarshal<,>), ConstantElementCount = 1)] out List<BoolStruct> ret);

        [ContiguousCollectionMarshaller]
        [CustomMarshaller(typeof(List<>), MarshalMode.ManagedToUnmanagedOut, typeof(ListGuaranteedUnmarshal<,>))]
        public static unsafe class ListGuaranteedUnmarshal<T, TUnmanagedElement> where TUnmanagedElement : unmanaged
        {
            public static bool AllocateContainerForManagedElementsFinallyCalled = false;
            public static List<T> AllocateContainerForManagedElementsFinally(byte* unmanaged, int length)
            {
                AllocateContainerForManagedElementsFinallyCalled = true;
                return null;
            }

            public static Span<T> GetManagedValuesDestination(List<T> managed) => default;
            public static ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(byte* nativeValue, int numElements) => default;
        }

        [CustomMarshaller(typeof(int), MarshalMode.ManagedToUnmanagedOut, typeof(ExceptionOnUnmarshal))]
        public static class ExceptionOnUnmarshal
        {
            public static int ConvertToManaged(int unmanaged) => throw new Exception();
        }

        [NativeMarshalling(typeof(BoolStructMarshaller))]
        public struct BoolStruct
        {
            public bool b1;
            public bool b2;
            public bool b3;
        }

        [CustomMarshaller(typeof(BoolStruct), MarshalMode.Default, typeof(BoolStructMarshaller))]
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
    }

    internal static class Program
    {
        public static void Main(string[] args)
        {
            int a = 12;
            int b = 13;
        }
    }
}
