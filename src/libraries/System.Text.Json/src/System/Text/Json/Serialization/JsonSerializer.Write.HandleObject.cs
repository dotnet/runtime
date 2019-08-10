// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        private static bool WriteObject(
            JsonSerializerOptions options,
            Utf8JsonWriter writer,
            ref WriteStack state)
        {
            // Write the start.
            if (!state.Current.StartObjectWritten)
            {
                // If true, we are writing a root object or a value that doesn't belong
                // to an object e.g. a dictionary value.
                if (state.Current.CurrentValue == null)
                {
                    state.Current.WriteObjectOrArrayStart(ClassType.Object, writer, writeNull: true);
                    return WriteEndObject(ref state);
                }

                state.Current.WriteObjectOrArrayStart(ClassType.Object, writer);
                state.Current.PropertyEnumerator = state.Current.JsonClassInfo.PropertyCache.GetEnumerator();
                state.Current.PropertyEnumeratorActive = true;
                state.Current.NextProperty();
            }
            else if (state.Current.MoveToNextProperty)
            {
                state.Current.NextProperty();
            }

            // Determine if we are done enumerating properties.
            // If the ClassType is unknown, there will be a policy property applied
            JsonClassInfo classInfo = state.Current.JsonClassInfo;
            if (classInfo.ClassType != ClassType.Unknown && state.Current.PropertyEnumeratorActive)
            {
                var kvp = (KeyValuePair<string, JsonPropertyInfo>)state.Current.PropertyEnumerator.Current;
                JsonPropertyInfo jsonPropertyInfo = kvp.Value;
                HandleObject(jsonPropertyInfo, options, writer, ref state);
                return false;
            }

            if (state.Current.ExtensionDataStatus == Serialization.ExtensionDataWriteStatus.Writing)
            {
                JsonPropertyInfo jsonPropertyInfo = state.Current.JsonClassInfo.DataExtensionProperty;
                if (jsonPropertyInfo != null)
                {
                    HandleObject(jsonPropertyInfo, options, writer, ref state);
                    return false;
                }
            }

            writer.WriteEndObject();
            return WriteEndObject(ref state);
        }

        private static bool WriteEndObject(ref WriteStack state)
        {
            if (state.Current.PopStackOnEndObject)
            {
                state.Pop();
            }
            else
            {
                state.Current.EndObject();
            }

            return true;
        }

        private static bool HandleObject(
                JsonPropertyInfo jsonPropertyInfo,
                JsonSerializerOptions options,
                Utf8JsonWriter writer,
                ref WriteStack state)
        {
            Debug.Assert(
                state.Current.JsonClassInfo.ClassType == ClassType.Object ||
                state.Current.JsonClassInfo.ClassType == ClassType.Unknown);

            if (!jsonPropertyInfo.ShouldSerialize)
            {
                state.Current.MoveToNextProperty = true;
                return true;
            }

            bool obtainedValue = false;
            object currentValue = null;

            // Check for polymorphism.
            if (jsonPropertyInfo.ClassType == ClassType.Unknown)
            {
                currentValue = jsonPropertyInfo.GetValueAsObject(state.Current.CurrentValue);
                obtainedValue = true;
                GetRuntimePropertyInfo(currentValue, state.Current.JsonClassInfo, ref jsonPropertyInfo, options);
            }

            state.Current.JsonPropertyInfo = jsonPropertyInfo;

            if (jsonPropertyInfo.ClassType == ClassType.Value)
            {
                jsonPropertyInfo.Write(ref state, writer);
                state.Current.MoveToNextProperty = true;
                return true;
            }

            // A property that returns an enumerator keeps the same stack frame.
            if (jsonPropertyInfo.ClassType == ClassType.Enumerable)
            {
                bool endOfEnumerable = HandleEnumerable(jsonPropertyInfo.ElementClassInfo, options, writer, ref state);
                if (endOfEnumerable)
                {
                    state.Current.MoveToNextProperty = true;
                }

                return endOfEnumerable;
            }

            // A property that returns a dictionary keeps the same stack frame.
            if (jsonPropertyInfo.ClassType == ClassType.Dictionary)
            {
                bool endOfEnumerable = HandleDictionary(jsonPropertyInfo.ElementClassInfo, options, writer, ref state);
                if (endOfEnumerable)
                {
                    state.Current.MoveToNextProperty = true;
                }

                return endOfEnumerable;
            }

            // A property that returns a type that is deserialized by passing an
            // IDictionary to its constructor keeps the same stack frame.
            if (jsonPropertyInfo.ClassType == ClassType.IDictionaryConstructible)
            {
                state.Current.IsIDictionaryConstructibleProperty = true;

                bool endOfEnumerable = HandleDictionary(jsonPropertyInfo.ElementClassInfo, options, writer, ref state);
                if (endOfEnumerable)
                {
                    state.Current.MoveToNextProperty = true;
                }

                return endOfEnumerable;
            }

            // A property that returns an object.
            if (!obtainedValue)
            {
                currentValue = jsonPropertyInfo.GetValueAsObject(state.Current.CurrentValue);
            }

            if (currentValue != null)
            {
                // A new stack frame is required.
                JsonPropertyInfo previousPropertyInfo = state.Current.JsonPropertyInfo;
                state.Current.MoveToNextProperty = true;

                JsonClassInfo nextClassInfo = jsonPropertyInfo.RuntimeClassInfo;
                state.Push(nextClassInfo, currentValue);

                // Set the PropertyInfo so we can obtain the property name in order to write it.
                state.Current.JsonPropertyInfo = previousPropertyInfo;
            }
            else
            {
                if (!jsonPropertyInfo.IgnoreNullValues)
                {
                    writer.WriteNull(jsonPropertyInfo.EscapedName.Value);
                }

                state.Current.MoveToNextProperty = true;
            }

            return true;
        }
    }
}
