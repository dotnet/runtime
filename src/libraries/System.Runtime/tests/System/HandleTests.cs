// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime;
using System.Reflection;
using Xunit;


public static class HandleTests
{
    [Fact]
    public static void  RuntimeFieldHandleTest()
    {
        Type t = typeof(Derived);
        FieldInfo f = t.GetField(nameof(Base.MyField));
        RuntimeFieldHandle h = f.FieldHandle;
        Assert.True(h.Value != null);
    }

    [Fact]
    public static void DefaultRuntimeFieldHandleHashCodeTest()
    {
        RuntimeFieldHandle rfh1 = new RuntimeFieldHandle();
        RuntimeFieldHandle rfh2 = new RuntimeFieldHandle();

        Assert.Equal(rfh1.GetHashCode(), rfh2.GetHashCode());
    }

    [Fact]
    public static void  RuntimeMethodHandleTest()
    {
        MethodInfo mi1 = typeof(Base).GetMethod(nameof(Base.MyMethod));
        MethodInfo mi2 = (MethodInfo)MethodBase.GetMethodFromHandle(mi1.MethodHandle);
        Assert.Equal(mi1, mi2);
    }

    [Fact]
    public static void DefaultRuntimeMethodHandleHashCodeTest()
    {
        RuntimeMethodHandle rmh1 = new RuntimeMethodHandle();
        RuntimeMethodHandle rmh2 = new RuntimeMethodHandle();

        Assert.Equal(rmh1.GetHashCode(), rmh2.GetHashCode());
    }

    [Fact]
    public static void  GenericMethodRuntimeMethodHandleTest()
    {
        // Make sure uninstantiated generic method has a valid handle
        MethodInfo mi1 = typeof(Base).GetMethod(nameof(Base.GenericMethod));
        MethodInfo mi2 = (MethodInfo)MethodBase.GetMethodFromHandle(mi1.MethodHandle);
        Assert.Equal(mi1, mi2);
    }

    [Fact]
    public static void  RuntimeTypeHandleTest()
    {
        RuntimeTypeHandle r1 = typeof(int).TypeHandle;
        RuntimeTypeHandle r2 = typeof(uint).TypeHandle;
        Assert.NotEqual(r1, r2);
    }
    
    private class Base
    {
        public event Action MyEvent { add { } remove { } }
#pragma warning disable 0649
        public int MyField;
#pragma warning restore 0649
        public int MyProperty { get; set; }

        public int MyProperty1 { get; private set; }
        public int MyProperty2 { private get; set; }

        public static void MyMethod() { }

        public static void GenericMethod<T>() { }
    }

    private class Derived : Base
    {
    }
}
