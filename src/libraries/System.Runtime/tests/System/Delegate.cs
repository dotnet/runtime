// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DelegateTestInternal;
using Xunit;

namespace DelegateTestInternal
{
    public static class TestExtensionMethod
    {
        public static DelegateTests.TestStruct TestFunc(this DelegateTests.TestClass testparam)
        {
            return testparam.structField;
        }
    }
}

public static unsafe class DelegateTests
{
    public struct TestStruct
    {
        public object o1;
        public object o2;
    }

    public class TestClass
    {
        public TestStruct structField;
    }

    public delegate int SomeDelegate(int x);
    private static int SquareNumber(int x)
    {
        return x * x;
    }

    private static void EmptyFunc()
    {
        return;
    }

    public delegate TestStruct StructReturningDelegate();

    [Fact]
    public static void TestClosedStaticDelegate()
    {
        TestClass foo = new TestClass();
        foo.structField.o1 = new object();
        foo.structField.o2 = new object();
        StructReturningDelegate testDelegate = foo.TestFunc;
        TestStruct returnedStruct = testDelegate();
        Assert.True(RuntimeHelpers.ReferenceEquals(foo.structField.o1, returnedStruct.o1));
        Assert.True(RuntimeHelpers.ReferenceEquals(foo.structField.o2, returnedStruct.o2));
    }

    public class A { }
    public class B : A { }
    public delegate A DynamicInvokeDelegate(A nonRefParam1, B nonRefParam2, ref A refParam, out B outParam);

    public static A DynamicInvokeTestFunction(A nonRefParam1, B nonRefParam2, ref A refParam, out B outParam)
    {
        outParam = (B)refParam;
        refParam = nonRefParam2;
        return nonRefParam1;
    }

    [Fact]
    public static void TestDynamicInvoke()
    {
        A a1 = new A();
        A a2 = new A();
        B b1 = new B();
        B b2 = new B();

        DynamicInvokeDelegate testDelegate = DynamicInvokeTestFunction;

        // Check that the delegate behaves as expected
        A refParam = b2;
        B outParam = null;
        A returnValue = testDelegate(a1, b1, ref refParam, out outParam);
        Assert.True(RuntimeHelpers.ReferenceEquals(returnValue, a1));
        Assert.True(RuntimeHelpers.ReferenceEquals(refParam, b1));
        Assert.True(RuntimeHelpers.ReferenceEquals(outParam, b2));

        // Check dynamic invoke behavior
        object[] parameters = new object[4];
        parameters[0] = a1;
        parameters[1] = b1;
        parameters[2] = b2;
        parameters[3] = null;

        object retVal = testDelegate.DynamicInvoke(parameters);
        Assert.True(RuntimeHelpers.ReferenceEquals(retVal, a1));
        Assert.True(RuntimeHelpers.ReferenceEquals(parameters[2], b1));
        Assert.True(RuntimeHelpers.ReferenceEquals(parameters[3], b2));

        // Check invoke on a delegate that takes no parameters.
        Action emptyDelegate = EmptyFunc;
        emptyDelegate.DynamicInvoke(new object[] { });
        emptyDelegate.DynamicInvoke(null);
    }

    [Fact]
    public static void TestDynamicInvokeCastingDefaultValues()
    {
        {
            // Passing Type.Missing without providing default.
            Delegate d = new DFoo1(Foo1);
            Assert.Throws<ArgumentException>(() => d.DynamicInvoke(7, Type.Missing));
        }

        {
            // Passing Type.Missing with default.
            Delegate d = new DFoo1WithDefault(Foo1);
            d.DynamicInvoke(7, Type.Missing);
        }

        return;
    }

    [Fact]
    public static void TestDynamicInvokeCastingByRef()
    {
        {
            Delegate d = new DFoo2(Foo2);
            Object[] args = { 7 };
            d.DynamicInvoke(args);
            Assert.Equal(args[0], 8);
        }

        {
            Delegate d = new DFoo2(Foo2);
            Object[] args = { null };
            d.DynamicInvoke(args);
            Assert.Equal(args[0], 1);
        }

        // for "byref ValueType" arguments, the incoming is allowed to be null. The target will receive default(ValueType).
        {
            Delegate d = new DFoo3(Foo3);
            Object[] args = { null };
            d.DynamicInvoke(args);
            MyStruct s = (MyStruct)(args[0]);
            Assert.Equal(s.X, 7);
            Assert.Equal(s.Y, 8);
        }

        // For "byref ValueType" arguments, the type must match exactly.
        {
            Delegate d = new DFoo2(Foo2);
            Object[] args = { (uint)7 };
            Assert.Throws<ArgumentException>(() => d.DynamicInvoke(args));
        }

        {
            Delegate d = new DFoo2(Foo2);
            Object[] args = { E4.One };
            Assert.Throws<ArgumentException>(() => d.DynamicInvoke(args));
        }

        return;
    }

