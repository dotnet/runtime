// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;

namespace System.Text.Json.Serialization
{
    public static partial class JsonSerializer
    {
        private static void HandleStartDictionary(JsonSerializerOptions options, ref Utf8JsonReader reader, ref ReadStack state)
        {
            Debug.Assert(!state.Current.IsProcessingEnumerable);

            JsonPropertyInfo jsonPropertyInfo = state.Current.JsonPropertyInfo;
            if (jsonPropertyInfo == null)
            {
                jsonPropertyInfo = state.Current.JsonClassInfo.CreateRootObject(options);
            }

            Debug.Assert(jsonPropertyInfo != null);

            // A nested object or dictionary so push new frame.
            if (state.Current.PropertyInitialized)
            {
                JsonClassInfo classInfoTemp = jsonPropertyInfo.RuntimeClassInfo;
                state.Push();
                state.Current.JsonClassInfo = jsonPropertyInfo.ElementClassInfo;
                state.Current.InitializeJsonPropertyInfo();
                state.Current.PropertyInitialized = true;

                ClassType classType = state.Current.JsonClassInfo.ClassType;
                if (classType == ClassType.Value &&
                    jsonPropertyInfo.ElementClassInfo.Type != typeof(object) &&
                    jsonPropertyInfo.ElementClassInfo.Type != typeof(JsonElement))
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(state.Current.JsonClassInfo.Type, reader, state.JsonPath);
                }

                JsonClassInfo classInfo = state.Current.JsonClassInfo;
                state.Current.ReturnValue = classInfo.CreateObject();
                return;
            }

            state.Current.PropertyInitialized = true;

            // If current property is already set (from a constructor, for example) leave as-is.
            if (jsonPropertyInfo.GetValueAsObject(state.Current.ReturnValue) == null)
            {
                // Create the dictionary.
                JsonClassInfo dictionaryClassInfo = jsonPropertyInfo.RuntimeClassInfo;
                IDictionary value = (IDictionary)dictionaryClassInfo.CreateObject();
                if (value != null)
                {
                    if (state.Current.ReturnValue != null)
                    {
                        state.Current.JsonPropertyInfo.SetValueAsObject(state.Current.ReturnValue, value);
                    }
                    else
                    {
                        // A dictionary is being returned directly, or a nested dictionary.
                        state.Current.SetReturnValue(value);
                    }
                }
            }
        }

        private static void HandleEndDictionary(JsonSerializerOptions options, ref Utf8JsonReader reader, ref ReadStack state)
        {
            if (state.Current.IsDictionaryProperty)
            {
                // We added the items to the dictionary already.
                state.Current.ResetProperty();
            }
            else
            {
                object value = state.Current.ReturnValue;

                if (state.IsLastFrame)
                {
                    // Set the return value directly since this will be returned to the user.
                    state.Current.Reset();
                    state.Current.ReturnValue = value;
                }
                else
                {
                    state.Pop();
                    ApplyObjectToEnumerable(value, ref state, ref reader);
                }
            }
        }
    }
}
