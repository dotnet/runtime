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
            async Task RunTestAsync<T>()
            {
                NotSupportedException ex = await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.DeserializeWrapper<T>("{}"));
                Assert.Contains("JsonConstructorAttribute", ex.ToString());
            }

            await RunTestAsync<PrivateParameterlessCtor>();
            await RunTestAsync<InternalParameterlessCtor>();
            await RunTestAsync<ProtectedParameterlessCtor>();
            await RunTestAsync<PrivateParameterizedCtor>();
            await RunTestAsync<InternalParameterizedCtor>();
            await RunTestAsync<ProtectedParameterizedCtor>();
            await RunTestAsync<PrivateParameterizedCtor_WithAttribute>();
            await RunTestAsync<InternalParameterizedCtor_WithAttribute>();
            await RunTestAsync<ProtectedParameterizedCtor_WithAttribute>();
        }

        [Fact]
        public async Task SinglePublicParameterizedCtor_SingleParameterlessCtor_NoAttribute_Supported_UseParameterlessCtor()
        {
            var obj1 = await Serializer.DeserializeWrapper<SinglePublicParameterizedCtor>(@"{""MyInt"":1,""MyString"":""1""}");
            Assert.Equal(@"{""MyInt"":0,""MyString"":null}", await Serializer.SerializeWrapper(obj1));
        }

        [Fact]
        public async Task MultiplePublicParameterizedCtors_SingleParameterlessCtor_NoAttribute_Supported_UseParameterlessCtor()
        {
            async Task RunTestAsync<T>()
            {
                var obj1 = await Serializer.DeserializeWrapper<T>(@"{""MyInt"":1,""MyString"":""1""}");
                Assert.Equal(@"{""MyInt"":0,""MyString"":null}", await Serializer.SerializeWrapper(obj1));
            }

            await RunTestAsync<SingleParameterlessCtor_MultiplePublicParameterizedCtor>();
            await RunTestAsync<SingleParameterlessCtor_MultiplePublicParameterizedCtor_Struct>();
        }

        [Fact]
        public async Task SinglePublicParameterizedCtor_NoPublicParameterlessCtor_NoAttribute_Supported()
        {
            async Task RunTestAsync<T>()
            {
                var obj1 = await Serializer.DeserializeWrapper<T>(@"{""MyInt"":1}");
                Assert.Equal(@"{""MyInt"":1}", await Serializer.SerializeWrapper(obj1));
            }

            await RunTestAsync<PublicParameterizedCtor>();
            await RunTestAsync<PrivateParameterlessConstructor_PublicParameterizedCtor>();
        }

        [Fact]
        public async Task SinglePublicParameterizedCtor_NoPublicParameterlessCtor_WithAttribute_Supported()
        {
            async Task RunTestAsync<T>()
            {
                var obj1 = await Serializer.DeserializeWrapper<T>(@"{""MyInt"":1}");
                Assert.Equal(@"{""MyInt"":1}", await Serializer.SerializeWrapper(obj1));
            }

            await RunTestAsync<PublicParameterizedCtor_WithAttribute>();
            await RunTestAsync<Struct_PublicParameterizedConstructor_WithAttribute>();
            await RunTestAsync<PrivateParameterlessConstructor_PublicParameterizedCtor_WithAttribute>();
        }

        [Fact]
        public Task Class_MultiplePublicParameterizedCtors_NoPublicParameterlessCtor_NoAttribute_NotSupported()
        {
            return Assert.ThrowsAsync<NotSupportedException>(() => Serializer.DeserializeWrapper<MultiplePublicParameterizedCtor>(@"{""MyInt"":1,""MyString"":""1""}"));
        }

        [Fact]
        public async Task Struct_MultiplePublicParameterizedCtors_NoPublicParameterlessCtor_NoAttribute_Supported_UseParameterlessCtor()
        {
            var obj = await Serializer.DeserializeWrapper<MultiplePublicParameterizedCtor_Struct>(@"{""myInt"":1,""myString"":""1""}");
            Assert.Equal(0, obj.MyInt);
            Assert.Null(obj.MyString);
            Assert.Equal(@"{""MyInt"":0,""MyString"":null}", await Serializer.SerializeWrapper(obj));
        }

        [Fact]
        public async Task NoPublicParameterlessCtor_MultiplePublicParameterizedCtors_WithAttribute_Supported()
        {
            var obj1 = await Serializer.DeserializeWrapper<MultiplePublicParameterizedCtor_WithAttribute>(@"{""MyInt"":1,""MyString"":""1""}");
            Assert.Equal(1, obj1.MyInt);
            Assert.Null(obj1.MyString);
            Assert.Equal(@"{""MyInt"":1,""MyString"":null}", await Serializer.SerializeWrapper(obj1));

            var obj2 = await Serializer.DeserializeWrapper<MultiplePublicParameterizedCtor_WithAttribute_Struct>(@"{""MyInt"":1,""MyString"":""1""}");
            Assert.Equal(1, obj2.MyInt);
            Assert.Equal("1", obj2.MyString);
            Assert.Equal(@"{""MyInt"":1,""MyString"":""1""}", await Serializer.SerializeWrapper(obj2));
        }

        [Fact]
        public async Task PublicParameterlessCtor_MultiplePublicParameterizedCtors_WithAttribute_Supported()
        {
            var obj = await Serializer.DeserializeWrapper<ParameterlessCtor_MultiplePublicParameterizedCtor_WithAttribute>(@"{""MyInt"":1,""MyString"":""1""}");
            Assert.Equal(1, obj.MyInt);
            Assert.Null(obj.MyString);
            Assert.Equal(@"{""MyInt"":1,""MyString"":null}", await Serializer.SerializeWrapper(obj));
        }