    [Fact]
    public static void TestDynamicInvokeCastingPrimitiveWiden()
    {
        {
            // For primitives, value-preserving widenings allowed.
            Delegate d = new DFoo1(Foo1);
            Object[] args = { 7, (short)7 };
            d.DynamicInvoke(args);
        }

        {
            // For primitives, conversion of enum to underlying integral prior to value-preserving widening allowed.
            Delegate d = new DFoo1(Foo1);
            Object[] args = { 7, E4.Seven };
            d.DynamicInvoke(args);
        }

        {
            // For primitives, conversion of enum to underlying integral prior to value-preserving widening allowed.
            Delegate d = new DFoo1(Foo1);
            Object[] args = { 7, E2.Seven };
            d.DynamicInvoke(args);
        }

        {
            // For primitives, conversion to enum after value-preserving widening allowed.
            Delegate d = new DFoo4(Foo4);
            Object[] args = { E4.Seven, 7 };
            d.DynamicInvoke(args);
        }

        {
            // For primitives, conversion to enum after value-preserving widening allowed.
            Delegate d = new DFoo4(Foo4);
            Object[] args = { E4.Seven, (short)7 };
            d.DynamicInvoke(args);
        }

        {
            // Size-preserving but non-value-preserving conversions NOT allowed.
            Delegate d = new DFoo1(Foo1);
            Object[] args = { 7, (uint)7 };
            Assert.Throws<ArgumentException>(() => d.DynamicInvoke(args));
        }

        {
            // Size-preserving but non-value-preserving conversions NOT allowed.
            Delegate d = new DFoo1(Foo1);
            Object[] args = { 7, U4.Seven };
            Assert.Throws<ArgumentException>(() => d.DynamicInvoke(args));
        }

        return;
    }

    [Fact]
    public static void TestDynamicInvokeCastingMisc()
    {
        {
            // DynamicInvoke allows "null" for any value type (converts to default(valuetype)).
            Delegate d = new DFoo5(Foo5);
            Object[] args = { null };
            d.DynamicInvoke(args);
        }

        {
            // DynamicInvoke allows conversion of T to Nullable<T>
            Delegate d = new DFoo6(Foo6);
            Object[] args = { 7 };
            d.DynamicInvoke(args);
        }

        {
            // DynamicInvoke allows conversion of T to Nullable<T> but T must match exactly.
            Delegate d = new DFoo6(Foo6);
            Object[] args = { (short)7 };
            Assert.Throws<ArgumentException>(() => d.DynamicInvoke(args));
        }

        {
            // DynamicInvoke allows conversion of T to Nullable<T> but T must match exactly.
            Delegate d = new DFoo6(Foo6);
            Object[] args = { E4.Seven };
            Assert.Throws<ArgumentException>(() => d.DynamicInvoke(args));
        }

        return;
    }

    private static void Foo1(int expected, int actual)
    {
        Assert.Equal(expected, actual);
    }

    private delegate void DFoo1(int expected, int actual);
    private delegate void DFoo1WithDefault(int expected, int actual = 7);

    private static void Foo2(ref int i)
    {
        i++;
    }

    private delegate void DFoo2(ref int i);

    private struct MyStruct
    {
        public int X;
        public int Y;
    }

    private static void Foo3(ref MyStruct s)
    {
        s.X += 7;
        s.Y += 8;
    }

    private delegate void DFoo3(ref MyStruct s);

    private static void Foo4(E4 expected, E4 actual)
    {
        Assert.Equal(expected, actual);
    }

    private delegate void DFoo4(E4 expected, E4 actual);

    private static void Foo5(MyStruct s)
    {
        Assert.Equal(s.X, 0);
        Assert.Equal(s.Y, 0);
    }

    private delegate void DFoo5(MyStruct s);

    private static void Foo6(Nullable<int> n)
    {
        Assert.True(n.HasValue);
        Assert.Equal(n.Value, 7);
    }

    private delegate void DFoo6(Nullable<int> s);

    private enum E2 : short
    {
        One = 1,
        Seven = 7,
    }

    private enum E4 : int
    {
        One = 1,
        Seven = 7,
    }

    private enum U4 : uint
    {
        One = 1,
        Seven = 7,
    }
}

