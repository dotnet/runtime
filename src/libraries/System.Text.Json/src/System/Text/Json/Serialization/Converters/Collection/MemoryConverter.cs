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
            int index = state.Current.EnumeratorIndex;

            JsonConverter<T> elementConverter = GetElementConverter(ref state);
            ReadOnlySpan<T> valueSpan = value.Span;

            if (elementConverter.CanUseDirectReadOrWrite && state.Current.NumberHandling == null)
            {
                // Fast path that avoids validation and extra indirection.
                for (; index < valueSpan.Length; index++)
                {
                    elementConverter.Write(writer, valueSpan[index], options);
                }
            }
            else
            {
                for (; index < value.Length; index++)
                {
                    T element = valueSpan[index];
                    if (!elementConverter.TryWrite(writer, element, options, ref state))
                    {
                        state.Current.EnumeratorIndex = index;
                        return false;
                    }

                    state.Current.EndCollectionElement();

                    if (ShouldFlush(writer, ref state))
                    {
                        state.Current.EnumeratorIndex = ++index;
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
