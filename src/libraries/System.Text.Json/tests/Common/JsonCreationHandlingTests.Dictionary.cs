// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests;

public abstract partial class JsonCreationHandlingTests : SerializerTests
{
    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_DictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyDictionaryOfStringToInt>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyDictionaryOfStringToInt));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_DictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyDictionaryOfStringToIntWithAttributeOnType>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_DictionaryOfStringToInt_PropertyOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4},"Property":{"e":5},"Property":{"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyDictionaryOfStringToInt>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_DictionaryOfStringToInt_PropertyOccurringMultipleTimes_NullInBetween()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4},"Property":null,"Property":{"a":1},"Property":{"b":2}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty<Dictionary<string, int>>>(json, options);
        CheckGenericDictionaryContent(obj.Property, 2);
    }

    internal class ClassWithReadOnlyPropertyDictionaryOfStringToInt
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public Dictionary<string, int> Property { get; } = new Dictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_DictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyDictionaryOfStringToIntWithoutPopulateAttribute)));
        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyDictionaryOfStringToIntWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_DictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyDictionaryOfStringToIntWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyDictionaryOfStringToIntWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_DictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyDictionaryOfStringToIntWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    internal class ClassWithReadOnlyPropertyDictionaryOfStringToIntWithoutPopulateAttribute
    {
        public Dictionary<string, int> Property { get; } = new Dictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal class ClassWithReadOnlyPropertyDictionaryOfStringToIntWithAttributeOnType
    {
        public Dictionary<string, int> Property { get; } = new Dictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_DictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyDictionaryOfStringToIntWithNumberHandling>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyDictionaryOfStringToIntWithNumberHandling));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_DictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyDictionaryOfStringToIntWithNumberHandlingWithAttributeOnType>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    internal struct ClassWithReadOnlyPropertyDictionaryOfStringToIntWithNumberHandling
    {
        public ClassWithReadOnlyPropertyDictionaryOfStringToIntWithNumberHandling() {}

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public Dictionary<string, int> Property { get; } = new Dictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_DictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute)));
        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_DictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_DictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    internal struct ClassWithReadOnlyPropertyDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute
    {
        public ClassWithReadOnlyPropertyDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute() {}

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public Dictionary<string, int> Property { get; } = new Dictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal struct ClassWithReadOnlyPropertyDictionaryOfStringToIntWithNumberHandlingWithAttributeOnType
    {
        public ClassWithReadOnlyPropertyDictionaryOfStringToIntWithNumberHandlingWithAttributeOnType() {}

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public Dictionary<string, int> Property { get; } = new Dictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_IDictionaryOfStringToInt_BackedBy_DictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToInt>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToInt));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_IDictionaryOfStringToInt_BackedBy_DictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToIntWithAttributeOnType>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_IDictionaryOfStringToInt_BackedBy_DictionaryOfStringToInt_PropertyOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4},"Property":{"e":5},"Property":{"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToInt>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_IDictionaryOfStringToInt_PropertyOccurringMultipleTimes_NullInBetween()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4},"Property":null,"Property":{"a":1},"Property":{"b":2}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty<IDictionary<string, int>>>(json, options);
        CheckGenericDictionaryContent(obj.Property, 2);
    }

    internal struct ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToInt
    {
        public ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToInt() {}

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public IDictionary<string, int> Property { get; } = new Dictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_IDictionaryOfStringToInt_BackedBy_DictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToIntWithoutPopulateAttribute)));
        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToIntWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_IDictionaryOfStringToInt_BackedBy_DictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToIntWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToIntWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_IDictionaryOfStringToInt_BackedBy_DictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToIntWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    internal struct ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToIntWithoutPopulateAttribute
    {
        public ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToIntWithoutPopulateAttribute() {}

        public IDictionary<string, int> Property { get; } = new Dictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal struct ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToIntWithAttributeOnType
    {
        public ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToIntWithAttributeOnType() {}

        public IDictionary<string, int> Property { get; } = new Dictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_IDictionaryOfStringToInt_BackedBy_DictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToIntWithNumberHandling>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToIntWithNumberHandling));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_IDictionaryOfStringToInt_BackedBy_DictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToIntWithNumberHandlingWithAttributeOnType>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    internal class ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToIntWithNumberHandling
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public IDictionary<string, int> Property { get; } = new Dictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_IDictionaryOfStringToInt_BackedBy_DictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute)));
        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_IDictionaryOfStringToInt_BackedBy_DictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_IDictionaryOfStringToInt_BackedBy_DictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    internal class ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public IDictionary<string, int> Property { get; } = new Dictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal class ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_DictionaryOfStringToIntWithNumberHandlingWithAttributeOnType
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public IDictionary<string, int> Property { get; } = new Dictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_IDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToInt>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        ((StructDictionary<string, int>)obj.Property).Validate();
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToInt));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_IDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithAttributeOnType>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        ((StructDictionary<string, int>)obj.Property).Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_IDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToInt_PropertyOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4},"Property":{"e":5},"Property":{"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToInt>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        ((StructDictionary<string, int>)obj.Property).Validate();
    }

    internal class ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToInt
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public IDictionary<string, int> Property { get; } = new StructDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_IDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithoutPopulateAttribute)));
        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        ((StructDictionary<string, int>)obj.Property).Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_IDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        ((StructDictionary<string, int>)obj.Property).Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_IDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        ((StructDictionary<string, int>)obj.Property).Validate();
    }

    internal class ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithoutPopulateAttribute
    {
        public IDictionary<string, int> Property { get; } = new StructDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal class ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithAttributeOnType
    {
        public IDictionary<string, int> Property { get; } = new StructDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_IDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithNumberHandling>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        ((StructDictionary<string, int>)obj.Property).Validate();
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithNumberHandling));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_IDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithNumberHandlingWithAttributeOnType>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        ((StructDictionary<string, int>)obj.Property).Validate();
    }

    internal struct ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithNumberHandling
    {
        public ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithNumberHandling() {}

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public IDictionary<string, int> Property { get; } = new StructDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_IDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute)));
        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        ((StructDictionary<string, int>)obj.Property).Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_IDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        ((StructDictionary<string, int>)obj.Property).Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_IDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        ((StructDictionary<string, int>)obj.Property).Validate();
    }

    internal struct ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute
    {
        public ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute() {}

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public IDictionary<string, int> Property { get; } = new StructDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal struct ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithNumberHandlingWithAttributeOnType
    {
        public ClassWithReadOnlyPropertyIDictionaryOfStringToInt_BackedBy_StructDictionaryOfStringToIntWithNumberHandlingWithAttributeOnType() {}

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public IDictionary<string, int> Property { get; } = new StructDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_IDictionary_BackedBy_DictionaryOfStringToJsonElement()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionary_BackedBy_DictionaryOfStringToJsonElement>(json, options);
        CheckDictionaryContent(obj.Property);
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyIDictionary_BackedBy_DictionaryOfStringToJsonElement));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_IDictionary_BackedBy_DictionaryOfStringToJsonElement()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionary_BackedBy_DictionaryOfStringToJsonElementWithAttributeOnType>(json, options);
        CheckDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_IDictionary_BackedBy_DictionaryOfStringToJsonElement_PropertyOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4},"Property":{"e":5},"Property":{"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionary_BackedBy_DictionaryOfStringToJsonElement>(json, options);
        CheckDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_IDictionary_PropertyOccurringMultipleTimes_NullInBetween()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4},"Property":null,"Property":{"a":1},"Property":{"b":2}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty<IDictionary>>(json, options);
        CheckDictionaryContent(obj.Property, 2);
    }

    internal struct ClassWithReadOnlyPropertyIDictionary_BackedBy_DictionaryOfStringToJsonElement
    {
        public ClassWithReadOnlyPropertyIDictionary_BackedBy_DictionaryOfStringToJsonElement() {}

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public IDictionary Property { get; } = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""{"a":1,"b":2,"c":3}""");
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_IDictionary_BackedBy_DictionaryOfStringToJsonElement()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyIDictionary_BackedBy_DictionaryOfStringToJsonElementWithoutPopulateAttribute)));
        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionary_BackedBy_DictionaryOfStringToJsonElementWithoutPopulateAttribute>(json, options);
        CheckDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_IDictionary_BackedBy_DictionaryOfStringToJsonElement()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyIDictionary_BackedBy_DictionaryOfStringToJsonElementWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionary_BackedBy_DictionaryOfStringToJsonElementWithoutPopulateAttribute>(json, options);
        CheckDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_IDictionary_BackedBy_DictionaryOfStringToJsonElement()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionary_BackedBy_DictionaryOfStringToJsonElementWithoutPopulateAttribute>(json, options);
        CheckDictionaryContent(obj.Property);
    }

    internal struct ClassWithReadOnlyPropertyIDictionary_BackedBy_DictionaryOfStringToJsonElementWithoutPopulateAttribute
    {
        public ClassWithReadOnlyPropertyIDictionary_BackedBy_DictionaryOfStringToJsonElementWithoutPopulateAttribute() {}

        public IDictionary Property { get; } = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""{"a":1,"b":2,"c":3}""");
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal struct ClassWithReadOnlyPropertyIDictionary_BackedBy_DictionaryOfStringToJsonElementWithAttributeOnType
    {
        public ClassWithReadOnlyPropertyIDictionary_BackedBy_DictionaryOfStringToJsonElementWithAttributeOnType() {}

        public IDictionary Property { get; } = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""{"a":1,"b":2,"c":3}""");
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_IDictionary_BackedBy_StructDictionaryOfStringToJsonElement()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionary_BackedBy_StructDictionaryOfStringToJsonElement>(json, options);
        CheckDictionaryContent(obj.Property);
        ((StructDictionary<string, JsonElement>)obj.Property).Validate();
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyIDictionary_BackedBy_StructDictionaryOfStringToJsonElement));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_IDictionary_BackedBy_StructDictionaryOfStringToJsonElement()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionary_BackedBy_StructDictionaryOfStringToJsonElementWithAttributeOnType>(json, options);
        CheckDictionaryContent(obj.Property);
        ((StructDictionary<string, JsonElement>)obj.Property).Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_IDictionary_BackedBy_StructDictionaryOfStringToJsonElement_PropertyOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4},"Property":{"e":5},"Property":{"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionary_BackedBy_StructDictionaryOfStringToJsonElement>(json, options);
        CheckDictionaryContent(obj.Property);
        ((StructDictionary<string, JsonElement>)obj.Property).Validate();
    }

    internal class ClassWithReadOnlyPropertyIDictionary_BackedBy_StructDictionaryOfStringToJsonElement
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public IDictionary Property { get; } = JsonSerializer.Deserialize<StructDictionary<string, JsonElement>>("""{"a":1,"b":2,"c":3}""");
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_IDictionary_BackedBy_StructDictionaryOfStringToJsonElement()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyIDictionary_BackedBy_StructDictionaryOfStringToJsonElementWithoutPopulateAttribute)));
        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionary_BackedBy_StructDictionaryOfStringToJsonElementWithoutPopulateAttribute>(json, options);
        CheckDictionaryContent(obj.Property);
        ((StructDictionary<string, JsonElement>)obj.Property).Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_IDictionary_BackedBy_StructDictionaryOfStringToJsonElement()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyIDictionary_BackedBy_StructDictionaryOfStringToJsonElementWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionary_BackedBy_StructDictionaryOfStringToJsonElementWithoutPopulateAttribute>(json, options);
        CheckDictionaryContent(obj.Property);
        ((StructDictionary<string, JsonElement>)obj.Property).Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_IDictionary_BackedBy_StructDictionaryOfStringToJsonElement()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIDictionary_BackedBy_StructDictionaryOfStringToJsonElementWithoutPopulateAttribute>(json, options);
        CheckDictionaryContent(obj.Property);
        ((StructDictionary<string, JsonElement>)obj.Property).Validate();
    }

    internal class ClassWithReadOnlyPropertyIDictionary_BackedBy_StructDictionaryOfStringToJsonElementWithoutPopulateAttribute
    {
        public IDictionary Property { get; } = JsonSerializer.Deserialize<StructDictionary<string, JsonElement>>("""{"a":1,"b":2,"c":3}""");
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal class ClassWithReadOnlyPropertyIDictionary_BackedBy_StructDictionaryOfStringToJsonElementWithAttributeOnType
    {
        public IDictionary Property { get; } = JsonSerializer.Deserialize<StructDictionary<string, JsonElement>>("""{"a":1,"b":2,"c":3}""");
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_StructDictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructDictionaryOfStringToInt>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        obj.Property.Validate();
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(StructWithWritablePropertyStructDictionaryOfStringToInt));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_StructDictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructDictionaryOfStringToIntWithAttributeOnType>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        obj.Property.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_StructDictionaryOfStringToInt_PropertyOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4},"Property":{"e":5},"Property":{"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructDictionaryOfStringToInt>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        obj.Property.Validate();
    }

    internal class StructWithWritablePropertyStructDictionaryOfStringToInt
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public StructDictionary<string, int> Property { get; set; } = new StructDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_StructDictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(StructWithWritablePropertyStructDictionaryOfStringToIntWithoutPopulateAttribute)));
        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructDictionaryOfStringToIntWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        obj.Property.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_StructDictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(StructWithWritablePropertyStructDictionaryOfStringToIntWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructDictionaryOfStringToIntWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        obj.Property.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_StructDictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructDictionaryOfStringToIntWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        obj.Property.Validate();
    }

    internal class StructWithWritablePropertyStructDictionaryOfStringToIntWithoutPopulateAttribute
    {
        public StructDictionary<string, int> Property { get; set; } = new StructDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal class StructWithWritablePropertyStructDictionaryOfStringToIntWithAttributeOnType
    {
        public StructDictionary<string, int> Property { get; set; } = new StructDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_StructDictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructDictionaryOfStringToIntWithNumberHandling>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        obj.Property.Validate();
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(StructWithWritablePropertyStructDictionaryOfStringToIntWithNumberHandling));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_StructDictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructDictionaryOfStringToIntWithNumberHandlingWithAttributeOnType>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        obj.Property.Validate();
    }

    internal struct StructWithWritablePropertyStructDictionaryOfStringToIntWithNumberHandling
    {
        public StructWithWritablePropertyStructDictionaryOfStringToIntWithNumberHandling() {}

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public StructDictionary<string, int> Property { get; set; } = new StructDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_StructDictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(StructWithWritablePropertyStructDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute)));
        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        obj.Property.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_StructDictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(StructWithWritablePropertyStructDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        obj.Property.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_StructDictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        obj.Property.Validate();
    }

    internal struct StructWithWritablePropertyStructDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute
    {
        public StructWithWritablePropertyStructDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute() {}

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public StructDictionary<string, int> Property { get; set; } = new StructDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal struct StructWithWritablePropertyStructDictionaryOfStringToIntWithNumberHandlingWithAttributeOnType
    {
        public StructWithWritablePropertyStructDictionaryOfStringToIntWithNumberHandlingWithAttributeOnType() {}

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public StructDictionary<string, int> Property { get; set; } = new StructDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_NullableStructDictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true);
        string json = """{"Field":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructDictionaryOfStringToInt>(json, options);
        CheckGenericDictionaryContent(obj.Field.Value);
        obj.Field.Value.Validate();
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(StructWithWritableFieldNullableStructDictionaryOfStringToInt));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_NullableStructDictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true);
        string json = """{"Field":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructDictionaryOfStringToIntWithAttributeOnType>(json, options);
        CheckGenericDictionaryContent(obj.Field.Value);
        obj.Field.Value.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_PopulatedFieldCanDeserializeNull_NullableStructDictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true);
        string json = """{"Field":null}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructDictionaryOfStringToInt>(json, options);
        Assert.Null(obj.Field);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_NullableStructDictionaryOfStringToInt_FieldOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true);
        string json = """{"Field":{"d":4},"Field":{"e":5},"Field":{"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructDictionaryOfStringToInt>(json, options);
        CheckGenericDictionaryContent(obj.Field.Value);
        obj.Field.Value.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_NullableStructDictionaryOfStringToInt_FieldOccurringMultipleTimes_NullInBetween()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true);
        string json = """{"Field":{"d":4},"Field":null,"Field":{"a":1},"Field":{"b":2}}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructDictionaryOfStringToInt>(json, options);
        CheckGenericDictionaryContent(obj.Field.Value, 2);
        obj.Field.Value.Validate();
    }

    internal struct StructWithWritableFieldNullableStructDictionaryOfStringToInt
    {
        public StructWithWritableFieldNullableStructDictionaryOfStringToInt() {}

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public StructDictionary<string, int>? Field = new StructDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_NullableStructDictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(StructWithWritableFieldNullableStructDictionaryOfStringToIntWithoutPopulateAttribute)), includeFields: true);
        string json = """{"Field":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructDictionaryOfStringToIntWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Field.Value);
        obj.Field.Value.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_NullableStructDictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true, modifier: ti =>
        {
            if (ti.Type == typeof(StructWithWritableFieldNullableStructDictionaryOfStringToIntWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Field":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructDictionaryOfStringToIntWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Field.Value);
        obj.Field.Value.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_NullableStructDictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true, configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Field":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructDictionaryOfStringToIntWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Field.Value);
        obj.Field.Value.Validate();
    }

    internal struct StructWithWritableFieldNullableStructDictionaryOfStringToIntWithoutPopulateAttribute
    {
        public StructWithWritableFieldNullableStructDictionaryOfStringToIntWithoutPopulateAttribute() {}

        public StructDictionary<string, int>? Field = new StructDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal struct StructWithWritableFieldNullableStructDictionaryOfStringToIntWithAttributeOnType
    {
        public StructWithWritableFieldNullableStructDictionaryOfStringToIntWithAttributeOnType() {}

        public StructDictionary<string, int>? Field = new StructDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ConcurrentDictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToInt>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToInt));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_ConcurrentDictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToIntWithAttributeOnType>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ConcurrentDictionaryOfStringToInt_PropertyOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4},"Property":{"e":5},"Property":{"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToInt>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ConcurrentDictionaryOfStringToInt_PropertyOccurringMultipleTimes_NullInBetween()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4},"Property":null,"Property":{"a":1},"Property":{"b":2}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty<ConcurrentDictionary<string, int>>>(json, options);
        CheckGenericDictionaryContent(obj.Property, 2);
    }

    internal class ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToInt
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public ConcurrentDictionary<string, int> Property { get; } = new ConcurrentDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_ConcurrentDictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToIntWithoutPopulateAttribute)));
        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToIntWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_ConcurrentDictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToIntWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToIntWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_ConcurrentDictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToIntWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    internal class ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToIntWithoutPopulateAttribute
    {
        public ConcurrentDictionary<string, int> Property { get; } = new ConcurrentDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal class ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToIntWithAttributeOnType
    {
        public ConcurrentDictionary<string, int> Property { get; } = new ConcurrentDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ConcurrentDictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToIntWithNumberHandling>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToIntWithNumberHandling));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_ConcurrentDictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToIntWithNumberHandlingWithAttributeOnType>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    internal class ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToIntWithNumberHandling
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public ConcurrentDictionary<string, int> Property { get; } = new ConcurrentDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_ConcurrentDictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute)));
        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_ConcurrentDictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_ConcurrentDictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    internal class ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public ConcurrentDictionary<string, int> Property { get; } = new ConcurrentDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal class ClassWithReadOnlyPropertyConcurrentDictionaryOfStringToIntWithNumberHandlingWithAttributeOnType
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public ConcurrentDictionary<string, int> Property { get; } = new ConcurrentDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_SortedDictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertySortedDictionaryOfStringToInt>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertySortedDictionaryOfStringToInt));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_SortedDictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithAttributeOnType>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_SortedDictionaryOfStringToInt_PropertyOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4},"Property":{"e":5},"Property":{"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertySortedDictionaryOfStringToInt>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_SortedDictionaryOfStringToInt_PropertyOccurringMultipleTimes_NullInBetween()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":4},"Property":null,"Property":{"a":1},"Property":{"b":2}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty<SortedDictionary<string, int>>>(json, options);
        CheckGenericDictionaryContent(obj.Property, 2);
    }

    internal struct ClassWithReadOnlyPropertySortedDictionaryOfStringToInt
    {
        public ClassWithReadOnlyPropertySortedDictionaryOfStringToInt() {}

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public SortedDictionary<string, int> Property { get; } = new SortedDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_SortedDictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithoutPopulateAttribute)));
        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_SortedDictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_SortedDictionaryOfStringToInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":{"d":4,"e":5,"f":6}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    internal struct ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithoutPopulateAttribute
    {
        public ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithoutPopulateAttribute() {}

        public SortedDictionary<string, int> Property { get; } = new SortedDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal struct ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithAttributeOnType
    {
        public ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithAttributeOnType() {}

        public SortedDictionary<string, int> Property { get; } = new SortedDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_SortedDictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithNumberHandling>(json, options);
        CheckGenericDictionaryContent(obj.Property);
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithNumberHandling));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_SortedDictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithNumberHandlingWithAttributeOnType>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    internal struct ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithNumberHandling
    {
        public ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithNumberHandling() {}

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public SortedDictionary<string, int> Property { get; } = new SortedDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_SortedDictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute)));
        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_SortedDictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_SortedDictionaryOfStringToInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":{"d":"4","e":"5","f":"6"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        CheckGenericDictionaryContent(obj.Property);
    }

    internal struct ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute
    {
        public ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithNumberHandlingWithoutPopulateAttribute() {}

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public SortedDictionary<string, int> Property { get; } = new SortedDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal struct ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithNumberHandlingWithAttributeOnType
    {
        public ClassWithReadOnlyPropertySortedDictionaryOfStringToIntWithNumberHandlingWithAttributeOnType() {}

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public SortedDictionary<string, int> Property { get; } = new SortedDictionary<string, int>() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
    }

    [Theory]
    [InlineData(typeof(ClassWithReadOnlyProperty<StructDictionary<string, int>>))]
    [InlineData(typeof(ClassWithReadOnlyProperty<StructDictionary<string, int>?>))]
    public async Task CreationHandlingSetWithAttribute_PopulateWithoutSetterOnValueTypeThrows_Dictionary(Type type)
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = "{}";
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper(json, type, options));
    }
}
