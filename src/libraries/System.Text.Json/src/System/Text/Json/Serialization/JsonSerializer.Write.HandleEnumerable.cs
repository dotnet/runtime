// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;

namespace System.Text.Json
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

            if (state.Current.CollectionEnumerator == null)
            {
                IEnumerable enumerable = (IEnumerable)state.Current.JsonPropertyInfo.GetValueAsObject(state.Current.CurrentValue);

                if (enumerable == null)
                {
                    // If applicable, we only want to ignore object properties.
                    if (state.Current.JsonClassInfo.ClassType != ClassType.Object ||
                        !state.Current.JsonPropertyInfo.IgnoreNullValues)
                    {
                        // Write a null object or enumerable.
                        state.Current.WriteObjectOrArrayStart(ClassType.Enumerable, writer, options, writeNull: true);
                    }

                    if (state.Current.PopStackOnEndCollection)
                    {
                        state.Pop(writer, options);
                    }

                    return true;
                }

                ResolvedReferenceHandling handling = options.HandleReference(ref state, out string referenceId, out bool writeAsReference, enumerable);

                if (handling == ResolvedReferenceHandling.Ignore)
                {
                    //Reference loop found and ignore handling specified, do not write anything and pop the frame from the stack in case the array has an independant frame.
                    return WriteEndArray(ref state, writer, options);
                }
                state.Current.CollectionEnumerator = enumerable.GetEnumerator();

                options.WriteStart(ref state.Current, ClassType.Enumerable, writer, options, writeAsReference: writeAsReference, referenceId: referenceId);

                if (handling == ResolvedReferenceHandling.IsReference)
                {
                    // We don't need to enumerate, this is a reference and was already written in WriteObjectOrArrayStart.
                    return WriteEndArray(ref state, writer, options);
                }
            }

            if (state.Current.CollectionEnumerator.MoveNext())
            {
                // Check for polymorphism.
                if (elementClassInfo.ClassType == ClassType.Unknown)
                {
                    object currentValue = state.Current.CollectionEnumerator.Current;
                    GetRuntimeClassInfo(currentValue, ref elementClassInfo, options);
                }

                if (elementClassInfo.ClassType == ClassType.Value)
                {
                    elementClassInfo.PolicyProperty.WriteEnumerable(ref state, writer);
                }
                else if (state.Current.CollectionEnumerator.Current == null)
                {
                    // Write a null object or enumerable.
                    writer.WriteNullValue();
                }
                else
                {
                    JsonPropertyInfo previousPropertyInfo = state.Current.JsonPropertyInfo;
                    // An object or another enumerator requires a new stack frame.
                    object nextValue = state.Current.CollectionEnumerator.Current;
                    state.Push(elementClassInfo, nextValue);
                }

                return false;
            }

            // We are done enumerating.
            writer.WriteEndArray();

            if (state.Current.WriteWrappingBraceOnEndCollection)
            {
                writer.WriteEndObject();
            }

            return WriteEndArray(ref state, writer, options);
        }

        private static bool WriteEndArray(ref WriteStack state, Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            if (state.Current.PopStackOnEndCollection)
            {
                state.Pop(writer, options);
            }
            else
            {
                options.PopReference(ref state, true);
                state.Current.EndArray();
            }

            return true;
        }
    }
}
