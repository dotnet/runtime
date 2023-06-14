// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharedTypes;
using Xunit;
using static ComInterfaceGenerator.Tests.UnmanagedToManagedCustomMarshallingTests;

namespace ComInterfaceGenerator.Tests
{
    internal unsafe partial class NativeExportsNE
    {
        internal partial class UnmanagedToManagedCustomMarshalling
        {
            [UnmanagedObjectUnwrapper<VTableGCHandlePair<INativeObject>>]
            internal partial interface INativeObject : IUnmanagedInterfaceType
            {
                private static void** s_vtable = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(INativeObject), sizeof(void*) * 6);
                static void* IUnmanagedInterfaceType.VirtualMethodTableManagedImplementation
                {
                    get
                    {
                        if (s_vtable[0] == null)
                        {
                            Native.PopulateUnmanagedVirtualMethodTable(s_vtable);
                        }
                        return s_vtable;
                    }
                }

                [VirtualMethodIndex(0, ImplicitThisParameter = true)]
                [return: MarshalUsing(typeof(IntWrapperMarshallerToIntWithFreeCounts))]
                IntWrapper GetData();
                [VirtualMethodIndex(1, ImplicitThisParameter = true)]
                void SetData([MarshalUsing(typeof(IntWrapperMarshallerToIntWithFreeCounts))] IntWrapper x);
                [VirtualMethodIndex(2, ImplicitThisParameter = true)]
                void ExchangeData([MarshalUsing(typeof(IntWrapperMarshallerToIntWithFreeCounts))] ref IntWrapper data);
                [VirtualMethodIndex(3, ImplicitThisParameter = true)]
                void SumAndSetData(
                    [MarshalUsing(CountElementName = nameof(numValues)), MarshalUsing(typeof(IntWrapperMarshallerToIntWithFreeCounts), ElementIndirectionDepth = 1)] IntWrapper[] values,
                    int numValues,
                    [MarshalUsing(typeof(IntWrapperMarshallerToIntWithFreeCounts))] out IntWrapper oldValue);
                [VirtualMethodIndex(4, ImplicitThisParameter = true)]
                void SumAndSetData(
                    [MarshalUsing(CountElementName = nameof(numValues)), MarshalUsing(typeof(IntWrapperMarshallerToIntWithFreeCounts), ElementIndirectionDepth = 1)] ref IntWrapper[] values,
                    int numValues,
                    [MarshalUsing(typeof(IntWrapperMarshallerToIntWithFreeCounts))] out IntWrapper oldValue);
                [VirtualMethodIndex(5, ImplicitThisParameter = true)]
                void MultiplyWithData(
                    [MarshalUsing(CountElementName = nameof(numValues)), MarshalUsing(typeof(IntWrapperMarshallerToIntWithFreeCounts), ElementIndirectionDepth = 1), In, Out] IntWrapper[] values123,
                    int numValues);
            }

            [UnmanagedObjectUnwrapper<VTableGCHandlePair<INativeObjectStateful>>]
            internal partial interface INativeObjectStateful : IUnmanagedInterfaceType
            {
                private static void** s_vtable = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(INativeObjectStateful), sizeof(void*) * 6);
                static void* IUnmanagedInterfaceType.VirtualMethodTableManagedImplementation
                {
                    get
                    {
                        if (s_vtable[0] == null)
                        {
                            Native.PopulateUnmanagedVirtualMethodTable(s_vtable);
                        }
                        return s_vtable;
                    }
                }

