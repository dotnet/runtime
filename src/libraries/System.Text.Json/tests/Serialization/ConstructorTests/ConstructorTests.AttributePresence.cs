//Licensed to the.NET Foundation under one or more agreements.
//The.NET Foundation licenses this file to you under the MIT license.
//See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class ConstructorTests
    {
        [Fact]
        public void NonPublicCtors_NotSupported()
        {
            void RunTest<T>()
            {
                NotSupportedException ex = Assert.Throws<NotSupportedException>(() => Serializer.Deserialize<T>("{}"));
                Assert.Contains("JsonConstructorAttribute", ex.ToString());
            }

            RunTest<PrivateParameterlessCtor>();
            RunTest<InternalParameterlessCtor>();
            RunTest<ProtectedParameterlessCtor>();
            RunTest<PrivateParameterizedCtor>();
            RunTest<InternalParameterizedCtor>();
            RunTest<ProtectedParameterizedCtor>();
            RunTest<PrivateParameterizedCtor_WithAttribute>();
            RunTest<InternalParameterizedCtor_WithAttribute>();
            RunTest<ProtectedParameterizedCtor_WithAttribute>();
        }

        [Fact]
        public void SinglePublicParameterizedCtor_SingleParameterlessCtor_NoAttribute_Supported_UseParameterlessCtor()
        {
            var obj1 = Serializer.Deserialize<SinglePublicParameterizedCtor>(@"{""MyInt"":1,""MyString"":""1""}");
            Assert.Equal(@"{""MyInt"":0,""MyString"":null}", JsonSerializer.Serialize(obj1));
        }

        [Fact]
        public void MultiplePublicParameterizedCtors_SingleParameterlessCtor_NoAttribute_Supported_UseParameterlessCtor()
        {
            void RunTest<T>()
            {
                var obj1 = Serializer.Deserialize<T>(@"{""MyInt"":1,""MyString"":""1""}");
                Assert.Equal(@"{""MyInt"":0,""MyString"":null}", JsonSerializer.Serialize(obj1));
            }

            RunTest<SingleParameterlessCtor_MultiplePublicParameterizedCtor>();
            RunTest<SingleParameterlessCtor_MultiplePublicParameterizedCtor_Struct>();
        }

        [Fact]
        public void SinglePublicParameterizedCtor_NoPublicParameterlessCtor_NoAttribute_Supported()
        {
            void RunTest<T>()
            {
                var obj1 = Serializer.Deserialize<T>(@"{""MyInt"":1}");
                Assert.Equal(@"{""MyInt"":1}", JsonSerializer.Serialize(obj1));
            }

            RunTest<PublicParameterizedCtor>();
            RunTest<PrivateParameterlessConstructor_PublicParameterizedCtor>();
        }

        [Fact]
        public void SinglePublicParameterizedCtor_NoPublicParameterlessCtor_WithAttribute_Supported()
        {
            void RunTest<T>()
            {
                var obj1 = Serializer.Deserialize<T>(@"{""MyInt"":1}");
                Assert.Equal(@"{""MyInt"":1}", JsonSerializer.Serialize(obj1));
            }

            RunTest<PublicParameterizedCtor_WithAttribute>();
            RunTest<Struct_PublicParameterizedConstructor_WithAttribute>();
            RunTest<PrivateParameterlessConstructor_PublicParameterizedCtor_WithAttribute>();
        }

        [Fact]
        public void Class_MultiplePublicParameterizedCtors_NoPublicParameterlessCtor_NoAttribute_NotSupported()
        {
            Assert.Throws<NotSupportedException>(() => Serializer.Deserialize<MultiplePublicParameterizedCtor>(@"{""MyInt"":1,""MyString"":""1""}"));
        }

        [Fact]
        public void Struct_MultiplePublicParameterizedCtors_NoPublicParameterlessCtor_NoAttribute_Supported_UseParameterlessCtor()
        {
            var obj = Serializer.Deserialize<MultiplePublicParameterizedCtor_Struct>(@"{""myInt"":1,""myString"":""1""}");
            Assert.Equal(0, obj.MyInt);
            Assert.Null(obj.MyString);
            Assert.Equal(@"{""MyInt"":0,""MyString"":null}", JsonSerializer.Serialize(obj));
        }

        [Fact]
        public void NoPublicParameterlessCtor_MultiplePublicParameterizedCtors_WithAttribute_Supported()
        {
            var obj1 = Serializer.Deserialize<MultiplePublicParameterizedCtor_WithAttribute>(@"{""MyInt"":1,""MyString"":""1""}");
            Assert.Equal(1, obj1.MyInt);
            Assert.Null(obj1.MyString);
            Assert.Equal(@"{""MyInt"":1,""MyString"":null}", JsonSerializer.Serialize(obj1));

            var obj2 = Serializer.Deserialize<MultiplePublicParameterizedCtor_WithAttribute_Struct>(@"{""MyInt"":1,""MyString"":""1""}");
            Assert.Equal(1, obj2.MyInt);
            Assert.Equal("1", obj2.MyString);
            Assert.Equal(@"{""MyInt"":1,""MyString"":""1""}", JsonSerializer.Serialize(obj2));
        }

        [Fact]
        public void PublicParameterlessCtor_MultiplePublicParameterizedCtors_WithAttribute_Supported()
        {
            var obj = Serializer.Deserialize<ParameterlessCtor_MultiplePublicParameterizedCtor_WithAttribute>(@"{""MyInt"":1,""MyString"":""1""}");
            Assert.Equal(1, obj.MyInt);
            Assert.Null(obj.MyString);
            Assert.Equal(@"{""MyInt"":1,""MyString"":null}", JsonSerializer.Serialize(obj));
        }

        [Fact]
        public void MultipleAttributes_NotSupported()
        {
            void RunTest<T>()
            {
                Assert.Throws<InvalidOperationException>(() => Serializer.Deserialize<T>("{}"));
            }

            RunTest<MultiplePublicParameterizedCtor_WithMultipleAttributes>();
            RunTest<PublicParameterlessConstructor_PublicParameterizedCtor_WithMultipleAttributes>();
            RunTest<PrivateParameterlessCtor_InternalParameterizedCtor_WithMultipleAttributes>();
            RunTest<ProtectedParameterlessCtor_PrivateParameterizedCtor_WithMultipleAttributes>();
            RunTest<PublicParameterlessCtor_PrivateParameterizedCtor_WithMultipleAttributes>();
            RunTest<PublicParameterizedCtor_PublicParameterizedCtor_WithMultipleAttributes>();
            RunTest<Struct_PublicParameterizedCtor_PrivateParameterizedCtor_WithMultipleAttributes>();
            RunTest<Point_2D_Struct_WithMultipleAttributes>();
            RunTest<Point_2D_Struct_WithMultipleAttributes_OneNonPublic>();
        }

        [Fact]
        public void AttributeIgnoredOnIEnumerable()
        {
            void RunTest<T>()
            {
                Assert.Throws<NotSupportedException>(() => Serializer.Deserialize<T>("[]"));
            }

            RunTest<Parameterized_StackWrapper>();
            RunTest<Parameterized_WrapperForICollection>();
        }

        [Fact]
        public void Struct_Use_DefaultCtor_ByDefault()
        {
            string json = @"{""X"":1,""Y"":2}";

            // By default, serializer uses default ctor to deserializer structs
            var point1 = Serializer.Deserialize<Point_2D_Struct>(json);
            Assert.Equal(0, point1.X);
            Assert.Equal(0, point1.Y);

            var point2 = Serializer.Deserialize<Point_2D_Struct_WithAttribute>(json);
            Assert.Equal(1, point2.X);
            Assert.Equal(2, point2.Y);
        }
    }
}
