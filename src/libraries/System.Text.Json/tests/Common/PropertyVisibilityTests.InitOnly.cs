// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class PropertyVisibilityTests
    {
        [Theory]
        [InlineData(typeof(ClassWithInitOnlyProperty))]
        [InlineData(typeof(StructWithInitOnlyProperty))]
        public virtual async Task InitOnlyProperties(Type type)
        {
            // Init-only property included by default.
            object obj = await Serializer.DeserializeWrapper(@"{""MyInt"":1}", type);
            Assert.Equal(1, (int)type.GetProperty("MyInt").GetValue(obj));

            // Init-only properties can be serialized.
            Assert.Equal(@"{""MyInt"":1}", await Serializer.SerializeWrapper(obj));
        }

        [Theory]
        [InlineData(typeof(ClassWithCustomNamedInitOnlyProperty))]
        [InlineData(typeof(StructWithCustomNamedInitOnlyProperty))]
        public virtual async Task CustomNamedInitOnlyProperties(Type type)
        {
            // Regression test for https://github.com/dotnet/runtime/issues/82730

            // Init-only property included by default.
            object obj = await Serializer.DeserializeWrapper(@"{""CustomMyInt"":1}", type);
            Assert.Equal(1, (int)type.GetProperty("MyInt").GetValue(obj));

            // Init-only properties can be serialized.
            Assert.Equal(@"{""CustomMyInt"":1}", await Serializer.SerializeWrapper(obj));
        }

        [Theory]
        [InlineData(typeof(Class_PropertyWith_PrivateInitOnlySetter))]
        [InlineData(typeof(Class_PropertyWith_InternalInitOnlySetter))]
        [InlineData(typeof(Class_PropertyWith_ProtectedInitOnlySetter))]
        public async Task NonPublicInitOnlySetter_Without_JsonInclude_Fails(Type type)
        {
            // Non-public init-only property setter ignored.
            object obj = await Serializer.DeserializeWrapper(@"{""MyInt"":1}", type);
            Assert.Equal(0, (int)type.GetProperty("MyInt").GetValue(obj));

            // Public getter can be used for serialization.
            Assert.Equal(@"{""MyInt"":0}", await Serializer.SerializeWrapper(obj, type));
        }

        [Theory]
        [InlineData(typeof(Class_PropertyWith_PrivateInitOnlySetter_WithAttribute))]
        [InlineData(typeof(Class_PropertyWith_InternalInitOnlySetter_WithAttribute))]
        [InlineData(typeof(Class_PropertyWith_ProtectedInitOnlySetter_WithAttribute))]
        public virtual async Task NonPublicInitOnlySetter_With_JsonInclude(Type type)
        {
            // Non-public init-only property setter included with [JsonInclude].
            object obj = await Serializer.DeserializeWrapper(@"{""MyInt"":1}", type);
            Assert.Equal(1, (int)type.GetProperty("MyInt").GetValue(obj));

            // Init-only properties can be serialized.
            Assert.Equal(@"{""MyInt"":1}", await Serializer.SerializeWrapper(obj));
        }

        [Fact]
        public async Task NullableStructWithInitOnlyProperty()
        {
            // Regression test for https://github.com/dotnet/runtime/issues/86483

            StructWithInitOnlyProperty? value = new StructWithInitOnlyProperty { MyInt = 42 };
            string json = await Serializer.SerializeWrapper(value);

            Assert.Equal("""{"MyInt":42}""", json);

            StructWithInitOnlyProperty? deserializedValue = await Serializer.DeserializeWrapper<StructWithInitOnlyProperty?>(json);
            Assert.Equal(deserializedValue, value);
        }

        public class ClassWithInitOnlyProperty
        {
            public int MyInt { get; init; }
        }

        public struct StructWithInitOnlyProperty
        {
            public int MyInt { get; init; }
        }

        public class ClassWithCustomNamedInitOnlyProperty
        {
            [JsonPropertyName("CustomMyInt")]
            public int MyInt { get; init; }
        }

        public struct StructWithCustomNamedInitOnlyProperty
        {
            [JsonPropertyName("CustomMyInt")]
            public int MyInt { get; init; }
        }

        public class Class_PropertyWith_PrivateInitOnlySetter
        {
            public int MyInt { get; private init; }
        }

        public class Class_PropertyWith_InternalInitOnlySetter
        {
            public int MyInt { get; internal init; }
        }

        public class Class_PropertyWith_ProtectedInitOnlySetter
        {
            public int MyInt { get; protected init; }
        }

        public class Class_PropertyWith_PrivateInitOnlySetter_WithAttribute
        {
            [JsonInclude]
            public int MyInt { get; private init; }
        }

        public class Class_PropertyWith_InternalInitOnlySetter_WithAttribute
        {
            [JsonInclude]
            public int MyInt { get; internal init; }
        }

        public class Class_PropertyWith_ProtectedInitOnlySetter_WithAttribute
        {
            [JsonInclude]
            public int MyInt { get; protected init; }
        }
    }
}