                [VirtualMethodIndex(3, ImplicitThisParameter = true, Direction = MarshalDirection.UnmanagedToManaged)]
                void SumAndSetData(
                    [MarshalUsing(typeof(StatefulUnmanagedToManagedCollectionMarshaller<,>), CountElementName = nameof(numValues))]
                    [MarshalUsing(typeof(IntWrapperMarshallerToIntWithFreeCounts), ElementIndirectionDepth = 1)] IntWrapper[] values,
                    int numValues,
                    [MarshalUsing(typeof(IntWrapperMarshallerToIntWithFreeCounts))] out IntWrapper oldValue);
                [VirtualMethodIndex(4, ImplicitThisParameter = true, Direction = MarshalDirection.UnmanagedToManaged)]
                void SumAndSetData(
                    [MarshalUsing(typeof(StatefulUnmanagedToManagedCollectionMarshaller<,>), CountElementName = nameof(numValues))]
                    [MarshalUsing(typeof(IntWrapperMarshallerToIntWithFreeCounts), ElementIndirectionDepth = 1)] ref IntWrapper[] values,
                    int numValues,
                    [MarshalUsing(typeof(IntWrapperMarshallerToIntWithFreeCounts))] out IntWrapper oldValue);

                [VirtualMethodIndex(5, ImplicitThisParameter = true, Direction = MarshalDirection.UnmanagedToManaged)]
                void MultiplyWithData(
                    [MarshalUsing(typeof(StatefulUnmanagedToManagedCollectionMarshaller<,>), CountElementName = nameof(numValues))]
                    [MarshalUsing(typeof(IntWrapperMarshallerToIntWithFreeCounts), ElementIndirectionDepth = 1), In, Out] IntWrapper[] values123,
                    int numValues);
            }

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "set_native_object_data")]
            public static partial void SetNativeObjectData(void* obj, int data);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "get_native_object_data")]
            public static partial int GetNativeObjectData(void* obj);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "exchange_native_object_data")]
            public static partial int ExchangeNativeObjectData(void* obj, ref int x);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "sum_and_set_native_object_data")]
            public static partial int SumAndSetNativeObjectData(void* obj, [MarshalUsing(CountElementName = nameof(numValues))] int[] arr, int numValues, out int oldValue);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "sum_and_set_native_object_data_with_ref")]
            public static partial int SumAndSetNativeObjectData(void* obj, [MarshalUsing(CountElementName = nameof(numValues))] ref int[] arr, int numValues, out int oldValue);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "multiply_with_native_object_data")]
            public static partial int MultiplyWithNativeObjectData(void* obj, [MarshalUsing(CountElementName = nameof(numValues))] int[] arr, int numValues);
        }
    }
    public class UnmanagedToManagedCustomMarshallingTests
    {
        [Fact]
        public unsafe void ValidateOnlyByRefFreed_Stateless()
        {
            const int startingValue = 13;
            const int newValue = 42;

            ManagedObjectImplementation impl = new ManagedObjectImplementation(startingValue);

            void* wrapper = VTableGCHandlePair<NativeExportsNE.UnmanagedToManagedCustomMarshalling.INativeObject>.Allocate(impl);

            try
            {
                int freeCalls = IntWrapperMarshallerToIntWithFreeCounts.NumCallsToFree;
                NativeExportsNE.UnmanagedToManagedCustomMarshalling.GetNativeObjectData(wrapper);

                Assert.Equal(freeCalls, IntWrapperMarshallerToIntWithFreeCounts.NumCallsToFree);

                NativeExportsNE.UnmanagedToManagedCustomMarshalling.SetNativeObjectData(wrapper, newValue);
                Assert.Equal(freeCalls, IntWrapperMarshallerToIntWithFreeCounts.NumCallsToFree);

                int finalValue = 10;

                NativeExportsNE.UnmanagedToManagedCustomMarshalling.ExchangeNativeObjectData(wrapper, ref finalValue);
                Assert.Equal(freeCalls + 1, IntWrapperMarshallerToIntWithFreeCounts.NumCallsToFree);
            }
            finally
            {
                VTableGCHandlePair<NativeExportsNE.UnmanagedToManagedCustomMarshalling.INativeObject>.Free(wrapper);
            }
        }

        [Fact]
        public unsafe void ValidateArrayElementsAndOutParameterNotFreed_Stateless()
        {
            const int startingValue = 13;

            ManagedObjectImplementation impl = new ManagedObjectImplementation(startingValue);

            void* wrapper = VTableGCHandlePair<NativeExportsNE.UnmanagedToManagedCustomMarshalling.INativeObject>.Allocate(impl);

            try
            {
                var values = new int[] { 1, 32, 63, 124, 255 };

                int freeCalls = IntWrapperMarshallerToIntWithFreeCounts.NumCallsToFree;

                NativeExportsNE.UnmanagedToManagedCustomMarshalling.SumAndSetNativeObjectData(wrapper, values, values.Length, out int _);

                Assert.Equal(freeCalls, IntWrapperMarshallerToIntWithFreeCounts.NumCallsToFree);
            }
            finally
            {
                VTableGCHandlePair<NativeExportsNE.UnmanagedToManagedCustomMarshalling.INativeObject>.Free(wrapper);
            }
        }

        [Fact]
        public unsafe void ValidateArrayElementsByRefFreed_Stateless()
        {
            const int startingValue = 13;

            ManagedObjectImplementation impl = new ManagedObjectImplementation(startingValue);

            void* wrapper = VTableGCHandlePair<NativeExportsNE.UnmanagedToManagedCustomMarshalling.INativeObject>.Allocate(impl);

            try
            {
                var values = new int[] { 1, 32, 63, 124, 255 };

                int freeCalls = IntWrapperMarshallerToIntWithFreeCounts.NumCallsToFree;

                NativeExportsNE.UnmanagedToManagedCustomMarshalling.SumAndSetNativeObjectData(wrapper, ref values, values.Length, out int _);

                Assert.Equal(freeCalls + values.Length, IntWrapperMarshallerToIntWithFreeCounts.NumCallsToFree);
            }
            finally
            {
                VTableGCHandlePair<NativeExportsNE.UnmanagedToManagedCustomMarshalling.INativeObject>.Free(wrapper);
            }
        }

        [Fact]
        public unsafe void ValidateArrayElementsByValueOutFreed_Stateless()
        {
            const int startingValue = 13;

            ManagedObjectImplementation impl = new ManagedObjectImplementation(startingValue);

            void* wrapper = VTableGCHandlePair<NativeExportsNE.UnmanagedToManagedCustomMarshalling.INativeObject>.Allocate(impl);

            try
            {
                var values = new int[] { 1, 32, 63, 124, 255 };
                var expected = values.Select(x => x * startingValue).ToArray();

                int elementFreeCalls = IntWrapperMarshallerToIntWithFreeCounts.NumCallsToFree;

                NativeExportsNE.UnmanagedToManagedCustomMarshalling.MultiplyWithNativeObjectData(wrapper, values, values.Length);

                Assert.Equal(expected, values);

                Assert.Equal(elementFreeCalls + values.Length, IntWrapperMarshallerToIntWithFreeCounts.NumCallsToFree);
            }
            finally
            {
                VTableGCHandlePair<NativeExportsNE.UnmanagedToManagedCustomMarshalling.INativeObject>.Free(wrapper);
            }
        }

        [Fact]
        public unsafe void ValidateArrayElementsAndOutParameterNotFreed_Stateful()
        {
            const int startingValue = 13;

            ManagedObjectImplementation impl = new ManagedObjectImplementation(startingValue);

            void* wrapper = VTableGCHandlePair<NativeExportsNE.UnmanagedToManagedCustomMarshalling.INativeObjectStateful>.Allocate(impl);

            try
            {
                var values = new int[] { 1, 32, 63, 124, 255 };

                int elementFreeCalls = IntWrapperMarshallerToIntWithFreeCounts.NumCallsToFree;
                int marshallerFreeCalls = StatefulUnmanagedToManagedCollectionMarshaller<IntWrapper, int>.In.NumCallsToFree;

                NativeExportsNE.UnmanagedToManagedCustomMarshalling.SumAndSetNativeObjectData(wrapper, values, values.Length, out int _);

                // We shouldn't free the elements, but we always free the stateful marshaller.
                Assert.Equal(elementFreeCalls, IntWrapperMarshallerToIntWithFreeCounts.NumCallsToFree);
                Assert.Equal(marshallerFreeCalls + 1, StatefulUnmanagedToManagedCollectionMarshaller<IntWrapper, int>.In.NumCallsToFree);
            }
            finally
            {
                VTableGCHandlePair<NativeExportsNE.UnmanagedToManagedCustomMarshalling.INativeObjectStateful>.Free(wrapper);
            }
        }

        [Fact]
        public unsafe void ValidateArrayElementsByRefFreed_Stateful()
        {
            const int startingValue = 13;

            ManagedObjectImplementation impl = new ManagedObjectImplementation(startingValue);

            void* wrapper = VTableGCHandlePair<NativeExportsNE.UnmanagedToManagedCustomMarshalling.INativeObjectStateful>.Allocate(impl);

            try
            {
                var values = new int[] { 1, 32, 63, 124, 255 };

                int elementFreeCalls = IntWrapperMarshallerToIntWithFreeCounts.NumCallsToFree;
                int marshallerFreeCalls = StatefulUnmanagedToManagedCollectionMarshaller<IntWrapper, int>.Ref.NumCallsToFree;

                NativeExportsNE.UnmanagedToManagedCustomMarshalling.SumAndSetNativeObjectData(wrapper, ref values, values.Length, out int _);

                Assert.Equal(elementFreeCalls + values.Length, IntWrapperMarshallerToIntWithFreeCounts.NumCallsToFree);
                Assert.Equal(marshallerFreeCalls + 1, StatefulUnmanagedToManagedCollectionMarshaller<IntWrapper, int>.Ref.NumCallsToFree);
            }
            finally
            {
                VTableGCHandlePair<NativeExportsNE.UnmanagedToManagedCustomMarshalling.INativeObjectStateful>.Free(wrapper);
            }
        }

        [Fact]
        public unsafe void ValidateArrayElementsByValueOutFreed_Stateful()
        {
            const int startingValue = 13;

            ManagedObjectImplementation impl = new ManagedObjectImplementation(startingValue);

            void* wrapper = VTableGCHandlePair<NativeExportsNE.UnmanagedToManagedCustomMarshalling.INativeObjectStateful>.Allocate(impl);

            try
            {
                var values = new int[] { 1, 32, 63, 124, 255 };
                var expected = values.Select(x => x * startingValue).ToArray();

                int elementFreeCalls = IntWrapperMarshallerToIntWithFreeCounts.NumCallsToFree;
                int marshallerFreeCalls = StatefulUnmanagedToManagedCollectionMarshaller<IntWrapper, int>.In.NumCallsToFree;

                NativeExportsNE.UnmanagedToManagedCustomMarshalling.MultiplyWithNativeObjectData(wrapper, values, values.Length);

                Assert.Equal(expected, values);

                Assert.Equal(elementFreeCalls + values.Length, IntWrapperMarshallerToIntWithFreeCounts.NumCallsToFree);
                Assert.Equal(marshallerFreeCalls + 1, StatefulUnmanagedToManagedCollectionMarshaller<IntWrapper, int>.In.NumCallsToFree);
            }
            finally
            {
                VTableGCHandlePair<NativeExportsNE.UnmanagedToManagedCustomMarshalling.INativeObjectStateful>.Free(wrapper);
            }
        }

        sealed unsafe class ManagedObjectImplementation : NativeExportsNE.UnmanagedToManagedCustomMarshalling.INativeObject, NativeExportsNE.UnmanagedToManagedCustomMarshalling.INativeObjectStateful
        {
            private IntWrapper _data;

            public ManagedObjectImplementation(int value)
            {
                _data = new() { i = value };
            }

            public void ExchangeData(ref IntWrapper x) => x = Interlocked.Exchange(ref _data, x);
            public IntWrapper GetData() => _data;
            public void MultiplyWithData(IntWrapper[] values, int numValues)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    values[i].i *= _data.i;
                }
            }
            public void SetData(IntWrapper x) => _data = x;
            public void SumAndSetData(ref IntWrapper[] values, int numValues, out IntWrapper oldValue) => SumAndSetData(values, numValues, out oldValue);
            public void SumAndSetData(IntWrapper[] values, int numValues, out IntWrapper oldValue)
            {
                int value = values.Sum(value => value.i);
                oldValue = _data;
                _data = new() { i = value };
            }

            static void* IUnmanagedInterfaceType.VirtualMethodTableManagedImplementation
            {
                get
                {
                    Assert.Fail("The VirtualMethodTableManagedImplementation property should not be called on implementing class types");
                    return null;
                }
            }
        }

        [CustomMarshaller(typeof(IntWrapper), MarshalMode.Default, typeof(IntWrapperMarshallerToIntWithFreeCounts))]
        public static unsafe class IntWrapperMarshallerToIntWithFreeCounts
        {
            [ThreadStatic]
            public static int NumCallsToFree = 0;

            public static int ConvertToUnmanaged(IntWrapper managed)
            {
                return managed.i;
            }

            public static IntWrapper ConvertToManaged(int unmanaged)
            {
                return new IntWrapper { i = unmanaged };
            }

            public static void Free(int _)
            {
                NumCallsToFree++;
            }
        }

        [CustomMarshaller(typeof(CustomMarshallerAttribute.GenericPlaceholder[]), MarshalMode.UnmanagedToManagedIn, typeof(StatefulUnmanagedToManagedCollectionMarshaller<,>.In))]
        [CustomMarshaller(typeof(CustomMarshallerAttribute.GenericPlaceholder[]), MarshalMode.UnmanagedToManagedRef, typeof(StatefulUnmanagedToManagedCollectionMarshaller<,>.Ref))]
        [ContiguousCollectionMarshaller]
        public unsafe static class StatefulUnmanagedToManagedCollectionMarshaller<TManaged, TUnmanaged>
            where TUnmanaged : unmanaged
        {
            public struct In
            {
                [ThreadStatic]
                public static int NumCallsToFree = 0;

                private TUnmanaged* _unmanaged;
                private TManaged[] _managed;

                public void FromUnmanaged(TUnmanaged* unmanaged)
                {
                    _unmanaged = unmanaged;
                }

                public Span<TManaged> GetManagedValuesDestination(int numElements)
                {
                    return _managed ??= new TManaged[numElements];
                }

                public ReadOnlySpan<TUnmanaged> GetUnmanagedValuesSource(int numElements)
                {
                    return new(_unmanaged, numElements);
                }

                public TManaged[] ToManaged()
                {
                    return _managed;
                }

                public void Free()
                {
                    NumCallsToFree++;
                }
            }

            public struct Ref
            {
                [ThreadStatic]
                public static int NumCallsToFree = 0;

                private TUnmanaged* _originalUnmanaged;
                private TUnmanaged* _unmanaged;
                private TManaged[] _managed;

                public void FromUnmanaged(TUnmanaged* unmanaged)
                {
                    _originalUnmanaged = unmanaged;
                }

                public Span<TManaged> GetManagedValuesDestination(int numElements)
                {
                    return _managed = new TManaged[numElements];
                }

                public ReadOnlySpan<TUnmanaged> GetUnmanagedValuesSource(int numElements)
                {
                    return new(_originalUnmanaged, numElements);
                }

                public TManaged[] ToManaged()
                {
                    return _managed;
                }

                public void Free()
                {
                    Marshal.FreeCoTaskMem((nint)_originalUnmanaged);
                    NumCallsToFree++;
                }

                public void FromManaged(TManaged[] managed)
                {
                    _managed = managed;
                }

                public TUnmanaged* ToUnmanaged()
                {
                    return _unmanaged = (TUnmanaged*)Marshal.AllocCoTaskMem(sizeof(TUnmanaged) * _managed.Length); 
                }

                public ReadOnlySpan<TManaged> GetManagedValuesSource()
                {
                    return _managed;
                }

                public Span<TUnmanaged> GetUnmanagedValuesDestination()
                {
                    return new(_unmanaged, _managed.Length);
                }
            }
        }
    }
}
