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
                        state.Pop();
                    }

                    return true;
                }

                if (options.ReferenceHandling.ShouldWritePreservedReferences())
                {
                    if (WriteReferenceEnumerable(ref state, writer, options, enumerable))
                    {
                        return WriteEndArray(ref state);
                    }
                }
                else
                {
                    state.Current.WriteObjectOrArrayStart(ClassType.Enumerable, writer, options);
                }

                state.Current.CollectionEnumerator = enumerable.GetEnumerator();
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

            // Used for ReferenceHandling.Preserve
            if (state.Current.WriteWrappingBraceOnEndCollection)
            {
                writer.WriteEndObject();
            }

            return WriteEndArray(ref state);
        }

        private static bool WriteEndArray(ref WriteStack state)
        {
            if (state.Current.PopStackOnEndCollection)
            {
                state.Pop();
            }
            else
            {
                state.Current.EndArray();
            }

            return true;
        }


        private static bool WriteReferenceEnumerable(ref WriteStack state, Utf8JsonWriter writer, JsonSerializerOptions options, IEnumerable enumerable)
        {
            ResolvedReferenceHandling handling = state.PreserveReference(enumerable, out string referenceId);

            if (handling == ResolvedReferenceHandling.IsReference)
            {
                // Object written before, write { "$ref": "#" } and finish.
                state.Current.WriteReferenceObjectOrArrayStart(ClassType.Enumerable, writer, options, writeAsReference: true, referenceId: referenceId);
                return true;
            }
            else if (handling == ResolvedReferenceHandling.Preserve)
            {
                // Reference-type array, write as object and append $id and $values, at the end it write EndObject token using WriteWrappingBraceOnEndCollection.
                state.Current.WriteReferenceObjectOrArrayStart(ClassType.Enumerable, writer, options, writeAsReference: false, referenceId: referenceId);
            }
            else
            {
                // Value type or Immutable, fallback on regular Write method.
                state.Current.WriteObjectOrArrayStart(ClassType.Enumerable, writer, options);
            }

            return false;
        }
    }
}
