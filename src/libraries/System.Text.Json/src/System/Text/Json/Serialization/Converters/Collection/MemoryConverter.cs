// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class MemoryConverter<T> : JsonCollectionConverter<Memory<T>, T>
    {
        internal override bool CanHaveMetadata => false;

        protected override void Add(in T value, ref ReadStack state)
        {
            ((List<T>)state.Current.ReturnValue!).Add(value);
        }

        protected override void CreateCollection(ref Utf8JsonReader reader, scoped ref ReadStack state, JsonSerializerOptions options)
        {
            state.Current.ReturnValue = new List<T>();
        }

        protected override void ConvertCollection(ref ReadStack state, JsonSerializerOptions options)
        {
            Memory<T> memory = ((List<T>)state.Current.ReturnValue!).ToArray().AsMemory();
            state.Current.ReturnValue = memory;
        }

        protected override bool OnWriteResume(Utf8JsonWriter writer, Memory<T> value, JsonSerializerOptions options, ref WriteStack state)
        {
            return ReadOnlyMemoryConverter<T>.OnWriteResume(writer, value.Span, options, ref state);
        }
    }
}
