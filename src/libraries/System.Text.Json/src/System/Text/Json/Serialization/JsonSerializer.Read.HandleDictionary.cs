// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.Text.Json.Serialization.Policies;

namespace System.Text.Json
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

                if (state.Current.IsProcessingIDictionaryConstructibleOrKeyValuePair)
                {
                    state.Current.TempDictionaryValues = (IDictionary)classInfo.CreateObject();
                }
                else
                {
                    if (classInfo.CreateObject == null)
                    {
                        ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(classInfo.Type, reader, state.JsonPath);
                        return;
                    }
                    state.Current.ReturnValue = classInfo.CreateObject();
                }
                return;
            }

            state.Current.PropertyInitialized = true;

            if (state.Current.IsProcessingIDictionaryConstructibleOrKeyValuePair)
            {
                JsonClassInfo dictionaryClassInfo = options.GetOrAddClass(jsonPropertyInfo.RuntimePropertyType);
                state.Current.TempDictionaryValues = (IDictionary)dictionaryClassInfo.CreateObject();
            }
            // Else if current property is already set (from a constructor, for example), leave as-is.
            else if (jsonPropertyInfo.GetValueAsObject(state.Current.ReturnValue) == null)
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
            else if (state.Current.IsIDictionaryConstructibleProperty)
            {
                Debug.Assert(state.Current.TempDictionaryValues != null);
                JsonDictionaryConverter converter = state.Current.JsonPropertyInfo.DictionaryConverter;
                state.Current.JsonPropertyInfo.SetValueAsObject(state.Current.ReturnValue, converter.CreateFromDictionary(ref state, state.Current.TempDictionaryValues, options));
                state.Current.ResetProperty();
            }
            else if (state.Current.IsKeyValuePairProperty)
            {
                JsonClassInfo elementClassInfo = state.Current.JsonPropertyInfo.ElementClassInfo;

                JsonPropertyInfo propertyInfo;
                if (elementClassInfo.ClassType == ClassType.KeyValuePair)
                {
                    propertyInfo = elementClassInfo.GetPolicyPropertyOfKeyValuePair();
                }
                else
                {
                    propertyInfo = elementClassInfo.GetPolicyProperty();
                }

                Debug.Assert(state.Current.TempDictionaryValues != null);
                state.Current.JsonPropertyInfo.SetValueAsObject(
                    state.Current.ReturnValue,
                    propertyInfo.CreateKeyValuePairInstance(ref state, state.Current.TempDictionaryValues, options));
                state.Current.ResetProperty();
            }
            else
            {
                object value;
                if (state.Current.TempDictionaryValues != null)
                {
                    if (state.Current.IsKeyValuePair)
                    {
                        JsonClassInfo elementClassInfo = state.Current.JsonClassInfo.ElementClassInfo;

                        JsonPropertyInfo propertyInfo;
                        if (elementClassInfo.ClassType == ClassType.KeyValuePair)
                        {
                            propertyInfo = elementClassInfo.GetPolicyPropertyOfKeyValuePair();
                        }
                        else
                        {
                            propertyInfo = elementClassInfo.GetPolicyProperty();
                        }

                        value = propertyInfo.CreateKeyValuePairInstance(ref state, state.Current.TempDictionaryValues, options);
                    }
                    else
                    {
                        JsonDictionaryConverter converter = state.Current.JsonPropertyInfo.DictionaryConverter;
                        value = converter.CreateFromDictionary(ref state, state.Current.TempDictionaryValues, options);
                    }
                }
                else
                {
                    value = state.Current.ReturnValue;
                }

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
