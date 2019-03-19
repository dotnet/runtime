// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Text.Json.Serialization
{
    public static partial class JsonSerializer
    {
        private static bool WriteObject(
            JsonSerializerOptions options,
            ref Utf8JsonWriter writer,
            ref WriteStack state)
        {
            JsonClassInfo classInfo = state.Current.JsonClassInfo;

            // Write the start.
            if (!state.Current.StartObjectWritten)
            {
                if (state.Current.JsonPropertyInfo?._escapedName == null)
                {
                    writer.WriteStartObject();
                }
                else
                {
                    writer.WriteStartObject(state.Current.JsonPropertyInfo._escapedName);
                }
                state.Current.StartObjectWritten = true;
            }

            // Determine if we are done enumerating properties.
            if (state.Current.PropertyIndex != classInfo.PropertyCount)
            {
                HandleObject(options, ref writer, ref state);
                return false;
            }

            writer.WriteEndObject();

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
                JsonSerializerOptions options,
                ref Utf8JsonWriter writer,
                ref WriteStack state)
        {
            Debug.Assert(state.Current.JsonClassInfo.ClassType == ClassType.Object);

            JsonPropertyInfo propertyInfo = state.Current.JsonClassInfo.GetProperty(state.Current.PropertyIndex);
            state.Current.JsonPropertyInfo = propertyInfo;

            ClassType propertyClassType = propertyInfo.ClassType;
            if (propertyClassType == ClassType.Value)
            {
                propertyInfo.Write(options, ref state.Current, ref writer);
                state.Current.NextProperty();
                return true;
            }

            // A property that returns an enumerator keeps the same stack frame.
            if (propertyClassType == ClassType.Enumerable)
            {
                bool endOfEnumerable = HandleEnumerable(propertyInfo.ElementClassInfo, options, ref writer, ref state);
                if (endOfEnumerable)
                {
                    state.Current.NextProperty();
                }

                return endOfEnumerable;
            }

            // A property that returns an object requires a new stack frame.
            object value = propertyInfo.GetValueAsObject(state.Current.CurrentValue, options);
            if (value != null)
            {
                JsonPropertyInfo previousPropertyInfo = state.Current.JsonPropertyInfo;

                state.Current.NextProperty();

                JsonClassInfo nextClassInfo = options.GetOrAddClass(propertyInfo.PropertyType);
                state.Push(nextClassInfo, value);

                // Set the PropertyInfo so we can obtain the property name in order to write it.
                state.Current.JsonPropertyInfo = previousPropertyInfo;
            }
            else if (!propertyInfo.IgnoreNullPropertyValueOnWrite(options))
            {
                writer.WriteNull(propertyInfo._escapedName);
            }

            return true;
        }
    }
}
