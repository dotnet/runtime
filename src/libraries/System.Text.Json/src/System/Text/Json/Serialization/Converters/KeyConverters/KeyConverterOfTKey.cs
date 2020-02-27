// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization.Converters
{
    internal abstract class KeyConverter<TKey> : JsonConverter<TKey> where TKey : notnull
    {
        // This is less API friendly than just call Read and keep the resulting dictionary key boxed in state.Current.
        // Maybe we can call this only for internal converters.
        public abstract TKey ReadKeyFromBytes(ReadOnlySpan<byte> bytes);

        internal override bool OnTryWrite(Utf8JsonWriter writer, TKey value, JsonSerializerOptions options, ref WriteStack state)
        {
            if (CanBePolymorphic)
            {
                JsonConverter runtimeConverter = GetPolymorphicConverter(value, options, ref state);
                // Redirect to the runtime-type key converter.
                runtimeConverter.WriteKeyAsObject(writer, value, options, ref state);
            }
            // If we need to apply the policy, we are forced to get a string since that is the only type that ConvertName can take as argument.
            else if (options.DictionaryKeyPolicy != null && !state.Current.IgnoreDictionaryKeyPolicy)
            {
                string keyAsString = value.ToString()!;
                keyAsString = options.DictionaryKeyPolicy.ConvertName(keyAsString);

                if (keyAsString == null)
                {
                    ThrowHelper.ThrowInvalidOperationException_SerializerDictionaryKeyNull(options.DictionaryKeyPolicy.GetType());
                }

                writer.WritePropertyName(keyAsString);
            }
            else
            {
                Write(writer, value, options);
            }

            return true;
        }

        private JsonConverter GetPolymorphicConverter(object value, JsonSerializerOptions options, ref WriteStack state)
        {
            Type runtimeType = value.GetType();
            JsonConverter runtimeConverter = options.GetOrAddKeyConverter(runtimeType, state.Current.JsonClassInfo.Type);

            // We don't support object itself as TKey, only the other supported types when they are boxed.
            if (runtimeConverter is JsonConverter<object>)
            {
                ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(state.Current.JsonClassInfo.Type);
            }

            return runtimeConverter;
        }

        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out TKey value)
        {
            // We already called reader.GetString(), there is no need to do it again for string keys.
            if (typeof(TKey) == typeof(string))
            {
                value = (TKey)(object)state.Current.JsonPropertyNameAsString!;
            }
            else
            {
                // Fast path that does not support continuation.
                if (!state.SupportContinuation && !options.ReferenceHandling.ShouldReadPreservedReferences())
                {
                    value = Read(ref reader, typeToConvert, options);
                }
                // Slow path where the reader is no longer in TokenType.PropertyName, is in the element; i.e. in TValue.
                // We remember the PropertyName bytes in the state and parse the key from there.
                else
                {
                    value = ReadKeyFromBytes(state.Current.DictionaryKeyName);
                }
            }

            return true;
        }

        internal override void WriteKeyAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options, ref WriteStack state)
            => OnTryWrite(writer, (TKey)value, options, ref state);
    }
}
