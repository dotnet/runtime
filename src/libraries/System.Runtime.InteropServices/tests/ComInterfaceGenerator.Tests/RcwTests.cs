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

[GeneratedComInterface]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("2c3f9903-b586-46b1-881b-adfce9af47b1")]
public partial interface IComInterface1
{
    int GetData();
    void SetData(int n);
}

internal sealed unsafe partial class MyGeneratedComWrappers : StrategyBasedComWrappers
{
    protected sealed override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count) => throw new UnreachableException("Not creating CCWs yet");
}

public static unsafe partial class Native
{
    [LibraryImport(NativeExportsNE.NativeExportsNE_Binary, EntryPoint = "get_com_object")]
    public static partial void* NewNativeObject();
}


public class RcwTests
{
    [Fact]
    public unsafe void CallRcwFromGeneratedComInterface()
    {
        var ptr = Native.NewNativeObject(); // new_native_object
        var cw = new MyGeneratedComWrappers();
        var obj = cw.GetOrCreateObjectForComInstance((nint)ptr, CreateObjectFlags.None);

        var intObj = (IComInterface1)obj;
        Assert.Equal(0, intObj.GetData());
        intObj.SetData(2);
        Assert.Equal(2, intObj.GetData());
    }
}
