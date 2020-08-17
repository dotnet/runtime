// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter for <cref>System.Collections.Generic.IDictionary{string, TValue}</cref> that
    /// (de)serializes as a JSON object with properties representing the dictionary element key and value.
    /// </summary>
    internal sealed class IDictionaryOfTKeyTValueConverter<TCollection, TKey, TValue>
        : DictionaryDefaultConverter<TCollection, TKey, TValue>
        where TCollection : IDictionary<TKey, TValue>
        where TKey : notnull
    {
        protected override void Add(TKey key, in TValue value, JsonSerializerOptions options, ref ReadStack state)
        {
            ((TCollection)state.Current.ReturnValue!)[key] = value;
        }

        protected override void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state)
        {
            JsonClassInfo classInfo = state.Current.JsonClassInfo;

            if (TypeToConvert.IsInterface || TypeToConvert.IsAbstract)
            {
                if (!TypeToConvert.IsAssignableFrom(RuntimeType))
                {
                    ThrowHelper.ThrowNotSupportedException_CannotPopulateCollection(TypeToConvert, ref reader, ref state);
                }

                state.Current.ReturnValue = new Dictionary<string, TValue>();
            }
            else
            {
                if (classInfo.CreateObject == null)
                {
                    ThrowHelper.ThrowNotSupportedException_DeserializeNoConstructor(TypeToConvert, ref reader, ref state);
                }

                TCollection returnValue = (TCollection)classInfo.CreateObject()!;

                if (returnValue.IsReadOnly)
                {
                    ThrowHelper.ThrowNotSupportedException_CannotPopulateCollection(TypeToConvert, ref reader, ref state);
                }

                state.Current.ReturnValue = returnValue;
            }
        }

        protected internal override bool OnWriteResume(
            Utf8JsonWriter writer,
            TCollection value,
            JsonSerializerOptions options,
            ref WriteStack state)
        {
            IEnumerator<KeyValuePair<TKey, TValue>> enumerator;
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
                enumerator = (IEnumerator<KeyValuePair<TKey, TValue>>)state.Current.CollectionEnumerator;
            }

            JsonConverter<TKey> keyConverter = _keyConverter ??= GetKeyConverter(KeyType, options);
            JsonConverter<TValue> valueConverter = _valueConverter ??= GetValueConverter(state.Current.JsonClassInfo.ElementClassInfo!);
            do
            {
                if (ShouldFlush(writer, ref state))
                {
                    state.Current.CollectionEnumerator = enumerator;
                    return false;
                }

                if (state.Current.PropertyState < StackFramePropertyState.Name)
                {
                    state.Current.PropertyState = StackFramePropertyState.Name;
                    TKey key = enumerator.Current.Key;
                    keyConverter.WriteWithQuotes(writer, key, options, ref state);
                }

                TValue element = enumerator.Current.Value;
                if (!valueConverter.TryWrite(writer, element, options, ref state))
                {
                    state.Current.CollectionEnumerator = enumerator;
                    return false;
                }

                state.Current.EndDictionaryElement();
            } while (enumerator.MoveNext());

            return true;
        }

        internal override Type RuntimeType
        {
            get
            {
                if (TypeToConvert.IsAbstract || TypeToConvert.IsInterface)
                {
                    return typeof(Dictionary<TKey, TValue>);
                }

                return TypeToConvert;
            }
        }
    }
}
