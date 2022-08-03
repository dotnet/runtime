// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SharedTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

using Xunit;

namespace LibraryImportGenerator.IntegrationTests
{
    partial class NativeExportsNE
    {
        public partial class Collections
        {
            public partial class Stateless
            {
                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "sum_int_array")]
                public static partial int Sum([MarshalUsing(typeof(ListMarshaller<,>))] List<int> values, int numValues);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "sum_int_array")]
                public static partial int SumWithBuffer([MarshalUsing(typeof(ListMarshallerWithBuffer<,>))] List<int> values, int numValues);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "double_values")]
                public static partial int DoubleValues([MarshalUsing(typeof(ListMarshallerWithPinning<,>))] List<BlittableIntWrapper> values, int length);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "sum_int_array_ref")]
                public static partial int SumInArray([MarshalUsing(typeof(ListMarshaller<,>))] in List<int> values, int numValues);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "duplicate_int_array")]
                public static partial void Duplicate([MarshalUsing(typeof(ListMarshaller<,>), CountElementName = "numValues")] ref List<int> values, int numValues);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "create_range_array")]
                [return: MarshalUsing(typeof(ListMarshaller<,>), CountElementName = "numValues")]
                public static partial List<int> CreateRange(int start, int end, out int numValues);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "create_range_array_out")]
                public static partial void CreateRange_Out(int start, int end, out int numValues, [MarshalUsing(typeof(ListMarshaller<,>), CountElementName = "numValues")] out List<int> res);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "get_long_bytes")]
                [return: MarshalUsing(typeof(ListMarshaller<,>), ConstantElementCount = sizeof(long))]
                public static partial List<byte> GetLongBytes(long l);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "and_bool_struct_array")]
                [return: MarshalAs(UnmanagedType.U1)]
                public static partial bool AndAllMembers([MarshalUsing(typeof(ListMarshaller<,>))] List<BoolStruct> pArray, int length);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "and_bool_struct_array_in")]
                [return: MarshalAs(UnmanagedType.U1)]
                public static partial bool AndAllMembersIn([MarshalUsing(typeof(ListMarshaller<,>))] in List<BoolStruct> pArray, int length);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "negate_bool_struct_array_ref")]
                public static partial void NegateBools(
                    [MarshalUsing(typeof(ListMarshaller<,>), CountElementName = "numValues")] ref List<BoolStruct> boolStruct,
                    int numValues);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "negate_bool_struct_array_out")]
                public static partial void NegateBools(
                    [MarshalUsing(typeof(ListMarshaller<,>))] List<BoolStruct> boolStruct,
                    int numValues,
                    [MarshalUsing(typeof(ListMarshaller<,>), CountElementName = "numValues")] out List<BoolStruct> pBoolStructOut);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "negate_bool_struct_array_return")]
                [return: MarshalUsing(typeof(ListMarshaller<,>), CountElementName = "numValues")]
                public static partial List<BoolStruct> NegateBools(
                    [MarshalUsing(typeof(ListMarshaller<,>))] List<BoolStruct> boolStruct,
                    int numValues);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "return_zero")]
                [return: MarshalUsing(typeof(ExceptionOnUnmarshal))]
                public static partial int GuaranteedUnmarshal([MarshalUsing(typeof(ListGuaranteedUnmarshal<,>), ConstantElementCount = 1)] out List<int> ret);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "return_zero")]
                [return: MarshalUsing(typeof(ExceptionOnUnmarshal))]
                public static partial int GuaranteedUnmarshal([MarshalUsing(typeof(ListGuaranteedUnmarshal<,>), ConstantElementCount = 1)] out List<BoolStruct> ret);

                [ContiguousCollectionMarshaller]
                [CustomMarshaller(typeof(List<>), MarshalMode.ManagedToUnmanagedOut, typeof(ListGuaranteedUnmarshal<,>))]
                public unsafe static class ListGuaranteedUnmarshal<T, TUnmanagedElement> where TUnmanagedElement : unmanaged
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
            }

            public partial class Stateful
            {
                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "sum_int_array")]
                public static partial int Sum([MarshalUsing(typeof(ListMarshallerStateful<,>))] List<int> values, int numValues);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "sum_int_array_ref")]
                public static partial int SumInArray([MarshalUsing(typeof(ListMarshallerStateful<,>))] in List<int> values, int numValues);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "duplicate_int_array")]
                public static partial void Duplicate([MarshalUsing(typeof(ListMarshallerStateful<,>), CountElementName = "numValues")] ref List<int> values, int numValues);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "create_range_array")]
                [return: MarshalUsing(typeof(ListMarshallerStateful<,>), CountElementName = "numValues")]
                public static partial List<int> CreateRange(int start, int end, out int numValues);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "create_range_array_out")]
                public static partial void CreateRange_Out(int start, int end, out int numValues, [MarshalUsing(typeof(ListMarshallerStateful<,>), CountElementName = "numValues")] out List<int> res);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "get_long_bytes")]
                [return: MarshalUsing(typeof(ListMarshallerStateful<,>), ConstantElementCount = sizeof(long))]
                public static partial List<byte> GetLongBytes(long l);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "and_bool_struct_array")]
                [return: MarshalAs(UnmanagedType.U1)]
                public static partial bool AndAllMembers([MarshalUsing(typeof(ListMarshallerStateful<,>))] List<BoolStruct> pArray, int length);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "and_bool_struct_array_in")]
                [return: MarshalAs(UnmanagedType.U1)]
                public static partial bool AndAllMembersIn([MarshalUsing(typeof(ListMarshallerStateful<,>))] in List<BoolStruct> pArray, int length);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "negate_bool_struct_array_ref")]
                public static partial void NegateBools(
                    [MarshalUsing(typeof(ListMarshallerStateful<,>), CountElementName = "numValues")] ref List<BoolStruct> boolStruct,
                    int numValues);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "negate_bool_struct_array_out")]
                public static partial void NegateBools(
                    [MarshalUsing(typeof(ListMarshallerStateful<,>))] List<BoolStruct> boolStruct,
                    int numValues,
                    [MarshalUsing(typeof(ListMarshallerStateful<,>), CountElementName = "numValues")] out List<BoolStruct> pBoolStructOut);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "negate_bool_struct_array_return")]
                [return: MarshalUsing(typeof(ListMarshallerStateful<,>), CountElementName = "numValues")]
                public static partial List<BoolStruct> NegateBools(
                    [MarshalUsing(typeof(ListMarshallerStateful<,>))] List<BoolStruct> boolStruct,
                    int numValues);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "return_zero")]
                [return: MarshalUsing(typeof(ExceptionOnUnmarshal))]
                public static partial int GuaranteedUnmarshal([MarshalUsing(typeof(ListGuaranteedUnmarshal<,>), ConstantElementCount = 1)] out List<int> ret);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = "return_zero")]
                [return: MarshalUsing(typeof(ExceptionOnUnmarshal))]
                public static partial int GuaranteedUnmarshal([MarshalUsing(typeof(ListGuaranteedUnmarshal<,>), ConstantElementCount = 1)] out List<BoolStruct> ret);

                [ContiguousCollectionMarshaller]
                [CustomMarshaller(typeof(List<>), MarshalMode.ManagedToUnmanagedOut, typeof(ListGuaranteedUnmarshal<,>.Marshaller))]
                public unsafe static class ListGuaranteedUnmarshal<T, TUnmanagedElement> where TUnmanagedElement : unmanaged
                {
                    public struct Marshaller
                    {
                        public static bool ToManagedFinallyCalled = false;
                        public List<T> ToManagedFinally()
                        {
                            ToManagedFinallyCalled = true;
                            return null;
                        }

                        public Span<T> GetManagedValuesDestination(int length) => default;
                        public ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(int length) => default;
                        public void FromUnmanaged(byte* value) { }
                        public void Free() {}
                    }
                }
            }
        }
    }

    public class CollectionTests
    {
        [Fact]
        public void BlittableElementColllection_ByValue()
        {
            var list = new List<int> { 1, 5, 79, 165, 32, 3 };
            Assert.Equal(list.Sum(), NativeExportsNE.Collections.Stateless.Sum(list, list.Count));
            Assert.Equal(list.Sum(), NativeExportsNE.Collections.Stateless.SumWithBuffer(list, list.Count));
            Assert.Equal(list.Sum(), NativeExportsNE.Collections.Stateful.Sum(list, list.Count));
        }

        [Fact]
        public void BlittableElementColllection_WithPinning()
        {
            var data = new List<int> { 1, 5, 79, 165, 32, 3 };
            var list = data.Select(i => new BlittableIntWrapper { i = i }).ToList();
            NativeExportsNE.Collections.Stateless.DoubleValues(list, list.Count);
            Assert.Equal(data.Select(i => i * 2), list.Select(wrapper => wrapper.i));
        }

        [Fact]
        public void BlittableElementColllection_ByValue_Null()
        {
            Assert.Equal(-1, NativeExportsNE.Collections.Stateless.Sum(null, 0));
            Assert.Equal(-1, NativeExportsNE.Collections.Stateful.Sum(null, 0));
        }

        [Fact]
        public void BlittableElementColllection_In()
        {
            var list = new List<int> { 1, 5, 79, 165, 32, 3 };
            Assert.Equal(list.Sum(), NativeExportsNE.Collections.Stateless.SumInArray(list, list.Count));
            Assert.Equal(list.Sum(), NativeExportsNE.Collections.Stateful.SumInArray(list, list.Count));
        }

        [Fact]
        public void BlittableElementCollection_Ref()
        {
            var original = new List<int> { 1, 5, 79, 165, 32, 3 };

            {
                List<int> list = original;
                NativeExportsNE.Collections.Stateless.Duplicate(ref list, list.Count);
                Assert.Equal((IEnumerable<int>)original, list);
            }
            {
                List<int> list = original;
                NativeExportsNE.Collections.Stateful.Duplicate(ref list, list.Count);
                Assert.Equal((IEnumerable<int>)original, list);
            }
        }

        [Fact]
        public void BlittableElementCollection_OutReturn()
        {
            int start = 5;
            int end = 20;
            IEnumerable<int> expected = Enumerable.Range(start, end - start);

            Assert.Equal(expected, NativeExportsNE.Collections.Stateless.CreateRange(start, end, out _));
            Assert.Equal(expected, NativeExportsNE.Collections.Stateful.CreateRange(start, end, out _));

            {
                List<int> res;
                NativeExportsNE.Collections.Stateless.CreateRange_Out(start, end, out _, out res);
                Assert.Equal(expected, res);
            }
            {
                List<int> res;
                NativeExportsNE.Collections.Stateful.CreateRange_Out(start, end, out _, out res);
                Assert.Equal(expected, res);
            }
        }

        [Fact]
        public void BlittableElementCollection_OutReturn_Null()
        {
            Assert.Null(NativeExportsNE.Collections.Stateless.CreateRange(1, 0, out _));
            Assert.Null(NativeExportsNE.Collections.Stateful.CreateRange(1, 0, out _));

            {
                List<int> res;
                NativeExportsNE.Collections.Stateless.CreateRange_Out(1, 0, out _, out res);
                Assert.Null(res);
            }
            {
                List<int> res;
                NativeExportsNE.Collections.Stateful.CreateRange_Out(1, 0, out _, out res);
                Assert.Null(res);
            }
        }

        [Fact]
        public void BlittableElementCollection_GuaranteedUnmarshal()
        {
            NativeExportsNE.Collections.Stateless.ListGuaranteedUnmarshal<int, int>.AllocateContainerForManagedElementsFinallyCalled = false;
            Assert.Throws<Exception>(() => NativeExportsNE.Collections.Stateless.GuaranteedUnmarshal(out List<int> _));
            Assert.True(NativeExportsNE.Collections.Stateless.ListGuaranteedUnmarshal<int, int>.AllocateContainerForManagedElementsFinallyCalled);

            NativeExportsNE.Collections.Stateful.ListGuaranteedUnmarshal<int, int>.Marshaller.ToManagedFinallyCalled = false;
            Assert.Throws<Exception>(() => NativeExportsNE.Collections.Stateful.GuaranteedUnmarshal(out List<int> _));
            Assert.True(NativeExportsNE.Collections.Stateful.ListGuaranteedUnmarshal<int, int>.Marshaller.ToManagedFinallyCalled);
        }

        [Fact]
        public void ConstantSizeCollection()
        {
            var longVal = 0x12345678ABCDEF10L;

            Assert.Equal(longVal, MemoryMarshal.Read<long>(CollectionsMarshal.AsSpan(NativeExportsNE.Collections.Stateless.GetLongBytes(longVal))));
            Assert.Equal(longVal, MemoryMarshal.Read<long>(CollectionsMarshal.AsSpan(NativeExportsNE.Collections.Stateful.GetLongBytes(longVal))));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void NonBlittableElementCollection_ByValue(bool result)
        {
            List<BoolStruct> list = GetBoolStructsToAnd(result);
            Assert.Equal(result, NativeExportsNE.Collections.Stateless.AndAllMembers(list, list.Count));
            Assert.Equal(result, NativeExportsNE.Collections.Stateful.AndAllMembers(list, list.Count));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void NonBlittableElementCollection_In(bool result)
        {
            List<BoolStruct> list = GetBoolStructsToAnd(result);
            Assert.Equal(result, NativeExportsNE.Collections.Stateless.AndAllMembersIn(list, list.Count));
            Assert.Equal(result, NativeExportsNE.Collections.Stateful.AndAllMembersIn(list, list.Count));
        }

        [Fact]
        public void NonBlittableElementCollection_Ref()
        {
            List<BoolStruct> original = GetBoolStructsToNegate();
            List<BoolStruct> expected = GetNegatedBoolStructs(original);

            {
                List<BoolStruct> list = original;
                NativeExportsNE.Collections.Stateless.NegateBools(ref list, list.Count);
                Assert.Equal(expected, list);
            }
            {
                List<BoolStruct> list = original;
                NativeExportsNE.Collections.Stateful.NegateBools(ref list, list.Count);
                Assert.Equal(expected, list);
            }
        }

        [Fact]
        public void NonBlittableElementCollection_Out()
        {
            List<BoolStruct> list = GetBoolStructsToNegate();
            List<BoolStruct> expected = GetNegatedBoolStructs(list);

            {
                List<BoolStruct> result;
                NativeExportsNE.Collections.Stateless.NegateBools(list, list.Count, out result);
                Assert.Equal(expected, result);
            }
            {
                List<BoolStruct> result;
                NativeExportsNE.Collections.Stateful.NegateBools(list, list.Count, out result);
                Assert.Equal(expected, result);
            }
        }

        [Fact]
        public void NonBlittableElementCollection_Return()
        {
            List<BoolStruct> list = GetBoolStructsToNegate();
            List<BoolStruct> expected = GetNegatedBoolStructs(list);

            {
                List<BoolStruct> result = NativeExportsNE.Collections.Stateless.NegateBools(list, list.Count);
                Assert.Equal(expected, result);
            }
            {
                List<BoolStruct> result = NativeExportsNE.Collections.Stateful.NegateBools(list, list.Count);
                Assert.Equal(expected, result);
            }
        }

        [Fact]
        public void NonBlittableElementCollection_GuaranteedUnmarshal()
        {
            NativeExportsNE.Collections.Stateless.ListGuaranteedUnmarshal<BoolStruct, BoolStructMarshaller.BoolStructNative>.AllocateContainerForManagedElementsFinallyCalled = false;
            Assert.Throws<Exception>(() => NativeExportsNE.Collections.Stateless.GuaranteedUnmarshal(out List<BoolStruct> _));
            Assert.True(NativeExportsNE.Collections.Stateless.ListGuaranteedUnmarshal<BoolStruct, BoolStructMarshaller.BoolStructNative>.AllocateContainerForManagedElementsFinallyCalled);

            NativeExportsNE.Collections.Stateful.ListGuaranteedUnmarshal<BoolStruct, BoolStructMarshaller.BoolStructNative>.Marshaller.ToManagedFinallyCalled = false;
            Assert.Throws<Exception>(() => NativeExportsNE.Collections.Stateful.GuaranteedUnmarshal(out List<BoolStruct> _));
            Assert.True(NativeExportsNE.Collections.Stateful.ListGuaranteedUnmarshal<BoolStruct, BoolStructMarshaller.BoolStructNative>.Marshaller.ToManagedFinallyCalled);
        }

        private static List<BoolStruct> GetBoolStructsToAnd(bool result) => new List<BoolStruct>
            {
                new BoolStruct
                {
                    b1 = true,
                    b2 = true,
                    b3 = true,
                },
                new BoolStruct
                {
                    b1 = true,
                    b2 = true,
                    b3 = true,
                },
                new BoolStruct
                {
                    b1 = true,
                    b2 = true,
                    b3 = result,
                },
            };

        private static List<BoolStruct> GetBoolStructsToNegate() => new List<BoolStruct>()
            {
                new BoolStruct
                {
                    b1 = true,
                    b2 = false,
                    b3 = true
                },
                new BoolStruct
                {
                    b1 = false,
                    b2 = true,
                    b3 = false
                },
                new BoolStruct
                {
                    b1 = true,
                    b2 = true,
                    b3 = true
                },
                new BoolStruct
                {
                    b1 = false,
                    b2 = false,
                    b3 = false
                }
            };

        private static List<BoolStruct> GetNegatedBoolStructs(List<BoolStruct> toNegate)
            => toNegate.Select(b => new BoolStruct() { b1 = !b.b1, b2 = !b.b2, b3 = !b.b3 }).ToList();
    }
}
