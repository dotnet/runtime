// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Remoting;
using Xunit;
using Xunit.Sdk;

namespace ComInterfaceGenerator.Tests;

internal unsafe partial class NativeExportsNE
{
    [LibraryImport(NativeExportsNE_Binary, EntryPoint = "get_com_object")]
    public static partial void* NewNativeObject();
}


public partial class GeneratedComInterfaceTests
{
    [Fact]
    public unsafe void CallNativeComObjectThroughGeneratedStub()
    {
        var ptr = NativeExportsNE.NewNativeObject(); // new_native_object
        var cw = new StrategyBasedComWrappers();
        var obj = cw.GetOrCreateObjectForComInstance((nint)ptr, CreateObjectFlags.None);

        var intObj = (IComInterface1)obj;
        Assert.Equal(0, intObj.GetData());
        intObj.SetData(2);
        Assert.Equal(2, intObj.GetData());
    }

    [Fact]
    public unsafe void DerivedInterfaceTypeProvidesBaseInterfaceUnmanagedToManagedMembers()
    {
        // Make sure that we have the correct derived and base types here.
        Assert.Contains(typeof(IComInterface1), typeof(IDerivedComInterface).GetInterfaces());

        IIUnknownDerivedDetails baseInterfaceDetails = StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetIUnknownDerivedDetails(typeof(IComInterface1).TypeHandle);
        IIUnknownDerivedDetails derivedInterfaceDetails = StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetIUnknownDerivedDetails(typeof(IDerivedComInterface).TypeHandle);

        var numBaseMethods = typeof(IComInterface1).GetMethods().Length;

        var numPointersToCompare = 3 + numBaseMethods;

        var expected = new ReadOnlySpan<nint>(baseInterfaceDetails.ManagedVirtualMethodTable, numPointersToCompare);
        var actual = new ReadOnlySpan<nint>(derivedInterfaceDetails.ManagedVirtualMethodTable, numPointersToCompare);

        Assert.True(expected.SequenceEqual(actual));
    }

    [Fact]
    public unsafe void CallBaseInterfaceMethod_EnsureQiCalledOnce()
    {
        var cw = new SingleQIComWrapper();
        var derivedImpl = new DerivedImpl();
        var nativeObj = cw.GetOrCreateComInterfaceForObject(derivedImpl, CreateComInterfaceFlags.None);
        var obj = cw.GetOrCreateObjectForComInstance(nativeObj, CreateObjectFlags.None);
        IDerivedComInterface iface = (IDerivedComInterface)obj;

        Assert.Equal(3, iface.GetData());
        // https://github.com/dotnet/runtime/issues/85795
        //iface.SetData(5);
        //Assert.Equal(5, iface.GetData());

        // https://github.com/dotnet/runtime/issues/85795
        //Assert.Equal("myName", iface.GetName());
        //iface.SetName("updated");
        //Assert.Equal("updated", iface.GetName());

        var qiCallCountObj = obj.GetType().GetRuntimeProperties().Where(p => p.Name == "IUnknownStrategy").Single().GetValue(obj);
        var countQi = (SingleQIComWrapper.CountQI)qiCallCountObj;
        Assert.Equal(1, countQi.QiCallCount);
    }

    [GeneratedComClass]
    partial class DerivedImpl : IDerivedComInterface
    {
        int data = 3;
        string myName = "myName";
        public int GetData() => data;
        [return: MarshalUsing(typeof(Utf16StringMarshaller))]
        public string GetName() => myName;
        public void SetData(int n) => data = n;
        public void SetName([MarshalUsing(typeof(Utf16StringMarshaller))] string name) => myName = name;
    }

    class SingleQIComWrapper : StrategyBasedComWrappers
    {
        public class CountQI : IIUnknownStrategy
        {
            public CountQI(IIUnknownStrategy iUnknown) => _iUnknownStrategy = iUnknown;
            private IIUnknownStrategy _iUnknownStrategy;
            public int QiCallCount = 0;
            public unsafe void* CreateInstancePointer(void* unknown) => _iUnknownStrategy.CreateInstancePointer(unknown);
            public unsafe int QueryInterface(void* instancePtr, in Guid iid, out void* ppObj)
            {
                QiCallCount++;
                return _iUnknownStrategy.QueryInterface(instancePtr, in iid, out ppObj);
            }
            public unsafe int Release(void* instancePtr) => _iUnknownStrategy.Release(instancePtr);
        }

        protected override IIUnknownStrategy GetOrCreateIUnknownStrategy()
            => new CountQI(base.GetOrCreateIUnknownStrategy());
    }
}
