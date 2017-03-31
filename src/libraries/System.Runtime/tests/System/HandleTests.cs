// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime;
using System.Reflection;
using Xunit;


public static class HandleTests
{
    public static void  RuntimeFieldHandleTest()
    {
        Type t = typeof(Derived);
        FieldInfo f = t.GetField(nameof(Base.MyField));
        RuntimeFieldHandle h = f.FieldHandle;
        Assert.True(h.Value != null);
    }

    public static void  RuntimeMethodHandleTest()
    {
        MethodInfo minfo = typeof( Co6006LateBoundDelegate).GetMethod( "Method1" );
        RuntimeMethodHandle rmh = minfo.MethodHandle;
        Assert.True(rmh.Value != null);
        Assert.True(rmh.GetFunctionPointer() != null);
        Object [] args = new Object[] { null, rmh.GetFunctionPointer() };
        MyDelegate dg = (MyDelegate)Activator.CreateInstance( typeof( MyDelegate ), args );
        dg();
        Assert.True(Co6006LateBoundDelegate.iInvokeCount == 1);
    }

    public static void  RuntimeTypeHandleTest()
    {
        RuntimeTypeHandle r1 = typeof(int).TypeHandle;
        Assert.True(r1 != null);
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

    }

    private class Derived : Base
    {
    }

    delegate void MyDelegate();
    public class Co6006LateBoundDelegate
    {
        public static int iInvokeCount = 0;
        public static void Method1()
        {
            iInvokeCount++;
        }
    }

}
