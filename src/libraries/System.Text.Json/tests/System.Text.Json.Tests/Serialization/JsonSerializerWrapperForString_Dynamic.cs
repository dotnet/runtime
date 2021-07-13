// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Tests
{
    internal sealed class JsonSerializerWrapperForString_Dynamic
        : JsonSerializerWrapperForString
    {
        protected internal override Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions options = null)
            => Task.FromResult(JsonSerializer.Deserialize<T>(json, options));

        protected internal override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions options = null)
            => Task.FromResult(JsonSerializer.Deserialize(json, type,  options));

        protected internal override Task<T> DeserializeWrapper<T>(string json, JsonTypeInfo<T> jsonTypeInfo) => throw new NotImplementedException();

        protected internal override Task<object> DeserializeWrapper(string json, Type type, JsonSerializerContext context) => throw new NotImplementedException();

        protected internal override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerOptions options = null)
            => Task.FromResult(JsonSerializer.Serialize(value, inputType, options));

        protected internal override Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions options = null)
            => Task.FromResult(JsonSerializer.Serialize<T>(value, options));

        protected internal override Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context) => throw new NotImplementedException();

        protected internal override Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo) => throw new NotImplementedException();
    }
}
