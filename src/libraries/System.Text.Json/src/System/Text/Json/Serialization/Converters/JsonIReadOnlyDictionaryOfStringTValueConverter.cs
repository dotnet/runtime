// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonIReadOnlyDictionaryOfStringTValueConverter<TCollection, TValue> : JsonDictionaryDefaultConverter<TCollection, TValue>
        where TCollection : IReadOnlyDictionary<string, TValue>
    {
        protected override void Add(TValue value, JsonSerializerOptions options, ref ReadStack state)
        {
            string key = state.Current.KeyName!;
            ((Dictionary<string, TValue>)state.Current.ReturnValue!)[key] = value;
        }

        protected override void CreateCollection(ref ReadStack state)
        {
            if (!TypeToConvert.IsAssignableFrom(RuntimeType))
            {
                ThrowHelper.ThrowNotSupportedException_DeserializeNoParameterlessConstructor(TypeToConvert);
            }

            state.Current.ReturnValue = new Dictionary<string, TValue>();
        }

        protected internal override bool OnWriteResume(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state)
        {
            IEnumerator<KeyValuePair<string, TValue>> enumerator;
            if (state.Current.CollectionEnumerator == null)
            {
                enumerator = value.GetEnumerator();
                if (!enumerator.MoveNext())
                {
                    return true;
                }
            }
            else
            {
                enumerator = (Dictionary<string, TValue>.Enumerator)state.Current.CollectionEnumerator;
            }

            JsonConverter<TValue> converter = GetValueConverter(ref state);
            do
            {
                string key = GetKeyName(enumerator.Current.Key, ref state, options);
                writer.WritePropertyName(key);

                TValue element = enumerator.Current.Value;
                if (!converter.TryWrite(writer, element, options, ref state))
                {
                    state.Current.CollectionEnumerator = enumerator;
                    return false;
                }

                state.Current.EndElement();
            } while (enumerator.MoveNext());

            return true;
        }

        internal override Type RuntimeType => typeof(Dictionary<string, TValue>);
    }
}
