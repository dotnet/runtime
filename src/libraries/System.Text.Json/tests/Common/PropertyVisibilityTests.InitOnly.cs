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

        [Theory]
        [InlineData(typeof(Class_WithIgnoredInitOnlyProperty))]
        [InlineData(typeof(Record_WithIgnoredPropertyInCtor))]
        public async Task InitOnlySetter_With_JsonIgnoreAlways(Type type)
        {
            object obj = await Serializer.DeserializeWrapper(@"{""MyInt"":42}", type);
            Assert.Equal(0, (int)type.GetProperty("MyInt").GetValue(obj));
        }

        [Theory]
        [InlineData(typeof(Class_WithIgnoredRequiredProperty))]
        public async Task RequiredSetter_With_JsonIgnoreAlways_ThrowsInvalidOperationException(Type type)
        {
            JsonSerializerOptions options = Serializer.CreateOptions(opts => opts.RespectRequiredConstructorParameters = true);
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.DeserializeWrapper(@"{""MyInt"":42}", type, options));
        }

        [Fact]
        public async Task JsonIgnoreOnInitOnlyProperty()
        {
            // Regression test for https://github.com/dotnet/runtime/issues/101877

            RecordWithIgnoredNestedInitOnlyProperty.InnerRecord inner = new(42, "Baz");
            RecordWithIgnoredNestedInitOnlyProperty value = new RecordWithIgnoredNestedInitOnlyProperty(inner);
            string json = await Serializer.SerializeWrapper(value);
            Assert.Equal("""{"foo":42}""", json);

            RecordWithIgnoredNestedInitOnlyProperty? deserializedValue = await Serializer.DeserializeWrapper<RecordWithIgnoredNestedInitOnlyProperty>(json);
            Assert.Equal(value, deserializedValue);
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

        public class Class_WithIgnoredInitOnlyProperty
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
            public int MyInt { get; init; }
        }
        
        public record Record_WithIgnoredPropertyInCtor(
            [property: JsonIgnore] int MyInt = 0);

        public class Class_WithIgnoredRequiredProperty
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
            public required int MyInt { get; set; }
        }

        public record RecordWithIgnoredNestedInitOnlyProperty(
            [property: JsonIgnore] RecordWithIgnoredNestedInitOnlyProperty.InnerRecord Other)
        {
            [JsonConstructor]
            public RecordWithIgnoredNestedInitOnlyProperty(int foo)
                : this(new InnerRecord(foo, "Baz"))
            {
            }

            [JsonPropertyName("foo")] public int Foo => Other.Foo;

            public record InnerRecord(int Foo, string Bar);
        }

        [Theory]
        [InlineData(typeof(ClassWithInitOnlyPropertyDefaults))]
        [InlineData(typeof(StructWithInitOnlyPropertyDefaults))]
        public async Task InitOnlyPropertyDefaultValues_PreservedWhenMissingFromJson(Type type)
        {
            // When no properties are present in JSON, C# default values should be preserved.
            object obj = await Serializer.DeserializeWrapper("{}", type);
            Assert.Equal("DefaultName", (string)type.GetProperty("Name").GetValue(obj));
            Assert.Equal(42, (int)type.GetProperty("Number").GetValue(obj));
        }

        [Theory]
        [InlineData(typeof(ClassWithInitOnlyPropertyDefaults))]
        [InlineData(typeof(StructWithInitOnlyPropertyDefaults))]
        public async Task InitOnlyPropertyDefaultValues_OverriddenWhenPresentInJson(Type type)
        {
            // When properties are present in JSON, specified values should be used.
            object obj = await Serializer.DeserializeWrapper("""{"Name":"Override","Number":99}""", type);
            Assert.Equal("Override", (string)type.GetProperty("Name").GetValue(obj));
            Assert.Equal(99, (int)type.GetProperty("Number").GetValue(obj));
        }

        [Theory]
        [InlineData(typeof(ClassWithInitOnlyPropertyDefaults))]
        [InlineData(typeof(StructWithInitOnlyPropertyDefaults))]
        public async Task InitOnlyPropertyDefaultValues_PartialOverride(Type type)
        {
            // When only some properties are present in JSON, the rest should keep defaults.
            object obj = await Serializer.DeserializeWrapper("""{"Number":99}""", type);
            Assert.Equal("DefaultName", (string)type.GetProperty("Name").GetValue(obj));
            Assert.Equal(99, (int)type.GetProperty("Number").GetValue(obj));
        }

        public class ClassWithInitOnlyPropertyDefaults
        {
            public string Name { get; init; } = "DefaultName";
            public int Number { get; init; } = 42;
        }

        public struct StructWithInitOnlyPropertyDefaults
        {
            public StructWithInitOnlyPropertyDefaults() { }
            public string Name { get; init; } = "DefaultName";
            public int Number { get; init; } = 42;
        }
    }
}
