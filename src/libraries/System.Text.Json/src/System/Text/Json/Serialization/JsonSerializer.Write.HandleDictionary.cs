// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        private static bool HandleDictionary(
            JsonClassInfo elementClassInfo,
            JsonSerializerOptions options,
            Utf8JsonWriter writer,
            ref WriteStack state)
        {
            JsonPropertyInfo jsonPropertyInfo = state.Current.JsonPropertyInfo;
            if (state.Current.CollectionEnumerator == null)
            {
                IEnumerable enumerable;

                enumerable = (IEnumerable)jsonPropertyInfo.GetValueAsObject(state.Current.CurrentValue);
                if (enumerable == null)
                {
                    if (!state.Current.JsonPropertyInfo.IgnoreNullValues)
                    {
                        // Write a null object or enumerable.
                        state.Current.WriteObjectOrArrayStart(ClassType.Enumerable, writer, writeNull: true);
                    }

                    return true;
                }

                if (enumerable is IDictionary dictionary)
                {
                    state.Current.CollectionEnumerator = dictionary.GetEnumerator();
                }
                else
                {
                    state.Current.CollectionEnumerator = enumerable.GetEnumerator();
                }

                if (state.Current.ExtensionDataStatus != ExtensionDataWriteStatus.Writing)
                {
                    state.Current.WriteObjectOrArrayStart(ClassType.Dictionary, writer);
                }
            }

            if (state.Current.CollectionEnumerator.MoveNext())
            {
                // Check for polymorphism.
                if (elementClassInfo.ClassType == ClassType.Unknown)
                {
                    object currentValue = ((IDictionaryEnumerator)state.Current.CollectionEnumerator).Entry.Value;
                    GetRuntimeClassInfo(currentValue, ref elementClassInfo, options);
                }

                if (elementClassInfo.ClassType == ClassType.Value)
                {
                    elementClassInfo.PolicyProperty.WriteDictionary(ref state, writer);
                }
                else if (state.Current.CollectionEnumerator.Current == null)
                {
                    writer.WriteNull(jsonPropertyInfo.Name);
                }
                else
                {
                    // An object or another enumerator requires a new stack frame.
                    var enumerator = (IDictionaryEnumerator)state.Current.CollectionEnumerator;
                    object value = enumerator.Value;
                    state.Push(elementClassInfo, value);
                    state.Current.KeyName = (string)enumerator.Key;
                }

                return false;
            }

            // We are done enumerating.
            if (state.Current.ExtensionDataStatus == ExtensionDataWriteStatus.Writing)
            {
                state.Current.ExtensionDataStatus = ExtensionDataWriteStatus.Finished;
            }
            else
            {
                writer.WriteEndObject();
            }

            if (state.Current.PopStackOnEndCollection)
            {
                state.Pop();
            }
            else
            {
                state.Current.EndDictionary();
            }

            return true;
        }

        internal static void WriteDictionary<TProperty>(
            JsonConverter<TProperty> converter,
            JsonSerializerOptions options,
            ref WriteStackFrame current,
            Utf8JsonWriter writer)
        {
            if (converter == null)
            {
                return;
            }

            Debug.Assert(current.CollectionEnumerator != null);

            string key;
            TProperty value;
            if (current.CollectionEnumerator is IEnumerator<KeyValuePair<string, TProperty>> enumerator)
            {
                // Avoid boxing for strongly-typed enumerators such as returned from IDictionary<string, TRuntimeProperty>
                value = enumerator.Current.Value;
                key = enumerator.Current.Key;
            }
            else if (current.CollectionEnumerator is IEnumerator<KeyValuePair<string, object>> polymorphicEnumerator)
            {
                value = (TProperty)polymorphicEnumerator.Current.Value;
                key = polymorphicEnumerator.Current.Key;
            }
            else if (current.IsIDictionaryConstructible || current.IsIDictionaryConstructibleProperty)
            {
                value = (TProperty)((DictionaryEntry)current.CollectionEnumerator.Current).Value;
                key = (string)((DictionaryEntry)current.CollectionEnumerator.Current).Key;
            }
            else
            {
                // Todo: support non-generic Dictionary here (IDictionaryEnumerator)
                throw new NotSupportedException();
            }

            if (value == null)
            {
                writer.WriteNull(key);
            }
            else
            {
                if (options.DictionaryKeyPolicy != null && 
                    current.ExtensionDataStatus != ExtensionDataWriteStatus.Writing) // We do not convert extension data.
                {
                    key = options.DictionaryKeyPolicy.ConvertName(key);

                    if (key == null)
                    {
                        ThrowHelper.ThrowInvalidOperationException_SerializerDictionaryKeyNull(options.DictionaryKeyPolicy.GetType());
                    }
                }

                JsonEncodedText escapedKey = JsonEncodedText.Encode(key);
                writer.WritePropertyName(escapedKey);
                converter.Write(writer, value, options);
            }
        }
    }
}
