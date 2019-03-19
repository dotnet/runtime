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

            JsonPropertyInfo propertyInfo = state.Current.JsonPropertyInfo;

            if (state.Current.Enumerator == null)
            {
                if (propertyInfo._name == null)
                {
                    writer.WriteStartArray();
                }
                else
                {
                    writer.WriteStartArray(propertyInfo._name);
                }

                IEnumerable enumerable = (IEnumerable)propertyInfo.GetValueAsObject(state.Current.CurrentValue, options);

                if (enumerable != null)
                {
                    state.Current.Enumerator = enumerable.GetEnumerator();
                }
            }

            if (state.Current.Enumerator != null && state.Current.Enumerator.MoveNext())
            {
                if (elementClassInfo.ClassType == ClassType.Value)
                {
                    elementClassInfo.GetPolicyProperty().WriteEnumerable(options, ref state.Current, ref writer);
                }
                else
                {
                    // An object or another enumerator requires a new stack frame
                    JsonClassInfo nextClassInfo = propertyInfo.ElementClassInfo;
                    object nextValue = state.Current.Enumerator.Current;
                    state.Push(nextClassInfo, nextValue);
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
