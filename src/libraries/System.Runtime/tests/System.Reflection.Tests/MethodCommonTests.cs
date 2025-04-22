// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Tests
{
    /// <summary>
    /// These tests are shared with MethodInfo.Invoke and MethodInvoker.Invoke by using
    /// the abstract Invoke(...) method below.
    /// </summary>
    public abstract class MethodCommonTests
    {
        public abstract object? Invoke(MethodInfo methodInfo, object? obj, object?[]? parameters);

        protected abstract bool SupportsMissing { get; }

        protected static MethodInfo GetMethod(Type type, string name)
        {
            return type.GetTypeInfo().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).First(method => method.Name.Equals(name));
        }

        [Fact]
        public void InvokeNullableRefs()
        {
            object?[] args;

            int? iNull = null;
            args = new object[] { iNull };
            Assert.True((bool)Invoke(GetMethod(nameof(NullableRefMethods.Null)), null, args));
            Assert.Null(args[0]);
            Assert.False(((int?)args[0]).HasValue);

            args = new object[] { iNull };
            Assert.True((bool)Invoke(GetMethod(nameof(NullableRefMethods.NullBoxed)), null, args));
            Assert.Null(args[0]);

            args = new object[] { iNull, 10 };
            Assert.True((bool)Invoke(GetMethod(nameof(NullableRefMethods.NullToValue)), null, args));
            Assert.IsType<int>(args[0]);
            Assert.Equal(10, (int)args[0]);

            iNull = 42;
            args = new object[] { iNull, 42 };
            Assert.True((bool)Invoke(GetMethod(nameof(NullableRefMethods.ValueToNull)), null, args));
            Assert.Null(args[0]);

            iNull = null;
            args = new object[] { iNull, 10 };
            Assert.True((bool)Invoke(GetMethod(nameof(NullableRefMethods.NullToValueBoxed)), null, args));
            Assert.IsType<int>(args[0]);
            Assert.Equal(10, (int)args[0]);

            static MethodInfo GetMethod(string name) => typeof(NullableRefMethods).GetMethod(
                name, BindingFlags.Public | BindingFlags.Static)!;
        }

        [Fact]
        public void InvokeBoxedNullableRefs()
        {
            object?[] args;

            object? iNull = null;
            args = new object[] { iNull };
            Assert.True((bool)Invoke(GetMethod(nameof(NullableRefMethods.Null)), null, args));
            Assert.Null(args[0]);

            args = new object[] { iNull };
            Assert.True((bool)Invoke(GetMethod(nameof(NullableRefMethods.NullBoxed)), null, args));
            Assert.Null(args[0]);

            args = new object[] { iNull, 10 };
            Assert.True((bool)Invoke(GetMethod(nameof(NullableRefMethods.NullToValue)), null, args));
            Assert.IsType<int>(args[0]);
            Assert.Equal(10, (int)args[0]);

            iNull = 42;
            args = new object[] { iNull, 42 };
            Assert.True((bool)Invoke(GetMethod(nameof(NullableRefMethods.ValueToNull)), null, args));
            Assert.Null(args[0]);

            iNull = null;
            args = new object[] { iNull, 10 };
            Assert.True((bool)Invoke(GetMethod(nameof(NullableRefMethods.NullToValueBoxed)), null, args));
            Assert.IsType<int>(args[0]);
            Assert.Equal(10, (int)args[0]);

            static MethodInfo GetMethod(string name) => typeof(NullableRefMethods).GetMethod(
                name, BindingFlags.Public | BindingFlags.Static)!;
        }

        [Fact]
        public void InvokeEnum()
        {
            // Enums only need to match by primitive type.
            Assert.True((bool)GetMethod(nameof(EnumMethods.PassColorsInt)).
                Invoke(null, new object[] { OtherColorsInt.Red }));

            // Widening allowed
            Assert.True((bool)GetMethod(nameof(EnumMethods.PassColorsInt)).
                Invoke(null, new object[] { ColorsShort.Red }));

            // Narrowing not allowed
            Assert.Throws<ArgumentException>(() => GetMethod(nameof(EnumMethods.PassColorsShort)).
                Invoke(null, new object[] { OtherColorsInt.Red }));

            static MethodInfo GetMethod(string name) => typeof(EnumMethods).GetMethod(
                name, BindingFlags.Public | BindingFlags.Static)!;
        }

        [Fact]
        public void InvokeNullableEnumParameterDefaultNo()
        {
            MethodInfo method = typeof(EnumMethods).GetMethod("NullableEnumDefaultNo", BindingFlags.Static | BindingFlags.NonPublic);

            Assert.Null(Invoke(method, null, new object?[] { default(object) }));
            Assert.Equal(YesNo.No, Invoke(method, null, new object?[] { YesNo.No }));
            Assert.Equal(YesNo.Yes, Invoke(method, null, new object?[] { YesNo.Yes }));

            if (SupportsMissing)
            {
                Assert.Equal(YesNo.No, Invoke(method, null, new object?[] { Type.Missing }));
            }
        }

        [Fact]
        public void InvokeNullableEnumParameterDefaultYes()
        {
            MethodInfo method = typeof(EnumMethods).GetMethod("NullableEnumDefaultYes", BindingFlags.Static | BindingFlags.NonPublic);

            Assert.Null(Invoke(method, null, new object?[] { default(object) }));
            Assert.Equal(YesNo.No, Invoke(method, null, new object?[] { YesNo.No }));
            Assert.Equal(YesNo.Yes, Invoke(method, null, new object?[] { YesNo.Yes }));

            if (SupportsMissing)
            {
                Assert.Equal(YesNo.Yes, Invoke(method, null, new object?[] { Type.Missing }));
            }
        }

        [Fact]
        public void InvokeNonNullableEnumParameterDefaultYes()
        {
            MethodInfo method = typeof(EnumMethods).GetMethod("NonNullableEnumDefaultYes", BindingFlags.Static | BindingFlags.NonPublic);

            Assert.Equal(YesNo.No, Invoke(method, null, new object[] { default(object) }));
            Assert.Equal(YesNo.No, Invoke(method, null, new object[] { YesNo.No }));
            Assert.Equal(YesNo.Yes, Invoke(method, null, new object[] { YesNo.Yes }));

            if (SupportsMissing)
            {
                Assert.Equal(YesNo.Yes, Invoke(method, null, new object[] { Type.Missing }));
            }
        }

        [Fact]
        public void InvokeNullableEnumParameterDefaultNull()
        {
            MethodInfo method = typeof(EnumMethods).GetMethod("NullableEnumDefaultNull", BindingFlags.Static | BindingFlags.NonPublic);

            Assert.Null(Invoke(method, null, new object?[] { default(object) }));
            Assert.Equal(YesNo.No, Invoke(method, null, new object?[] { YesNo.No }));
            Assert.Equal(YesNo.Yes, Invoke(method, null, new object?[] { YesNo.Yes }));

            if (SupportsMissing)
            {
                Assert.Null(Invoke(method, null, new object?[] { Type.Missing }));
            }
        }

        [Fact]
        public void ValueTypeMembers_WithOverrides()
        {
            ValueTypeWithOverrides obj = new() { Id = 1 };

            // ToString is overridden.
            Assert.Equal("Hello", (string)Invoke(GetMethod(typeof(ValueTypeWithOverrides), nameof(ValueTypeWithOverrides.ToString)),
                obj, null));

            // Ensure a normal method works.
            Assert.Equal(1, (int)Invoke(GetMethod(typeof(ValueTypeWithOverrides), nameof(ValueTypeWithOverrides.GetId)),
                obj, null));
        }

        [Fact]
        public void ValueTypeMembers_WithoutOverrides()
        {
            ValueTypeWithoutOverrides obj = new() { Id = 1 };

            // ToString is not overridden.
            Assert.Equal(typeof(ValueTypeWithoutOverrides).ToString(), (string) Invoke(GetMethod(typeof(ValueTypeWithoutOverrides), nameof(ValueTypeWithoutOverrides.ToString)),
                obj, null));

            // Ensure a normal method works.
            Assert.Equal(1, (int)Invoke(GetMethod(typeof(ValueTypeWithoutOverrides), nameof(ValueTypeWithoutOverrides.GetId)),
                obj, null));
        }

        [Fact]
        public void NullableOfTMembers()
        {
            // Ensure calling a method on Nullable<T> works.
            MethodInfo mi = GetMethod(typeof(int?), nameof(Nullable<int>.GetValueOrDefault));
            Assert.Equal(42, Invoke(mi, 42, null));
        }

        [Fact]
        public void CopyBackWithByRefArgs()
        {
            object i = 42;
            object[] args = new object[] { i };
            Invoke(GetMethod(typeof(CopyBackMethods), nameof(CopyBackMethods.IncrementByRef)), null, args);
            Assert.Equal(43, (int)args[0]);
            Assert.NotSame(i, args[0]); // A copy should be made; a boxed instance should never be directly updated.

            i = 42;
            args = new object[] { i };
            Invoke(GetMethod(typeof(CopyBackMethods), nameof(CopyBackMethods.IncrementByNullableRef)), null, args);
            Assert.Equal(43, (int)args[0]);
            Assert.NotSame(i, args[0]);

            object o = null;
            args = new object[] { o };
            Invoke(GetMethod(typeof(CopyBackMethods), nameof(CopyBackMethods.SetToNonNullByRef)), null, args);
            Assert.NotNull(args[0]);

            o = new object();
            args = new object[] { o };
            Invoke(GetMethod(typeof(CopyBackMethods), nameof(CopyBackMethods.SetToNullByRef)), null, args);
            Assert.Null(args[0]);
        }

        [Fact]
        public unsafe void TestFunctionPointerDirect()
        {
            // Sanity checks for direct invocation.
            void* fn = FunctionPointerMethods.GetFunctionPointer();
            Assert.True(FunctionPointerMethods.GetFunctionPointer()(42));
            Assert.True(FunctionPointerMethods.CallFcnPtr_IntPtr((IntPtr)fn, 42));
            Assert.True(FunctionPointerMethods.CallFcnPtr_Void(fn, 42));
            Assert.False(FunctionPointerMethods.GetFunctionPointer()(41));
            Assert.False(FunctionPointerMethods.CallFcnPtr_IntPtr((IntPtr)fn, 41));
            Assert.False(FunctionPointerMethods.CallFcnPtr_Void(fn, 41));
        }

        [Fact]
        public unsafe void TestFunctionPointerAsIntPtrArgType()
        {
            void* fn = FunctionPointerMethods.GetFunctionPointer();

            MethodInfo m;

            m = GetMethod(typeof(FunctionPointerMethods), nameof(FunctionPointerMethods.CallFcnPtr_IntPtr));
            Assert.True((bool)Invoke(m, null, new object[] { (IntPtr)fn, 42 }));
            Assert.False((bool)Invoke(m, null, new object[] { (IntPtr)fn, 41 }));

            m = GetMethod(typeof(FunctionPointerMethods), nameof(FunctionPointerMethods.CallFcnPtr_Void));
            Assert.True((bool)Invoke(m, null, new object[] { (IntPtr)fn, 42 }));
            Assert.False((bool)Invoke(m, null, new object[] { (IntPtr)fn, 41 }));
        }

        [Fact]
        public unsafe void TestFunctionPointerAsUIntPtrArgType()
        {
            void* fn = FunctionPointerMethods.GetFunctionPointer();

            MethodInfo m;

            m = GetMethod(typeof(FunctionPointerMethods), nameof(FunctionPointerMethods.CallFcnPtr_UIntPtr));
            Assert.True((bool)Invoke(m, null, new object[] { (UIntPtr)fn, 42 }));
            Assert.False((bool)Invoke(m, null, new object[] { (UIntPtr)fn, 41 }));

            m = GetMethod(typeof(FunctionPointerMethods), nameof(FunctionPointerMethods.CallFcnPtr_Void));
            Assert.True((bool)Invoke(m, null, new object[] { (UIntPtr)fn, 42 }));
            Assert.False((bool)Invoke(m, null, new object[] { (UIntPtr)fn, 41 }));
        }

        [Fact]
        public unsafe void TestFunctionPointerAsArgType()
        {
            void* fn = FunctionPointerMethods.GetFunctionPointer();
            MethodInfo m = GetMethod(typeof(FunctionPointerMethods), nameof(FunctionPointerMethods.CallFcnPtr_FP));
            Assert.True((bool)Invoke(m, null, new object[] { (IntPtr)fn, 42 }));
            Assert.False((bool)Invoke(m, null, new object[] { (IntPtr)fn, 41 }));
        }

        [Fact]
        public unsafe void TestFunctionPointerAsReturnType()
        {
            MethodInfo m = GetMethod(typeof(FunctionPointerMethods), nameof(FunctionPointerMethods.GetFunctionPointer));
            object ret = Invoke(m, null, null);
            Assert.IsType<IntPtr>(ret);
            Assert.True((IntPtr)ret != 0);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50957", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoAOT))]
        public static void VerifyInnerException()
        {
            MethodInfo method = typeof(TestClassThatThrows).GetMethod(nameof(TestClassThatThrows.Throw))!;
            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, null));
            Assert.Contains("Here", ex.InnerException.ToString());
        }

        private class TestClassThatThrows
        {
            public static void Throw() => throw new Exception("Here");
        }

        public static IEnumerable<object[]> PropertyTestData()
        {
            yield return new object[] { "Bool", TestClass.BoolValue };
            yield return new object[] { "Byte", TestClass.ByteValue };
            yield return new object[] { "ByteEnum", TestClass.ByteEnumValue };
            yield return new object[] { "Char", TestClass.CharValue };
            yield return new object[] { "DateTimeOffset", TestClass.DateTimeOffsetValue };
            yield return new object[] { "DateTime", TestClass.DateTimeValue };
            yield return new object[] { "Decimal", TestClass.DecimalValue };
            yield return new object[] { "Double", TestClass.DoubleValue };
            yield return new object[] { "Guid", TestClass.GuidValue };
            yield return new object[] { "Int16", TestClass.Int16Value };
            yield return new object[] { "Int16Enum", TestClass.Int16EnumValue };
            yield return new object[] { "Int32", TestClass.Int32Value };
            yield return new object[] { "Int32Enum", TestClass.Int32EnumValue };
            yield return new object[] { "Int64", TestClass.Int64Value };
            yield return new object[] { "Int64Enum", TestClass.Int64EnumValue };
            yield return new object[] { "NInt", TestClass.NIntValue };
            yield return new object[] { "NUInt", TestClass.NUIntValue };
            yield return new object[] { "Object", TestClass.ObjectValue };
            yield return new object[] { "SByte", TestClass.SByteValue };
            yield return new object[] { "SByteEnum", TestClass.SByteEnumValue };
            yield return new object[] { "Single", TestClass.SingleValue };
            yield return new object[] { "UInt16", TestClass.UInt16Value };
            yield return new object[] { "UInt16Enum", TestClass.UInt16EnumValue };
            yield return new object[] { "UInt32", TestClass.UInt32Value };
            yield return new object[] { "UInt32Enum", TestClass.UInt32EnumValue };
            yield return new object[] { "UInt64", TestClass.UInt64Value };
            yield return new object[] { "UInt64Enum", TestClass.UInt64EnumValue };
        }

        [Theory]
        [MemberData(nameof(PropertyTestData))]
        public void TestProperties(string typeName, object value)
        {
            TestClass testClass = new();
            MethodInfo setter = typeof(TestClass).GetProperty(typeName).GetSetMethod();
            setter.Invoke(testClass, new object[] { value });
            MethodInfo getter = typeof(TestClass).GetProperty(typeName).GetGetMethod();
            object ret = getter.Invoke(testClass, null);
            Assert.Equal(value, ret);
        }

        [Fact]
        public void TestObject2Method()
        {
            TestClass testClass = new TestClass();
            MethodInfo method = typeof(TestClass).GetMethod(nameof(TestClass.Object2));
            method.Invoke(testClass, new object[] { new object(), new object() });
            Assert.Equal(nameof(TestClass.Object2), testClass.MethodCalled);
        }

        [Fact]
        public void TestObject3Method()
        {
            TestClass testClass = new TestClass();
            MethodInfo method = typeof(TestClass).GetMethod(nameof(TestClass.Object3));
            method.Invoke(testClass, new object[] { new object(), new object(), new object() });
            Assert.Equal(nameof(TestClass.Object3), testClass.MethodCalled);
        }

        [Fact]
        public void TestObject4Method()
        {
            TestClass testClass = new TestClass();
            MethodInfo method = typeof(TestClass).GetMethod(nameof(TestClass.Object4));
            method.Invoke(testClass, new object[] { new object(), new object(), new object(), new object() });
            Assert.Equal(nameof(TestClass.Object4), testClass.MethodCalled);
        }

        [Fact]
        public void TestObject5Method()
        {
            TestClass testClass = new TestClass();
            MethodInfo method = typeof(TestClass).GetMethod(nameof(TestClass.Object5));
            method.Invoke(testClass, new object[] { new object(), new object(), new object(), new object(), new object() });
            Assert.Equal(nameof(TestClass.Object5), testClass.MethodCalled);
        }

        [Fact]
        public void TestObject6Method()
        {
            TestClass testClass = new TestClass();
            MethodInfo method = typeof(TestClass).GetMethod(nameof(TestClass.Object6));
            method.Invoke(testClass, new object[] { new object(), new object(), new object(), new object(), new object(), new object() });
            Assert.Equal(nameof(TestClass.Object6), testClass.MethodCalled);
        }

        [Fact]
        public void TestVoidMethod()
        {
            TestClass testClass = new TestClass();
            MethodInfo method = typeof(TestClass).GetMethod(nameof(TestClass.Void));
            method.Invoke(testClass, null);
            Assert.Equal(nameof(TestClass.Void), testClass.MethodCalled);
        }

        [Fact]
        public void TestIEnumerable1Method()
        {
            TestClass testClass = new TestClass();
            MethodInfo method = typeof(TestClass).GetMethod(nameof(TestClass.IEnumerableOfT1));
            method.Invoke(testClass, new object[] { new List<string>() });
            Assert.Equal(nameof(TestClass.IEnumerableOfT1), testClass.MethodCalled);
        }

        [Fact]
        public void TestIEnumerable2Method()
        {
            TestClass testClass = new TestClass();
            MethodInfo method = typeof(TestClass).GetMethod(nameof(TestClass.IEnumerableOfT2));
            method.Invoke(testClass, new object[] { new List<string>(), new List<string>() });
            Assert.Equal(nameof(TestClass.IEnumerableOfT2), testClass.MethodCalled);
        }

        /// <summary>
        /// Covers signatures used by reflection invoke intrinsics including primitives, common data types and other common signatures.
        /// </summary>
        private class TestClass
        {
            public const bool BoolValue = true;
            public const byte ByteValue = Byte.MaxValue;
            public const ByteEnumType ByteEnumValue = (ByteEnumType)Byte.MaxValue;
            public const char CharValue = 'S';
            public static readonly DateTimeOffset DateTimeOffsetValue = DateTimeOffset.MaxValue;
            public static readonly DateTime DateTimeValue = DateTime.MaxValue;
            public const decimal DecimalValue = 42m;
            public const double DoubleValue = 42d;
            public static readonly Guid GuidValue = new Guid("18B2A161-48B6-4D6C-AF0E-E618C73C5777");
            public const short Int16Value = Int16.MaxValue;
            public const Int16EnumType Int16EnumValue = (Int16EnumType)Int16.MaxValue;
            public const int Int32Value =  Int32.MaxValue;
            public const Int32EnumType Int32EnumValue = (Int32EnumType)Int32.MaxValue;
            public const long Int64Value = Int64.MaxValue;
            public const Int64EnumType Int64EnumValue = (Int64EnumType)Int64.MaxValue;
            public const nint NIntValue = 42;
            public const nuint NUIntValue = 42;
            public static readonly object ObjectValue = new object();
            public const sbyte SByteValue = SByte.MaxValue;
            public const SByteEnumType SByteEnumValue = (SByteEnumType)SByte.MaxValue;
            public const float SingleValue = 42f;
            public const ushort UInt16Value = UInt16.MaxValue;
            public const UInt16EnumType UInt16EnumValue = (UInt16EnumType)UInt16.MaxValue;
            public const uint UInt32Value = UInt32.MaxValue;
            public const UInt32EnumType UInt32EnumValue = (UInt32EnumType)UInt32.MaxValue;
            public const ulong UInt64Value = UInt64.MaxValue;
            public const UInt64EnumType UInt64EnumValue = (UInt64EnumType)UInt64.MaxValue;

            public string MethodCalled;

            public bool Bool { get; set; }
            public byte Byte { get; set; }
            public ByteEnumType ByteEnum { get; set; }
            public char Char { get; set; }
            public DateTime DateTime { get; set; }
            public DateTimeOffset DateTimeOffset { get; set; }
            public decimal Decimal { get; set; }
            public double Double { get; set; }
            public float Single { get; set; }
            public Guid Guid { get; set; }
            public short Int16 { get; set; }
            public Int16EnumType Int16Enum { get; set; }
            public int Int32 { get; set; }
            public Int32EnumType Int32Enum { get; set; }
            public long Int64 { get; set; }
            public Int64EnumType Int64Enum { get; set; }
            public nint NInt { get; set; }
            public nuint NUInt { get; set; }
            public object Object { get; set; }
            public sbyte SByte { get; set; }
            public SByteEnumType SByteEnum { get; set; }
            public ushort UInt16 { get; set; }
            public UInt16EnumType UInt16Enum { get; set; }
            public uint UInt32 { get; set; }
            public UInt32EnumType UInt32Enum { get; set; }
            public ulong UInt64 { get; set; }
            public UInt64EnumType UInt64Enum { get; set; }

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

            public enum ByteEnumType : byte { }
            public enum SByteEnumType : sbyte { }
            public enum Int16EnumType : short { }
            public enum Int32EnumType : int { }
            public enum Int64EnumType : long { }
            public enum UInt16EnumType : ushort { }
            public enum UInt32EnumType : uint { }
            public enum UInt64EnumType : ulong { }
        }
    }
}
