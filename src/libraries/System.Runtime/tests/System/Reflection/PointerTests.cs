// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Tests;
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

        public bool* Return(int expected)
        {
            return unchecked((bool*)expected);
        }
    }

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
            void* result = Pointer.Unbox(Pointer.Box(ptr, typeof(int*)));
            Assert.Equal((IntPtr)ptr, (IntPtr)result);
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

        [Theory]
        [MemberData(nameof(Pointers))]
        public void PointerFieldSetValue(int value)
        {
            var obj = new PointerHolder();
            FieldInfo field = typeof(PointerHolder).GetField("field");
            field.SetValue(obj, Pointer.Box(unchecked((void*)value), typeof(int*)));
            Assert.Equal(value, unchecked((int)obj.field));
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
            Assert.IsType<Pointer>(actualValue);
            void* actualPointer = Pointer.Unbox(actualValue);
            Assert.Equal(value, unchecked((int)actualPointer));
        }

        [Theory]
        [MemberData(nameof(Pointers))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Pointers through Invoke not implemented: https://github.com/dotnet/corert/issues/2113")]
        public void PointerPropertySetValue(int value)
        {
            var obj = new PointerHolder();
            PropertyInfo property = typeof(PointerHolder).GetProperty("Property");
            property.SetValue(obj, Pointer.Box(unchecked((void*)value), typeof(char*)));
            Assert.Equal(value, unchecked((int)obj.Property));
        }

        [Theory]
        [MemberData(nameof(Pointers))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Pointers through Invoke not implemented: https://github.com/dotnet/corert/issues/2113")]
        public void IntPtrPropertySetValue(int value)
        {
            var obj = new PointerHolder();
            PropertyInfo property = typeof(PointerHolder).GetProperty("Property");
            property.SetValue(obj, (IntPtr)value);
            Assert.Equal(value, unchecked((int)obj.Property));
        }

        [Theory]
        [MemberData(nameof(Pointers))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Pointers through Invoke not implemented: https://github.com/dotnet/corert/issues/2113")]
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
        [MemberData(nameof(Pointers))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Pointers through Invoke not implemented: https://github.com/dotnet/corert/issues/2113")]
        public void PointerPropertyGetValue(int value)
        {
            var obj = new PointerHolder();
            obj.Property = unchecked((char*)value);
            PropertyInfo property = typeof(PointerHolder).GetProperty("Property");
            object actualValue = property.GetValue(obj);
            Assert.IsType<Pointer>(actualValue);
            void* actualPointer = Pointer.Unbox(actualValue);
            Assert.Equal(value, unchecked((int)actualPointer));
        }

        [Theory]
        [MemberData(nameof(Pointers))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Pointers through Invoke not implemented: https://github.com/dotnet/corert/issues/2113")]
        public void PointerMethodParameter(int value)
        {
            var obj = new PointerHolder();
            MethodInfo method = typeof(PointerHolder).GetMethod("Method");
            method.Invoke(obj, new[] { Pointer.Box(unchecked((void*)value), typeof(byte*)), value });
        }

        [Theory]
        [MemberData(nameof(Pointers))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Pointers through Invoke not implemented: https://github.com/dotnet/corert/issues/2113")]
        public void IntPtrMethodParameter(int value)
        {
            var obj = new PointerHolder();
            MethodInfo method = typeof(PointerHolder).GetMethod("Method");
            method.Invoke(obj, new object[] { (IntPtr)value, value });
        }

        [Theory]
        [MemberData(nameof(Pointers))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Pointers through Invoke not implemented: https://github.com/dotnet/corert/issues/2113")]
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
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Pointers through Invoke not implemented: https://github.com/dotnet/corert/issues/2113")]
        public void PointerMethodReturn(int value)
        {
            var obj = new PointerHolder();
            MethodInfo method = typeof(PointerHolder).GetMethod("Return");
            object actualValue = method.Invoke(obj, new object[] { value });
            Assert.IsType<Pointer>(actualValue);
            void* actualPointer = Pointer.Unbox(actualValue);
            Assert.Equal(value, unchecked((int)actualPointer));
        }
    }
}
