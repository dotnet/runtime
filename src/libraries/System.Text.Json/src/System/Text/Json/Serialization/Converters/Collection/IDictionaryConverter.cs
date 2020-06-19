// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter for <cref>System.Collections.IDictionary</cref> that (de)serializes as a JSON object with properties
    /// representing the dictionary element key and value.
    /// </summary>
    internal sealed class IDictionaryConverter<TCollection>
        : DictionaryDefaultConverter<TCollection, string, object?>
        where TCollection : IDictionary
    {
        protected override void Add(in string key, in object? value, JsonSerializerOptions options, ref ReadStack state)
        {
            ((IDictionary)state.Current.ReturnValue!)[key] = value;
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

                state.Current.ReturnValue = new Dictionary<string, object>();
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

        protected internal override bool OnWriteResume(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state)
        {
            IDictionaryEnumerator enumerator;
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
                enumerator = (IDictionaryEnumerator)state.Current.CollectionEnumerator;
            }

            JsonConverter<object?> converter = GetValueConverter(ref state);
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
                    object key = enumerator.Key;
                    // Optimize for string since that's the hot path.
                    if (key is string keyString)
                    {
                        JsonConverter<string> stringKeyConverter = GetKeyConverter(state.Current.JsonClassInfo);
                        stringKeyConverter.WriteWithQuotes(writer, keyString, options, ref state);
                    }
                    else
                    {
                        // IDictionary is a spacial case since it has polymorphic object semantics on serialization
                        // but needs to use JsonConverter<string> on deserialization.
                        JsonClassInfo classInfo = options.GetOrAddClass(typeof(object));
                        JsonConverter<object> objectKeyConverter = (JsonConverter<object>)classInfo.PropertyInfoForClassInfo.ConverterBase;
                        objectKeyConverter.WriteWithQuotes(writer, key, options, ref state);
                    }
                }

                object? element = enumerator.Value;
                if (!converter.TryWrite(writer, element, options, ref state))
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
                    return typeof(Dictionary<string, object>);
                }

                return TypeToConvert;
            }
        }
    }
}
