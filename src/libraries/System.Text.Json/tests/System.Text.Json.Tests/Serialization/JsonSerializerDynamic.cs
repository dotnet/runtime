// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Tests
{
    internal sealed class JsonSerializerDynamic : JsonSerializerWrapper
    {
        public override T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json);

        public override T Deserialize<T>(string json, JsonSerializerOptions options) => JsonSerializer.Deserialize<T>(json, options);

        public override object Deserialize(string json, Type type) => JsonSerializer.Deserialize(json, type);
        public override object Deserialize(string json, Type type, JsonSerializerOptions options) => JsonSerializer.Deserialize(json, type, options);

        public override string Serialize<T>(T value) => JsonSerializer.Serialize(value);

        public override string Serialize<T>(T value, JsonSerializerOptions options) => JsonSerializer.Serialize(value, options);

        public override string Serialize(object value, Type type) => JsonSerializer.Serialize(value, type);
    }
}
