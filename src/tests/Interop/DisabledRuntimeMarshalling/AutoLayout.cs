// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;
using static DisabledRuntimeMarshallingNative;

namespace DisabledRuntimeMarshalling;

public unsafe class PInvokes_AutoLayout
{
    [Fact]
    public static void AutoLayoutStruct()
    {
        short s = 42;
        bool b = true;
        Assert.Throws<MarshalDirectiveException>(() => DisabledRuntimeMarshallingNative.CallWithAutoLayoutStruct(new AutoLayoutStruct()));
    }

    [Fact]
    public static void StructWithAutoLayoutField()
    {
        short s = 42;
        bool b = true;
        AssertThrowsMarshalDirectiveOrTypeLoad(() => DisabledRuntimeMarshallingNative.CallWithAutoLayoutStruct(new SequentialWithAutoLayoutField()));
    }

    [Fact]
    public static void StructWithNestedAutoLayoutField()
    {
        short s = 42;
        bool b = true;
        AssertThrowsMarshalDirectiveOrTypeLoad(() => DisabledRuntimeMarshallingNative.CallWithAutoLayoutStruct(new SequentialWithAutoLayoutNestedField()));
    }

    private static void AssertThrowsMarshalDirectiveOrTypeLoad(Action testCode)
    {
        try
        {
             testCode();
             return;
        }
        catch (Exception ex) when(ex is MarshalDirectiveException or TypeLoadException)
        {
            return;
        }
        catch (Exception ex)
        {
            Assert.False(true, $"Expected either a MarshalDirectiveException or a TypeLoadException, but received a '{ex.GetType().FullName}' exception: '{ex.ToString()}'");
        }
        Assert.False(true, $"Expected either a MarshalDirectiveException or a TypeLoadException, but received no exception.");
    }
}
