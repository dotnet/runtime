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
            JsonPropertyInfo jsonPropertyInfo;

            jsonPropertyInfo = state.Current.JsonPropertyInfo;

            bool skip = jsonPropertyInfo != null && !jsonPropertyInfo.ShouldDeserialize;
            if (skip || state.Current.Skip())
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

            Type arrayType = jsonPropertyInfo.RuntimePropertyType;
            if (!typeof(IEnumerable).IsAssignableFrom(arrayType) || (arrayType.IsArray && arrayType.GetArrayRank() > 1))
            {
                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(arrayType, reader, state.PropertyPath);
            }

            Debug.Assert(state.Current.IsPropertyEnumerable || state.Current.IsDictionary);

            if (state.Current.EnumerableCreated)
            {
                // A nested json array so push a new stack frame.
                Type elementType = state.Current.JsonClassInfo.ElementClassInfo.GetPolicyProperty().RuntimePropertyType;

                state.Push();

                state.Current.Initialize(elementType, options);
                state.Current.PopStackOnEnd = true;
            }
            else
            {
                state.Current.EnumerableCreated = true;
            }

            jsonPropertyInfo = state.Current.JsonPropertyInfo;

            // If current property is already set (from a constructor, for example) leave as-is.
            if (jsonPropertyInfo.GetValueAsObject(state.Current.ReturnValue) == null)
            {
                // Create the enumerable.
                object value = ReadStackFrame.CreateEnumerableValue(ref reader, ref state, options);
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
            ref ReadStack state,
            ref Utf8JsonReader reader)
        {
            bool lastFrame = state.IsLastFrame;

            if (state.Current.Drain)
            {
                // The array is not being applied to the object.
                state.Pop();
                return lastFrame;
            }

            IEnumerable value = ReadStackFrame.GetEnumerableValue(state.Current);
            if (value == null)
            {
                // We added the items to the list property already.
                state.Current.ResetProperty();
                return false;
            }

            bool setPropertyDirectly;
            if (state.Current.TempEnumerableValues != null)
            {
                JsonEnumerableConverter converter = state.Current.JsonPropertyInfo.EnumerableConverter;
                Debug.Assert(converter != null);

                Type elementType = state.Current.GetElementType();
                value = converter.CreateFromList(elementType, (IList)value);
                setPropertyDirectly = true;
            }
            else
            {
                setPropertyDirectly = false;
            }

            bool valueReturning = state.Current.PopStackOnEnd;
            if (state.Current.PopStackOnEnd)
            {
                state.Pop();
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
                else if (state.Current.IsEnumerable || state.Current.IsDictionary)
                {
                    // Returning a non-converted list.
                    return true;
                }
                // else there must be an outer object, so we'll return false here.
            }

            ApplyObjectToEnumerable(value, options, ref state, ref reader, setPropertyDirectly: setPropertyDirectly);

            if (!valueReturning)
            {
                state.Current.ResetProperty();
            }

            return false;
        }

        // If this method is changed, also change ApplyValueToEnumerable.
        internal static void ApplyObjectToEnumerable(
            object value,
            JsonSerializerOptions options,
            ref ReadStack state,
            ref Utf8JsonReader reader,
            bool setPropertyDirectly = false)
        {
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
            else if (!setPropertyDirectly && state.Current.IsPropertyEnumerable)
            {
                Debug.Assert(state.Current.JsonPropertyInfo != null);
                Debug.Assert(state.Current.ReturnValue != null);
                if (state.Current.TempEnumerableValues != null)
                {
                    state.Current.TempEnumerableValues.Add(value);
                }
                else
                {
                    ((IList)state.Current.JsonPropertyInfo.GetValueAsObject(state.Current.ReturnValue)).Add(value);
                }
            }
            else if (state.Current.IsDictionary)
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
                    ThrowHelper.ThrowJsonException_DeserializeDuplicateKey(key, reader, state.PropertyPath);
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
            JsonSerializerOptions options,
            ref ReadStack state,
            ref Utf8JsonReader reader)
        {
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
            else if (state.Current.IsPropertyEnumerable)
            {
                Debug.Assert(state.Current.JsonPropertyInfo != null);
                Debug.Assert(state.Current.ReturnValue != null);
                if (state.Current.TempEnumerableValues != null)
                {
                    ((IList<TProperty>)state.Current.TempEnumerableValues).Add(value);
                }
                else
                {
                    ((IList<TProperty>)state.Current.JsonPropertyInfo.GetValueAsObject(state.Current.ReturnValue)).Add(value);
                }
            }
            else if (state.Current.IsDictionary)
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
                    ThrowHelper.ThrowJsonException_DeserializeDuplicateKey(key, reader, state.PropertyPath);
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
