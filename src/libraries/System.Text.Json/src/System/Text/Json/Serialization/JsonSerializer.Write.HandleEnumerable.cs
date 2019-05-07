// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;

namespace System.Text.Json.Serialization
{
    public static partial class JsonSerializer
    {
        private static bool HandleEnumerable(
            JsonClassInfo elementClassInfo,
            JsonSerializerOptions options,
            Utf8JsonWriter writer,
            ref WriteStack state)
        {
            Debug.Assert(state.Current.JsonPropertyInfo.ClassType == ClassType.Enumerable);

            JsonPropertyInfo jsonPropertyInfo = state.Current.JsonPropertyInfo;
            if (!jsonPropertyInfo.ShouldSerialize)
            {
                // Ignore writing this property.
                return true;
            }

            if (state.Current.Enumerator == null)
            {
                IEnumerable enumerable = (IEnumerable)jsonPropertyInfo.GetValueAsObject(state.Current.CurrentValue);

                if (enumerable == null)
                {
                    if (!state.Current.JsonPropertyInfo.IgnoreNullValues)
                    {
                        // Write a null object or enumerable.
                        state.Current.WriteObjectOrArrayStart(ClassType.Enumerable, writer, writeNull: true);
                    }

                    return true;
                }

                state.Current.Enumerator = enumerable.GetEnumerator();

                state.Current.WriteObjectOrArrayStart(ClassType.Enumerable, writer);
            }

            if (state.Current.Enumerator.MoveNext())
            {
                // Check for polymorphism.
                if (elementClassInfo.ClassType == ClassType.Unknown)
                {
                    object currentValue = state.Current.Enumerator.Current;
                    GetRuntimeClassInfo(currentValue, ref elementClassInfo, options);
                }

                if (elementClassInfo.ClassType == ClassType.Value)
                {
                    elementClassInfo.GetPolicyProperty().WriteEnumerable(options, ref state.Current, writer);
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

            if (state.Current.PopStackOnEnd)
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
