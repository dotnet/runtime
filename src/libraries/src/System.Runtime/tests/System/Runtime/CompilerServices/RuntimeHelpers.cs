// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public static class RuntimeHelpersTests
{
    [Fact]
    public static void TestGetHashCode()
    {
        // Int32 RuntimeHelpers.GetHashCode(Object)
        object obj1 = new object();
        int h1 = RuntimeHelpers.GetHashCode(obj1);
        int h2 = RuntimeHelpers.GetHashCode(obj1);

        Assert.Equal(h1, h2);

        object obj2 = new object();
        int h3 = RuntimeHelpers.GetHashCode(obj2);
        Assert.NotEqual(h1, h3); // Could potentially clash but very unlikely

        int i123 = 123;
        int h4 = RuntimeHelpers.GetHashCode(i123);
        Assert.NotEqual(i123.GetHashCode(), h4);

        int h5 = RuntimeHelpers.GetHashCode(null);
        Assert.Equal(h5, 0);
    }

    public struct TestStruct
    {
        public int i1;
        public int i2;
        public override bool Equals(object obj)
        {
            if (!(obj is TestStruct))
                return false;

            TestStruct that = (TestStruct)obj;

            return this.i1 == that.i1 && this.i2 == that.i2;
        }
        public override int GetHashCode()
        {
            return i1 ^ i2;
        }
    }

    [Fact]
    public static unsafe void TestGetObjectValue()
    {
        // Object RuntimeHelpers.GetObjectValue(Object)
        TestStruct t = new TestStruct() { i1 = 2, i2 = 4 };
        object tOV = RuntimeHelpers.GetObjectValue(t);
        Assert.Equal(t, (TestStruct)tOV);

        Object o = new object();
        object oOV = RuntimeHelpers.GetObjectValue(o);
        Assert.Equal(o, oOV);

        int i = 3;
        object iOV = RuntimeHelpers.GetObjectValue(i);
        Assert.Equal(i, (int)iOV);
    }

    [Fact]
    public static unsafe void TestOffsetToStringData()
    {
        // RuntimeHelpers.OffsetToStringData
        char[] expectedValues = new char[] { 'a', 'b', 'c', 'd', 'e', 'f' };
        string s = "abcdef";

        fixed (char* values = s) // Compiler will use OffsetToStringData with fixed statements
        {
            for (int i = 0; i < expectedValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], values[i]);
            }
        }
    }

    [Fact]
    public static void TestInitializeArray()
    {
        // Void RuntimeHelpers.InitializeArray(Array, RuntimeFieldHandle)
        char[] expected = new char[] { 'a', 'b', 'c' }; // Compiler will use RuntimeHelpers.InitializeArrary these
    }

    [Fact]
    public static void TestRunClassConstructor()
    {
        RuntimeTypeHandle t = typeof(HasCctor).TypeHandle;
        RuntimeHelpers.RunClassConstructor(t);
        Assert.Equal(HasCctorReceiver.S, "Hello");
        return;
    }

    internal class HasCctor
    {
        static HasCctor()
        {
            HasCctorReceiver.S = "Hello" + (Guid.NewGuid().ToString().Substring(String.Empty.Length, 0));  // Make sure the preinitialization optimization doesn't eat this.
        }
    }

    internal class HasCctorReceiver
    {
        public static String S;
    }
}
