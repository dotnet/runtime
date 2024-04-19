// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class ReadOnlyMemoryConverter<T> : JsonCollectionConverter<ReadOnlyMemory<T>, T>
    {
        internal override bool CanHaveMetadata => false;
        public override bool HandleNull => true;

        internal override bool OnTryRead(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options,
            scoped ref ReadStack state,
            out ReadOnlyMemory<T> value)
        {
            if (reader.TokenType is JsonTokenType.Null)
            {
                value = default;
                return true;
            }

            return base.OnTryRead(ref reader, typeToConvert, options, ref state, out value);
        }

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
            ReadOnlyMemory<T> memory = ((List<T>)state.Current.ReturnValue!).ToArray().AsMemory();
            state.Current.ReturnValue = memory;
        }

        protected override bool OnWriteResume(Utf8JsonWriter writer, ReadOnlyMemory<T> value, JsonSerializerOptions options, ref WriteStack state)
        {
            return OnWriteResume(writer, value.Span, options, ref state);
        }

        internal static bool OnWriteResume(Utf8JsonWriter writer, ReadOnlySpan<T> value, JsonSerializerOptions options, ref WriteStack state)
        {
            int index = state.Current.EnumeratorIndex;

            JsonConverter<T> elementConverter = GetElementConverter(ref state);

            if (elementConverter.CanUseDirectReadOrWrite && state.Current.NumberHandling == null)
            {
                // Fast path that avoids validation and extra indirection.
                for (; index < value.Length; index++)
                {
                    elementConverter.Write(writer, value[index], options);
                }
            }
            else
            {
                for (; index < value.Length; index++)
                {
                    if (!elementConverter.TryWrite(writer, value[index], options, ref state))
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
