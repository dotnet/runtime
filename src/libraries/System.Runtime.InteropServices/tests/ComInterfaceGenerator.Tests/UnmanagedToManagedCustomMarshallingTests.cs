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

                private static void** s_vtable = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(INativeObject), sizeof(void*) * 2);
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

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "exchange_native_object_data")]
            public static partial int ExchangeNativeObjectData(void* obj, ref int x);
        }
    }
    public class UnmanagedToManagedCustomMarshallingTests
    {
        [Fact]
        public unsafe void ValidateImplicitThisUnmanagedToManagedFunctionCallsSucceed()
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
                VTableGCHandlePair<NativeExportsNE.ImplicitThis.INativeObject>.Free(wrapper);
            }
        }

        sealed class ManagedObjectImplementation : NativeExportsNE.UnmanagedToManagedCustomMarshalling.INativeObject
        {
            private IntWrapper _data;

            public ManagedObjectImplementation(int value)
            {
                _data = new() { i = value };
            }

            public void ExchangeData(ref IntWrapper x) => x = Interlocked.Exchange(ref _data, x);
            public IntWrapper GetData() => _data;
            public void SetData(IntWrapper x) => _data = x;
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

            public static void Free(int unmanaged)
            {
                NumCallsToFree++;
            }
        }
    }
}
