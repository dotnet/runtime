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

    public unsafe class GeneratedComClassTests
    {
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
    }
}
