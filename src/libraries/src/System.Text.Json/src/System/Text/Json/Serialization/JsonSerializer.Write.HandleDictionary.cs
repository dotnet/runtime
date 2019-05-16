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
            JsonPropertyInfo jsonPropertyInfo = state.Current.JsonPropertyInfo;
            if (!jsonPropertyInfo.ShouldSerialize)
            {
                // Ignore writing this property.
                return true;
            }

            if (state.Current.Enumerator == null)
            {
                // Verify that the Dictionary can be serialized by having <string> as first generic argument.
                Type[] args = jsonPropertyInfo.RuntimePropertyType.GetGenericArguments();
                if (args.Length == 0 || args[0].UnderlyingSystemType != typeof(string))
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(state.Current.JsonClassInfo.Type, state.PropertyPath);
                }

                IEnumerable enumerable = (IEnumerable)jsonPropertyInfo.GetValueAsObject(state.Current.CurrentValue);
                if (enumerable == null)
                {
                    // Write a null object or enumerable.
                    state.Current.WriteObjectOrArrayStart(ClassType.Dictionary, writer, writeNull : true);
                    return true;
                }

                state.Current.Enumerator = enumerable.GetEnumerator();
                state.Current.WriteObjectOrArrayStart(ClassType.Dictionary, writer);
            }

            if (state.Current.Enumerator.MoveNext())
            {
                // Check for polymorphism.
                if (elementClassInfo.ClassType == ClassType.Unknown)
                {
                    object currentValue = ((IDictionaryEnumerator)state.Current.Enumerator).Entry.Value;
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
                    var enumerator = (IDictionaryEnumerator)state.Current.Enumerator;
                    object value = enumerator.Value;
                    state.Push(elementClassInfo, value);
                    state.Current.KeyName = (string)enumerator.Key;
                }

                return false;
            }

            // We are done enumerating.
            writer.WriteEndObject();

            if (state.Current.PopStackOnEnd)
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
            else if (current.Enumerator is IEnumerator<KeyValuePair<string, object>> polymorphicEnumerator)
            {
                value = (TProperty)polymorphicEnumerator.Current.Value;
                key = polymorphicEnumerator.Current.Key;
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
                byte[] utf8Key = Encoding.UTF8.GetBytes(key);
#if true
                // temporary behavior until the writer can accept escaped string.
                converter.Write(utf8Key, value, writer);
#else
                int valueIdx = JsonWriterHelper.NeedsEscaping(utf8Key);
                if (valueIdx == -1)
                {
                    converter.Write(utf8Key, value, writer);
                }
                else
                {
                    byte[] pooledKey = null;
                    int length = JsonWriterHelper.GetMaxEscapedLength(utf8Key.Length, valueIdx);

                    Span<byte> escapedKey = length <= JsonConstants.StackallocThreshold ?
                        stackalloc byte[length] :
                        (pooledKey = ArrayPool<byte>.Shared.Rent(length));

                    JsonWriterHelper.EscapeString(utf8Key, escapedKey, valueIdx, out int written);

                    converter.Write(escapedKey.Slice(0, written), value, writer);

                    if (pooledKey != null)
                    {
                        // We clear the array because it is "user data" (although a property name).
                        new Span<byte>(pooledKey, 0, written).Clear();
                        ArrayPool<byte>.Shared.Return(pooledKey);
                    }
                }
#endif
            }
        }
    }
}
