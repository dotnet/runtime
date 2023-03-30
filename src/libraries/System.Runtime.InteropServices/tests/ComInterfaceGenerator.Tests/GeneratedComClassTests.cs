﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Xunit;

namespace ComInterfaceGenerator.Tests
{
    unsafe partial class NativeExportsNE
    {
        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "set_com_object_data")]
        public static partial void SetComObjectData(void* obj, int data);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "get_com_object_data")]
        public static partial int GetComObjectData(void* obj);
    }

    [GeneratedComClass]
    partial class ManagedObjectExposedToCom : IComInterface1
    {
        public int Data { get; set; }
        int IComInterface1.GetData() => Data;
        void IComInterface1.SetData(int n) => Data = n;
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
            var iid = typeof(IComInterface1).GUID;
            Assert.Equal(0, Marshal.QueryInterface(ptr, ref iid, out nint iComInterface));
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
            var iid = typeof(IComInterface1).GUID;
            Assert.Equal(0, Marshal.QueryInterface(ptr, ref iid, out nint iComInterface));
            Assert.NotEqual(0, iComInterface);
            Marshal.Release(iComInterface);
            Marshal.Release(ptr);
        }

        [Fact]
        public void CallsToComInterfaceWriteChangesToManagedObject()
        {
            ManagedObjectExposedToCom obj = new();
            StrategyBasedComWrappers wrappers = new();
            void* ptr = (void*)wrappers.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.None);
            Assert.NotEqual(0, (nint)ptr);
            obj.Data = 3;
            Assert.Equal(3, obj.Data);
            NativeExportsNE.SetComObjectData(ptr, 42);
            Assert.Equal(42, obj.Data);
            Marshal.Release((nint)ptr);
        }

        [Fact]
        public void CallsToComInterfaceReadChangesFromManagedObject()
        {
            ManagedObjectExposedToCom obj = new();
            StrategyBasedComWrappers wrappers = new();
            void* ptr = (void*)wrappers.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.None);
            Assert.NotEqual(0, (nint)ptr);
            obj.Data = 3;
            Assert.Equal(3, obj.Data);
            obj.Data = 12;
            Assert.Equal(obj.Data, NativeExportsNE.GetComObjectData(ptr));
            Marshal.Release((nint)ptr);
        }
    }
}
