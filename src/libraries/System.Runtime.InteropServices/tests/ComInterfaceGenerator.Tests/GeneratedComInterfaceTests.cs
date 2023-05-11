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
using Xunit;
using Xunit.Sdk;

namespace ComInterfaceGenerator.Tests;

internal unsafe partial class NativeExportsNE
{
    [LibraryImport(NativeExportsNE_Binary, EntryPoint = "get_com_object")]
    public static partial void* NewNativeObject();
}


public class GeneratedComInterfaceTests
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
}
