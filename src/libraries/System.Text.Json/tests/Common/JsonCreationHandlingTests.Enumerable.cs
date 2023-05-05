// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests;

public abstract partial class JsonCreationHandlingTests : SerializerTests
{
    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyListOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyListOfInt));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_ListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyListOfIntWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ListOfInt_PropertyOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":[5],"Property":[6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyListOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ListOfInt_PropertyOccurringMultipleTimes_NullInBetween()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":null,"Property":[1],"Property":[2]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty<List<int>>>(json, options);
        Assert.Equal(Enumerable.Range(1, 2), obj.Property);
    }

    internal class ClassWithReadOnlyPropertyListOfInt
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public List<int> Property { get; } = new List<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_ListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyListOfIntWithoutPopulateAttribute)));
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyListOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_ListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyListOfIntWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyListOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_ListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyListOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    internal class ClassWithReadOnlyPropertyListOfIntWithoutPopulateAttribute
    {
        public List<int> Property { get; } = new List<int>() { 1, 2, 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal class ClassWithReadOnlyPropertyListOfIntWithAttributeOnType
    {
        public List<int> Property { get; } = new List<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ListOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyListOfIntWithNumberHandling>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyListOfIntWithNumberHandling));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_ListOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyListOfIntWithNumberHandlingWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    internal struct ClassWithReadOnlyPropertyListOfIntWithNumberHandling
    {
        public ClassWithReadOnlyPropertyListOfIntWithNumberHandling() {}

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public List<int> Property { get; } = new List<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_ListOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyListOfIntWithNumberHandlingWithoutPopulateAttribute)));
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyListOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_ListOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyListOfIntWithNumberHandlingWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyListOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_ListOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyListOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    internal struct ClassWithReadOnlyPropertyListOfIntWithNumberHandlingWithoutPopulateAttribute
    {
        public ClassWithReadOnlyPropertyListOfIntWithNumberHandlingWithoutPopulateAttribute() {}

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public List<int> Property { get; } = new List<int>() { 1, 2, 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal struct ClassWithReadOnlyPropertyListOfIntWithNumberHandlingWithAttributeOnType
    {
        public ClassWithReadOnlyPropertyListOfIntWithNumberHandlingWithAttributeOnType() {}

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public List<int> Property { get; } = new List<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_IListOfInt_BackedBy_ListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfInt));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_IListOfInt_BackedBy_ListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfIntWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_IListOfInt_BackedBy_ListOfInt_PropertyOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":[5],"Property":[6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_IListOfInt_PropertyOccurringMultipleTimes_NullInBetween()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":null,"Property":[1],"Property":[2]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty<IList<int>>>(json, options);
        Assert.Equal(Enumerable.Range(1, 2), obj.Property);
    }

    internal struct ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfInt
    {
        public ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfInt() {}

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public IList<int> Property { get; } = new List<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_IListOfInt_BackedBy_ListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfIntWithoutPopulateAttribute)));
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_IListOfInt_BackedBy_ListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfIntWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_IListOfInt_BackedBy_ListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    internal struct ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfIntWithoutPopulateAttribute
    {
        public ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfIntWithoutPopulateAttribute() {}

        public IList<int> Property { get; } = new List<int>() { 1, 2, 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal struct ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfIntWithAttributeOnType
    {
        public ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfIntWithAttributeOnType() {}

        public IList<int> Property { get; } = new List<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_IListOfInt_BackedBy_ListOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfIntWithNumberHandling>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfIntWithNumberHandling));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_IListOfInt_BackedBy_ListOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfIntWithNumberHandlingWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    internal class ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfIntWithNumberHandling
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public IList<int> Property { get; } = new List<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_IListOfInt_BackedBy_ListOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfIntWithNumberHandlingWithoutPopulateAttribute)));
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_IListOfInt_BackedBy_ListOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfIntWithNumberHandlingWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_IListOfInt_BackedBy_ListOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    internal class ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfIntWithNumberHandlingWithoutPopulateAttribute
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public IList<int> Property { get; } = new List<int>() { 1, 2, 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal class ClassWithReadOnlyPropertyIListOfInt_BackedBy_ListOfIntWithNumberHandlingWithAttributeOnType
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public IList<int> Property { get; } = new List<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_IListOfInt_BackedBy_StructListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructList<int>)obj.Property).Validate();
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfInt));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_IListOfInt_BackedBy_StructListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructList<int>)obj.Property).Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_IListOfInt_BackedBy_StructListOfInt_PropertyOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":[5],"Property":[6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructList<int>)obj.Property).Validate();
    }

    internal class ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfInt
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public IList<int> Property { get; } = new StructList<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_IListOfInt_BackedBy_StructListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithoutPopulateAttribute)));
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructList<int>)obj.Property).Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_IListOfInt_BackedBy_StructListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructList<int>)obj.Property).Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_IListOfInt_BackedBy_StructListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructList<int>)obj.Property).Validate();
    }

    internal class ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithoutPopulateAttribute
    {
        public IList<int> Property { get; } = new StructList<int>() { 1, 2, 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal class ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithAttributeOnType
    {
        public IList<int> Property { get; } = new StructList<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_IListOfInt_BackedBy_StructListOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithNumberHandling>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructList<int>)obj.Property).Validate();
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithNumberHandling));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_IListOfInt_BackedBy_StructListOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithNumberHandlingWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructList<int>)obj.Property).Validate();
    }

    internal struct ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithNumberHandling
    {
        public ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithNumberHandling() {}

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public IList<int> Property { get; } = new StructList<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_IListOfInt_BackedBy_StructListOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithNumberHandlingWithoutPopulateAttribute)));
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructList<int>)obj.Property).Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_IListOfInt_BackedBy_StructListOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithNumberHandlingWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructList<int>)obj.Property).Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_IListOfInt_BackedBy_StructListOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructList<int>)obj.Property).Validate();
    }

    internal struct ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithNumberHandlingWithoutPopulateAttribute
    {
        public ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithNumberHandlingWithoutPopulateAttribute() {}

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public IList<int> Property { get; } = new StructList<int>() { 1, 2, 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal struct ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithNumberHandlingWithAttributeOnType
    {
        public ClassWithReadOnlyPropertyIListOfInt_BackedBy_StructListOfIntWithNumberHandlingWithAttributeOnType() {}

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public IList<int> Property { get; } = new StructList<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_IList_BackedBy_ListOfJsonElement()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIList_BackedBy_ListOfJsonElement>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyIList_BackedBy_ListOfJsonElement));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_IList_BackedBy_ListOfJsonElement()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIList_BackedBy_ListOfJsonElementWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_IList_BackedBy_ListOfJsonElement_PropertyOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":[5],"Property":[6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIList_BackedBy_ListOfJsonElement>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_IList_PropertyOccurringMultipleTimes_NullInBetween()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":null,"Property":[1],"Property":[2]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty<IList>>(json, options);
        Assert.Equal(Enumerable.Range(1, 2), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
    }

    internal struct ClassWithReadOnlyPropertyIList_BackedBy_ListOfJsonElement
    {
        public ClassWithReadOnlyPropertyIList_BackedBy_ListOfJsonElement() {}

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public IList Property { get; } = JsonSerializer.Deserialize<List<JsonElement>>("[1,2,3]");
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_IList_BackedBy_ListOfJsonElement()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyIList_BackedBy_ListOfJsonElementWithoutPopulateAttribute)));
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIList_BackedBy_ListOfJsonElementWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_IList_BackedBy_ListOfJsonElement()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyIList_BackedBy_ListOfJsonElementWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIList_BackedBy_ListOfJsonElementWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_IList_BackedBy_ListOfJsonElement()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIList_BackedBy_ListOfJsonElementWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
    }

    internal struct ClassWithReadOnlyPropertyIList_BackedBy_ListOfJsonElementWithoutPopulateAttribute
    {
        public ClassWithReadOnlyPropertyIList_BackedBy_ListOfJsonElementWithoutPopulateAttribute() {}

        public IList Property { get; } = JsonSerializer.Deserialize<List<JsonElement>>("[1,2,3]");
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal struct ClassWithReadOnlyPropertyIList_BackedBy_ListOfJsonElementWithAttributeOnType
    {
        public ClassWithReadOnlyPropertyIList_BackedBy_ListOfJsonElementWithAttributeOnType() {}

        public IList Property { get; } = JsonSerializer.Deserialize<List<JsonElement>>("[1,2,3]");
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_IList_BackedBy_StructListOfJsonElement()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIList_BackedBy_StructListOfJsonElement>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
        ((StructList<JsonElement>)obj.Property).Validate();
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyIList_BackedBy_StructListOfJsonElement));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_IList_BackedBy_StructListOfJsonElement()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIList_BackedBy_StructListOfJsonElementWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
        ((StructList<JsonElement>)obj.Property).Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_IList_BackedBy_StructListOfJsonElement_PropertyOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":[5],"Property":[6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIList_BackedBy_StructListOfJsonElement>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
        ((StructList<JsonElement>)obj.Property).Validate();
    }

    internal class ClassWithReadOnlyPropertyIList_BackedBy_StructListOfJsonElement
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public IList Property { get; } = JsonSerializer.Deserialize<StructList<JsonElement>>("[1,2,3]");
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_IList_BackedBy_StructListOfJsonElement()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyIList_BackedBy_StructListOfJsonElementWithoutPopulateAttribute)));
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIList_BackedBy_StructListOfJsonElementWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
        ((StructList<JsonElement>)obj.Property).Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_IList_BackedBy_StructListOfJsonElement()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyIList_BackedBy_StructListOfJsonElementWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIList_BackedBy_StructListOfJsonElementWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
        ((StructList<JsonElement>)obj.Property).Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_IList_BackedBy_StructListOfJsonElement()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyIList_BackedBy_StructListOfJsonElementWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
        ((StructList<JsonElement>)obj.Property).Validate();
    }

    internal class ClassWithReadOnlyPropertyIList_BackedBy_StructListOfJsonElementWithoutPopulateAttribute
    {
        public IList Property { get; } = JsonSerializer.Deserialize<StructList<JsonElement>>("[1,2,3]");
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal class ClassWithReadOnlyPropertyIList_BackedBy_StructListOfJsonElementWithAttributeOnType
    {
        public IList Property { get; } = JsonSerializer.Deserialize<StructList<JsonElement>>("[1,2,3]");
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_StructListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructListOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(StructWithWritablePropertyStructListOfInt));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_StructListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructListOfIntWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_StructListOfInt_PropertyOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":[5],"Property":[6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructListOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
    }

    internal class StructWithWritablePropertyStructListOfInt
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public StructList<int> Property { get; set; } = new StructList<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_StructListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(StructWithWritablePropertyStructListOfIntWithoutPopulateAttribute)));
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructListOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_StructListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(StructWithWritablePropertyStructListOfIntWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructListOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_StructListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructListOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
    }

    internal class StructWithWritablePropertyStructListOfIntWithoutPopulateAttribute
    {
        public StructList<int> Property { get; set; } = new StructList<int>() { 1, 2, 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal class StructWithWritablePropertyStructListOfIntWithAttributeOnType
    {
        public StructList<int> Property { get; set; } = new StructList<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_StructListOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructListOfIntWithNumberHandling>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(StructWithWritablePropertyStructListOfIntWithNumberHandling));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_StructListOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructListOfIntWithNumberHandlingWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
    }

    internal struct StructWithWritablePropertyStructListOfIntWithNumberHandling
    {
        public StructWithWritablePropertyStructListOfIntWithNumberHandling() {}

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public StructList<int> Property { get; set; } = new StructList<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_StructListOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(StructWithWritablePropertyStructListOfIntWithNumberHandlingWithoutPopulateAttribute)));
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructListOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_StructListOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(StructWithWritablePropertyStructListOfIntWithNumberHandlingWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructListOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_StructListOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructListOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
    }

    internal struct StructWithWritablePropertyStructListOfIntWithNumberHandlingWithoutPopulateAttribute
    {
        public StructWithWritablePropertyStructListOfIntWithNumberHandlingWithoutPopulateAttribute() {}

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public StructList<int> Property { get; set; } = new StructList<int>() { 1, 2, 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal struct StructWithWritablePropertyStructListOfIntWithNumberHandlingWithAttributeOnType
    {
        public StructWithWritablePropertyStructListOfIntWithNumberHandlingWithAttributeOnType() {}

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public StructList<int> Property { get; set; } = new StructList<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_NullableStructListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true);
        string json = """{"Field":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructListOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Field.Value);
        obj.Field.Value.Validate();
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(StructWithWritableFieldNullableStructListOfInt));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_NullableStructListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true);
        string json = """{"Field":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructListOfIntWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Field.Value);
        obj.Field.Value.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_NullableStructListOfInt_FieldOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true);
        string json = """{"Field":[4],"Field":[5],"Field":[6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructListOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Field.Value);
        obj.Field.Value.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_NullableStructListOfInt_FieldOccurringMultipleTimes_NullInBetween()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true);
        string json = """{"Field":[4],"Field":null,"Field":[1],"Field":[2]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructListOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 2), obj.Field.Value);
        obj.Field.Value.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_PopulatedFieldCanDeserializeNull_NullableStructListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true);
        string json = """{"Field":null}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructListOfInt>(json, options);
        Assert.Null(obj.Field);
    }

    internal struct StructWithWritableFieldNullableStructListOfInt
    {
        public StructWithWritableFieldNullableStructListOfInt() {}

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public StructList<int>? Field = new StructList<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_NullableStructListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(StructWithWritableFieldNullableStructListOfIntWithoutPopulateAttribute)), includeFields: true);
        string json = """{"Field":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructListOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Field.Value);
        obj.Field.Value.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_NullableStructListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true, modifier: ti =>
        {
            if (ti.Type == typeof(StructWithWritableFieldNullableStructListOfIntWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Field":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructListOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Field.Value);
        obj.Field.Value.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_NullableStructListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true, configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Field":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructListOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Field.Value);
        obj.Field.Value.Validate();
    }

    internal struct StructWithWritableFieldNullableStructListOfIntWithoutPopulateAttribute
    {
        public StructWithWritableFieldNullableStructListOfIntWithoutPopulateAttribute() {}

        public StructList<int>? Field = new StructList<int>() { 1, 2, 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal struct StructWithWritableFieldNullableStructListOfIntWithAttributeOnType
    {
        public StructWithWritableFieldNullableStructListOfIntWithAttributeOnType() {}

        public StructList<int>? Field = new StructList<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_QueueOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyQueueOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyQueueOfInt));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_QueueOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyQueueOfIntWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_QueueOfInt_PropertyOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":[5],"Property":[6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyQueueOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_QueueOfInt_PropertyOccurringMultipleTimes_NullInBetween()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":null,"Property":[1],"Property":[2]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty<Queue<int>>>(json, options);
        Assert.Equal(Enumerable.Range(1, 2), obj.Property);
    }

    internal class ClassWithReadOnlyPropertyQueueOfInt
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public Queue<int> Property { get; } = new Queue<int>(new int[] { 1, 2, 3 });
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_QueueOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyQueueOfIntWithoutPopulateAttribute)));
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyQueueOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_QueueOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyQueueOfIntWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyQueueOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_QueueOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyQueueOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    internal class ClassWithReadOnlyPropertyQueueOfIntWithoutPopulateAttribute
    {
        public Queue<int> Property { get; } = new Queue<int>(new int[] { 1, 2, 3 });
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal class ClassWithReadOnlyPropertyQueueOfIntWithAttributeOnType
    {
        public Queue<int> Property { get; } = new Queue<int>(new int[] { 1, 2, 3 });
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_QueueOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyQueueOfIntWithNumberHandling>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyQueueOfIntWithNumberHandling));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_QueueOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyQueueOfIntWithNumberHandlingWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    internal class ClassWithReadOnlyPropertyQueueOfIntWithNumberHandling
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public Queue<int> Property { get; } = new Queue<int>(new int[] { 1, 2, 3 });
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_QueueOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyQueueOfIntWithNumberHandlingWithoutPopulateAttribute)));
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyQueueOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_QueueOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyQueueOfIntWithNumberHandlingWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyQueueOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_QueueOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyQueueOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    internal class ClassWithReadOnlyPropertyQueueOfIntWithNumberHandlingWithoutPopulateAttribute
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public Queue<int> Property { get; } = new Queue<int>(new int[] { 1, 2, 3 });
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal class ClassWithReadOnlyPropertyQueueOfIntWithNumberHandlingWithAttributeOnType
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public Queue<int> Property { get; } = new Queue<int>(new int[] { 1, 2, 3 });
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_Queue()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyQueue>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyQueue));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_Queue()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyQueueWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_Queue_PropertyOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":[5],"Property":[6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyQueue>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_Queue_PropertyOccurringMultipleTimes_NullInBetween()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":null,"Property":[1],"Property":[2]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty<Queue>>(json, options);
        Assert.Equal(Enumerable.Range(1, 2), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
    }

    internal struct ClassWithReadOnlyPropertyQueue
    {
        public ClassWithReadOnlyPropertyQueue() {}

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public Queue Property { get; } = JsonSerializer.Deserialize<Queue>("[1,2,3]");
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_Queue()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyQueueWithoutPopulateAttribute)));
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyQueueWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_Queue()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyQueueWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyQueueWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_Queue()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyQueueWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
    }

    internal struct ClassWithReadOnlyPropertyQueueWithoutPopulateAttribute
    {
        public ClassWithReadOnlyPropertyQueueWithoutPopulateAttribute() {}

        public Queue Property { get; } = JsonSerializer.Deserialize<Queue>("[1,2,3]");
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal struct ClassWithReadOnlyPropertyQueueWithAttributeOnType
    {
        public ClassWithReadOnlyPropertyQueueWithAttributeOnType() {}

        public Queue Property { get; } = JsonSerializer.Deserialize<Queue>("[1,2,3]");
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ConcurrentQueueOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentQueueOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyConcurrentQueueOfInt));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_ConcurrentQueueOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentQueueOfIntWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ConcurrentQueueOfInt_PropertyOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":[5],"Property":[6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentQueueOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ConcurrentQueueOfInt_PropertyOccurringMultipleTimes_NullInBetween()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":null,"Property":[1],"Property":[2]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty<ConcurrentQueue<int>>>(json, options);
        Assert.Equal(Enumerable.Range(1, 2), obj.Property);
    }

    internal struct ClassWithReadOnlyPropertyConcurrentQueueOfInt
    {
        public ClassWithReadOnlyPropertyConcurrentQueueOfInt() {}

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public ConcurrentQueue<int> Property { get; } = new ConcurrentQueue<int>(new int[] { 1, 2, 3 });
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_ConcurrentQueueOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyConcurrentQueueOfIntWithoutPopulateAttribute)));
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentQueueOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_ConcurrentQueueOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyConcurrentQueueOfIntWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentQueueOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_ConcurrentQueueOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentQueueOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    internal struct ClassWithReadOnlyPropertyConcurrentQueueOfIntWithoutPopulateAttribute
    {
        public ClassWithReadOnlyPropertyConcurrentQueueOfIntWithoutPopulateAttribute() {}

        public ConcurrentQueue<int> Property { get; } = new ConcurrentQueue<int>(new int[] { 1, 2, 3 });
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal struct ClassWithReadOnlyPropertyConcurrentQueueOfIntWithAttributeOnType
    {
        public ClassWithReadOnlyPropertyConcurrentQueueOfIntWithAttributeOnType() {}

        public ConcurrentQueue<int> Property { get; } = new ConcurrentQueue<int>(new int[] { 1, 2, 3 });
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ConcurrentQueueOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentQueueOfIntWithNumberHandling>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyConcurrentQueueOfIntWithNumberHandling));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_ConcurrentQueueOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentQueueOfIntWithNumberHandlingWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    internal class ClassWithReadOnlyPropertyConcurrentQueueOfIntWithNumberHandling
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public ConcurrentQueue<int> Property { get; } = new ConcurrentQueue<int>(new int[] { 1, 2, 3 });
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_ConcurrentQueueOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyConcurrentQueueOfIntWithNumberHandlingWithoutPopulateAttribute)));
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentQueueOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_ConcurrentQueueOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyConcurrentQueueOfIntWithNumberHandlingWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentQueueOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_ConcurrentQueueOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentQueueOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    internal class ClassWithReadOnlyPropertyConcurrentQueueOfIntWithNumberHandlingWithoutPopulateAttribute
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public ConcurrentQueue<int> Property { get; } = new ConcurrentQueue<int>(new int[] { 1, 2, 3 });
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal class ClassWithReadOnlyPropertyConcurrentQueueOfIntWithNumberHandlingWithAttributeOnType
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public ConcurrentQueue<int> Property { get; } = new ConcurrentQueue<int>(new int[] { 1, 2, 3 });
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_StackOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyStackOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property);
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyStackOfInt));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_StackOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyStackOfIntWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_StackOfInt_PropertyOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":[5],"Property":[6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyStackOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_StackOfInt_PropertyOccurringMultipleTimes_NullInBetween()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":null,"Property":[1],"Property":[2]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty<Stack<int>>>(json, options);
        Assert.Equal(Enumerable.Range(1, 2).Reverse(), obj.Property);
    }

    internal class ClassWithReadOnlyPropertyStackOfInt
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public Stack<int> Property { get; } = new Stack<int>(new int[] { 1, 2, 3 });
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_StackOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyStackOfIntWithoutPopulateAttribute)));
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyStackOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_StackOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyStackOfIntWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyStackOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_StackOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyStackOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property);
    }

    internal class ClassWithReadOnlyPropertyStackOfIntWithoutPopulateAttribute
    {
        public Stack<int> Property { get; } = new Stack<int>(new int[] { 1, 2, 3 });
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal class ClassWithReadOnlyPropertyStackOfIntWithAttributeOnType
    {
        public Stack<int> Property { get; } = new Stack<int>(new int[] { 1, 2, 3 });
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_StackOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyStackOfIntWithNumberHandling>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property);
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyStackOfIntWithNumberHandling));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_StackOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyStackOfIntWithNumberHandlingWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property);
    }

    internal struct ClassWithReadOnlyPropertyStackOfIntWithNumberHandling
    {
        public ClassWithReadOnlyPropertyStackOfIntWithNumberHandling() {}

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public Stack<int> Property { get; } = new Stack<int>(new int[] { 1, 2, 3 });
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_StackOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyStackOfIntWithNumberHandlingWithoutPopulateAttribute)));
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyStackOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_StackOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyStackOfIntWithNumberHandlingWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyStackOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_StackOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyStackOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property);
    }

    internal struct ClassWithReadOnlyPropertyStackOfIntWithNumberHandlingWithoutPopulateAttribute
    {
        public ClassWithReadOnlyPropertyStackOfIntWithNumberHandlingWithoutPopulateAttribute() {}

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public Stack<int> Property { get; } = new Stack<int>(new int[] { 1, 2, 3 });
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal struct ClassWithReadOnlyPropertyStackOfIntWithNumberHandlingWithAttributeOnType
    {
        public ClassWithReadOnlyPropertyStackOfIntWithNumberHandlingWithAttributeOnType() {}

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public Stack<int> Property { get; } = new Stack<int>(new int[] { 1, 2, 3 });
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_Stack()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyStack>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyStack));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_Stack()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyStackWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_Stack_PropertyOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":[5],"Property":[6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyStack>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_Stack_PropertyOccurringMultipleTimes_NullInBetween()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":null,"Property":[1],"Property":[2]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty<Stack>>(json, options);
        Assert.Equal(Enumerable.Range(1, 2).Reverse(), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
    }

    internal struct ClassWithReadOnlyPropertyStack
    {
        public ClassWithReadOnlyPropertyStack() {}

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public Stack Property { get; } = JsonSerializer.Deserialize<Stack>("[1,2,3]");
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_Stack()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyStackWithoutPopulateAttribute)));
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyStackWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_Stack()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyStackWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyStackWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_Stack()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyStackWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property.Cast<JsonElement>().Select(x => x.GetInt32()));
    }

    internal struct ClassWithReadOnlyPropertyStackWithoutPopulateAttribute
    {
        public ClassWithReadOnlyPropertyStackWithoutPopulateAttribute() {}

        public Stack Property { get; } = JsonSerializer.Deserialize<Stack>("[1,2,3]");
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal struct ClassWithReadOnlyPropertyStackWithAttributeOnType
    {
        public ClassWithReadOnlyPropertyStackWithAttributeOnType() {}

        public Stack Property { get; } = JsonSerializer.Deserialize<Stack>("[1,2,3]");
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ConcurrentStackOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentStackOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property);
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyConcurrentStackOfInt));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_ConcurrentStackOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentStackOfIntWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ConcurrentStackOfInt_PropertyOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":[5],"Property":[6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentStackOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ConcurrentStackOfInt_PropertyOccurringMultipleTimes_NullInBetween()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":null,"Property":[1],"Property":[2]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty<ConcurrentStack<int>>>(json, options);
        Assert.Equal(Enumerable.Range(1, 2).Reverse(), obj.Property);
    }

    internal class ClassWithReadOnlyPropertyConcurrentStackOfInt
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public ConcurrentStack<int> Property { get; } = new ConcurrentStack<int>(new int[] { 1, 2, 3 });
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_ConcurrentStackOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyConcurrentStackOfIntWithoutPopulateAttribute)));
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentStackOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_ConcurrentStackOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyConcurrentStackOfIntWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentStackOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_ConcurrentStackOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentStackOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property);
    }

    internal class ClassWithReadOnlyPropertyConcurrentStackOfIntWithoutPopulateAttribute
    {
        public ConcurrentStack<int> Property { get; } = new ConcurrentStack<int>(new int[] { 1, 2, 3 });
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal class ClassWithReadOnlyPropertyConcurrentStackOfIntWithAttributeOnType
    {
        public ConcurrentStack<int> Property { get; } = new ConcurrentStack<int>(new int[] { 1, 2, 3 });
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ConcurrentStackOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentStackOfIntWithNumberHandling>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property);
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyConcurrentStackOfIntWithNumberHandling));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_ConcurrentStackOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentStackOfIntWithNumberHandlingWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property);
    }

    internal class ClassWithReadOnlyPropertyConcurrentStackOfIntWithNumberHandling
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public ConcurrentStack<int> Property { get; } = new ConcurrentStack<int>(new int[] { 1, 2, 3 });
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_ConcurrentStackOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyConcurrentStackOfIntWithNumberHandlingWithoutPopulateAttribute)));
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentStackOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_ConcurrentStackOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyConcurrentStackOfIntWithNumberHandlingWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentStackOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_ConcurrentStackOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyConcurrentStackOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6).Reverse(), obj.Property);
    }

    internal class ClassWithReadOnlyPropertyConcurrentStackOfIntWithNumberHandlingWithoutPopulateAttribute
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public ConcurrentStack<int> Property { get; } = new ConcurrentStack<int>(new int[] { 1, 2, 3 });
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal class ClassWithReadOnlyPropertyConcurrentStackOfIntWithNumberHandlingWithAttributeOnType
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public ConcurrentStack<int> Property { get; } = new ConcurrentStack<int>(new int[] { 1, 2, 3 });
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ICollectionOfInt_BackedBy_ListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfInt));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_ICollectionOfInt_BackedBy_ListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ICollectionOfInt_BackedBy_ListOfInt_PropertyOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":[5],"Property":[6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ICollectionOfInt_PropertyOccurringMultipleTimes_NullInBetween()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":null,"Property":[1],"Property":[2]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty<ICollection<int>>>(json, options);
        Assert.Equal(Enumerable.Range(1, 2), obj.Property);
    }

    internal struct ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfInt
    {
        public ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfInt() {}

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public ICollection<int> Property { get; } = new List<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_ICollectionOfInt_BackedBy_ListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithoutPopulateAttribute)));
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_ICollectionOfInt_BackedBy_ListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_ICollectionOfInt_BackedBy_ListOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    internal struct ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithoutPopulateAttribute
    {
        public ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithoutPopulateAttribute() {}

        public ICollection<int> Property { get; } = new List<int>() { 1, 2, 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal struct ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithAttributeOnType
    {
        public ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithAttributeOnType() {}

        public ICollection<int> Property { get; } = new List<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ICollectionOfInt_BackedBy_ListOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithNumberHandling>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithNumberHandling));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_ICollectionOfInt_BackedBy_ListOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithNumberHandlingWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    internal struct ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithNumberHandling
    {
        public ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithNumberHandling() {}

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public ICollection<int> Property { get; } = new List<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_ICollectionOfInt_BackedBy_ListOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithNumberHandlingWithoutPopulateAttribute)));
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_ICollectionOfInt_BackedBy_ListOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithNumberHandlingWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_ICollectionOfInt_BackedBy_ListOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    internal struct ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithNumberHandlingWithoutPopulateAttribute
    {
        public ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithNumberHandlingWithoutPopulateAttribute() {}

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public ICollection<int> Property { get; } = new List<int>() { 1, 2, 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal struct ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithNumberHandlingWithAttributeOnType
    {
        public ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_ListOfIntWithNumberHandlingWithAttributeOnType() {}

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public ICollection<int> Property { get; } = new List<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ICollectionOfInt_BackedBy_StructCollectionOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructCollection<int>)obj.Property).Validate();
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfInt));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_ICollectionOfInt_BackedBy_StructCollectionOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfIntWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructCollection<int>)obj.Property).Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ICollectionOfInt_BackedBy_StructCollectionOfInt_PropertyOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":[5],"Property":[6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructCollection<int>)obj.Property).Validate();
    }

    internal class ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfInt
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public ICollection<int> Property { get; } = new StructCollection<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_ICollectionOfInt_BackedBy_StructCollectionOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfIntWithoutPopulateAttribute)));
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructCollection<int>)obj.Property).Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_ICollectionOfInt_BackedBy_StructCollectionOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfIntWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructCollection<int>)obj.Property).Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_ICollectionOfInt_BackedBy_StructCollectionOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructCollection<int>)obj.Property).Validate();
    }

    internal class ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfIntWithoutPopulateAttribute
    {
        public ICollection<int> Property { get; } = new StructCollection<int>() { 1, 2, 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal class ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfIntWithAttributeOnType
    {
        public ICollection<int> Property { get; } = new StructCollection<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ICollectionOfInt_BackedBy_StructCollectionOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfIntWithNumberHandling>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructCollection<int>)obj.Property).Validate();
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfIntWithNumberHandling));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_ICollectionOfInt_BackedBy_StructCollectionOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfIntWithNumberHandlingWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructCollection<int>)obj.Property).Validate();
    }

    internal class ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfIntWithNumberHandling
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public ICollection<int> Property { get; } = new StructCollection<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_ICollectionOfInt_BackedBy_StructCollectionOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfIntWithNumberHandlingWithoutPopulateAttribute)));
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructCollection<int>)obj.Property).Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_ICollectionOfInt_BackedBy_StructCollectionOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfIntWithNumberHandlingWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructCollection<int>)obj.Property).Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_ICollectionOfInt_BackedBy_StructCollectionOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructCollection<int>)obj.Property).Validate();
    }

    internal class ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfIntWithNumberHandlingWithoutPopulateAttribute
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public ICollection<int> Property { get; } = new StructCollection<int>() { 1, 2, 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal class ClassWithReadOnlyPropertyICollectionOfInt_BackedBy_StructCollectionOfIntWithNumberHandlingWithAttributeOnType
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public ICollection<int> Property { get; } = new StructCollection<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_StructCollectionOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructCollectionOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(StructWithWritablePropertyStructCollectionOfInt));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_StructCollectionOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructCollectionOfIntWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_StructCollectionOfInt_PropertyOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":[5],"Property":[6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructCollectionOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
    }

    internal struct StructWithWritablePropertyStructCollectionOfInt
    {
        public StructWithWritablePropertyStructCollectionOfInt() {}

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public StructCollection<int> Property { get; set; } = new StructCollection<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_StructCollectionOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(StructWithWritablePropertyStructCollectionOfIntWithoutPopulateAttribute)));
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructCollectionOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_StructCollectionOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(StructWithWritablePropertyStructCollectionOfIntWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructCollectionOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_StructCollectionOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructCollectionOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
    }

    internal struct StructWithWritablePropertyStructCollectionOfIntWithoutPopulateAttribute
    {
        public StructWithWritablePropertyStructCollectionOfIntWithoutPopulateAttribute() {}

        public StructCollection<int> Property { get; set; } = new StructCollection<int>() { 1, 2, 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal struct StructWithWritablePropertyStructCollectionOfIntWithAttributeOnType
    {
        public StructWithWritablePropertyStructCollectionOfIntWithAttributeOnType() {}

        public StructCollection<int> Property { get; set; } = new StructCollection<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_StructCollectionOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true);
        string json = """{"Field":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldStructCollectionOfIntWithNumberHandling>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Field);
        obj.Field.Validate();
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(StructWithWritableFieldStructCollectionOfIntWithNumberHandling));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_StructCollectionOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true);
        string json = """{"Field":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldStructCollectionOfIntWithNumberHandlingWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Field);
        obj.Field.Validate();
    }

    internal struct StructWithWritableFieldStructCollectionOfIntWithNumberHandling
    {
        public StructWithWritableFieldStructCollectionOfIntWithNumberHandling() {}

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public StructCollection<int> Field = new StructCollection<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_StructCollectionOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(StructWithWritableFieldStructCollectionOfIntWithNumberHandlingWithoutPopulateAttribute)), includeFields: true);
        string json = """{"Field":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldStructCollectionOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Field);
        obj.Field.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_StructCollectionOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true, modifier: ti =>
        {
            if (ti.Type == typeof(StructWithWritableFieldStructCollectionOfIntWithNumberHandlingWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Field":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldStructCollectionOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Field);
        obj.Field.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_StructCollectionOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true, configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Field":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldStructCollectionOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Field);
        obj.Field.Validate();
    }

    internal struct StructWithWritableFieldStructCollectionOfIntWithNumberHandlingWithoutPopulateAttribute
    {
        public StructWithWritableFieldStructCollectionOfIntWithNumberHandlingWithoutPopulateAttribute() {}

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public StructCollection<int> Field = new StructCollection<int>() { 1, 2, 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal struct StructWithWritableFieldStructCollectionOfIntWithNumberHandlingWithAttributeOnType
    {
        public StructWithWritableFieldStructCollectionOfIntWithNumberHandlingWithAttributeOnType() {}

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public StructCollection<int> Field = new StructCollection<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_NullableStructCollectionOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true);
        string json = """{"Field":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructCollectionOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Field.Value);
        obj.Field.Value.Validate();
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(StructWithWritableFieldNullableStructCollectionOfInt));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_NullableStructCollectionOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true);
        string json = """{"Field":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructCollectionOfIntWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Field.Value);
        obj.Field.Value.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_NullableStructCollectionOfInt_FieldOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true);
        string json = """{"Field":[4],"Field":[5],"Field":[6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructCollectionOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Field.Value);
        obj.Field.Value.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_NullableStructCollectionOfInt_FieldOccurringMultipleTimes_NullInBetween()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true);
        string json = """{"Field":[4],"Field":null,"Field":[1],"Field":[2]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructCollectionOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 2), obj.Field.Value);
        obj.Field.Value.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_PopulatedFieldCanDeserializeNull_NullableStructCollectionOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true);
        string json = """{"Field":null}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructCollectionOfInt>(json, options);
        Assert.Null(obj.Field);
    }

    internal class StructWithWritableFieldNullableStructCollectionOfInt
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public StructCollection<int>? Field = new StructCollection<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_NullableStructCollectionOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(StructWithWritableFieldNullableStructCollectionOfIntWithoutPopulateAttribute)), includeFields: true);
        string json = """{"Field":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructCollectionOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Field.Value);
        obj.Field.Value.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_NullableStructCollectionOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true, modifier: ti =>
        {
            if (ti.Type == typeof(StructWithWritableFieldNullableStructCollectionOfIntWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Field":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructCollectionOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Field.Value);
        obj.Field.Value.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_NullableStructCollectionOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true, configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Field":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructCollectionOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Field.Value);
        obj.Field.Value.Validate();
    }

    internal class StructWithWritableFieldNullableStructCollectionOfIntWithoutPopulateAttribute
    {
        public StructCollection<int>? Field = new StructCollection<int>() { 1, 2, 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal class StructWithWritableFieldNullableStructCollectionOfIntWithAttributeOnType
    {
        public StructCollection<int>? Field = new StructCollection<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ISetOfInt_BackedBy_HashSetOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfInt));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_ISetOfInt_BackedBy_HashSetOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ISetOfInt_BackedBy_HashSetOfInt_PropertyOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":[5],"Property":[6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ISetOfInt_PropertyOccurringMultipleTimes_NullInBetween()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":null,"Property":[1],"Property":[2]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty<ISet<int>>>(json, options);
        Assert.Equal(Enumerable.Range(1, 2), obj.Property);
    }

    internal class ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfInt
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public ISet<int> Property { get; } = new HashSet<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_ISetOfInt_BackedBy_HashSetOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithoutPopulateAttribute)));
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_ISetOfInt_BackedBy_HashSetOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_ISetOfInt_BackedBy_HashSetOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    internal class ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithoutPopulateAttribute
    {
        public ISet<int> Property { get; } = new HashSet<int>() { 1, 2, 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal class ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithAttributeOnType
    {
        public ISet<int> Property { get; } = new HashSet<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ISetOfInt_BackedBy_HashSetOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithNumberHandling>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithNumberHandling));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_ISetOfInt_BackedBy_HashSetOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithNumberHandlingWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    internal struct ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithNumberHandling
    {
        public ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithNumberHandling() {}

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public ISet<int> Property { get; } = new HashSet<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_ISetOfInt_BackedBy_HashSetOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithNumberHandlingWithoutPopulateAttribute)));
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_ISetOfInt_BackedBy_HashSetOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithNumberHandlingWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_ISetOfInt_BackedBy_HashSetOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
    }

    internal struct ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithNumberHandlingWithoutPopulateAttribute
    {
        public ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithNumberHandlingWithoutPopulateAttribute() {}

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public ISet<int> Property { get; } = new HashSet<int>() { 1, 2, 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal struct ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithNumberHandlingWithAttributeOnType
    {
        public ClassWithReadOnlyPropertyISetOfInt_BackedBy_HashSetOfIntWithNumberHandlingWithAttributeOnType() {}

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public ISet<int> Property { get; } = new HashSet<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ISetOfInt_BackedBy_StructSetOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructSet<int>)obj.Property).Validate();
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfInt));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_ISetOfInt_BackedBy_StructSetOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfIntWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructSet<int>)obj.Property).Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ISetOfInt_BackedBy_StructSetOfInt_PropertyOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":[5],"Property":[6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructSet<int>)obj.Property).Validate();
    }

    internal struct ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfInt
    {
        public ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfInt() {}

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public ISet<int> Property { get; } = new StructSet<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_ISetOfInt_BackedBy_StructSetOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfIntWithoutPopulateAttribute)));
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructSet<int>)obj.Property).Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_ISetOfInt_BackedBy_StructSetOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfIntWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructSet<int>)obj.Property).Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_ISetOfInt_BackedBy_StructSetOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructSet<int>)obj.Property).Validate();
    }

    internal struct ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfIntWithoutPopulateAttribute
    {
        public ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfIntWithoutPopulateAttribute() {}

        public ISet<int> Property { get; } = new StructSet<int>() { 1, 2, 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal struct ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfIntWithAttributeOnType
    {
        public ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfIntWithAttributeOnType() {}

        public ISet<int> Property { get; } = new StructSet<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_ISetOfInt_BackedBy_StructSetOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfIntWithNumberHandling>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructSet<int>)obj.Property).Validate();
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfIntWithNumberHandling));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_ISetOfInt_BackedBy_StructSetOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfIntWithNumberHandlingWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructSet<int>)obj.Property).Validate();
    }

    internal class ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfIntWithNumberHandling
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public ISet<int> Property { get; } = new StructSet<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_ISetOfInt_BackedBy_StructSetOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfIntWithNumberHandlingWithoutPopulateAttribute)));
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructSet<int>)obj.Property).Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_ISetOfInt_BackedBy_StructSetOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfIntWithNumberHandlingWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructSet<int>)obj.Property).Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_ISetOfInt_BackedBy_StructSetOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        ((StructSet<int>)obj.Property).Validate();
    }

    internal class ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfIntWithNumberHandlingWithoutPopulateAttribute
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public ISet<int> Property { get; } = new StructSet<int>() { 1, 2, 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal class ClassWithReadOnlyPropertyISetOfInt_BackedBy_StructSetOfIntWithNumberHandlingWithAttributeOnType
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public ISet<int> Property { get; } = new StructSet<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_StructSetOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructSetOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(StructWithWritablePropertyStructSetOfInt));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_StructSetOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructSetOfIntWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_StructSetOfInt_PropertyOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":[4],"Property":[5],"Property":[6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructSetOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
    }

    internal class StructWithWritablePropertyStructSetOfInt
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public StructSet<int> Property { get; set; } = new StructSet<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_StructSetOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(StructWithWritablePropertyStructSetOfIntWithoutPopulateAttribute)));
        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructSetOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_StructSetOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(StructWithWritablePropertyStructSetOfIntWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructSetOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_StructSetOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructSetOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
    }

    internal class StructWithWritablePropertyStructSetOfIntWithoutPopulateAttribute
    {
        public StructSet<int> Property { get; set; } = new StructSet<int>() { 1, 2, 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal class StructWithWritablePropertyStructSetOfIntWithAttributeOnType
    {
        public StructSet<int> Property { get; set; } = new StructSet<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_StructSetOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructSetOfIntWithNumberHandling>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(StructWithWritablePropertyStructSetOfIntWithNumberHandling));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_StructSetOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructSetOfIntWithNumberHandlingWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
    }

    internal struct StructWithWritablePropertyStructSetOfIntWithNumberHandling
    {
        public StructWithWritablePropertyStructSetOfIntWithNumberHandling() {}

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public StructSet<int> Property { get; set; } = new StructSet<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_StructSetOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(StructWithWritablePropertyStructSetOfIntWithNumberHandlingWithoutPopulateAttribute)));
        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructSetOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_StructSetOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(StructWithWritablePropertyStructSetOfIntWithNumberHandlingWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructSetOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_StructSetOfInt_WithNumberHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Property":["4","5","6"]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritablePropertyStructSetOfIntWithNumberHandlingWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Property);
        obj.Property.Validate();
    }

    internal struct StructWithWritablePropertyStructSetOfIntWithNumberHandlingWithoutPopulateAttribute
    {
        public StructWithWritablePropertyStructSetOfIntWithNumberHandlingWithoutPopulateAttribute() {}

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public StructSet<int> Property { get; set; } = new StructSet<int>() { 1, 2, 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal struct StructWithWritablePropertyStructSetOfIntWithNumberHandlingWithAttributeOnType
    {
        public StructWithWritablePropertyStructSetOfIntWithNumberHandlingWithAttributeOnType() {}

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public StructSet<int> Property { get; set; } = new StructSet<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_NullableStructSetOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true);
        string json = """{"Field":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructSetOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Field.Value);
        obj.Field.Value.Validate();
        CheckTypeHasSinglePropertyWithPopulateHandling(options, typeof(StructWithWritableFieldNullableStructSetOfInt));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CanPopulate_NullableStructSetOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true);
        string json = """{"Field":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructSetOfIntWithAttributeOnType>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Field.Value);
        obj.Field.Value.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_NullableStructSetOfInt_FieldOccurringMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true);
        string json = """{"Field":[4],"Field":[5],"Field":[6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructSetOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Field.Value);
        obj.Field.Value.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_NullableStructSetOfInt_FieldOccurringMultipleTimes_NullInBetween()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true);
        string json = """{"Field":[4],"Field":null,"Field":[1],"Field":[2]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructSetOfInt>(json, options);
        Assert.Equal(Enumerable.Range(1, 2), obj.Field.Value);
        obj.Field.Value.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_PopulatedFieldCanDeserializeNull_NullableStructSetOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true);
        string json = """{"Field":null}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructSetOfInt>(json, options);
        Assert.Null(obj.Field);
    }

    internal struct StructWithWritableFieldNullableStructSetOfInt
    {
        public StructWithWritableFieldNullableStructSetOfInt() {}

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public StructSet<int>? Field = new StructSet<int>() { 1, 2, 3 };
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadata_CanPopulate_NullableStructSetOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: GetFirstPropertyToPopulateForTypeModifier(typeof(StructWithWritableFieldNullableStructSetOfIntWithoutPopulateAttribute)), includeFields: true);
        string json = """{"Field":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructSetOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Field.Value);
        obj.Field.Value.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithMetadataOnType_CanPopulate_NullableStructSetOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true, modifier: ti =>
        {
            if (ti.Type == typeof(StructWithWritableFieldNullableStructSetOfIntWithoutPopulateAttribute))
            {
                ti.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate;
            }
        });

        string json = """{"Field":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructSetOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Field.Value);
        obj.Field.Value.Validate();
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulate_NullableStructSetOfInt()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true, configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Field":[4,5,6]}""";
        var obj = await Serializer.DeserializeWrapper<StructWithWritableFieldNullableStructSetOfIntWithoutPopulateAttribute>(json, options);
        Assert.Equal(Enumerable.Range(1, 6), obj.Field.Value);
        obj.Field.Value.Validate();
    }

    internal struct StructWithWritableFieldNullableStructSetOfIntWithoutPopulateAttribute
    {
        public StructWithWritableFieldNullableStructSetOfIntWithoutPopulateAttribute() {}

        public StructSet<int>? Field = new StructSet<int>() { 1, 2, 3 };
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal struct StructWithWritableFieldNullableStructSetOfIntWithAttributeOnType
    {
        public StructWithWritableFieldNullableStructSetOfIntWithAttributeOnType() {}

        public StructSet<int>? Field = new StructSet<int>() { 1, 2, 3 };
    }

    [Theory]
    [InlineData(typeof(ClassWithReadOnlyProperty<StructList<int>>))]
    [InlineData(typeof(ClassWithReadOnlyProperty<StructList<int>?>))]
    [InlineData(typeof(ClassWithReadOnlyProperty<StructCollection<int>>))]
    [InlineData(typeof(ClassWithReadOnlyProperty<StructCollection<int>?>))]
    [InlineData(typeof(ClassWithReadOnlyProperty<StructSet<int>>))]
    [InlineData(typeof(ClassWithReadOnlyProperty<StructSet<int>?>))]
    public async Task CreationHandlingSetWithAttribute_PopulateWithoutSetterOnValueTypeThrows_Enumerable(Type type)
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = "{}";
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper(json, type, options));
    }
}
