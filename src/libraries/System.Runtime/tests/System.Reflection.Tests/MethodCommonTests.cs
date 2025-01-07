// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    }
}
