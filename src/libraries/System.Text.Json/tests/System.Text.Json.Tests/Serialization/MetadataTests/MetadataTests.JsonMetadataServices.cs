// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Drawing;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Serialization.Tests;
using Xunit;

namespace System.Text.Json.Tests.Serialization
{
    public abstract partial class MetadataTests
    {
        [Fact]
        public void CreatePropertyInfo()
        {
            JsonSerializerOptions options = new();

            // Null options
            ArgumentNullException ane = Assert.Throws<ArgumentNullException>(() => JsonMetadataServices.CreatePropertyInfo<int>(
                options: null,
                isProperty: true,
                isPublic: false,
                isVirtual: false,
                declaringType: typeof(Point),
                propertyTypeInfo: JsonMetadataServices.CreateValueInfo<int>(options, JsonMetadataServices.Int32Converter),
                converter: null,
                getter: null,
                setter: null,
                ignoreCondition: default,
                numberHandling: default,
                hasJsonInclude: false,
                propertyName: "MyInt",
                jsonPropertyName: null));
            Assert.Contains("options", ane.ToString());

            // Null declaring type
            ane = Assert.Throws<ArgumentNullException>(() => JsonMetadataServices.CreatePropertyInfo<int>(
                options: options,
                isProperty: true,
                isPublic: false,
                isVirtual: false,
                declaringType: null,
                propertyTypeInfo: JsonMetadataServices.CreateValueInfo<int>(options, JsonMetadataServices.Int32Converter),
                converter: null,
                getter: null,
                setter: null,
                ignoreCondition: default,
                numberHandling: default,
                hasJsonInclude: false,
                propertyName: "MyInt",
                jsonPropertyName: null));
            Assert.Contains("declaringType", ane.ToString());

            // Null property type info
            ane = Assert.Throws<ArgumentNullException>(() => JsonMetadataServices.CreatePropertyInfo<int>(
                options: options,
                isProperty: true,
                isPublic: false,
                isVirtual: false,
                declaringType: typeof(Point),
                propertyTypeInfo: null,
                converter: null,
                getter: null,
                setter: null,
                ignoreCondition: default,
                numberHandling: default,
                hasJsonInclude: false,
                propertyName: "MyInt",
                jsonPropertyName: null));
            Assert.Contains("propertyTypeInfo", ane.ToString());

            // Null property name
            ane = Assert.Throws<ArgumentNullException>(() => JsonMetadataServices.CreatePropertyInfo<int>(
                options: options,
                isProperty: true,
                isPublic: false,
                isVirtual: false,
                declaringType: typeof(Point),
                propertyTypeInfo: JsonMetadataServices.CreateValueInfo<int>(options, JsonMetadataServices.Int32Converter),
                converter: null,
                getter: null,
                setter: null,
                ignoreCondition: default,
                numberHandling: default,
                hasJsonInclude: false,
                propertyName: null,
                jsonPropertyName: null));
            Assert.Contains("propertyName", ane.ToString());

            // Invalid converter
            InvalidOperationException ioe = Assert.Throws<InvalidOperationException>(() => JsonMetadataServices.CreatePropertyInfo<MyClass>(
                options: options,
                isProperty: true,
                isPublic: false,
                isVirtual: false,
                declaringType: typeof(Point),
                // Converter invalid because you'd need to create with JsonMetadataServices.CreatePropertyInfo<MyDerivedClass> instead.
                propertyTypeInfo: JsonMetadataServices.CreateValueInfo<MyClass>(options, new DerivedClassConverter()),
                converter: null,
                getter: null,
                setter: null,
                ignoreCondition: default,
                numberHandling: default,
                hasJsonInclude: false,
                propertyName: "MyProp",
                jsonPropertyName: null));
            string ioeAsStr = ioe.ToString();
            Assert.Contains("Point.MyProp", ioeAsStr);
            Assert.Contains("MyClass", ioeAsStr);

            // Fields cannot be virtual.
            ioe = Assert.Throws<InvalidOperationException>(() => JsonMetadataServices.CreatePropertyInfo<int>(
                options: options,
                isProperty: false,
                isPublic: false,
                isVirtual: true,
                declaringType: typeof(Point),
                propertyTypeInfo: JsonMetadataServices.CreateValueInfo<int>(options, JsonMetadataServices.Int32Converter),
                converter: null,
                getter: null,
                setter: null,
                ignoreCondition: default,
                numberHandling: default,
                hasJsonInclude: false,
                propertyName: "X",
                jsonPropertyName: null));
            Assert.Contains("field", ioe.ToString());

            // Source generator tests verify that generated metadata is actually valid.
        }

