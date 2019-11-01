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

        Assert.True(h.Value != IntPtr.Zero);

        Assert.Equal(h.GetHashCode(), h.GetHashCode());
        Assert.Equal(default(RuntimeFieldHandle).GetHashCode(), default(RuntimeFieldHandle).GetHashCode());

        Assert.True(h.Equals(h));
        Assert.False(h.Equals(default(RuntimeFieldHandle)));
        Assert.False(default(RuntimeFieldHandle).Equals(h));
        Assert.True(default(RuntimeFieldHandle).Equals(default(RuntimeFieldHandle)));

        Assert.True(h.Equals((object)h));
        Assert.False(h.Equals(new object()));
        Assert.False(h.Equals(null));

        Assert.False(h == default(RuntimeFieldHandle));
        Assert.True(h != default(RuntimeFieldHandle));
    }

    [Fact]
    public static void  RuntimeMethodHandleTest()
    {
        MethodInfo mi = typeof(Base).GetMethod(nameof(Base.MyMethod));
        RuntimeMethodHandle h = mi.MethodHandle;
        Assert.Equal(mi, MethodBase.GetMethodFromHandle(h));

        Assert.True(h.Value != IntPtr.Zero);

        Assert.Equal(h.GetHashCode(), h.GetHashCode());
        Assert.Equal(default(RuntimeMethodHandle).GetHashCode(), default(RuntimeMethodHandle).GetHashCode());

        Assert.True(h.Equals(h));
        Assert.False(h.Equals(default(RuntimeMethodHandle)));
        Assert.False(default(RuntimeMethodHandle).Equals(h));
        Assert.True(default(RuntimeMethodHandle).Equals(default(RuntimeMethodHandle)));

        Assert.True(h.Equals((object)h));
        Assert.False(h.Equals(new object()));
        Assert.False(h.Equals(null));

        Assert.False(h == default(RuntimeMethodHandle));
        Assert.True(h != default(RuntimeMethodHandle));
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
        RuntimeTypeHandle h = typeof(int).TypeHandle;
        Assert.NotEqual(h, typeof(uint).TypeHandle);

        Assert.True(h.Value != IntPtr.Zero);

        Assert.Equal(h.GetHashCode(), h.GetHashCode());
        Assert.Equal(default(RuntimeTypeHandle).GetHashCode(), default(RuntimeTypeHandle).GetHashCode());

        Assert.True(h.Equals(h));
        Assert.False(h.Equals(default(RuntimeTypeHandle)));
        Assert.False(default(RuntimeTypeHandle).Equals(h));
        Assert.True(default(RuntimeTypeHandle).Equals(default(RuntimeTypeHandle)));

        Assert.True(h.Equals((object)h));
        Assert.False(h.Equals(typeof(int)));
        Assert.False(h.Equals(new object()));
        Assert.False(h.Equals(null));

        Assert.False(h == null);
        Assert.False(null == h);
        Assert.True(h != null);
        Assert.True(null != h);
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
