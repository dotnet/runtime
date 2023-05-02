// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

#nullable enable

public static class Program
{
    // Validates that expected the components are trimmed when
    // the IsReflectionEnabledByDefault feature switch is turned off.
    public static int Main()
    {
        MyPoco valueToSerialize = new MyPoco { Value = 42 };

        // The default resolver should not surface DefaultJsonTypeInfoResolver.
        if (JsonSerializerOptions.Default.TypeInfoResolver is not IList<IJsonTypeInfoResolver> { Count: 0 })
        {
            return -1;
        }

        // Serializing with options unset should throw NotSupportedException.
        try
        {
            JsonSerializer.Serialize(valueToSerialize);
            return -2;
        }
        catch (NotSupportedException)
        {
        }

        // Serializing with default options unset should throw InvalidOperationException.
        var options = new JsonSerializerOptions();
        try
        {
            JsonSerializer.Serialize(valueToSerialize, options);
            return -3;
        }
        catch (InvalidOperationException)
        {
        }

        // Serializing with a custom resolver should work as expected.
        options.TypeInfoResolver = new MyJsonResolver();
        if (JsonSerializer.Serialize(valueToSerialize, options) != "{\"Value\":42}")
        {
            return -4;
        }

        // The Default resolver should have been trimmed from the application.
        Type? reflectionResolver = GetJsonType("System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver");
        if (reflectionResolver != null)
        {
            return -5;
        }

        return 100;
    }

    // The intention of this method is to ensure the trimmer doesn't preserve the Type.
    private static Type? GetJsonType(string name) =>
        typeof(JsonSerializer).Assembly.GetType(name, throwOnError: false);
}

public class MyPoco
{
    public int Value { get; set; }
}

public class MyJsonResolver : JsonSerializerContext, IJsonTypeInfoResolver
{
    public MyJsonResolver() : base(null) { }
    protected override JsonSerializerOptions? GeneratedSerializerOptions => null;
    public override JsonTypeInfo? GetTypeInfo(Type type) => GetTypeInfo(type, Options);

    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        if (type == typeof(int))
        {
            return Create_Int32(options);
        }

        if (type == typeof(MyPoco))
        {
            return Create_MyPoco(options);
        }

        return null;
    }

    private global::System.Text.Json.Serialization.Metadata.JsonTypeInfo<global::MyPoco> Create_MyPoco(global::System.Text.Json.JsonSerializerOptions options)
    {
        global::System.Text.Json.Serialization.Metadata.JsonTypeInfo<global::MyPoco>? jsonTypeInfo = null;
        global::System.Text.Json.Serialization.Metadata.JsonObjectInfoValues<global::MyPoco> objectInfo = new global::System.Text.Json.Serialization.Metadata.JsonObjectInfoValues<global::MyPoco>()
        {
            ObjectCreator = static () => new global::MyPoco(),
            ObjectWithParameterizedConstructorCreator = null,
            PropertyMetadataInitializer = _ => MyPocoPropInit(options),
            ConstructorParameterMetadataInitializer = null,
            NumberHandling = default,
        };

        jsonTypeInfo = global::System.Text.Json.Serialization.Metadata.JsonMetadataServices.CreateObjectInfo<global::MyPoco>(options, objectInfo);
        jsonTypeInfo.OriginatingResolver = this;
        return jsonTypeInfo;
    }

    private static global::System.Text.Json.Serialization.Metadata.JsonPropertyInfo[] MyPocoPropInit(global::System.Text.Json.JsonSerializerOptions options)
    {
        global::System.Text.Json.Serialization.Metadata.JsonPropertyInfo[] properties = new global::System.Text.Json.Serialization.Metadata.JsonPropertyInfo[1];

        global::System.Text.Json.Serialization.Metadata.JsonPropertyInfoValues<global::System.Int32> info0 = new global::System.Text.Json.Serialization.Metadata.JsonPropertyInfoValues<global::System.Int32>()
        {
            IsProperty = true,
            IsPublic = true,
            IsVirtual = false,
            DeclaringType = typeof(global::MyPoco),
            Converter = null,
            Getter = static (obj) => ((global::MyPoco)obj).Value,
            Setter = static (obj, value) => ((global::MyPoco)obj).Value = value!,
            IgnoreCondition = null,
            HasJsonInclude = false,
            IsExtensionData = false,
            NumberHandling = default,
            PropertyName = "Value",
            JsonPropertyName = null
        };

        global::System.Text.Json.Serialization.Metadata.JsonPropertyInfo propertyInfo0 = global::System.Text.Json.Serialization.Metadata.JsonMetadataServices.CreatePropertyInfo<global::System.Int32>(options, info0);
        properties[0] = propertyInfo0;

        return properties;
    }

    private global::System.Text.Json.Serialization.Metadata.JsonTypeInfo<global::System.Int32> Create_Int32(global::System.Text.Json.JsonSerializerOptions options)
    {
        global::System.Text.Json.Serialization.Metadata.JsonTypeInfo<global::System.Int32>? jsonTypeInfo = null;
        jsonTypeInfo = global::System.Text.Json.Serialization.Metadata.JsonMetadataServices.CreateValueInfo<global::System.Int32>(options, global::System.Text.Json.Serialization.Metadata.JsonMetadataServices.Int32Converter);
        jsonTypeInfo.OriginatingResolver = this;
        return jsonTypeInfo;
    }
}
