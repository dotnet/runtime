// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SharedTypes.ComInterfaces;
using Xunit;

namespace ComInterfaceGenerator.Tests
{
    unsafe partial class NativeExportsNE
    {
        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "set_com_object_data")]
        public static partial void SetComObjectData(void* obj, int data);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "get_com_object_data")]
        public static partial int GetComObjectData(void* obj);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "set_com_object_data")]
        public static partial void SetComObjectData(IGetAndSetInt obj, int data);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "get_com_object_data")]
        public static partial int GetComObjectData(IGetAndSetInt obj);
    }

    [GeneratedComClass]
    partial class ManagedObjectExposedToCom : IGetAndSetInt
    {
        public int Data { get; set; }
        int IGetAndSetInt.GetInt() => Data;
        void IGetAndSetInt.SetInt(int n) => Data = n;
    }

    [GeneratedComClass]
    partial class DerivedComObject : ManagedObjectExposedToCom
    {
    }

    [GeneratedComInterface]
    [Guid("781E56C2-A530-4A8F-90FE-01244426E0CC")]
    partial interface IActivationFactory
    {
        void GetActivationFactory(in Guid iid, [MarshalAs(UnmanagedType.Interface, IidParameterIndex = 0)] out object factory);
    }

    [GeneratedComClass]
    partial class ActivationFactory : IActivationFactory
    {
        public void GetActivationFactory(in Guid iid, out object factory) => factory = new ManagedObjectExposedToCom();
    }

    public unsafe class GeneratedComClassTests
    {
        private const int E_NOINTERFACE = unchecked((int)0x80004002);

        [Fact]
        public void ComInstanceProvidesInterfaceForDirectlyImplementedComInterface()
        {
            ManagedObjectExposedToCom obj = new();
            StrategyBasedComWrappers wrappers = new();
            nint ptr = wrappers.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.None);
            Assert.NotEqual(0, ptr);
            Assert.Equal(0, Marshal.QueryInterface(ptr, typeof(IGetAndSetInt).GUID, out nint iComInterface));
            Assert.NotEqual(0, iComInterface);
            Marshal.Release(iComInterface);
            Marshal.Release(ptr);
        }

        [Fact]
        public void ComInstanceProvidesInterfaceForIndirectlyImplementedComInterface()
        {
            DerivedComObject obj = new();
            StrategyBasedComWrappers wrappers = new();
            nint ptr = wrappers.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.None);
            Assert.NotEqual(0, ptr);
            Assert.Equal(0, Marshal.QueryInterface(ptr, typeof(IGetAndSetInt).GUID, out nint iComInterface));
            Assert.NotEqual(0, iComInterface);
            Marshal.Release(iComInterface);
            Marshal.Release(ptr);
        }

        [Fact]
        public void CallsToComInterfaceWithMarshallerWriteChangesToManagedObject()
        {
            ManagedObjectExposedToCom obj = new();
            obj.Data = 3;
            Assert.Equal(3, obj.Data);
            NativeExportsNE.SetComObjectData(obj, 42);
            Assert.Equal(42, obj.Data);
        }

        [Fact]
        public void CallsToComInterfaceWithMarshallerReadChangesFromManagedObject()
        {
            ManagedObjectExposedToCom obj = new();
            obj.Data = 3;
            Assert.Equal(3, obj.Data);
            obj.Data = 12;
            Assert.Equal(obj.Data, NativeExportsNE.GetComObjectData(obj));
        }

        [Fact]
        public void OutObjectWithIidParameterIndexUsesRequestedIidAndPropagatesQIFailure()
        {
            ActivationFactory factory = new();
            StrategyBasedComWrappers wrappers = new();
            nint unknown = wrappers.GetOrCreateComInterfaceForObject(factory, CreateComInterfaceFlags.None);
            Assert.NotEqual(0, unknown);

            try
            {
                Assert.Equal(0, Marshal.QueryInterface(unknown, typeof(IActivationFactory).GUID, out nint activationFactoryPtr));
                Assert.NotEqual(0, activationFactoryPtr);
                try
                {
                    void** vtable = *(void***)activationFactoryPtr;
                    Guid unsupportedIid = new("6B29FC40-CA47-1067-B31D-00DD010662DA");
                    nint result = 0;
                    int hr = ((delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int>)vtable[3])((void*)activationFactoryPtr, (Guid*)(&unsupportedIid), (void**)(&result));
                    Assert.Equal(E_NOINTERFACE, hr);
                    Assert.Equal(0, result);

                    Guid supportedIid = typeof(IGetAndSetInt).GUID;
                    result = 0;
                    hr = ((delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int>)vtable[3])((void*)activationFactoryPtr, (Guid*)(&supportedIid), (void**)(&result));
                    Assert.Equal(0, hr);
                    Assert.NotEqual(0, result);
                    Assert.Equal(0, Marshal.QueryInterface(result, supportedIid, out nint sameRequestedInterface));
                    Assert.Equal(result, sameRequestedInterface);
                    Marshal.Release(sameRequestedInterface);
                    Marshal.Release(result);
                }
                finally
                {
                    Marshal.Release(activationFactoryPtr);
                }
            }
            finally
            {
                Marshal.Release(unknown);
            }
        }
    }
}
