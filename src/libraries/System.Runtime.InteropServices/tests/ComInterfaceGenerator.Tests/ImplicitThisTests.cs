// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Xunit;

namespace ComInterfaceGenerator.Tests
{
    internal unsafe partial class NativeExportsNE
    {
        internal partial class ImplicitThis
        {
            public readonly record struct NoCasting;

            internal partial interface INativeObject : IUnmanagedInterfaceType<INativeObject, NoCasting>
            {
                static int IUnmanagedInterfaceType<INativeObject, NoCasting>.VirtualMethodTableLength => 2;

                static NoCasting IUnmanagedInterfaceType<INativeObject, NoCasting>.TypeKey => default;

                private static void** s_vtable = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(INativeObject), sizeof(void*) * IUnmanagedVirtualMethodTableProvider<NoCasting>.GetVirtualMethodTableLength<INativeObject>());
                static void* IUnmanagedInterfaceType<INativeObject, NoCasting>.VirtualMethodTableManagedImplementation
                {
                    get
                    {
                        if (s_vtable[0] == null)
                        {
                            Native.PopulateUnmanagedVirtualMethodTable(new Span<IntPtr>(s_vtable, IUnmanagedVirtualMethodTableProvider<NoCasting>.GetVirtualMethodTableLength<INativeObject>()));
                        }
                        return s_vtable;
                    }
                }

                static void* IUnmanagedInterfaceType<INativeObject, NoCasting>.GetUnmanagedWrapperForObject(INativeObject obj)
                {
                    return VTableGCHandlePair<INativeObject, NoCasting>.Allocate(obj);
                }

                static INativeObject IUnmanagedInterfaceType<INativeObject, NoCasting>.GetObjectForUnmanagedWrapper(void* ptr)
                {
                    return VTableGCHandlePair<INativeObject, NoCasting>.GetObject(ptr);
                }

                [VirtualMethodIndex(0, ImplicitThisParameter = true)]
                int GetData();
                [VirtualMethodIndex(1, ImplicitThisParameter = true)]
                void SetData(int x);
            }

            [NativeMarshalling(typeof(NativeObjectMarshaller))]
            public class NativeObject : INativeObject.Native, IUnmanagedVirtualMethodTableProvider<NoCasting>, IDisposable
            {
                private readonly void* _pointer;

                public NativeObject(void* pointer)
                {
                    _pointer = pointer;
                }

                public VirtualMethodTableInfo GetVirtualMethodTableInfoForKey(NoCasting typeKey) => new VirtualMethodTableInfo((IntPtr)_pointer, new ReadOnlySpan<IntPtr>(*(void**)_pointer, 2));

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

            void* wrapper = IUnmanagedVirtualMethodTableProvider<NativeExportsNE.ImplicitThis.NoCasting>.GetUnmanagedWrapperForObject<NativeExportsNE.ImplicitThis.INativeObject>(impl);

            Assert.Equal(startingValue, NativeExportsNE.ImplicitThis.GetNativeObjectData(wrapper));
            NativeExportsNE.ImplicitThis.SetNativeObjectData(wrapper, newValue);
            Assert.Equal(newValue, NativeExportsNE.ImplicitThis.GetNativeObjectData(wrapper));
            // Verify that we actually updated the managed instance.
            Assert.Equal(newValue, impl.GetData());

            VTableGCHandlePair<NativeExportsNE.ImplicitThis.INativeObject, NativeExportsNE.ImplicitThis.NoCasting>.Free(wrapper);
        }

        class ManagedObjectImplementation : NativeExportsNE.ImplicitThis.INativeObject
        {
            private int _data;

            public ManagedObjectImplementation(int value)
            {
                _data = value;
            }

            public int GetData() => _data;
            public void SetData(int x) => _data = x;
        }
    }
}
