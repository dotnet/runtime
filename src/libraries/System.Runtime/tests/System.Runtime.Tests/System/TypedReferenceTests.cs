// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// TypedReferenceTest.cs
//
// Authors:
//  Marek Safar  <marek.safar@gmail.com>
//
// Copyright (C) 2015 Xamarin Inc (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Tests
{
    public static class TypedReferenceTests
    {
        struct OneStruct
        {
            public string field;
            public int b;
        }

        class OtherType
        {
            public OneStruct oneStruct;
        }

        class ClassWithReadOnlyField
        {
            public readonly OneStruct Value;
            public ClassWithReadOnlyField(OneStruct value) => Value = value;
        }

        [Fact]
        public static void NegativeMakeTypedReference()
        {
            OtherType data = new OtherType { oneStruct = new OneStruct { field = "field", b = 2343 } };
            Type dataType = data.GetType();
            Assert.Throws<ArgumentNullException>(() => { TypedReference.MakeTypedReference(null, dataType.GetFields()); });
            Assert.Throws<ArgumentNullException>(() => { TypedReference.MakeTypedReference(data, null); });
            AssertExtensions.Throws<ArgumentException>("flds", null, () => { TypedReference.MakeTypedReference(data, Array.Empty<FieldInfo>()); });
            AssertExtensions.Throws<ArgumentException>(null, () => { TypedReference.MakeTypedReference(data, new FieldInfo[] { dataType.GetField("oneStruct"), null }); });
            AssertExtensions.Throws<ArgumentException>(null, () => { TypedReference.MakeTypedReference(data, new FieldInfo[] { dataType.GetField("oneStruct"), typeof(OneStruct).GetField("b") }); });
        }

        [Fact]
        public static void MakeTypedReference_ToObjectTests()
        {
            OneStruct structObj = new OneStruct { field = "field", b = 2343 };
            OtherType data = new OtherType { oneStruct = structObj };
            Type dataType = data.GetType();
            TypedReference reference = TypedReference.MakeTypedReference(data, new FieldInfo[] { dataType.GetField("oneStruct"), typeof(OneStruct).GetField("field") });
            Assert.Equal("field", TypedReference.ToObject(reference));

            reference = TypedReference.MakeTypedReference(data, new FieldInfo[] { dataType.GetField("oneStruct") });
            Assert.Equal(structObj, TypedReference.ToObject(reference));
        }

        [Fact]
        public static unsafe void MakeTypedReference_ToObjectTests_WithPointer()
        {
            float* pointer = (float*)(nuint)0x12345678;
            TypedReference reference = __makeref(pointer);
            object obj = TypedReference.ToObject(reference);

            // TypedReference-s over pointers use the UIntPtr type when boxing
            Assert.NotNull(obj);
            Assert.IsType<UIntPtr>(obj);
            Assert.Equal((nuint)0x12345678, (nuint)obj);
        }

        [Fact]
        public static unsafe void MakeTypedReference_ToObjectTests_WithFunctionPointer()
        {
            delegate*<int, float, string> pointer = (delegate*<int, float, string>)(void*)(nuint)0x12345678;
            TypedReference reference = __makeref(pointer);
            object obj = TypedReference.ToObject(reference);

            // TypedReference-s over function pointers use the UIntPtr type when boxing
            Assert.NotNull(obj);
            Assert.IsType<UIntPtr>(obj);
            Assert.Equal((nuint)0x12345678, (nuint)obj);
        }

        [Fact]
        public static void MakeTypedReference_ReadOnlyField_Succeeds()
        {
            var os = new OneStruct() { b = 42, field = "data" };
            var c = new ClassWithReadOnlyField(os);
            TypedReference tr = TypedReference.MakeTypedReference(c, new FieldInfo[] { c.GetType().GetField("Value") }); // doesn't throw
            Assert.Equal(os, TypedReference.ToObject(tr));
        }

        [Fact]
        public static void GetTargetTypeTests()
        {
            int intValue = 13223;
            TypedReference reference = __makeref(intValue);
            Assert.Equal(intValue.GetType(), TypedReference.GetTargetType(reference));

            long lValue = long.MaxValue;
            reference = __makeref(lValue);
            Assert.Equal(lValue.GetType(), TypedReference.GetTargetType(reference));

            string strValue = "a value";
            reference = __makeref(strValue);
            Assert.Equal(strValue.GetType(), TypedReference.GetTargetType(reference));

            char charValue = 'A';
            reference = __makeref(charValue);
            Assert.Equal(charValue.GetType(), TypedReference.GetTargetType(reference));

            byte byteValue = byte.MaxValue;
            reference = __makeref(byteValue);
            Assert.Equal(byteValue.GetType(), TypedReference.GetTargetType(reference));

            double doubleValue = double.MaxValue;
            reference = __makeref(doubleValue);
            Assert.Equal(doubleValue.GetType(), TypedReference.GetTargetType(reference));

            float floatValue = float.MaxValue;
            reference = __makeref(floatValue);
            Assert.Equal(floatValue.GetType(), TypedReference.GetTargetType(reference));

            bool boolValue = true;
            reference = __makeref(boolValue);
            Assert.Equal(boolValue.GetType(), TypedReference.GetTargetType(reference));
        }

        [Fact]
        public static unsafe void PointerTypeTests()
        {
            void* pointerValue = (void*)0x123456;
            TypedReference reference = __makeref(pointerValue);
            Assert.Equal(typeof(void*), TypedReference.GetTargetType(reference));

            // Pointer types get boxed as UIntPtr
            object obj = TypedReference.ToObject(reference);
            Assert.Equal(typeof(UIntPtr), obj.GetType());
            Assert.Equal((UIntPtr)pointerValue, (UIntPtr)obj);
        }
    }
}
