// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.Text.Json.Serialization.Converters;

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
            if (state.Current.CollectionPropertyInitialized)
            {
                state.Push();
                state.Current.JsonClassInfo = jsonPropertyInfo.ElementClassInfo;
                state.Current.InitializeJsonPropertyInfo();
                state.Current.CollectionPropertyInitialized = true;

                ClassType classType = state.Current.JsonClassInfo.ClassType;
                if (classType == ClassType.Value &&
                    jsonPropertyInfo.ElementClassInfo.Type != typeof(object) &&
                    jsonPropertyInfo.ElementClassInfo.Type != typeof(JsonElement))
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(state.Current.JsonClassInfo.Type, reader, state.JsonPath);
                }

                JsonClassInfo classInfo = state.Current.JsonClassInfo;

                if (state.Current.IsProcessingIDictionaryConstructible)
                {
                    state.Current.TempDictionaryValues = (IDictionary)classInfo.CreateConcreteDictionary();
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

            state.Current.CollectionPropertyInitialized = true;

            if (state.Current.IsProcessingIDictionaryConstructible)
            {
                JsonClassInfo dictionaryClassInfo;
                if (jsonPropertyInfo.DeclaredPropertyType == jsonPropertyInfo.ImplementedPropertyType)
                {
                    dictionaryClassInfo = options.GetOrAddClass(jsonPropertyInfo.RuntimePropertyType);
                }
                else
                {
                    dictionaryClassInfo = options.GetOrAddClass(jsonPropertyInfo.DeclaredPropertyType);
                }

                state.Current.TempDictionaryValues = (IDictionary)dictionaryClassInfo.CreateConcreteDictionary();
            }
            else
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
            if (state.Current.SkipProperty)
            {
                // Todo: determine if this is reachable.
                return;
            }

            if (state.Current.IsDictionaryProperty)
            {
                // Handle special case of DataExtensionProperty where we just added a dictionary element to the extension property.
                // Since the JSON value is not a dictionary element (it's a normal property in JSON) a JsonTokenType.EndObject
                // encountered here is from the outer object so forward to HandleEndObject().
                if (state.Current.JsonClassInfo.DataExtensionProperty == state.Current.JsonPropertyInfo)
                {
                    HandleEndObject(ref reader, ref state);
                }
                else
                {
                    // We added the items to the dictionary already.
                    state.Current.EndProperty();
                }
            }
            else if (state.Current.IsIDictionaryConstructibleProperty)
            {
                Debug.Assert(state.Current.TempDictionaryValues != null);
                JsonDictionaryConverter converter = state.Current.JsonPropertyInfo.DictionaryConverter;
                state.Current.JsonPropertyInfo.SetValueAsObject(state.Current.ReturnValue, converter.CreateFromDictionary(ref state, state.Current.TempDictionaryValues, options));
                state.Current.EndProperty();
            }
            else
            {
                object value;
                if (state.Current.TempDictionaryValues != null)
                {
                    JsonDictionaryConverter converter = state.Current.JsonPropertyInfo.DictionaryConverter;
                    value = converter.CreateFromDictionary(ref state, state.Current.TempDictionaryValues, options);
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
