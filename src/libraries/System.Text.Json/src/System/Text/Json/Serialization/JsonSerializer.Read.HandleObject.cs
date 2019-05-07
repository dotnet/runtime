// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Text.Json.Serialization
{
    public static partial class JsonSerializer
    {
        private static void HandleStartObject(JsonSerializerOptions options, ref Utf8JsonReader reader, ref ReadStack state)
        {
            if (state.Current.Skip())
            {
                state.Push();
                state.Current.Drain = true;
                return;
            }

            if (state.Current.IsProcessingEnumerable)
            {
                Type objType = state.Current.GetElementType();
                state.Push();
                state.Current.Initialize(objType, options);
            }
            else if (state.Current.JsonPropertyInfo != null)
            {
                if (state.Current.IsDictionary)
                {
                    // Verify that the Dictionary can be deserialized by having <string> as first generic argument.
                    Type[] args = state.Current.JsonClassInfo.Type.GetGenericArguments();
                    if (args.Length == 0 || args[0].UnderlyingSystemType != typeof(string))
                    {
                        ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(state.Current.JsonClassInfo.Type, reader, state.PropertyPath);
                    }

                    if (state.Current.ReturnValue == null)
                    {
                        // The Dictionary created below will be returned to corresponding Parse() etc method.
                        // Ensure any nested array creates a new frame.
                        state.Current.EnumerableCreated = true;
                    }
                    else
                    {
                        ClassType classType = state.Current.JsonClassInfo.ElementClassInfo.ClassType;

                        // Verify that the second parameter is not a value.
                        if (state.Current.JsonClassInfo.ElementClassInfo.ClassType == ClassType.Value)
                        {
                            ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(state.Current.JsonClassInfo.Type, reader, state.PropertyPath);
                        }

                        // A nested object, dictionary or enumerable.
                        JsonClassInfo classInfoTemp = state.Current.JsonClassInfo;
                        state.Push();
                        state.Current.JsonClassInfo = classInfoTemp.ElementClassInfo;
                        state.Current.InitializeJsonPropertyInfo();
                    }
                }
                else
                {
                    // Nested object.
                    Type objType = state.Current.JsonPropertyInfo.RuntimePropertyType;
                    state.Push();
                    state.Current.Initialize(objType, options);
                }
            }

            JsonClassInfo classInfo = state.Current.JsonClassInfo;
            state.Current.ReturnValue = classInfo.CreateObject();
        }

        private static bool HandleEndObject(JsonSerializerOptions options, ref ReadStack state, ref Utf8JsonReader reader)
        {
            bool isLastFrame = state.IsLastFrame;
            if (state.Current.Drain)
            {
                state.Pop();
                return isLastFrame;
            }

            state.Current.JsonClassInfo.UpdateSortedPropertyCache(ref state.Current);

            object value = state.Current.ReturnValue;

            if (isLastFrame)
            {
                state.Current.Reset();
                state.Current.ReturnValue = value;
                return true;
            }

            state.Pop();
            ApplyObjectToEnumerable(value, options, ref state, ref reader);
            return false;
        }
    }
}
