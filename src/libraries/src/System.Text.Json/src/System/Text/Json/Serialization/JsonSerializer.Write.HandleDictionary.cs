// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization
{
    public static partial class JsonSerializer
    {
        private static bool HandleDictionary(
            JsonClassInfo elementClassInfo,
            JsonSerializerOptions options,
            Utf8JsonWriter writer,
            ref WriteStack state)
        {
            Debug.Assert(state.Current.JsonPropertyInfo.ClassType == ClassType.Dictionary);

            JsonPropertyInfo jsonPropertyInfo = state.Current.JsonPropertyInfo;
            if (!jsonPropertyInfo.ShouldSerialize)
            {
                // Ignore writing this property.
                return true;
            }

            if (state.Current.Enumerator == null)
            {
                IEnumerable enumerable = (IEnumerable)jsonPropertyInfo.GetValueAsObject(state.Current.CurrentValue, options);

                if (enumerable == null)
                {
                    // Write a null object or enumerable.
                    writer.WriteNull(jsonPropertyInfo.Name);
                    return true;
                }

                state.Current.Enumerator = enumerable.GetEnumerator();

                if (jsonPropertyInfo.Name == null)
                {
                    writer.WriteStartObject();
                }
                else
                {
                    writer.WriteStartObject(jsonPropertyInfo.Name);
                }
            }

            if (state.Current.Enumerator.MoveNext())
            {
                // Check for polymorphism.
                if (elementClassInfo.ClassType == ClassType.Unknown)
                {
                    //todo:test
                    object currentValue = ((IDictionaryEnumerator)(state.Current.Enumerator)).Entry;
                    GetRuntimeClassInfo(currentValue, ref elementClassInfo, options);
                }

                if (elementClassInfo.ClassType == ClassType.Value)
                {
                    elementClassInfo.GetPolicyProperty().WriteDictionary(options, ref state.Current, writer);
                }
                else if (state.Current.Enumerator.Current == null)
                {
                    writer.WriteNull(jsonPropertyInfo.Name);
                }
                else
                {
                    // An object or another enumerator requires a new stack frame.
                    object nextValue = state.Current.Enumerator.Current;
                    state.Push(elementClassInfo, nextValue);
                }

                return false;
            }

            // We are done enumerating.
            writer.WriteEndObject();

            state.Current.EndDictionary();

            return true;
        }

        internal static void WriteDictionary<TProperty>(
            JsonValueConverter<TProperty> converter,
            JsonSerializerOptions options,
            ref WriteStackFrame current,
            Utf8JsonWriter writer)
        {
            if (converter == null)
            {
                return;
            }

            Debug.Assert(current.Enumerator != null);

            string key;
            TProperty value;
            if (current.Enumerator is IEnumerator<KeyValuePair<string, TProperty>> enumerator)
            {
                // Avoid boxing for strongly-typed enumerators such as returned from IDictionary<string, TRuntimeProperty>
                value = enumerator.Current.Value;
                key = enumerator.Current.Key;
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
                byte[] pooledKey = null;
                byte[] utf8Key = Encoding.UTF8.GetBytes(key);
                int length = JsonWriterHelper.GetMaxEscapedLength(utf8Key.Length, 0);

                Span<byte> escapedKey = length <= JsonConstants.StackallocThreshold ?
                    stackalloc byte[length] :
                    (pooledKey = ArrayPool<byte>.Shared.Rent(length));

                JsonWriterHelper.EscapeString(utf8Key, escapedKey, 0, out int written);

                converter.Write(escapedKey.Slice(0, written), value, writer);

                if (pooledKey != null)
                {
                    ArrayPool<byte>.Shared.Return(pooledKey);
                }
            }
        }
    }
}
