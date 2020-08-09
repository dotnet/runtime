// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Implementation of <cref>JsonObjectConverter{T}</cref> that supports the deserialization
    /// of JSON objects using parameterized constructors.
    /// </summary>
    internal sealed class ObjectCodeGenConverter<T> : ObjectDefaultConverter<T>
    {
        internal override bool OnTryRead(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options,
            ref ReadStack state,
            [MaybeNullWhen(false)] out T value)
        {
            if (!state.UseFastPath)
            {
                return base.OnTryRead(ref reader, typeToConvert, options, ref state, out value);
            }

            JsonClassInfo jsonClassInfo = state.Current.JsonClassInfo;
            Debug.Assert(jsonClassInfo.Deserialize != null);

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
            }

            object objValue = jsonClassInfo.Deserialize(ref reader, ref state, jsonClassInfo.Options);

            Debug.Assert(objValue is T);
            value = (T)objValue;

            return true;
        }

        internal override bool OnTryWrite(
            Utf8JsonWriter writer,
            T value,
            JsonSerializerOptions options,
            ref WriteStack state)
        {
            if (state.SupportContinuation || options.ReferenceHandler != null)
            {
                return base.OnTryWrite(writer, value, options, ref state);
            }

            JsonClassInfo jsonClassInfo = state.Current.JsonClassInfo;
            Debug.Assert(jsonClassInfo.Serialize != null);

            writer.WriteStartObject();
            jsonClassInfo.Serialize(writer, value!, ref state, options);
            writer.WriteEndObject();

            return true;
        }
    }
}
