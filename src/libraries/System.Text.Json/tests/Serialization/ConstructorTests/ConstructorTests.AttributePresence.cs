// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class ConstructorTests
    {
        [Fact]
        public async Task NonPublicCtors_NotSupported()
        {
            async Task RunTest<T>()
            {
                NotSupportedException ex = await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.DeserializeAsync<T>("{}"));
                Assert.Contains("JsonConstructorAttribute", ex.ToString());
            }

            await RunTest<PrivateParameterlessCtor>();
            await RunTest<InternalParameterlessCtor>();
            await RunTest<ProtectedParameterlessCtor>();
            await RunTest<PrivateParameterizedCtor>();
            await RunTest<InternalParameterizedCtor>();
            await RunTest<ProtectedParameterizedCtor>();
            await RunTest<PrivateParameterizedCtor_WithAttribute>();
            await RunTest<InternalParameterizedCtor_WithAttribute>();
            await RunTest<ProtectedParameterizedCtor_WithAttribute>();
        }

        [Fact]
        public async Task SinglePublicParameterizedCtor_SingleParameterlessCtor_NoAttribute_Supported_UseParameterlessCtor()
        {
            var obj1 = await Serializer.DeserializeAsync<SinglePublicParameterizedCtor>(@"{""MyInt"":1,""MyString"":""1""}");
            Assert.Equal(@"{""MyInt"":0,""MyString"":null}", JsonSerializer.Serialize(obj1));
        }

        [Fact]
        public async Task MultiplePublicParameterizedCtors_SingleParameterlessCtor_NoAttribute_Supported_UseParameterlessCtor()
        {
            async Task RunTest<T>()
            {
                var obj1 = await Serializer.DeserializeAsync<T>(@"{""MyInt"":1,""MyString"":""1""}");
                Assert.Equal(@"{""MyInt"":0,""MyString"":null}", JsonSerializer.Serialize(obj1));
            }

            await RunTest<SingleParameterlessCtor_MultiplePublicParameterizedCtor>();
            await RunTest<SingleParameterlessCtor_MultiplePublicParameterizedCtor_Struct>();
        }

        [Fact]
        public async Task SinglePublicParameterizedCtor_NoPublicParameterlessCtor_NoAttribute_Supported()
        {
            async Task RunTest<T>()
            {
                var obj1 = await Serializer.DeserializeAsync<T>(@"{""MyInt"":1}");
                Assert.Equal(@"{""MyInt"":1}", JsonSerializer.Serialize(obj1));
            }

            await RunTest<PublicParameterizedCtor>();
            await RunTest<PrivateParameterlessConstructor_PublicParameterizedCtor>();
        }

        [Fact]
        public async Task SinglePublicParameterizedCtor_NoPublicParameterlessCtor_WithAttribute_Supported()
        {
            async Task RunTest<T>()
            {
                var obj1 = await Serializer.DeserializeAsync<T>(@"{""MyInt"":1}");
                Assert.Equal(@"{""MyInt"":1}", JsonSerializer.Serialize(obj1));
            }

            await RunTest<PublicParameterizedCtor_WithAttribute>();
            await RunTest<Struct_PublicParameterizedConstructor_WithAttribute>();
            await RunTest<PrivateParameterlessConstructor_PublicParameterizedCtor_WithAttribute>();
        }

        [Fact]
        public async Task Class_MultiplePublicParameterizedCtors_NoPublicParameterlessCtor_NoAttribute_NotSupported()
        {
            await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.DeserializeAsync<MultiplePublicParameterizedCtor>(@"{""MyInt"":1,""MyString"":""1""}"));
        }

        [Fact]
        public async Task Struct_MultiplePublicParameterizedCtors_NoPublicParameterlessCtor_NoAttribute_Supported_UseParameterlessCtor()
        {
            var obj = await Serializer.DeserializeAsync<MultiplePublicParameterizedCtor_Struct>(@"{""myInt"":1,""myString"":""1""}");
            Assert.Equal(0, obj.MyInt);
            Assert.Null(obj.MyString);
            Assert.Equal(@"{""MyInt"":0,""MyString"":null}", JsonSerializer.Serialize(obj));
        }

        [Fact]
        public async Task NoPublicParameterlessCtor_MultiplePublicParameterizedCtors_WithAttribute_Supported()
        {
            var obj1 = await Serializer.DeserializeAsync<MultiplePublicParameterizedCtor_WithAttribute>(@"{""MyInt"":1,""MyString"":""1""}");
            Assert.Equal(1, obj1.MyInt);
            Assert.Null(obj1.MyString);
            Assert.Equal(@"{""MyInt"":1,""MyString"":null}", JsonSerializer.Serialize(obj1));

            var obj2 = await Serializer.DeserializeAsync<MultiplePublicParameterizedCtor_WithAttribute_Struct>(@"{""MyInt"":1,""MyString"":""1""}");
            Assert.Equal(1, obj2.MyInt);
            Assert.Equal("1", obj2.MyString);
            Assert.Equal(@"{""MyInt"":1,""MyString"":""1""}", JsonSerializer.Serialize(obj2));
        }

        [Fact]
        public async Task PublicParameterlessCtor_MultiplePublicParameterizedCtors_WithAttribute_Supported()
        {
            var obj = await Serializer.DeserializeAsync<ParameterlessCtor_MultiplePublicParameterizedCtor_WithAttribute>(@"{""MyInt"":1,""MyString"":""1""}");
            Assert.Equal(1, obj.MyInt);
            Assert.Null(obj.MyString);
            Assert.Equal(@"{""MyInt"":1,""MyString"":null}", JsonSerializer.Serialize(obj));
        }

        [Fact]
        public async Task MultipleAttributes_NotSupported()
        {
            async Task RunTest<T>()
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.DeserializeAsync<T>("{}"));
            }

            await RunTest<MultiplePublicParameterizedCtor_WithMultipleAttributes>();
            await RunTest<PublicParameterlessConstructor_PublicParameterizedCtor_WithMultipleAttributes>();
            await RunTest<PrivateParameterlessCtor_InternalParameterizedCtor_WithMultipleAttributes>();
            await RunTest<ProtectedParameterlessCtor_PrivateParameterizedCtor_WithMultipleAttributes>();
            await RunTest<PublicParameterlessCtor_PrivateParameterizedCtor_WithMultipleAttributes>();
            await RunTest<PublicParameterizedCtor_PublicParameterizedCtor_WithMultipleAttributes>();
            await RunTest<Struct_PublicParameterizedCtor_PrivateParameterizedCtor_WithMultipleAttributes>();
            await RunTest<Point_2D_Struct_WithMultipleAttributes>();
            await RunTest<Point_2D_Struct_WithMultipleAttributes_OneNonPublic>();
        }

        [Fact]
        public async Task AttributeIgnoredOnIEnumerable()
        {
            async Task RunTest<T>()
            {
                await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.DeserializeAsync<T>("[]"));
            }

            await RunTest<Parameterized_StackWrapper>();
            await RunTest<Parameterized_WrapperForICollection>();
        }

        [Fact]
        public async Task Struct_Use_DefaultCtor_ByDefault()
        {
            string json = @"{""X"":1,""Y"":2}";

            // By default, serializer uses default ctor to deserializer structs
            var point1 = await Serializer.DeserializeAsync<Point_2D_Struct>(json);
            Assert.Equal(0, point1.X);
            Assert.Equal(0, point1.Y);

            var point2 = await Serializer.DeserializeAsync<Point_2D_Struct_WithAttribute>(json);
            Assert.Equal(1, point2.X);
            Assert.Equal(2, point2.Y);
        }
    }
}
