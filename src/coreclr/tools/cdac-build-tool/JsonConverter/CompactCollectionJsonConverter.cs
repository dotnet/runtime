// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Diagnostics.DataContract.BuildTool;

namespace Microsoft.DotNet.Diagnostics.DataContract.JsonConverter;

/// <summary>
///  Parses or serializes dictionary collections in the "compact" JSON format:
///  the keys become property names and the values the property values.
///  <see cref="https://github.com/dotnet/runtime/blob/main/docs/design/datacontracts/data_descriptor.md"/>.
/// </summary>
public class CompactCollectionModelJsonConverter<TCollection, T> : JsonConverter<TCollection>
    where TCollection : IEnumerable<KeyValuePair<string, T>>
{
    public override TCollection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, TCollection collection, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var (key, value) in collection)
        {
            writer.WritePropertyName(key);
            JsonSerializer.Serialize(writer, value, options);
        }
        writer.WriteEndObject();
    }
}

public sealed class GlobalsCollectionModelJsonConverter : CompactCollectionModelJsonConverter<DataDescriptorModel.GlobalsCollectionModel, DataDescriptorModel.GlobalModel>
{
}

public sealed class TypesCollectionModelJsonConverter : CompactCollectionModelJsonConverter<DataDescriptorModel.TypesCollectionModel, DataDescriptorModel.TypeModel>
{
}
