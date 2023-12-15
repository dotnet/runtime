// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests;

public abstract partial class JsonCreationHandlingTests : SerializerTests
{
    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulateReadOnlyProperty_SimpleClass()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"StringValue":"NewValue"}}""";

        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyProperty_SimpleClass>(json, options);
        Assert.NotNull(obj);
        Assert.NotNull(obj.Property);
        Assert.Equal("NewValue", obj.Property.StringValue);
        Assert.Equal(43, obj.Property.IntValue);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulateReadOnlyProperty_SimpleClass_PropertyOccurredMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"StringValue":"NewValue"}, "Property":{}}""";

        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyProperty_SimpleClass>(json, options);
        Assert.NotNull(obj);
        Assert.NotNull(obj.Property);
        Assert.Equal("NewValue", obj.Property.StringValue);
        Assert.Equal(43, obj.Property.IntValue);

        json = """{"Property":{"StringValue":"NewValue"}, "Property":{}, "Property":{"IntValue":45}}""";
        obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyProperty_SimpleClass>(json, options);
        Assert.NotNull(obj);
        Assert.NotNull(obj.Property);
        Assert.Equal("NewValue", obj.Property.StringValue);
        Assert.Equal(45, obj.Property.IntValue);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_PopulateReadOnlyProperty_SimpleClass_DeserializingNull()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":null}""";
        await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.DeserializeWrapper<ClassWithReadOnlyProperty_SimpleClass>(json, options));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_PopulateWritableProperty_SimpleClass_DeserializingNull()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":null}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty_SimpleClass>(json, options);
        Assert.Null(obj.Property);

        json = """{"Property":null,"Property":{}}""";
        obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty_SimpleClass>(json, options);
        Assert.NotNull(obj.Property);
        Assert.Equal("SomeValue", obj.Property.StringValue);
        Assert.Equal(42, obj.Property.IntValue);

        json = """{"Property":null,"Property":{"StringValue":"NewValue"}}""";
        obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty_SimpleClass>(json, options);
        Assert.NotNull(obj.Property);
        Assert.Equal("NewValue", obj.Property.StringValue);
        Assert.Equal(42, obj.Property.IntValue);

        json = """{"Property":null,"Property":{"StringValue":"NewValue"},"Property":{"IntValue":45}}""";
        obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty_SimpleClass>(json, options);
        Assert.NotNull(obj.Property);
        Assert.Equal("NewValue", obj.Property.StringValue);
        Assert.Equal(45, obj.Property.IntValue);
    }

    [Fact]
    public async Task CreationHandlingEffectCancelledWithModifier_ClassWithReadOnlyProperty_SimpleClass()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReadOnlyProperty_SimpleClass))
            {
                Assert.Equal(JsonObjectCreationHandling.Populate, ti.Properties[0].ObjectCreationHandling);
                ti.Properties[0].ObjectCreationHandling = null;
            }
        });

        string json = """{"Property":{"StringValue":"NewValue"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyProperty_SimpleClass>(json, options);
        Assert.NotNull(obj);
        Assert.NotNull(obj.Property);
        Assert.Equal("InitialValue", obj.Property.StringValue);
        Assert.Equal(43, obj.Property.IntValue);
    }

    [Fact]
    public async Task CreationHandlingEffectCancelledWithModifier_ClassWithWritableProperty_SimpleClass()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithWritableProperty_SimpleClass))
            {
                Assert.Equal(JsonObjectCreationHandling.Populate, ti.Properties[0].ObjectCreationHandling);
                ti.Properties[0].ObjectCreationHandling = null;
            }
        });

        string json = """{"Property":{}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty_SimpleClass>(json, options);
        Assert.NotNull(obj);
        Assert.NotNull(obj.Property);
        Assert.Equal("SomeValue", obj.Property.StringValue);
        Assert.Equal(42, obj.Property.IntValue);

        json = """{"Property":{"StringValue":"NewValue"}}""";
        obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty_SimpleClass>(json, options);
        Assert.NotNull(obj);
        Assert.NotNull(obj.Property);
        Assert.Equal("NewValue", obj.Property.StringValue);
        Assert.Equal(42, obj.Property.IntValue);
    }

    internal class ClassWithReadOnlyProperty_SimpleClass
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public SimpleClass Property { get; } = new()
        {
            StringValue = "InitialValue",
            IntValue = 43,
        };
    }

    internal class ClassWithWritableProperty_SimpleClass
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public SimpleClass Property { get; set; } = new()
        {
            StringValue = "InitialValue",
            IntValue = 43,
        };
    }

    internal class SimpleClass
    {
        public string StringValue { get; set; } = "SomeValue";
        public int IntValue { get; set; } = 42;
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulateWritableProperty_SimpleStruct()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"StringValue":"NewValue"}}""";

        var obj = await Serializer.DeserializeWrapper<StructWithWritableProperty_SimpleStruct>(json, options);
        Assert.Equal("NewValue", obj.Property.StringValue);
        Assert.Equal(43, obj.Property.IntValue);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulateWritableProperty_SimpleStruct_PropertyOccurredMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"StringValue":"NewValue"}, "Property":{}}""";

        var obj = await Serializer.DeserializeWrapper<StructWithWritableProperty_SimpleStruct>(json, options);
        Assert.Equal("NewValue", obj.Property.StringValue);
        Assert.Equal(43, obj.Property.IntValue);

        json = """{"Property":{"StringValue":"NewValue"}, "Property":{}, "Property":{"IntValue":45}}""";
        obj = await Serializer.DeserializeWrapper<StructWithWritableProperty_SimpleStruct>(json, options);
        Assert.Equal("NewValue", obj.Property.StringValue);
        Assert.Equal(45, obj.Property.IntValue);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_PopulateWritableProperty_SimpleStruct_DeserializingNull()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":null}""";
        await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<StructWithWritableProperty_SimpleStruct>(json, options));

        json = """{"Property":{}, "Property":null}""";
        await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<StructWithWritableProperty_SimpleStruct>(json, options));
    }

    internal struct StructWithReadOnlyProperty_SimpleStruct
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public SimpleStruct Property { get; } = new()
        {
            StringValue = "InitialValue",
            IntValue = 43,
        };

        public StructWithReadOnlyProperty_SimpleStruct() { }
    }

    internal struct StructWithWritableProperty_SimpleStruct
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public SimpleStruct Property { get; set; } = new()
        {
            StringValue = "InitialValue",
            IntValue = 43,
        };

        public StructWithWritableProperty_SimpleStruct() { }
    }

    internal struct SimpleStruct
    {
        public string StringValue { get; set; } = "SomeValue";
        public int IntValue { get; set; } = 42;

        public SimpleStruct() { }
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulateReadOnlyProperty_SimpleClassWithSmallParametrizedCtor()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"StringValue":"NewValue"}}""";

        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyProperty_SimpleClassWithSmallParametrizedCtor>(json, options);
        Assert.NotNull(obj);
        Assert.NotNull(obj.Property);
        Assert.Equal("NewValue", obj.Property.StringValue);
        Assert.Equal(43, obj.Property.IntValue);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulateReadOnlyProperty_SimpleClassWithSmallParametrizedCtor_PropertyOccurredMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"StringValue":"NewValue"}, "Property":{}}""";

        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyProperty_SimpleClassWithSmallParametrizedCtor>(json, options);
        Assert.NotNull(obj);
        Assert.NotNull(obj.Property);
        Assert.Equal("NewValue", obj.Property.StringValue);
        Assert.Equal(43, obj.Property.IntValue);

        json = """{"Property":{"StringValue":"NewValue"}, "Property":{}, "Property":{"IntValue":45}}""";
        obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyProperty_SimpleClassWithSmallParametrizedCtor>(json, options);
        Assert.NotNull(obj);
        Assert.NotNull(obj.Property);
        Assert.Equal("NewValue", obj.Property.StringValue);
        Assert.Equal(45, obj.Property.IntValue);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_PopulateReadOnlyProperty_SimpleClassWithSmallParametrizedCtor_DeserializingNull()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":null}""";
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper<ClassWithReadOnlyProperty_SimpleClassWithSmallParametrizedCtor>(json, options));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_PopulateWritableProperty_SimpleClassWithSmallParametrizedCtor_DeserializingNull()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":null}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty_SimpleClassWithSmallParametrizedCtor>(json, options);
        Assert.Null(obj.Property);

        json = """{"Property":null,"Property":{}}""";
        obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty_SimpleClassWithSmallParametrizedCtor>(json, options);
        Assert.NotNull(obj.Property);
        Assert.Null(obj.Property.StringValue);
        Assert.Equal(0, obj.Property.IntValue);

        json = """{"Property":null,"Property":{"StringValue":"NewValue"}}""";
        obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty_SimpleClassWithSmallParametrizedCtor>(json, options);
        Assert.NotNull(obj.Property);
        Assert.Equal("NewValue", obj.Property.StringValue);
        Assert.Equal(0, obj.Property.IntValue);

        json = """{"Property":null,"Property":{"StringValue":"NewValue"},"Property":{"IntValue":45}}""";
        obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty_SimpleClassWithSmallParametrizedCtor>(json, options);
        Assert.NotNull(obj.Property);
        Assert.Equal("NewValue", obj.Property.StringValue);
        Assert.Equal(45, obj.Property.IntValue);
    }

    internal class ClassWithReadOnlyProperty_SimpleClassWithSmallParametrizedCtor
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public SimpleClassWithSmallParametrizedCtor Property { get; } = new("InitialValue", 43);
    }

    internal class ClassWithWritableProperty_SimpleClassWithSmallParametrizedCtor
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public SimpleClassWithSmallParametrizedCtor Property { get; set; } = new("InitialValue", 43);
    }

    internal class SimpleClassWithSmallParametrizedCtor
    {
        public SimpleClassWithSmallParametrizedCtor(string? stringValue, int intValue)
        {
            StringValue = stringValue;
            IntValue = intValue;
        }

        public string? StringValue { get; set; }
        public int IntValue { get; set; }
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulateReadOnlyProperty_SimpleClassWithLargeParametrizedCtor()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"A":"NewValue"}}""";

        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyProperty_SimpleClassWithLargeParametrizedCtor>(json, options);
        Assert.NotNull(obj);
        Assert.NotNull(obj.Property);
        Assert.Equal("NewValue", obj.Property.A);
        Assert.Equal(43, obj.Property.B);
        Assert.Equal("InitialValue2", obj.Property.C);
        Assert.Equal(44, obj.Property.D);
        Assert.Equal("InitialValue3", obj.Property.E);
        Assert.Equal(45, obj.Property.F);
        Assert.Equal("InitialValue4", obj.Property.G);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulateReadOnlyProperty_SimpleClassWithLargeParametrizedCtor_PropertyOccurredMultipleTimes()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":{"A":"NewValue"}, "Property":{}}""";

        var obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyProperty_SimpleClassWithLargeParametrizedCtor>(json, options);
        Assert.NotNull(obj);
        Assert.NotNull(obj.Property);
        Assert.Equal("NewValue", obj.Property.A);
        Assert.Equal(43, obj.Property.B);
        Assert.Equal("InitialValue2", obj.Property.C);
        Assert.Equal(44, obj.Property.D);
        Assert.Equal("InitialValue3", obj.Property.E);
        Assert.Equal(45, obj.Property.F);
        Assert.Equal("InitialValue4", obj.Property.G);

        json = """{"Property":{"A":"NewValue"}, "Property":{}, "Property":{"B":99}}""";
        obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyProperty_SimpleClassWithLargeParametrizedCtor>(json, options);
        Assert.NotNull(obj);
        Assert.NotNull(obj.Property);
        Assert.Equal("NewValue", obj.Property.A);
        Assert.Equal(99, obj.Property.B);
        Assert.Equal("InitialValue2", obj.Property.C);
        Assert.Equal(44, obj.Property.D);
        Assert.Equal("InitialValue3", obj.Property.E);
        Assert.Equal(45, obj.Property.F);
        Assert.Equal("InitialValue4", obj.Property.G);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_PopulateReadOnlyProperty_SimpleClassWithLargeParametrizedCtor_DeserializingNull()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":null}""";
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper<ClassWithReadOnlyProperty_SimpleClassWithLargeParametrizedCtor>(json, options));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_PopulateWritableProperty_SimpleClassWithLargeParametrizedCtor_DeserializingNull()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """{"Property":null}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty_SimpleClassWithLargeParametrizedCtor>(json, options);
        Assert.Null(obj.Property);

        json = """{"Property":null,"Property":{}}""";
        obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty_SimpleClassWithLargeParametrizedCtor>(json, options);
        Assert.NotNull(obj);
        Assert.NotNull(obj.Property);
        Assert.Null(obj.Property.A);
        Assert.Equal(0, obj.Property.B);
        Assert.Null(obj.Property.C);
        Assert.Equal(0, obj.Property.D);
        Assert.Null(obj.Property.E);
        Assert.Equal(0, obj.Property.F);
        Assert.Null(obj.Property.G);

        json = """{"Property":null,"Property":{},"Property":{"A":"NewValue"}}""";
        obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty_SimpleClassWithLargeParametrizedCtor>(json, options);
        Assert.NotNull(obj);
        Assert.NotNull(obj.Property);
        Assert.Equal("NewValue", obj.Property.A);
        Assert.Equal(0, obj.Property.B);
        Assert.Null(obj.Property.C);
        Assert.Equal(0, obj.Property.D);
        Assert.Null(obj.Property.E);
        Assert.Equal(0, obj.Property.F);
        Assert.Null(obj.Property.G);

        json = """{"Property":null,"Property":{},"Property":{"A":"NewValue"},"Property":{"F":99}}""";
        obj = await Serializer.DeserializeWrapper<ClassWithWritableProperty_SimpleClassWithLargeParametrizedCtor>(json, options);
        Assert.NotNull(obj);
        Assert.NotNull(obj.Property);
        Assert.Equal("NewValue", obj.Property.A);
        Assert.Equal(0, obj.Property.B);
        Assert.Null(obj.Property.C);
        Assert.Equal(0, obj.Property.D);
        Assert.Null(obj.Property.E);
        Assert.Equal(99, obj.Property.F);
        Assert.Null(obj.Property.G);
    }

    internal class ClassWithReadOnlyProperty_SimpleClassWithLargeParametrizedCtor
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public SimpleClassWithLargeParametrizedCtor Property { get; } = new("InitialValue1", 43, "InitialValue2", 44, "InitialValue3", 45, "InitialValue4");
    }

    internal class ClassWithWritableProperty_SimpleClassWithLargeParametrizedCtor
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public SimpleClassWithLargeParametrizedCtor Property { get; set; } = new("InitialValue1", 43, "InitialValue2", 44, "InitialValue3", 45, "InitialValue4");
    }

    internal class SimpleClassWithLargeParametrizedCtor
    {
        public SimpleClassWithLargeParametrizedCtor(string? a, int b, string? c, int d, string? e, int f, string? g)
        {
            A = a;
            B = b;
            C = c;
            D = d;
            E = e;
            F = f;
            G = g;
        }

        public string? A { get; set; }
        public int B { get; set; }
        public string? C { get; set; }
        public int D { get; set; }
        public string? E { get; set; }
        public int F { get; set; }
        public string? G { get; set; }
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulateAndSerialize_ClassWithEnabledPolymorphismOnSerialization()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        ClassWithProperty_BaseClassWithPolymorphismOnSerializationOnly obj = new();
        string json = await Serializer.SerializeWrapper(obj, options);
        Assert.Equal("""{"Property":{"DerivedClassProp":"derived","BaseClassProp":"base"}}""", json);

        json = """{"Property":{"BaseClassProp":"BaseNewValue","DerivedClassProp":"DerivedNewValue"}}""";
        obj = await Serializer.DeserializeWrapper<ClassWithProperty_BaseClassWithPolymorphismOnSerializationOnly>(json, options);
        Assert.IsType<DerivedClass_DerivingFrom_BaseClassWithPolymorphismOnSerializationOnly>(obj.Property);
        Assert.Equal("BaseNewValue", obj.Property.BaseClassProp);
        // DerivedNewValue should be ignored because we didn't setup polymorphic deserialization
        Assert.Equal("derived", ((DerivedClass_DerivingFrom_BaseClassWithPolymorphismOnSerializationOnly)obj.Property).DerivedClassProp);

        json = """{"Property":null}""";
        obj = await Serializer.DeserializeWrapper<ClassWithProperty_BaseClassWithPolymorphismOnSerializationOnly>(json, options);
        Assert.Null(obj.Property);

        json = """{"Property":null,"Property":{"BaseClassProp":"BaseNewValue","DerivedClassProp":"DerivedNewValue"}}""";
        obj = await Serializer.DeserializeWrapper<ClassWithProperty_BaseClassWithPolymorphismOnSerializationOnly>(json, options);
        Assert.IsType<BaseClassWithPolymorphismOnSerializationOnly>(obj.Property);
        Assert.Equal("BaseNewValue", obj.Property.BaseClassProp);
    }

    public class ClassWithProperty_BaseClassWithPolymorphismOnSerializationOnly
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public BaseClassWithPolymorphismOnSerializationOnly Property { get; set; } =
            new DerivedClass_DerivingFrom_BaseClassWithPolymorphismOnSerializationOnly()
            {
                BaseClassProp = "base",
                DerivedClassProp = "derived",
            };
    }

    [JsonDerivedType(typeof(DerivedClass_DerivingFrom_BaseClassWithPolymorphismOnSerializationOnly))]
    public class BaseClassWithPolymorphismOnSerializationOnly
    {
        public string BaseClassProp { get; set; } = "BaseInitial";
    }

    public class DerivedClass_DerivingFrom_BaseClassWithPolymorphismOnSerializationOnly : BaseClassWithPolymorphismOnSerializationOnly
    {
        public string DerivedClassProp { get; set; } = "DerivedInitial";
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CannotPopulateOrSerialize_ClassWithEnabledPolymorphismOnBothSerializationAndDeserialization()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        ClassWithProperty_BaseClassWithPolymorphism obj = new();
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await Serializer.SerializeWrapper(obj, options));

        string json = """{"Property":{"$type":"derived","BaseClassProp":"BaseNewValue","DerivedClassProp":"DerivedNewValue"}}""";
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await Serializer.DeserializeWrapper<ClassWithProperty_BaseClassWithPolymorphism>(json, options));
    }

    public class ClassWithProperty_BaseClassWithPolymorphism
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public BaseClassWithPolymorphism Property { get; set; } =
            new DerivedClass_DerivingFrom_BaseClassWithPolymorphism()
            {
                BaseClassProp = "base",
                DerivedClassProp = "derived",
            };
    }

    [JsonDerivedType(typeof(DerivedClass_DerivingFrom_BaseClassWithPolymorphism), "derived")]
    public class BaseClassWithPolymorphism
    {
        public string BaseClassProp { get; set; } = "BaseInitial";
    }

    public class DerivedClass_DerivingFrom_BaseClassWithPolymorphism : BaseClassWithPolymorphism
    {
        public string DerivedClassProp { get; set; } = "DerivedInitial";
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CannotPopulateOrSerialize_ClassWithEnabledPolymorphismOnBothSerializationAndDeserializationRecursive()
    {
        // Verifies that polymorphism check doesn't have ordering issue during Configure.
        // This may happen if polymorphism is setup after properties are configured.
        JsonSerializerOptions options = Serializer.CreateOptions();
        BaseClassRecursive obj = new();
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await Serializer.SerializeWrapper(obj, options));

        string json = """{"Next":{"$type":"derived","BaseClassProp":"BaseNewValue","DerivedClassProp":"DerivedNewValue"}}""";
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await Serializer.DeserializeWrapper<BaseClassRecursive>(json, options));

        json = """{}""";
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await Serializer.DeserializeWrapper<BaseClassRecursive>(json, options));
    }

    [JsonDerivedType(typeof(BaseClassWithPolymorphism), "base")]
    [JsonDerivedType(typeof(DerivedClass_DerivingFrom_BaseClassWithPolymorphism), "derived")]
    public class BaseClassRecursive
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public BaseClassRecursive Next { get; set; }

        public string BaseClassProp { get; set; } = "BaseInitial";
    }

    public class DerivedClass_DerivingFrom_BaseClassRecursive : BaseClassRecursive
    {
        public string DerivedClassProp { get; set; } = "DerivedInitial";
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CannotPopulateOrSerialize_ClassWithReadOnlyPropertyAndIgnoreReadOnlyProperties()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.IgnoreReadOnlyProperties = true;
        });

        ClassWithReadOnlyInitializedProperty<SimpleClass> obj = new();
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await Serializer.SerializeWrapper(obj, options));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await Serializer.DeserializeWrapper<ClassWithReadOnlyInitializedProperty<SimpleClass>>("{}", options));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulateOrSerialize_ClassWithReadOnlyFieldAndIgnoreReadOnlyProperties()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true, configure: (opt) =>
        {
            opt.IgnoreReadOnlyProperties = true;
        });

        ClassWithReadOnlyInitializedField<SimpleClass> obj = new();
        string json = await Serializer.SerializeWrapper(obj, options);
        Assert.Equal("""{"Field":{"StringValue":"SomeValue","IntValue":42}}""", json);

        json = """{"Field":{"StringValue":"SomeNewValue"},"Field":{"IntValue":43}}""";
        obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyInitializedField<SimpleClass>>(json, options);
        Assert.Equal("SomeNewValue", obj.Field.StringValue);
        Assert.Equal(43, obj.Field.IntValue);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulateOrSerialize_ClassWithWritablePropertyAndIgnoreReadOnlyProperties()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.IgnoreReadOnlyProperties = true;
        });

        ClassWithInitializedProperty<SimpleClass> obj = new();
        string json = await Serializer.SerializeWrapper(obj, options);
        Assert.Equal("""{"Property":{"StringValue":"SomeValue","IntValue":42}}""", json);

        json = """{"Property":{"StringValue":"SomeNewValue"},"Property":{"IntValue":43}}""";
        obj = await Serializer.DeserializeWrapper<ClassWithInitializedProperty<SimpleClass>>(json, options);
        Assert.Equal("SomeNewValue", obj.Property.StringValue);
        Assert.Equal(43, obj.Property.IntValue);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulateOrSerialize_ClassWithReadOnlyPropertyAndIgnoreReadOnlyFields()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.IgnoreReadOnlyFields = true;
        });

        ClassWithReadOnlyInitializedProperty<SimpleClass> obj = new();
        string json = await Serializer.SerializeWrapper(obj, options);
        Assert.Equal("""{"Property":{"StringValue":"SomeValue","IntValue":42}}""", json);

        json = """{"Property":{"StringValue":"SomeNewValue"},"Property":{"IntValue":43}}""";
        obj = await Serializer.DeserializeWrapper<ClassWithReadOnlyInitializedProperty<SimpleClass>>(json, options);
        Assert.Equal("SomeNewValue", obj.Property.StringValue);
        Assert.Equal(43, obj.Property.IntValue);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CannotPopulateOrSerialize_ClassWithReadOnlyFieldAndIgnoreReadOnlyFields()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true, configure: (opt) =>
        {
            opt.IgnoreReadOnlyFields = true;
        });

        ClassWithReadOnlyInitializedField<SimpleClass> obj = new();
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await Serializer.SerializeWrapper(obj, options));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await Serializer.DeserializeWrapper<ClassWithReadOnlyInitializedField<SimpleClass>>("{}", options));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulateOrSerialize_ClassWithFieldAndIgnoreReadOnlyFields()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true, configure: (opt) =>
        {
            opt.IgnoreReadOnlyFields = true;
        });

        ClassWithInitializedField<SimpleClass> obj = new();
        string json = await Serializer.SerializeWrapper(obj, options);
        Assert.Equal("""{"Field":{"StringValue":"SomeValue","IntValue":42}}""", json);

        json = """{"Field":{"StringValue":"SomeNewValue"},"Field":{"IntValue":43}}""";
        obj = await Serializer.DeserializeWrapper<ClassWithInitializedField<SimpleClass>>(json, options);
        Assert.Equal("SomeNewValue", obj.Field.StringValue);
        Assert.Equal(43, obj.Field.IntValue);
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CannotPopulateOrSerialize_ReferenceHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.ReferenceHandler = ReferenceHandler.Preserve;
        });

        ClassWithInitializedProperty<SimpleClass> obj = new();
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await Serializer.SerializeWrapper(obj, options));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await Serializer.DeserializeWrapper<ClassWithInitializedProperty<SimpleClass>>("{}", options));
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CannotPopulateOrSerialize_ReferenceHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        ClassWithWritablePropertyWithoutPopulate<SimpleClass> obj = new();
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await Serializer.SerializeWrapper(obj, options));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await Serializer.DeserializeWrapper<ClassWithWritablePropertyWithoutPopulate<SimpleClass>>("{}", options));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttributeOnType_CannotPopulateOrSerialize_ReferenceHandling()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.ReferenceHandler = ReferenceHandler.Preserve;
        });

        ClassWithDefaultPopulateAndProperty<SimpleClass> obj = new();
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await Serializer.SerializeWrapper(obj, options));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await Serializer.DeserializeWrapper<ClassWithDefaultPopulateAndProperty<SimpleClass>>("{}", options));
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulateOrSerialize_RequiredProperties()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();

        string json = """{"Property":{"RequiredValue":"NewValue"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithInitializedProperty<ClassWithRequiredProperty>>(json, options);
        Assert.Equal("NewValue", obj.Property.RequiredValue);

        json = """{"Property":{}}""";
        var exception = await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<ClassWithInitializedProperty<ClassWithRequiredProperty>>(json, options));
        Assert.Contains(nameof(ClassWithRequiredProperty.RequiredValue), exception.Message);

        json = """{"Property":null}""";
        obj = await Serializer.DeserializeWrapper<ClassWithInitializedProperty<ClassWithRequiredProperty>>(json, options);
        Assert.Null(obj.Property);

        json = """{"Property":null,"Property":{}}""";
        exception = await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<ClassWithInitializedProperty<ClassWithRequiredProperty>>(json, options));
        Assert.Contains(nameof(ClassWithRequiredProperty.RequiredValue), exception.Message);

        json = """{"Property":null,"Property":{"RequiredValue":"NewValue"},"Property":{}}""";
        exception = await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<ClassWithInitializedProperty<ClassWithRequiredProperty>>(json, options));
        Assert.Contains(nameof(ClassWithRequiredProperty.RequiredValue), exception.Message);

        json = """{"Property":null,"Property":{"RequiredValue":"NewValue","OptionalValue":"test"},"Property":{"RequiredValue":"NewValue2"}}""";
        obj = await Serializer.DeserializeWrapper<ClassWithInitializedProperty<ClassWithRequiredProperty>>(json, options);
        Assert.Equal("test", obj.Property.OptionalValue);
        Assert.Equal("NewValue2", obj.Property.RequiredValue);

        json = """{"Property":{"RequiredValue":"NewValue", "OptionalValue":"test"},"Property":{}}""";
        exception = await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<ClassWithInitializedProperty<ClassWithRequiredProperty>>(json, options));
        Assert.Contains(nameof(ClassWithRequiredProperty.RequiredValue), exception.Message);

        json = """{"Property":{"RequiredValue":"NewValue", "OptionalValue":"test"},"Property":{"OptionalValue":"aaa"}}""";
        exception = await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<ClassWithInitializedProperty<ClassWithRequiredProperty>>(json, options));
        Assert.Contains(nameof(ClassWithRequiredProperty.RequiredValue), exception.Message);

        json = """{"Property":{"RequiredValue":"NewValue", "OptionalValue":"test"},"Property":{"RequiredValue":"NewValue2"}}""";
        obj = await Serializer.DeserializeWrapper<ClassWithInitializedProperty<ClassWithRequiredProperty>>(json, options);
        Assert.Equal("test", obj.Property.OptionalValue);
        Assert.Equal("NewValue2", obj.Property.RequiredValue);
    }

    public class ClassWithRequiredProperty
    {
        [JsonRequired]
        public string RequiredValue { get; set; } = "InitialRequiredValue";

        public string OptionalValue { get; set; } = "InitialOptionalValue";
    }

    [Fact]
    public async Task CreationHandlingSetWithOptions_CanPopulateOrSerialize_RequiredPropertiesRecursive()
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: (opt) =>
        {
            opt.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
        });

        string json = """{"Value":1,"Next":null}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithRecursiveRequiredProperty>(json, options);
        Check(obj, 1);

        json = """{"Value":1}""";
        var exception = await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<ClassWithRecursiveRequiredProperty>(json, options));
        Assert.Contains(nameof(ClassWithRecursiveRequiredProperty.Next), exception.Message);

        json = """{"Value":1,"Next":{"Value":2,"Next":null},"Next":{"Next":{"Value":3,"Next":null}}}""";
        obj = await Serializer.DeserializeWrapper<ClassWithRecursiveRequiredProperty>(json, options);
        Check(obj, 3);

        json = """{"Value":1,"Next":{"Value":2,"Next":null},"Next":{"Next":{"Value":3}}}""";
        exception = await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<ClassWithRecursiveRequiredProperty>(json, options));
        Assert.Contains(nameof(ClassWithRecursiveRequiredProperty.Next), exception.Message);

        json = """{"Value":1,"Next":{"Value":2},"Next":{"Next":{"Value":3,"Next":null}}}""";
        exception = await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<ClassWithRecursiveRequiredProperty>(json, options));
        Assert.Contains(nameof(ClassWithRecursiveRequiredProperty.Next), exception.Message);

        static void Check(ClassWithRecursiveRequiredProperty obj, int lastNumber)
        {
            Assert.True(lastNumber >= 1);

            for (int i = 1; i <= lastNumber; i++)
            {
                Assert.Equal(obj.Value, i);

                if (i == lastNumber)
                {
                    Assert.Null(obj.Next);
                }
                else
                {
                    Assert.NotNull(obj.Next);
                    obj = obj.Next;
                }
            }
        }
    }

    public class ClassWithRecursiveRequiredProperty
    {
        public int Value { get; set; }

        [JsonRequired]
        public ClassWithRecursiveRequiredProperty Next { get; set; }
    }

    [Theory]
    [InlineData(typeof(SimpleClass))]
    [InlineData(typeof(SimpleClassWithSmallParametrizedCtor))]
    [InlineData(typeof(SimpleClassWithLargeParametrizedCtor))]
    public async Task CreationHandlingSetWithAttribute_CanPopulateOrSerialize_Callbacks(Type type)
    {
        Type finalType = typeof(ClassWithWritableProperty<>).MakeGenericType(type);
        int timesOnDeserializingCalled = 0;
        int timesOnDeserializedCalled = 0;

        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == type)
            {
                ti.OnDeserializing = (obj) =>
                {
                    Assert.Equal(timesOnDeserializingCalled, timesOnDeserializedCalled);
                    timesOnDeserializingCalled++;
                };
                ti.OnDeserialized = (obj) =>
                {
                    timesOnDeserializedCalled++;
                    Assert.Equal(timesOnDeserializingCalled, timesOnDeserializedCalled);
                };
            }
        });

        string json = """{"Property": {}}""";
        await DeserializeAndEnsureCallbacksCalled(json, 1);

        json = """{"Property": null}""";
        await DeserializeAndEnsureCallbacksCalled(json, 0);

        json = """{"Property": null, "Property":{}}""";
        await DeserializeAndEnsureCallbacksCalled(json, 1);

        json = """{"Property": null, "Property":{}, "Property":{}, "Property":{}}""";
        await DeserializeAndEnsureCallbacksCalled(json, 3);

        async Task DeserializeAndEnsureCallbacksCalled(string json, int expectedTimesCallbacksCalled)
        {
            var obj = await Serializer.DeserializeWrapper(json, finalType, options);
            Assert.Equal(expectedTimesCallbacksCalled, timesOnDeserializingCalled);
            Assert.Equal(timesOnDeserializingCalled, timesOnDeserializedCalled);
            timesOnDeserializingCalled = 0;
            timesOnDeserializedCalled = 0;
        }
    }

    [Theory]
    [InlineData(typeof(IInterfaceWithInterfacePropertyWithPopulateOnType), typeof(ClassImplementingInterfaceWithPopulateOnTypeWithInterfaceProperty), true)]
    [InlineData(typeof(ClassImplementingInterfaceWithPopulateOnTypeWithInterfaceProperty), null, false)]
    [InlineData(typeof(IInterfaceWithInterfaceProperty), typeof(ClassImplementingInterfaceWithInterfaceProperty), false)]
    [InlineData(typeof(ClassImplementingInterfaceWithInterfaceProperty), null, true)]
    public async Task CreationHandlingSetWithAttributeOnType_Interfaces(Type typeToDeserialize, Type? typeToCreate, bool shouldPopulate)
    {
        JsonSerializerOptions options = Serializer.CreateOptions(modifier: ti =>
        {
            if (ti.Type == typeToDeserialize)
            {
                if (typeToCreate != null)
                {
                    ti.CreateObject = () => Activator.CreateInstance(typeToCreate);
                }

                if (shouldPopulate)
                {
                    Assert.Equal(JsonObjectCreationHandling.Populate, ti.PreferredPropertyObjectCreationHandling);
                }
                else
                {
                    Assert.Null(ti.PreferredPropertyObjectCreationHandling);
                }
            }
        });

        string json = """{"Property":{"StringValue": "NewValue", "IntValue": 43}}""";
        object obj = await Serializer.DeserializeWrapper(json, typeToDeserialize, options);
        ISimpleInterface prop = (ISimpleInterface)typeToDeserialize.GetProperty("Property").GetValue(obj);
        Assert.NotNull(prop);

        if (shouldPopulate)
        {
            Assert.Equal("NewValue", prop.StringValue);
            Assert.Equal(43, prop.IntValue);
        }
        else
        {
            // no getter therefore value is unused
            Assert.Equal("InitialValue", prop.StringValue);
            Assert.Equal(42, prop.IntValue);
        }
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal interface IInterfaceWithInterfacePropertyWithPopulateOnType
    {
        public ISimpleInterface Property { get; }
    }

    internal class ClassImplementingInterfaceWithPopulateOnTypeWithInterfaceProperty : IInterfaceWithInterfacePropertyWithPopulateOnType
    {
        public ISimpleInterface Property { get; } = new SimpleClassImplementingInterface() { StringValue = "InitialValue", IntValue = 42 };
    }

    internal interface IInterfaceWithInterfaceProperty
    {
        public ISimpleInterface Property { get; }
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    internal class ClassImplementingInterfaceWithInterfaceProperty : IInterfaceWithInterfaceProperty
    {
        public ISimpleInterface Property { get; } = new SimpleClassImplementingInterface() { StringValue = "InitialValue", IntValue = 42 };
    }

    internal class SimpleClassImplementingInterface : ISimpleInterface
    {
        public string StringValue { get; set; }
        public int IntValue { get; set; }
    }

    internal interface ISimpleInterface
    {
        public string StringValue { get; set; }
        public int IntValue { get; set; }
    }

    [Theory]
    [MemberData(nameof(CombinationsForPopulatableProperty))]
    public async Task ReplaceOnType_SetWithMetadata_PopulatableProperty_PropagatesCorrectly(bool expectPopulate, JsonObjectCreationHandling optionsValue, JsonObjectCreationHandling? typeValue, JsonObjectCreationHandling? propertyValue)
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: opt =>
        {
            opt.PreferredObjectCreationHandling = optionsValue;
        },
        modifier: ti =>
        {
            if (ti.Type == typeof(ClassWithReplaceOnTypeAndWritableProperty_SimpleClass))
            {
                Assert.Equal(JsonObjectCreationHandling.Replace, ti.PreferredPropertyObjectCreationHandling);
                ti.PreferredPropertyObjectCreationHandling = typeValue;
                ti.Properties[0].ObjectCreationHandling = propertyValue;
            }
        });

        string json = """{"Property":{"StringValue":"NewValue"}}""";
        var obj = await Serializer.DeserializeWrapper<ClassWithReplaceOnTypeAndWritableProperty_SimpleClass>(json, options);
        Assert.Equal("NewValue", obj.Property.StringValue);

        if (expectPopulate)
        {
            Assert.Equal(43, obj.Property.IntValue);
        }
        else
        {
            Assert.Equal(42, obj.Property.IntValue);
        }
    }

    public static IEnumerable<object[]> CombinationsForPopulatableProperty()
    {
        foreach (JsonObjectCreationHandling optionsSett in new JsonObjectCreationHandling[] { JsonObjectCreationHandling.Replace, JsonObjectCreationHandling.Populate })
        {
            foreach (JsonObjectCreationHandling? typeSett in new JsonObjectCreationHandling?[] { null, JsonObjectCreationHandling.Replace, JsonObjectCreationHandling.Populate })
            {
                foreach (JsonObjectCreationHandling? propSett in new JsonObjectCreationHandling?[] { null, JsonObjectCreationHandling.Replace, JsonObjectCreationHandling.Populate })
                {
                    JsonObjectCreationHandling effectiveHandling = propSett ?? typeSett ?? optionsSett;
                    bool result = effectiveHandling == JsonObjectCreationHandling.Populate;
                    yield return new object[] { result, optionsSett, typeSett, propSett };
                }
            }
        }
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Replace)]
    internal class ClassWithReplaceOnTypeAndWritableProperty_SimpleClass
    {
        public SimpleClass Property { get; set; } = new SimpleClass()
        {
            StringValue = "InitialValue",
            IntValue = 43,
        };
    }

    [Theory]
    [MemberData(nameof(CombinationsForNonPopulatableProperty))]
    public async Task ReplaceOnType_SetWithMetadata_NonPopulatableProperty_PropagatesCorrectly(bool expectError, JsonObjectCreationHandling optionsValue, JsonObjectCreationHandling? typeValue, JsonObjectCreationHandling? propertyValue)
    {
        JsonSerializerOptions options = Serializer.CreateOptions(configure: opt =>
        {
            opt.PreferredObjectCreationHandling = optionsValue;
        },
        modifier: ti =>
        {
            if (ti.Type == typeof(SimpleClassWitNonPopulatableProperty))
            {
                ti.PreferredPropertyObjectCreationHandling = typeValue;
                ti.Properties[0].ObjectCreationHandling = propertyValue;
            }
        });

        string json = """{"Property":9}""";
        
        if (expectError)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper<SimpleClassWitNonPopulatableProperty>(json, options));
        }
        else
        {
            var obj = await Serializer.DeserializeWrapper<SimpleClassWitNonPopulatableProperty>(json, options);
            Assert.Equal(9, obj.Property);
        }
    }

    public static IEnumerable<object[]> CombinationsForNonPopulatableProperty()
    {
        foreach (JsonObjectCreationHandling optionsSett in new JsonObjectCreationHandling[] { JsonObjectCreationHandling.Replace, JsonObjectCreationHandling.Populate })
        {
            foreach (JsonObjectCreationHandling? typeSett in new JsonObjectCreationHandling?[] { null, JsonObjectCreationHandling.Replace, JsonObjectCreationHandling.Populate })
            {
                foreach (JsonObjectCreationHandling? propSett in new JsonObjectCreationHandling?[] { null, JsonObjectCreationHandling.Replace, JsonObjectCreationHandling.Populate })
                {
                    bool result = propSett == JsonObjectCreationHandling.Populate;
                    yield return new object[] { result, optionsSett, typeSett, propSett };
                }
            }
        }
    }

    internal class SimpleClassWitNonPopulatableProperty
    {
        public int Property { get; set; } = 7;
    }

    [Fact]
    public async Task CreationHandlingSetWithAttribute_CanPopulate_Class()
    {
        JsonSerializerOptions options = Serializer.CreateOptions();
        string json = """
            {
                "PopulatedPropertyReadOnly":
                {
                    "IntValue": 43
                },
                "PopulatedPropertySimple":
                {
                    "IntValue": 44
                }
            }
            """;

        var obj = await Serializer.DeserializeWrapper<ClassWithClassProperty>(json, options);
        Assert.NotNull(obj);
        Assert.Equal("InitialForPopulate1", obj.PopulatedPropertyReadOnly.StringValue);
        Assert.Equal(43, obj.PopulatedPropertyReadOnly.IntValue);

        Assert.Equal("InitialForPopulate2", obj.PopulatedPropertySimple.StringValue);
        Assert.Equal(44, obj.PopulatedPropertySimple.IntValue);
    }

    internal class ClassWithClassProperty
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public SomeClass PopulatedPropertyReadOnly { get; } = new() { StringValue = "InitialForPopulate1" };

        private SomeClass _populatedSimple = new() { StringValue = "InitialForPopulate2" };

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public SomeClass PopulatedPropertySimple
        {
            get => _populatedSimple;
            set => Assert.Fail("Setter should not be used");
        }

        private SomeClass _populatedWithChildren =
            new()
            {
                StringValue = "InitialForPopulate3",
                ReplacedChild = new()
                {
                    StringValue = "ShouldBeReplaced",
                    IntValue = 123,
                },
                PopulatedChild = new()
                {
                    StringValue = "InitialForPopulate4",
                    IntValue = 43,
                    ReplacedChild = new() { StringValue = "ShouldBeReplaced" }
                },
            };

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public SomeClass PopulatedPropertyWithChildren
        {
            get => _populatedWithChildren;
            set => Assert.Fail("Setter should not be used");
        }
    }

    internal class SomeClass
    {
        public string StringValue { get; set; } = "InitialSomeClass";
        public int IntValue { get; set; } = 42;
        public SomeClass? ReplacedChild { get; set; }

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public SomeClass? PopulatedChild { get; set; }
    }

    [Theory]
    [InlineData((JsonObjectCreationHandling)(-1))]
    [InlineData((JsonObjectCreationHandling)2)]
    [InlineData((JsonObjectCreationHandling)int.MaxValue)]
    public void JsonObjectCreationHandlingAttribute_InvalidConstructorArgument_ThrowsArgumentOutOfRangeException(JsonObjectCreationHandling handling)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new JsonObjectCreationHandlingAttribute(handling));

        Assert.Contains("handling", ex.ToString());
    }

    [Fact]
    public async Task JsonObjectCreationHandlingAttribute_InvalidAnnotations_ThrowsArgumentOutOfRangeException()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Serializer.SerializeWrapper(new ClassWithInvalidTypeAnnotation()));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Serializer.SerializeWrapper(new ClassWithInvalidPropertyAnnotation()));
    }

    [JsonObjectCreationHandling((JsonObjectCreationHandling)(-1))]
    public class ClassWithInvalidTypeAnnotation
    {
    }

    public class ClassWithInvalidPropertyAnnotation
    {
        [JsonObjectCreationHandling((JsonObjectCreationHandling)(-1))]
        public List<int> Property { get; }
    }

    [Theory]
    [InlineData(typeof(ClassWithParameterizedConstructorWithPopulateProperty))]
    [InlineData(typeof(ClassWithParameterizedConstructorWithPopulateType))]
    public async Task ClassWithParameterizedCtor_UsingPopulateConfiguration_ThrowsNotSupportedException(Type type)
    {
        object instance = Activator.CreateInstance(type, "Jim");
        string json = """{"Username":"Jim","PhoneNumbers":["123456"]}""";

        await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.SerializeWrapper(instance, type));
        await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.DeserializeWrapper(json, type));
        Assert.Throws<NotSupportedException>(() => Serializer.GetTypeInfo(type));
    }

    public class ClassWithParameterizedConstructorWithPopulateProperty(string name)
    {
        public string Name { get; } = name;

        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public List<string> PhoneNumbers { get; } = new();
    }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public class ClassWithParameterizedConstructorWithPopulateType(string name)
    {
        public string Name { get; } = name;

        public List<string> PhoneNumbers { get; } = new();
    }

    [Fact]
    public async Task ClassWithParameterizedCtor_NoPopulateConfiguration_WorksWithGlobalPopulateConfiguration()
    {
        string json = """{"Username":"Jim","PhoneNumbers":["123456"]}""";

        JsonSerializerOptions options = Serializer.CreateOptions(makeReadOnly: false);
        options.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;

        ClassWithParameterizedConstructorNoPopulate result = await Serializer.DeserializeWrapper<ClassWithParameterizedConstructorNoPopulate>(json, options);
        Assert.Empty(result.PhoneNumbers);
    }

    public class ClassWithParameterizedConstructorNoPopulate(string name)
    {
        public string Name { get; } = name;

        public List<string> PhoneNumbers { get; } = new();
    }
}