        private class MyClass { }
        private class MyDerivedClass : MyClass { }

        [Fact]
        public void CreateObjectInfo()
        {
            JsonSerializerOptions options = new();

            // Null options
            ArgumentNullException ane = Assert.Throws<ArgumentNullException>(() => JsonMetadataServices.CreateObjectInfo<MyClass>(
                options: null,
                createObjectFunc: null,
                propInitFunc: (context) => Array.Empty<JsonPropertyInfo>(),
                numberHandling: default,
                serializeFunc: null));
            Assert.Contains("options", ane.ToString());

            // Null prop init func is fine if serialize func is provided.
            JsonMetadataServices.CreateObjectInfo<MyClass>(
                options,
                createObjectFunc: null,
                propInitFunc: null,
                numberHandling: default,
                serializeFunc: (writer, obj) => { });

            // Null serialize func is fine if prop init func is provided.
            JsonMetadataServices.CreateObjectInfo<MyClass>(
                options,
                createObjectFunc: null,
                propInitFunc: (context) => Array.Empty<JsonPropertyInfo>(),
                numberHandling: default,
                serializeFunc: null);

            // Null prop init func and serialize func
            InvalidOperationException ioe = Assert.Throws<InvalidOperationException>(() => JsonMetadataServices.CreateObjectInfo<MyClass>(
                options,
                createObjectFunc: null,
                propInitFunc: null,
                numberHandling: default,
                serializeFunc: null));
            string ioeAsStr = ioe.ToString();
            Assert.Contains("propInitFunc", ioeAsStr);
            Assert.Contains("serializeFunc", ioeAsStr);
        }

        [Fact]
        public void CreateValueInfo()
        {
            JsonSerializerOptions options = new();

            // Use converter that returns same type.
            Assert.NotNull(JsonMetadataServices.CreateValueInfo<MyClass>(options, new ClassConverter()));

            // Use converter that returns derived type.
            Assert.NotNull(JsonMetadataServices.CreateValueInfo<MyClass>(options, new DerivedClassConverter()));

            // Null options
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => JsonMetadataServices.CreateValueInfo<MyClass>(options: null, new DerivedClassConverter()));
            Assert.Contains("options", ex.ToString());
        }

        [Fact]
        public void CreateArrayInfo()
        {
            JsonSerializerOptions options = new();

            // Null options
            ArgumentNullException ane = Assert.Throws<ArgumentNullException>(() => JsonMetadataServices.CreateArrayInfo<int>(
                options: null,
                elementInfo: JsonMetadataServices.CreateValueInfo<int>(options, JsonMetadataServices.Int32Converter),
                numberHandling: default,
                serializeFunc: null));
            Assert.Contains("options", ane.ToString());

            // Null element info
            ane = Assert.Throws<ArgumentNullException>(() => JsonMetadataServices.CreateArrayInfo<int>(
                options,
                elementInfo: null,
                numberHandling: default,
                serializeFunc: null));
            Assert.Contains("elementInfo", ane.ToString());
        }

