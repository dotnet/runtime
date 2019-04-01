// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;

namespace System.Text.Json.Serialization
{
    public static partial class JsonSerializer
    {
        private static bool WriteEnumerable(
            JsonSerializerOptions options,
            ref Utf8JsonWriter writer,
            ref WriteStack state)
        {
            return HandleEnumerable(state.Current.JsonClassInfo.ElementClassInfo, options, ref writer, ref state);
        }

        private static bool HandleEnumerable(
            JsonClassInfo elementClassInfo,
            JsonSerializerOptions options,
            ref Utf8JsonWriter writer,
            ref WriteStack state)
        {
            Debug.Assert(state.Current.JsonPropertyInfo.ClassType == ClassType.Enumerable);

            JsonPropertyInfo jsonPropertyInfo = state.Current.JsonPropertyInfo;

            if (state.Current.Enumerator == null)
            {
                if (jsonPropertyInfo._name == null)
                {
                    writer.WriteStartArray();
                }
                else
                {
                    writer.WriteStartArray(jsonPropertyInfo._name);
                }

                IEnumerable enumerable = (IEnumerable)jsonPropertyInfo.GetValueAsObject(state.Current.CurrentValue, options);

                if (enumerable != null)
                {
                    state.Current.Enumerator = enumerable.GetEnumerator();
                }
            }

            if (state.Current.Enumerator != null && state.Current.Enumerator.MoveNext())
            {
                // If the enumerator contains typeof(object), get the run-time type
                if (elementClassInfo.ClassType == ClassType.Object && jsonPropertyInfo.ElementClassInfo.Type == typeof(object))
                {
                    object currentValue = state.Current.Enumerator.Current;
                    if (currentValue != null)
                    {
                        Type runtimeType = currentValue.GetType();
                        
                        // Ignore object() instances since they are handled as an empty object.
                        if (runtimeType != typeof(object))
                        {
                            elementClassInfo = options.GetOrAddClass(runtimeType);
                        }
                    }
                }

                if (elementClassInfo.ClassType == ClassType.Value)
                {
                    elementClassInfo.GetPolicyProperty().WriteEnumerable(options, ref state.Current, ref writer);
                }
                else if (state.Current.Enumerator.Current == null)
                {
                    // Write a null object or enumerable.
                    writer.WriteNullValue();
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
            writer.WriteEndArray();

            if (state.Current.PopStackOnEndArray)
            {
                state.Pop();
            }
            else
            {
                state.Current.EndArray();
            }

            return true;
        }
    }
}
