// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Tests
{
    public unsafe class FunctionPointerIntrinsicTests
    {
        private Type _invokeHelpersType;

        public FunctionPointerIntrinsicTests()
        {
            _invokeHelpersType = typeof(object).Assembly.GetType("System.Reflection.InvokeHelpers");
            Assert.NotNull(_invokeHelpersType);
        }

        private MethodInfo GetIntrinsic(string methodName)
        {
            MethodInfo methodInfo = _invokeHelpersType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(methodInfo);
            return methodInfo;
        }

        private IntPtr GetFunctionPointer(string methodName)
        {
            return typeof(TestClass).GetMethod(methodName).MethodHandle.GetFunctionPointer();
        }

        private IntPtr GetGetterFunctionPointer(string typeName)
        {
            return typeof(TestClass).GetProperty(typeName).GetGetMethod().MethodHandle.GetFunctionPointer();
        }

        private IntPtr GetSetterFunctionPointer(string typeName)
        {
            return typeof(TestClass).GetProperty(typeName).GetSetMethod().MethodHandle.GetFunctionPointer();
        }

        public static IEnumerable<object[]> PropertyTestData()
        {
            yield return new object[] { "Bool", TestClass.BoolValue };
            yield return new object[] { "Byte", TestClass.ByteValue };
            yield return new object[] { "Char", TestClass.CharValue };
            yield return new object[] { "DateTimeOffset", TestClass.DateTimeOffsetValue };
            yield return new object[] { "DateTime", TestClass.DateTimeValue };
            yield return new object[] { "Decimal", TestClass.DecimalValue };
            yield return new object[] { "Double", TestClass.DoubleValue };
            yield return new object[] { "Guid", TestClass.GuidValue };
            yield return new object[] { "Int16", TestClass.Int16Value };
            yield return new object[] { "Int32", TestClass.Int32Value };
            yield return new object[] { "Int64", TestClass.Int64Value };
            yield return new object[] { "NInt", TestClass.NIntValue };
            yield return new object[] { "NUInt", TestClass.NUIntValue };
            yield return new object[] { "Object", TestClass.ObjectValue };
            yield return new object[] { "SByte", TestClass.SByteValue };
            yield return new object[] { "Single", TestClass.SingleValue };
            yield return new object[] { "UInt16", TestClass.UInt16Value };
            yield return new object[] { "UInt32", TestClass.UInt32Value };
            yield return new object[] { "UInt64", TestClass.UInt64Value };
        }

        [Theory]
        [MemberData(nameof(PropertyTestData))]
        public void TestProperties(string typeName, object value)
        {
            TestClass testClass = new ();
            MethodInfo setterIntrinsic = GetIntrinsic($"InvokeObject{typeName}Void");
            setterIntrinsic.Invoke(null, new object[] { testClass, GetSetterFunctionPointer(typeName), value });
            MethodInfo getterIntrinsic = GetIntrinsic($"InvokeObject{typeName}");
            object ret = getterIntrinsic.Invoke(null, new object[] { testClass, GetGetterFunctionPointer(typeName) });
            Assert.Equal(value, ret);
        }

        [Fact]
        public void TestObject2Method()
        {
            TestClass testClass = new TestClass();
            IntPtr functionPointer = typeof(TestClass).GetMethod("Object2").MethodHandle.GetFunctionPointer();
            GetIntrinsic($"InvokeObjectObjectObjectVoid").Invoke(null, new object[]
                { testClass, functionPointer, new object(), new object() });

            Assert.Equal("Object2", testClass.MethodCalled);
        }

        [Fact]
        public void TestObject3Method()
        {
            TestClass testClass = new TestClass();
            IntPtr functionPointer = typeof(TestClass).GetMethod("Object3").MethodHandle.GetFunctionPointer();
            GetIntrinsic($"InvokeObjectObjectObjectObjectVoid").Invoke(null, new object[]
                { testClass, functionPointer, new object(), new object(), new object() });

            Assert.Equal("Object3", testClass.MethodCalled);
        }

        [Fact]
        public void TestObject4Method()
        {
            TestClass testClass = new TestClass();
            IntPtr functionPointer = typeof(TestClass).GetMethod("Object4").MethodHandle.GetFunctionPointer();
            GetIntrinsic($"InvokeObjectObjectObjectObjectObjectVoid").Invoke(null, new object[]
                { testClass, functionPointer, new object(), new object(), new object(), new object() });

            Assert.Equal("Object4", testClass.MethodCalled);
        }

        [Fact]
        public void TestObject5Method()
        {
            TestClass testClass = new TestClass();
            IntPtr functionPointer = typeof(TestClass).GetMethod("Object5").MethodHandle.GetFunctionPointer();
            GetIntrinsic($"InvokeObjectObjectObjectObjectObjectObjectVoid").Invoke(null, new object[]
                { testClass, functionPointer, new object(), new object(), new object(), new object(), new object() });

            Assert.Equal("Object5", testClass.MethodCalled);
        }

        [Fact]
        public void TestObject6Method()
        {
            TestClass testClass = new TestClass();
            IntPtr functionPointer = typeof(TestClass).GetMethod("Object6").MethodHandle.GetFunctionPointer();
            GetIntrinsic($"InvokeObjectObjectObjectObjectObjectObjectObjectVoid").Invoke(null, new object[]
                { testClass, functionPointer, new object(), new object(), new object(), new object(), new object(), new object() });

            Assert.Equal("Object6", testClass.MethodCalled);
        }

        [Fact]
        public void TestVoidMethod()
        {
            TestClass testClass = new();

            MethodInfo intrinsic = GetIntrinsic($"InvokeObjectVoid");
            intrinsic.Invoke(null, new object[] { testClass, GetFunctionPointer("Void") });
            Assert.Equal("Void", testClass.MethodCalled);
        }

        [Fact]
        public void TestIEnumerable1Method()
        {
            TestClass testClass = new();

            MethodInfo intrinsic = GetIntrinsic($"InvokeObjectIEnumerableOfObjectVoid");
            intrinsic.Invoke(null, new object[] { testClass, GetFunctionPointer("IEnumerableOfT1"), new List<string>(),  });
            Assert.Equal("IEnumerableOfT1", testClass.MethodCalled);
        }

        [Fact]
        public void TestIEnumerable2Method()
        {
            TestClass testClass = new();

            MethodInfo intrinsic = GetIntrinsic($"InvokeObjectIEnumerableOfObjectIEnumerableOfObjectVoid");
            intrinsic.Invoke(null, new object[] { testClass, GetFunctionPointer("IEnumerableOfT2"), new List<string>(), new List<string>() });
            Assert.Equal("IEnumerableOfT2", testClass.MethodCalled);
        }

        public class TestClass
        {
            public const bool BoolValue = true;
            public const byte ByteValue = 42;
            public const char CharValue = 'S';
            public static readonly DateTimeOffset DateTimeOffsetValue = DateTimeOffset.MaxValue;
            public static readonly DateTime DateTimeValue = DateTime.MaxValue;
            public const decimal DecimalValue = 42m;
            public const double DoubleValue = 42d;
            public static readonly Guid GuidValue = new Guid("18B2A161-48B6-4D6C-AF0E-E618C73C5777");
            public const short Int16Value = 42;
            public const int Int32Value = 42;
            public const long Int64Value = 42;
            public const nint NIntValue = 42;
            public const nuint NUIntValue = 42;
            public static readonly object ObjectValue = new object();
            public const sbyte SByteValue = 42;
            public const float SingleValue = 42f;
            public const ushort UInt16Value = 42;
            public const uint UInt32Value = 42;
            public const ulong UInt64Value = 42;

            public string MethodCalled;

            public bool Bool { get; set; }
            public byte Byte { get; set; }
            public char Char { get; set; }
            public DateTime DateTime { get; set; }
            public DateTimeOffset DateTimeOffset { get; set; }
            public decimal Decimal { get; set; }
            public double Double { get; set; }
            public float Single { get; set; }
            public Guid Guid { get; set; }
            public int Int32 { get; set; }
            public nint NInt { get; set; }
            public nuint NUInt { get; set; }
            public long Int64 { get; set; }
            public object Object { get; set; }
            public sbyte SByte { get; set; }
            public short Int16 { get; set; }
            public ushort UInt16 { get; set; }
            public uint UInt32 { get; set; }
            public ulong UInt64 { get; set; }

            public void Void() { MethodCalled = "Void"; }

            public void Object2(object arg1, object arg2)
            {
                Assert.NotNull(arg1);
                Assert.NotNull(arg2);
                MethodCalled = "Object2";
            }

            public void Object3(object arg1, object arg2, object arg3)
            {
                Assert.NotNull(arg1);
                Assert.NotNull(arg2);
                Assert.NotNull(arg3);
                MethodCalled = "Object3";
            }

            public void Object4(object arg1, object arg2, object arg3, object arg4)
            {
                Assert.NotNull(arg1);
                Assert.NotNull(arg2);
                Assert.NotNull(arg3);
                Assert.NotNull(arg4);
                MethodCalled = "Object4";
            }

            public void Object5(object arg1, object arg2, object arg3, object arg4, object arg5)
            {
                Assert.NotNull(arg1);
                Assert.NotNull(arg2);
                Assert.NotNull(arg3);
                Assert.NotNull(arg4);
                Assert.NotNull(arg5);
                MethodCalled = "Object5";
            }

            public void Object6(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
            {
                Assert.NotNull(arg1);
                Assert.NotNull(arg2);
                Assert.NotNull(arg3);
                Assert.NotNull(arg4);
                Assert.NotNull(arg5);
                Assert.NotNull(arg6);
                MethodCalled = "Object6";
            }

            public void IEnumerableOfT1(IEnumerable<object> arg1)
            {
                Assert.NotNull(arg1);
                MethodCalled = "IEnumerableOfT1";
            }

            public void IEnumerableOfT2(IEnumerable<object> arg1, IEnumerable<object> arg2)
            {
                Assert.NotNull(arg1);
                Assert.NotNull(arg2);
                MethodCalled = "IEnumerableOfT2";
            }
        }
    }
}
