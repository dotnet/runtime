// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Tests
{
    public unsafe class PointerHolder
    {
        public int* field;
        public char* Property { get; set; }
        public void Method(byte* ptr, int expected)
        {
            Assert.Equal(expected, unchecked((int)ptr));
        }

        public void MethodWithSystemPointer(Pointer ptr, int expected)
        {
            Assert.Equal(expected, unchecked((int)Pointer.Unbox(ptr)));
        }

        public bool* Return(int expected)
        {
            return unchecked((bool*)expected);
        }

        public object ReturnWithSystemPointer(int expected)
        {
            return Pointer.Box((byte*)expected, typeof(byte*));
        }

        public static void MethodWithVoidPointer(void* vp) { }
    }

    public unsafe class PointerTypeHolder
    {
        public static long* StaticField;
        public int* Field;
    }

    unsafe delegate void MethodDelegate(byte* ptr, int expected);
    unsafe delegate void MethodDelegateWithSystemPointer(Pointer ptr, int expected);

    unsafe delegate bool* ReturnDelegate(int expected);
    unsafe delegate object ReturnDelegateWithSystemPointer(int expected);

    public unsafe class PointerTests
    {
        [Fact]
        public void Box_TypeNull()
        {
            ArgumentNullException ex = AssertExtensions.Throws<ArgumentNullException>("type", () =>
            {
                Pointer.Box((void*)0, null);
            });
        }

        [Fact]
        public void Box_NonPointerType()
        {
            AssertExtensions.Throws<ArgumentException>("ptr", () =>
            {
                Pointer.Box((void*)0, typeof(int));
            });
        }

        [Fact]
        public void Unbox_Null()
        {
            AssertExtensions.Throws<ArgumentException>("ptr", () =>
            {
                Pointer.Unbox(null);
            });
        }

        [Fact]
        public void Unbox_NotPointer()
        {
            AssertExtensions.Throws<ArgumentException>("ptr", () =>
            {
                Pointer.Unbox(new object());
            });
        }

        [Theory]
        [MemberData(nameof(Pointers))]
        public void PointerValueRoundtrips(int value)
        {
            void* ptr = unchecked((void*)value);
            object boxed = Pointer.Box(ptr, typeof(int*));
            void* result = Pointer.Unbox(boxed);
            Assert.Equal((IntPtr)ptr, (IntPtr)result);
            Assert.Equal((IntPtr)ptr, (IntPtr)((Pointer)boxed).Value);
        }

        public static IEnumerable<object[]> Pointers =>
            new[]
            {
                new object[] { 0 },
                new object[] { 1 },
                new object[] { -1 },
                new object[] { int.MaxValue },
                new object[] { int.MinValue },
            };

        public static IEnumerable<object[]> PointerTypes =>
            new[]
            {
                new object[] { typeof(void*) },
                new object[] { typeof(int*) },
                new object[] { typeof(long*) },
                new object[] { typeof(int**) },
                new object[] { typeof(Guid*) },
            };

        [Theory]
        [MemberData(nameof(PointerTypes))]
        public void PointerProperties_ReturnsBoxedType(Type type)
        {
            Pointer pointer = (Pointer)Pointer.Box((void*)1, type);
            Assert.Equal(type, pointer.PointerType);
            Assert.Equal((IntPtr)1, (IntPtr)pointer.Value);
        }

        [Fact]
        public void PointerProperties_StaticFieldGetValue()
        {
            PointerTypeHolder.StaticField = (long*)2;
            FieldInfo field = typeof(PointerTypeHolder).GetField(nameof(PointerTypeHolder.StaticField));
            Pointer pointer = Assert.IsType<Pointer>(field.GetValue(null));
            Assert.Equal(typeof(long*), pointer.PointerType);
            Assert.Equal((IntPtr)2, (IntPtr)pointer.Value);
        }

        [Fact]
        public void PointerProperties_FieldGetValueDirect()
        {
            var obj = new PointerTypeHolder();
            obj.Field = (int*)3;
            FieldInfo field = typeof(PointerTypeHolder).GetField(nameof(PointerTypeHolder.Field));
            Pointer pointer = Assert.IsType<Pointer>(field.GetValueDirect(__makeref(obj)));
            Assert.Equal(typeof(int*), pointer.PointerType);
            Assert.Equal((IntPtr)3, (IntPtr)pointer.Value);
        }

        [Theory]
        [MemberData(nameof(Pointers))]
        public void PointerFieldSetValue(int value)
        {
            var obj = new PointerHolder();
            FieldInfo field = typeof(PointerHolder).GetField("field");
            field.SetValue(obj, Pointer.Box(unchecked((void*)value), typeof(int*)));
            Assert.Equal(value, unchecked((int)obj.field));
        }

        [Fact]
        public void PointerFieldSetNullValue()
        {
            var obj = new PointerHolder();
            FieldInfo field = typeof(PointerHolder).GetField("field");
            field.SetValue(obj, null);
            Assert.Equal(0, unchecked((int)obj.field));
        }

        [Theory]
        [MemberData(nameof(Pointers))]
        public void IntPtrFieldSetValue(int value)
        {
            var obj = new PointerHolder();
            FieldInfo field = typeof(PointerHolder).GetField("field");
            field.SetValue(obj, (IntPtr)value);
            Assert.Equal(value, unchecked((int)obj.field));
        }

        [Theory]
        [MemberData(nameof(Pointers))]
        public void PointerFieldSetValue_InvalidType(int value)
        {
            var obj = new PointerHolder();
            FieldInfo field = typeof(PointerHolder).GetField("field");
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                field.SetValue(obj, Pointer.Box(unchecked((void*)value), typeof(long*)));
            });
        }

        [Theory]
        [MemberData(nameof(Pointers))]
        public void PointerFieldGetValue(int value)
        {
            var obj = new PointerHolder();
            obj.field = unchecked((int*)value);
            FieldInfo field = typeof(PointerHolder).GetField("field");
            object actualValue = field.GetValue(obj);
            Pointer pointer = Assert.IsType<Pointer>(actualValue);
            Assert.Equal(typeof(int*), pointer.PointerType);
            void* actualPointer = Pointer.Unbox(actualValue);
            Assert.Equal(value, unchecked((int)actualPointer));
            Assert.Equal((IntPtr)actualPointer, (IntPtr)pointer.Value);
        }

        [Theory]
        [MemberData(nameof(Pointers))]
        public void PointerPropertySetValue(int value)
        {
            var obj = new PointerHolder();
            PropertyInfo property = typeof(PointerHolder).GetProperty("Property");
            property.SetValue(obj, Pointer.Box(unchecked((void*)value), typeof(char*)));
            Assert.Equal(value, unchecked((int)obj.Property));
        }

        [Theory]
        [MemberData(nameof(Pointers))]
        public void IntPtrPropertySetValue(int value)
        {
            var obj = new PointerHolder();
            PropertyInfo property = typeof(PointerHolder).GetProperty("Property");
            property.SetValue(obj, (IntPtr)value);
            Assert.Equal(value, unchecked((int)obj.Property));
        }

        [Theory]
        [MemberData(nameof(Pointers))]
        public void PointerPropertySetValue_InvalidType(int value)
        {
            var obj = new PointerHolder();
            PropertyInfo property = typeof(PointerHolder).GetProperty("Property");
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                property.SetValue(obj, Pointer.Box(unchecked((void*)value), typeof(long*)));
            });
        }

        [Theory]
        [ActiveIssue("https://github.com/mono/mono/issues/15152", TestRuntimes.Mono)]
        [MemberData(nameof(Pointers))]
        public void PointerPropertyGetValue(int value)
        {
            var obj = new PointerHolder();
            obj.Property = unchecked((char*)value);
            PropertyInfo property = typeof(PointerHolder).GetProperty("Property");
            object actualValue = property.GetValue(obj);
            Pointer pointer = Assert.IsType<Pointer>(actualValue);
            Assert.Equal(typeof(char*), pointer.PointerType);
            void* actualPointer = Pointer.Unbox(actualValue);
            Assert.Equal(value, unchecked((int)actualPointer));
            Assert.Equal((IntPtr)actualPointer, (IntPtr)pointer.Value);
        }

        [Theory]
        [MemberData(nameof(Pointers))]
        public void PointerMethodParameter(int value)
        {
            var obj = new PointerHolder();
            MethodInfo method = typeof(PointerHolder).GetMethod("Method");
            method.Invoke(obj, new[] { Pointer.Box(unchecked((void*)value), typeof(byte*)), value });
        }

        [Fact]
        public void PointerNullMethodParameter()
        {
            var obj = new PointerHolder();
            MethodInfo method = typeof(PointerHolder).GetMethod("Method");
            method.Invoke(obj, new object[] { null, 0 });
        }

        [Theory]
        [MemberData(nameof(Pointers))]
        public void IntPtrMethodParameter(int value)
        {
            var obj = new PointerHolder();
            MethodInfo method = typeof(PointerHolder).GetMethod("Method");
            method.Invoke(obj, new object[] { (IntPtr)value, value });
        }

        public static IEnumerable<object[]> PointersUInt =>
            new[]
            {
                new object[] { UIntPtr.Zero },
                new object[] { 0 },
                new object[] { 1 },
                new object[] { uint.MinValue },
                new object[] { uint.MaxValue },
            };

        [Theory]
        [MemberData(nameof(PointersUInt))]
        public void UIntPtrMethodParameter(uint value)
        {
            var obj = new PointerHolder();
            MethodInfo method = typeof(PointerHolder).GetMethod(nameof(PointerHolder.MethodWithVoidPointer));
            method.Invoke(obj, new object[] { (UIntPtr)value });
        }

        [Theory]
        [MemberData(nameof(Pointers))]
        public void PointerMethodParameter_InvalidType(int value)
        {
            var obj = new PointerHolder();
            MethodInfo method = typeof(PointerHolder).GetMethod("Method");
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                method.Invoke(obj, new[] { Pointer.Box(unchecked((void*)value), typeof(long*)), value });
            });
        }

        [Theory]
        [MemberData(nameof(Pointers))]
        public void PointerMethodReturn(int value)
        {
            var obj = new PointerHolder();
            MethodInfo method = typeof(PointerHolder).GetMethod("Return");
            object actualValue = method.Invoke(obj, new object[] { value });
            Pointer pointer = Assert.IsType<Pointer>(actualValue);
            Assert.Equal(typeof(bool*), pointer.PointerType);
            void* actualPointer = Pointer.Unbox(actualValue);
            Assert.Equal(value, unchecked((int)actualPointer));
            Assert.Equal((IntPtr)actualPointer, (IntPtr)pointer.Value);
        }

        [Theory]
        [MemberData(nameof(Pointers))]
        public void PointerMethodDelegateParameter(int value)
        {
            var obj = new PointerHolder();
            MethodDelegate d = obj.Method;
            d.DynamicInvoke(Pointer.Box(unchecked((void*)value), typeof(byte*)), value);
        }

        [Theory]
        [MemberData(nameof(Pointers))]
        public void MethodDelegateParameter_SystemPointer(int value)
        {
            var obj = new PointerHolder();
            MethodDelegateWithSystemPointer d = obj.MethodWithSystemPointer;
            d.DynamicInvoke(Pointer.Box(unchecked((void*)value), typeof(byte*)), value);
        }

        [Fact]
        public void PointerNullMethodDelegateParameter()
        {
            var obj = new PointerHolder();
            MethodDelegate d = obj.Method;
            d.DynamicInvoke(null, 0);
        }

        [Fact]
        public void PointerNullMethodDelegateParameter_InvalidType_SystemPointer()
        {
            // An null is not converted to a System.Pointer.
            var obj = new PointerHolder();
            MethodDelegateWithSystemPointer d = obj.MethodWithSystemPointer;
            try
            {
                d.DynamicInvoke(null, 0);
            }
            catch (TargetInvocationException e) when (e.InnerException is ArgumentException)
            {
                return;
            }

            Assert.Fail("Inner exception should be ArgumentException.");
        }

        [Theory]
        [MemberData(nameof(Pointers))]
        public void IntPtrMethodDelegateParameter(int value)
        {
            var obj = new PointerHolder();
            MethodDelegate d = obj.Method;
            d.DynamicInvoke((IntPtr)value, value);
        }

        [Theory]
        [MemberData(nameof(Pointers))]
        public void IntPtrMethodDelegateParameter_InvalidType_SystemPointer(int value)
        {
            // An IntPtr is not converted to a System.Pointer.
            var obj = new PointerHolder();
            MethodDelegateWithSystemPointer d = obj.MethodWithSystemPointer;
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                d.DynamicInvoke((IntPtr)value, value);
            });
        }

        [Theory]
        [MemberData(nameof(Pointers))]
        public void PointerMethodDelegateParameter_InvalidType(int value)
        {
            var obj = new PointerHolder();
            MethodDelegate d = obj.Method;
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                d.DynamicInvoke(Pointer.Box(unchecked((void*)value), typeof(long*)), value);
            });
        }

        [Theory]
        [MemberData(nameof(Pointers))]
        public void PointerMethodDelegateParameter_InvalidType_SystemPointer(int value)
        {
            // Although the type boxed doesn't match, when unboxing void* is returned.
            var obj = new PointerHolder();
            MethodDelegateWithSystemPointer d = obj.MethodWithSystemPointer;
            d.DynamicInvoke(Pointer.Box(unchecked((void*)value), typeof(long*)), value);
        }

        [Theory]
        [MemberData(nameof(Pointers))]
        public void PointerMethodDelegateReturn(int value)
        {
            var obj = new PointerHolder();
            ReturnDelegate d = obj.Return;
            object actualValue = d.DynamicInvoke(value);
            Pointer pointer = Assert.IsType<Pointer>(actualValue);
            Assert.Equal(typeof(bool*), pointer.PointerType);
            void* actualPointer = Pointer.Unbox(actualValue);
            Assert.Equal(value, unchecked((int)actualPointer));
            Assert.Equal((IntPtr)actualPointer, (IntPtr)pointer.Value);
        }

        [Theory]
        [MemberData(nameof(Pointers))]
        public void PointerMethodDelegateReturn_SystemPointer(int value)
        {
            var obj = new PointerHolder();
            ReturnDelegateWithSystemPointer d = obj.ReturnWithSystemPointer;
            object actualValue = d.DynamicInvoke(value);
            Pointer pointer = Assert.IsType<Pointer>(actualValue);
            Assert.Equal(typeof(byte*), pointer.PointerType);
            void* actualPointer = Pointer.Unbox(actualValue);
            Assert.Equal(value, unchecked((int)actualPointer));
            Assert.Equal((IntPtr)actualPointer, (IntPtr)pointer.Value);
        }
    }
}
