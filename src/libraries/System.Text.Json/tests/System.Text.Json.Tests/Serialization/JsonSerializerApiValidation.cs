// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Serialization.Tests;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class JsonSerializerApiValidation_Span : JsonSerializerApiValidation
    {
        public JsonSerializerApiValidation_Span() : base(JsonSerializerWrapperForString.SpanSerializer) { }
    }

    public class JsonSerializerApiValidation_String : JsonSerializerApiValidation
    {
        public JsonSerializerApiValidation_String() : base(JsonSerializerWrapperForString.StringSerializer) { }
    }

    public class JsonSerializerApiValidation_AsyncStream : JsonSerializerApiValidation
    {
        public JsonSerializerApiValidation_AsyncStream() : base(JsonSerializerWrapperForString.AsyncStreamSerializer) { }
    }

    public class JsonSerializerApiValidation_SyncStream : JsonSerializerApiValidation
    {
        public JsonSerializerApiValidation_SyncStream() : base(JsonSerializerWrapperForString.SyncStreamSerializer) { }
    }

    public class JsonSerializerApiValidation_Writer : JsonSerializerApiValidation
    {
        public JsonSerializerApiValidation_Writer() : base(JsonSerializerWrapperForString.ReaderWriterSerializer) { }
    }

    public class JsonSerializerApiValidation_Document : JsonSerializerApiValidation
    {
        public JsonSerializerApiValidation_Document() : base(JsonSerializerWrapperForString.DocumentSerializer) { }
    }

    public class JsonSerializerApiValidation_Element : JsonSerializerApiValidation
    {
        public JsonSerializerApiValidation_Element() : base(JsonSerializerWrapperForString.ElementSerializer) { }
    }

    public class JsonSerializerApiValidation_Node : JsonSerializerApiValidation
    {
        public JsonSerializerApiValidation_Node() : base(JsonSerializerWrapperForString.NodeSerializer) { }
    }
}

/// <summary>
/// Verifies input values for public JsonSerializer methods.
/// </summary>
public abstract class JsonSerializerApiValidation
{
    private class MyPoco { }

    internal partial class MyDummyContext : JsonSerializerContext
    {
        public MyDummyContext() : base(new JsonSerializerOptions(), new JsonSerializerOptions()) { }
        public MyDummyContext(JsonSerializerOptions options) : base(options, new JsonSerializerOptions()) { }
        public override JsonTypeInfo? GetTypeInfo(Type type) => throw new NotImplementedException();
    }

    private JsonTypeInfo<MyPoco> myDummyTypeInfo = JsonMetadataServices.CreateObjectInfo<MyPoco>(
        new JsonSerializerOptions(),
        createObjectFunc: static () => throw new NotImplementedException(),
        propInitFunc: null,
        default,
        serializeFunc: (Utf8JsonWriter writer, MyPoco value) => throw new NotImplementedException());

    private JsonSerializerWrapperForString Serializer { get; }

    public JsonSerializerApiValidation(JsonSerializerWrapperForString serializer)
    {
        Serializer = serializer;
    }

    [Fact]
    public async Task DeserializeNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await Serializer.DeserializeWrapper<MyPoco>(json: "{}", jsonTypeInfo: null));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await Serializer.DeserializeWrapper(json: "{}", type: null));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await Serializer.DeserializeWrapper(json: "{}", type: typeof(MyPoco), context: null));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await Serializer.DeserializeWrapper(json: "{}", type: null, context: new MyDummyContext()));

        if (!Serializer.SupportsNullValueOnDeserialize)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await Serializer.DeserializeWrapper(json: null, type: typeof(MyPoco), context: new MyDummyContext()));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await Serializer.DeserializeWrapper(json: null, type: typeof(MyPoco)));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await Serializer.DeserializeWrapper<MyPoco>(json: null));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await Serializer.DeserializeWrapper<MyPoco>(json: null, jsonTypeInfo: myDummyTypeInfo));
        }
    }

    [Fact]
    public async Task SerializeNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await Serializer.SerializeWrapper<MyPoco>(value: new MyPoco(), jsonTypeInfo: null));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await Serializer.SerializeWrapper(value: new MyPoco(), inputType: null));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await Serializer.SerializeWrapper(value: new MyPoco(), inputType: typeof(MyPoco), context: null));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await Serializer.SerializeWrapper(value: new MyPoco(), inputType: null, context: new MyDummyContext()));
    }
}