#if !BUILDING_SOURCE_GENERATOR_TESTS // These are compile-time warnings from the source generator.
        [Fact]
        public async Task MultipleAttributes_NotSupported()
        {
            async Task RunTestAsync<T>()
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.DeserializeWrapper<T>("{}"));
            }

            await RunTestAsync<MultiplePublicParameterizedCtor_WithMultipleAttributes>();
            await RunTestAsync<PublicParameterlessConstructor_PublicParameterizedCtor_WithMultipleAttributes>();
            await RunTestAsync<PrivateParameterlessCtor_InternalParameterizedCtor_WithMultipleAttributes>();
            await RunTestAsync<ProtectedParameterlessCtor_PrivateParameterizedCtor_WithMultipleAttributes>();
            await RunTestAsync<PublicParameterlessCtor_PrivateParameterizedCtor_WithMultipleAttributes>();
            await RunTestAsync<PublicParameterizedCtor_PublicParameterizedCtor_WithMultipleAttributes>();
            await RunTestAsync<Struct_PublicParameterizedCtor_PrivateParameterizedCtor_WithMultipleAttributes>();
            await RunTestAsync<Point_2D_Struct_WithMultipleAttributes>();
            await RunTestAsync<Point_2D_Struct_WithMultipleAttributes_OneNonPublic>();
        }
#endif

        [Fact]
        public async Task AttributeIgnoredOnIEnumerable()
        {
            async Task RunTestAsync<T>()
            {
                await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.DeserializeWrapper<T>("[]"));
            }

            await RunTestAsync<Parameterized_StackWrapper>();
            await RunTestAsync<Parameterized_WrapperForICollection>();
        }

        [Fact]
        public async Task Struct_Use_DefaultCtor_ByDefault()
        {
            string json = @"{""X"":1,""Y"":2}";

            // By default, serializer uses default ctor to deserializer structs
            var point1 = await Serializer.DeserializeWrapper<Point_2D_Struct>(json);
            Assert.Equal(0, point1.X);
            Assert.Equal(0, point1.Y);

            var point2 = await Serializer.DeserializeWrapper<Point_2D_Struct_WithAttribute>(json);
            Assert.Equal(1, point2.X);
            Assert.Equal(2, point2.Y);
        }
    }
}
