// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SharedTypes.ComInterfaces;
using Xunit;

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

        var intObj = (IGetAndSetInt)obj;
        Assert.Equal(0, intObj.GetInt());
        intObj.SetInt(2);
        Assert.Equal(2, intObj.GetInt());
    }
}
