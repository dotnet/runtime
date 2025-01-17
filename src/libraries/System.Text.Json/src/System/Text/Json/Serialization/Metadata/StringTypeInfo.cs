// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Metadata
{
    //internal class StringTypeInfo : JsonTypeInfo
    //{
    //    internal static readonly StringTypeInfo Instance = new StringTypeInfo();

    //    private StringTypeInfo() { }

    //    internal override object? DeserializeAsObject(ref Utf8JsonReader reader, ref ReadStack state) => throw new NotImplementedException();
    //    internal override object? DeserializeAsObject(Stream utf8Json) => throw new NotImplementedException();
    //    internal override ValueTask<object?> DeserializeAsObjectAsync(Stream utf8Json, CancellationToken cancellationToken) => throw new NotImplementedException();
    //    internal override void SerializeAsObject(Utf8JsonWriter writer, object? rootValue) => throw new NotImplementedException();
    //    internal override void SerializeAsObject(Stream utf8Json, object? rootValue) => throw new NotImplementedException();
    //    internal override Task SerializeAsObjectAsync(PipeWriter pipeWriter, object? rootValue, int flushThreshold, CancellationToken cancellationToken) => throw new NotImplementedException();
    //    internal override Task SerializeAsObjectAsync(Stream utf8Json, object? rootValue, CancellationToken cancellationToken) => throw new NotImplementedException();
    //    internal override Task SerializeAsObjectAsync(PipeWriter utf8Json, object? rootValue, CancellationToken cancellationToken) => throw new NotImplementedException();
    //    private protected override JsonPropertyInfo CreateJsonPropertyInfo(JsonTypeInfo declaringTypeInfo, Type? declaringType, JsonSerializerOptions options) => throw new NotImplementedException();
    //    private protected override JsonPropertyInfo CreatePropertyInfoForTypeInfo() => throw new NotImplementedException();
    //    private protected override void SetCreateObject(Delegate? createObject) => throw new NotImplementedException();
    //}
}