        [Fact]
        public void CreateListInfo()
        {
            JsonSerializerOptions options = new();

            // Null options
            ArgumentNullException ane = Assert.Throws<ArgumentNullException>(() => JsonMetadataServices.CreateListInfo<List<int>, int>(
                options: null,
                createObjectFunc: null,
                elementInfo: JsonMetadataServices.CreateValueInfo<int>(options, JsonMetadataServices.Int32Converter),
                numberHandling: default,
                serializeFunc: null));
            Assert.Contains("options", ane.ToString());

            // Null element info
            ane = Assert.Throws<ArgumentNullException>(() => JsonMetadataServices.CreateListInfo<GenericListWrapper<int>, int>(
                options: options,
                createObjectFunc: null,
                elementInfo: null,
                numberHandling: default,
                serializeFunc: null));
            Assert.Contains("elementInfo", ane.ToString());
        }

        [Fact]
        public void CreateDictionaryInfo()
        {
            JsonSerializerOptions options = new();

            // Null options
            ArgumentNullException ane = Assert.Throws<ArgumentNullException>(() => JsonMetadataServices.CreateDictionaryInfo<Dictionary<string, int>, string, int>(
                options: null,
                createObjectFunc: null,
                keyInfo: JsonMetadataServices.CreateValueInfo<string>(options, JsonMetadataServices.StringConverter),
                valueInfo: JsonMetadataServices.CreateValueInfo<int>(options, JsonMetadataServices.Int32Converter),
                numberHandling: default,
                serializeFunc: null));
            Assert.Contains("options", ane.ToString());

            // Null key info
            ane = Assert.Throws<ArgumentNullException>(() => JsonMetadataServices.CreateDictionaryInfo<StringToGenericDictionaryWrapper<int>, string, int>(
                options: options,
                createObjectFunc: null,
                keyInfo: null,
                valueInfo: JsonMetadataServices.CreateValueInfo<int>(options, JsonMetadataServices.Int32Converter),
                numberHandling: default,
                serializeFunc: null));
            Assert.Contains("keyInfo", ane.ToString());

            // Null value info
            ane = Assert.Throws<ArgumentNullException>(() => JsonMetadataServices.CreateDictionaryInfo<StringToGenericDictionaryWrapper<int>, string, int>(
                options: options,
                createObjectFunc: null,
                keyInfo: JsonMetadataServices.CreateValueInfo<string>(options, JsonMetadataServices.StringConverter),
                valueInfo: null,
                numberHandling: default,
                serializeFunc: null));
            Assert.Contains("valueInfo", ane.ToString());
        }

        private class ClassConverter : JsonConverter<MyClass>
        {
            public override MyClass? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();
            public override void Write(Utf8JsonWriter writer, MyClass value, JsonSerializerOptions options) => throw new NotImplementedException();
        }

        private class DerivedClassConverter : JsonConverter<MyDerivedClass>
        {
            public override MyDerivedClass? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();
            public override void Write(Utf8JsonWriter writer, MyDerivedClass value, JsonSerializerOptions options) => throw new NotImplementedException();
        }

        [Fact]
        public void GetEnumConverter()
        {
            JsonConverter<DayOfWeek> converter = JsonMetadataServices.GetEnumConverter<DayOfWeek>(new JsonSerializerOptions());
            Assert.NotNull(converter);
            Assert.Throws<ArgumentNullException>(() => JsonMetadataServices.GetEnumConverter<DayOfWeek>(null!));
        }

        [Fact]
        public void GetNullableConverter()
        {
            JsonSerializerOptions options = new();
            JsonConverter<DayOfWeek> enumConverter = JsonMetadataServices.GetEnumConverter<DayOfWeek>(options);
            JsonTypeInfo<DayOfWeek> enumInfo = JsonMetadataServices.CreateValueInfo<DayOfWeek>(options, enumConverter);
            JsonConverter<DayOfWeek?> nullableConverter = JsonMetadataServices.GetNullableConverter(enumInfo);
            Assert.NotNull(nullableConverter);
            Assert.Throws<ArgumentNullException>(() => JsonMetadataServices.GetNullableConverter<DayOfWeek>(null!));
        }
    }
}
