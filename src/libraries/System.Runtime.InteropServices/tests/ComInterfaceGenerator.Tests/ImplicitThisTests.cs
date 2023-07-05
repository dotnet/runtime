// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using Xunit;

namespace ComInterfaceGenerator.Tests
{
    internal unsafe partial class NativeExportsNE
    {
        internal partial class ImplicitThis
        {
            [UnmanagedObjectUnwrapperAttribute<VTableGCHandlePair<INativeObject>>]
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
                int GetData();
                [VirtualMethodIndex(1, ImplicitThisParameter = true)]
                void SetData(int x);
                [VirtualMethodIndex(2, ImplicitThisParameter = true)]
                void ExchangeData(ref int x);
                [VirtualMethodIndex(3, ImplicitThisParameter = true)]
                void SumAndSetData(
                    [MarshalUsing(CountElementName = nameof(numValues))] int[] values,
                    int numValues,
                    out int oldValue);
                [VirtualMethodIndex(4, ImplicitThisParameter = true)]
                void SumAndSetData(
                    [MarshalUsing(CountElementName = nameof(numValues))] ref int[] values,
                    int numValues,
                    out int oldValue);
                [VirtualMethodIndex(5, ImplicitThisParameter = true)]
                void MultiplyWithData(
                    [MarshalUsing(CountElementName = nameof(numValues))] int[] values,
                    int numValues);
            }

            [NativeMarshalling(typeof(NativeObjectMarshaller))]
            public class NativeObject : INativeObject.Native, IUnmanagedVirtualMethodTableProvider, IDisposable
            {
                private readonly void* _pointer;

                public NativeObject(void* pointer)
                {
                    _pointer = pointer;
                }

                public VirtualMethodTableInfo GetVirtualMethodTableInfoForKey(Type type)
                {
                    Assert.Equal(typeof(INativeObject), type);
                    return new VirtualMethodTableInfo(_pointer, *(void***)_pointer);
                }

                public void Dispose()
                {
                    DeleteNativeObject(_pointer);
                }
            }

            [CustomMarshaller(typeof(NativeObject), MarshalMode.ManagedToUnmanagedOut, typeof(NativeObjectMarshaller))]
            static class NativeObjectMarshaller
            {
                public static NativeObject ConvertToManaged(void* value) => new NativeObject(value);
            }

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "new_native_object")]
            public static partial NativeObject NewNativeObject();

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "delete_native_object")]
            public static partial void DeleteNativeObject(void* obj);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "set_native_object_data")]
            public static partial void SetNativeObjectData(void* obj, int data);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "get_native_object_data")]
            public static partial int GetNativeObjectData(void* obj);
        }
    }

    public class ImplicitThisTests
    {
        [Fact]
        public void ValidateImplicitThisFunctionCallsSucceed()
        {
            const int value = 42;

            using NativeExportsNE.ImplicitThis.NativeObject obj = NativeExportsNE.ImplicitThis.NewNativeObject();

            NativeExportsNE.ImplicitThis.INativeObject nativeObjInterface = obj;

            nativeObjInterface.SetData(value);

            Assert.Equal(value, nativeObjInterface.GetData());
        }

        [Fact]
        public unsafe void ValidateImplicitThisUnmanagedToManagedFunctionCallsSucceed()
        {
            const int startingValue = 13;
            const int newValue = 42;

            ManagedObjectImplementation impl = new ManagedObjectImplementation(startingValue);

            void* wrapper = VTableGCHandlePair<NativeExportsNE.ImplicitThis.INativeObject>.Allocate(impl);

            try
            {
                Assert.Equal(startingValue, NativeExportsNE.ImplicitThis.GetNativeObjectData(wrapper));
                NativeExportsNE.ImplicitThis.SetNativeObjectData(wrapper, newValue);
                Assert.Equal(newValue, NativeExportsNE.ImplicitThis.GetNativeObjectData(wrapper));
                // Verify that we actually updated the managed instance.
                Assert.Equal(newValue, impl.GetData());
            }
            finally
            {
                VTableGCHandlePair<NativeExportsNE.ImplicitThis.INativeObject>.Free(wrapper);
            }
        }

        sealed class ManagedObjectImplementation : NativeExportsNE.ImplicitThis.INativeObject
        {
            private int _data;

            public ManagedObjectImplementation(int value)
            {
                _data = value;
            }

            public void ExchangeData(ref int x) => x = Interlocked.Exchange(ref _data, x);
            public int GetData() => _data;
            public void MultiplyWithData([MarshalUsing(CountElementName = "numValues")] int[] values, int numValues)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] *= _data;
                }
            }
            public void SetData(int x) => _data = x;
            public void SumAndSetData([MarshalUsing(CountElementName = "numValues")] int[] values, int numValues, out int oldValue)
            {
                int value = values.Sum();
                oldValue = _data;
                _data = value;
            }

            public void SumAndSetData([MarshalUsing(CountElementName = "numValues")] ref int[] values, int numValues, out int oldValue) => SumAndSetData(values, numValues, out oldValue);
        }
    }
}
