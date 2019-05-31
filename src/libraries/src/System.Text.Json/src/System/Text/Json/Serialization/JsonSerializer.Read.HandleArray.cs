// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization
{
    public static partial class JsonSerializer
    {
        private static void HandleStartArray(
            JsonSerializerOptions options,
            ref Utf8JsonReader reader,
            ref ReadStack state)
        {
            JsonPropertyInfo jsonPropertyInfo = state.Current.JsonPropertyInfo;

            if (state.Current.SkipProperty)
            {
                // The array is not being applied to the object.
                state.Push();
                state.Current.Drain = true;
                return;
            }

            if (jsonPropertyInfo == null)
            {
                jsonPropertyInfo = state.Current.JsonClassInfo.CreateRootObject(options);
            }
            else if (state.Current.JsonClassInfo.ClassType == ClassType.Unknown)
            {
                jsonPropertyInfo = state.Current.JsonClassInfo.CreatePolymorphicProperty(jsonPropertyInfo, typeof(object), options);
            }

            // Verify that we don't have a multidimensional array.
            Type arrayType = jsonPropertyInfo.RuntimePropertyType;
            if (!typeof(IEnumerable).IsAssignableFrom(arrayType) || (arrayType.IsArray && arrayType.GetArrayRank() > 1))
            {
                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(arrayType, reader, state.JsonPath);
            }

            Debug.Assert(state.Current.IsProcessingEnumerableOrDictionary);

            if (state.Current.PropertyInitialized)
            {
                // A nested json array so push a new stack frame.
                Type elementType = jsonPropertyInfo.ElementClassInfo.Type;

                state.Push();
                state.Current.Initialize(elementType, options);
                state.Current.PropertyInitialized = true;
            }
            else
            {
                state.Current.PropertyInitialized = true;
            }

            jsonPropertyInfo = state.Current.JsonPropertyInfo;

            // If current property is already set (from a constructor, for example) leave as-is.
            if (jsonPropertyInfo.GetValueAsObject(state.Current.ReturnValue) == null)
            {
                // Create the enumerable.
                object value = ReadStackFrame.CreateEnumerableValue(ref reader, ref state, options);

                // If value is not null, then we don't have a converter so apply the value.
                if (value != null)
                {
                    if (state.Current.ReturnValue != null)
                    {
                        state.Current.JsonPropertyInfo.SetValueAsObject(state.Current.ReturnValue, value);
                    }
                    else
                    {
                        // Primitive arrays being returned without object
                        state.Current.SetReturnValue(value);
                    }
                }
            }
        }

        private static bool HandleEndArray(
            JsonSerializerOptions options,
            ref Utf8JsonReader reader,
            ref ReadStack state)
        {
            bool lastFrame = state.IsLastFrame;

            if (state.Current.Drain)
            {
                // The array is not being applied to the object.
                state.Pop();
                return lastFrame;
            }

            IEnumerable value = ReadStackFrame.GetEnumerableValue(state.Current);

            if (state.Current.TempEnumerableValues != null)
            {
                // We have a converter; possibilities:
                // - Add value to current frame's current property or TempEnumerableValues.
                // - Add value to previous frame's current property or TempEnumerableValues.
                // - Set current property on current frame to value.
                // - Set current property on previous frame to value.
                // - Set ReturnValue if root frame and value is the actual return value.
                JsonEnumerableConverter converter = state.Current.JsonPropertyInfo.EnumerableConverter;
                Debug.Assert(converter != null);

                value = converter.CreateFromList(ref state, (IList)value, options);
                state.Current.TempEnumerableValues = null;
            }
            else if (state.Current.IsEnumerableProperty)
            {
                // We added the items to the list already.
                state.Current.ResetProperty();
                return false;
            }

            if (lastFrame)
            {
                if (state.Current.ReturnValue == null)
                {
                    // Returning a converted list or object.
                    state.Current.Reset();
                    state.Current.ReturnValue = value;
                    return true;
                }
                else if (state.Current.IsEnumerable || state.Current.IsDictionary || state.Current.IsImmutableDictionary)
                {
                    // Returning a non-converted list.
                    return true;
                }
                // else there must be an outer object, so we'll return false here.
            }
            else if (state.Current.IsEnumerable)
            {
                state.Pop();
            }

            ApplyObjectToEnumerable(value, ref state, ref reader);

            return false;
        }

        // If this method is changed, also change ApplyValueToEnumerable.
        internal static void ApplyObjectToEnumerable(
            object value,
            ref ReadStack state,
            ref Utf8JsonReader reader,
            bool setPropertyDirectly = false)
        {
            Debug.Assert(!state.Current.SkipProperty);

            if (state.Current.IsEnumerable)
            {
                if (state.Current.TempEnumerableValues != null)
                {
                    state.Current.TempEnumerableValues.Add(value);
                }
                else
                {
                    ((IList)state.Current.ReturnValue).Add(value);
                }
            }
            else if (!setPropertyDirectly && state.Current.IsEnumerableProperty)
            {
                Debug.Assert(state.Current.JsonPropertyInfo != null);
                Debug.Assert(state.Current.ReturnValue != null);
                if (state.Current.TempEnumerableValues != null)
                {
                    state.Current.TempEnumerableValues.Add(value);
                }
                else
                {
                    IList list = (IList)state.Current.JsonPropertyInfo.GetValueAsObject(state.Current.ReturnValue);
                    if (list == null)
                    {
                        state.Current.JsonPropertyInfo.SetValueAsObject(state.Current.ReturnValue, value);
                    }
                    else
                    {
                        list.Add(value);
                    }
                }
            }
            else if (state.Current.IsDictionary || (state.Current.IsDictionaryProperty && !setPropertyDirectly))
            {
                Debug.Assert(state.Current.ReturnValue != null);
                IDictionary dictionary = (IDictionary)state.Current.JsonPropertyInfo.GetValueAsObject(state.Current.ReturnValue);

                string key = state.Current.KeyName;
                Debug.Assert(!string.IsNullOrEmpty(key));
                if (!dictionary.Contains(key))
                {
                    dictionary.Add(key, value);
                }
                else
                {
                    ThrowHelper.ThrowJsonException_DeserializeDuplicateKey(key, reader, state.JsonPath);
                }
            }
            else if (state.Current.IsImmutableDictionary || (state.Current.IsImmutableDictionaryProperty && !setPropertyDirectly))
            {
                Debug.Assert(state.Current.TempDictionaryValues != null);
                IDictionary dictionary = (IDictionary)state.Current.JsonPropertyInfo.GetValueAsObject(state.Current.TempDictionaryValues);

                string key = state.Current.KeyName;
                Debug.Assert(!string.IsNullOrEmpty(key));
                if (!dictionary.Contains(key))
                {
                    dictionary.Add(key, value);
                }
                else
                {
                    ThrowHelper.ThrowJsonException_DeserializeDuplicateKey(key, reader, state.JsonPath);
                }
            }
            else
            {
                Debug.Assert(state.Current.JsonPropertyInfo != null);
                state.Current.JsonPropertyInfo.SetValueAsObject(state.Current.ReturnValue, value);
            }
        }

        // If this method is changed, also change ApplyObjectToEnumerable.
        internal static void ApplyValueToEnumerable<TProperty>(
            ref TProperty value,
            ref ReadStack state,
            ref Utf8JsonReader reader)
        {
            Debug.Assert(!state.Current.SkipProperty);

            if (state.Current.IsEnumerable)
            {
                if (state.Current.TempEnumerableValues != null)
                {
                    ((IList<TProperty>)state.Current.TempEnumerableValues).Add(value);
                }
                else
                {
                    ((IList<TProperty>)state.Current.ReturnValue).Add(value);
                }
            }
            else if (state.Current.IsEnumerableProperty)
            {
                Debug.Assert(state.Current.JsonPropertyInfo != null);
                Debug.Assert(state.Current.ReturnValue != null);
                if (state.Current.TempEnumerableValues != null)
                {
                    ((IList<TProperty>)state.Current.TempEnumerableValues).Add(value);
                }
                else
                {
                    IList<TProperty> list = (IList<TProperty>)state.Current.JsonPropertyInfo.GetValueAsObject(state.Current.ReturnValue);
                    if (list == null)
                    {
                        state.Current.JsonPropertyInfo.SetValueAsObject(state.Current.ReturnValue, value);
                    }
                    else
                    {
                        list.Add(value);
                    }
                }
            }
            else if (state.Current.IsProcessingDictionary)
            {
                Debug.Assert(state.Current.ReturnValue != null);
                IDictionary<string, TProperty> dictionary = (IDictionary<string, TProperty>)state.Current.JsonPropertyInfo.GetValueAsObject(state.Current.ReturnValue);

                string key = state.Current.KeyName;
                Debug.Assert(!string.IsNullOrEmpty(key));
                if (!dictionary.ContainsKey(key)) // The IDictionary.TryAdd extension method is not available in netstandard.
                {
                    dictionary.Add(key, value);
                }
                else
                {
                    ThrowHelper.ThrowJsonException_DeserializeDuplicateKey(key, reader, state.JsonPath);
                }
            }
            else if (state.Current.IsProcessingImmutableDictionary)
            {
                Debug.Assert(state.Current.TempDictionaryValues != null);
                IDictionary<string, TProperty> dictionary = (IDictionary<string, TProperty>)state.Current.TempDictionaryValues;

                string key = state.Current.KeyName;
                Debug.Assert(!string.IsNullOrEmpty(key));
                if (!dictionary.ContainsKey(key)) // The IDictionary.TryAdd extension method is not available in netstandard.
                {
                    dictionary.Add(key, value);
                }
                else
                {
                    ThrowHelper.ThrowJsonException_DeserializeDuplicateKey(key, reader, state.JsonPath);
                }
            }
            else
            {
                Debug.Assert(state.Current.JsonPropertyInfo != null);
                state.Current.JsonPropertyInfo.SetValueAsObject(state.Current.ReturnValue, value);
            }
        }
    }
}
