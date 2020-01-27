// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Concurrent;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonImmutableDictionaryOfStringTValueConverter<TCollection, TValue> : JsonDictionaryDefaultConverter<TCollection, TValue>
        where TCollection : IReadOnlyDictionary<string, TValue>
    {
        protected override void Add(TValue value, JsonSerializerOptions options, ref ReadStack state)
        {
            string key = state.Current.KeyName!;
            ((Dictionary<string, TValue>)state.Current.ReturnValue!)[key] = value;
        }

        internal override bool CanHaveMetadata => false;

        protected override void CreateCollection(ref ReadStack state)
        {
            state.Current.ReturnValue = new Dictionary<string, TValue>();
        }

        protected override void ConvertCollection(ref ReadStack state, JsonSerializerOptions options)
        {
            state.Current.ReturnValue = GetCreatorDelegate(options)((Dictionary<string, TValue>)state.Current.ReturnValue!);
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

        internal override Type RuntimeType => TypeToConvert;

        private Func<IEnumerable<KeyValuePair<string, TValue>>, TCollection>? _creatorDelegate;

        private Func<IEnumerable<KeyValuePair<string, TValue>>, TCollection> GetCreatorDelegate(JsonSerializerOptions options)
        {
            if (_creatorDelegate == null)
            {
                _creatorDelegate = options.MemberAccessorStrategy.CreateImmutableDictionaryCreateRangeDelegate<TValue, TCollection>();
            }

            return _creatorDelegate;
        }
    }
}
