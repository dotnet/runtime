// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        private static void HandleStartArray(JsonSerializerOptions options, ref ReadStack state)
        {
            if (state.Current.SkipProperty)
            {
                // The array is not being applied to the object.
                state.Push();
                state.Current.Drain = true;
                return;
            }

            JsonPropertyInfo jsonPropertyInfo = state.Current.JsonPropertyInfo;
            if (jsonPropertyInfo == null)
            {
                jsonPropertyInfo = state.Current.JsonClassInfo.CreateRootProperty(options);
            }

            // Verify that we are processing a valid enumerable or dictionary.
            if (((ClassType.Enumerable | ClassType.Dictionary) & jsonPropertyInfo.ClassType) == 0)
            {
                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(jsonPropertyInfo.RuntimePropertyType);
            }

            if (state.Current.CollectionPropertyInitialized)
            {
                // An array nested in a dictionary or array, so push a new stack frame.
                Type elementType = jsonPropertyInfo.ElementClassInfo.Type;

                state.Push();
                state.Current.Initialize(elementType, options);
            }

            state.Current.CollectionPropertyInitialized = true;

            // The current JsonPropertyInfo will be null if the current type is not one of
            // ClassType.Value | ClassType.Enumerable | ClassType.Dictionary.
            // We should not see ClassType.Value here because we handle it on a different code
            // path invoked in the main read loop.
            // Only ClassType.Enumerable is valid here since we just saw a StartArray token.
            if (state.Current.JsonPropertyInfo == null ||
                state.Current.JsonPropertyInfo.ClassType != ClassType.Enumerable)
            {
                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(state.Current.JsonClassInfo.Type);
            }

            // Set or replace the existing enumerable value.
            object value = ReadStackFrame.CreateEnumerableValue(ref state);

            // If value is not null, then we don't have a converter so apply the value.
            if (value != null)
            {
                state.Current.DetermineEnumerablePopulationStrategy(value);

                if (state.Current.ReturnValue != null)
                {
                    state.Current.JsonPropertyInfo.SetValueAsObject(state.Current.ReturnValue, value);
                }
                else
                {
                    state.Current.ReturnValue = value;
                }
            }
        }

        private static bool HandleEndArray(
            JsonSerializerOptions options,
            ref ReadStack state)
        {
            bool lastFrame = state.IsLastFrame;

            if (state.Current.Drain)
            {
                // The array is not being applied to the object.
                state.Pop();
                return lastFrame;
            }

            IEnumerable value = ReadStackFrame.GetEnumerableValue(ref state.Current);
            bool setPropertyDirectly = false;

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

                // Since we used a converter, we just processed an array or an immutable collection. This means we created a new enumerable object.
                // If we are processing an enumerable property, replace the current value of the property with the new instance.
                if (state.Current.IsProcessingProperty(ClassType.Enumerable))
                {
                    setPropertyDirectly = true;
                }
            }
            else if (state.Current.IsProcessingProperty(ClassType.Enumerable))
            {
                // We added the items to the list already.
                state.Current.EndProperty();
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
                else if (state.Current.IsProcessingCollectionObject())
                {
                    // Returning a non-converted list.
                    return true;
                }
                // else there must be an outer object, so we'll return false here.
            }
            else if (state.Current.IsProcessingObject(ClassType.Enumerable))
            {
                state.Pop();
            }

            ApplyObjectToEnumerable(value, ref state, setPropertyDirectly);
            return false;
        }

        // If this method is changed, also change ApplyValueToEnumerable.
        internal static void ApplyObjectToEnumerable(
            object value,
            ref ReadStack state,
            bool setPropertyDirectly = false)
        {
            Debug.Assert(!state.Current.SkipProperty);

            if (state.Current.IsProcessingObject(ClassType.Enumerable))
            {
                if (state.Current.TempEnumerableValues != null)
                {
                    state.Current.TempEnumerableValues.Add(value);
                }
                else
                {
                    if (state.Current.AddObjectToEnumerable == null)
                    {
                        if (state.Current.ReturnValue is IList list)
                        {
                            list.Add(value);
                        }
                        else
                        {
                            ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(value.GetType());
                            return;
                        }
                    }
                    else
                    {
                        state.Current.JsonPropertyInfo.AddObjectToEnumerableWithReflection(state.Current.AddObjectToEnumerable, value);
                    }
                }
            }
            else if (!setPropertyDirectly && state.Current.IsProcessingProperty(ClassType.Enumerable))
            {
                Debug.Assert(state.Current.JsonPropertyInfo != null);
                Debug.Assert(state.Current.ReturnValue != null);

                if (state.Current.TempEnumerableValues != null)
                {
                    state.Current.TempEnumerableValues.Add(value);
                }
                else
                {
                    JsonPropertyInfo jsonPropertyInfo = state.Current.JsonPropertyInfo;

                    object currentEnumerable = jsonPropertyInfo.GetValueAsObject(state.Current.ReturnValue);
                    if (currentEnumerable == null || !state.Current.CollectionPropertyInitialized)
                    {
                        jsonPropertyInfo.SetValueAsObject(state.Current.ReturnValue, value);
                    }
                    else if (state.Current.AddObjectToEnumerable == null)
                    {
                        ((IList)currentEnumerable).Add(value);
                    }
                    else
                    {
                        jsonPropertyInfo.AddObjectToEnumerableWithReflection(state.Current.AddObjectToEnumerable, value);
                    }
                }

            }
            else if (state.Current.IsProcessingObject(ClassType.Dictionary) || (state.Current.IsProcessingProperty(ClassType.Dictionary) && !setPropertyDirectly))
            {
                string key = state.Current.KeyName;
                Debug.Assert(!string.IsNullOrEmpty(key));

                if (state.Current.TempDictionaryValues != null)
                {
                    (state.Current.TempDictionaryValues)[key] = value;
                }
                else
                {
                    Debug.Assert(state.Current.ReturnValue != null);

                    object currentDictionary = state.Current.JsonPropertyInfo.GetValueAsObject(state.Current.ReturnValue);

                    if (currentDictionary is IDictionary dict)
                    {
                        Debug.Assert(!dict.IsReadOnly);
                        dict[key] = value;
                    }
                    else
                    {
                        state.Current.JsonPropertyInfo.AddObjectToDictionary(currentDictionary, key, value);
                    }
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
            ref ReadStack state)
        {
            Debug.Assert(!state.Current.SkipProperty);

            if (state.Current.IsProcessingObject(ClassType.Enumerable))
            {
                if (state.Current.TempEnumerableValues != null)
                {
                    ((IList<TProperty>)state.Current.TempEnumerableValues).Add(value);
                }
                else
                {
                    AddValueToEnumerable(ref state, state.Current.ReturnValue, value);
                }
            }
            else if (state.Current.IsProcessingProperty(ClassType.Enumerable))
            {
                if (state.Current.TempEnumerableValues != null)
                {
                    ((IList<TProperty>)state.Current.TempEnumerableValues).Add(value);
                }
                else
                {
                    Debug.Assert(state.Current.JsonPropertyInfo != null);
                    Debug.Assert(state.Current.ReturnValue != null);

                    JsonPropertyInfo jsonPropertyInfo = state.Current.JsonPropertyInfo;

                    object currentEnumerable = jsonPropertyInfo.GetValueAsObject(state.Current.ReturnValue);
                    if (currentEnumerable == null)
                    {
                        jsonPropertyInfo.SetValueAsObject(state.Current.ReturnValue, value);
                    }
                    else
                    {
                        AddValueToEnumerable(ref state, currentEnumerable, value);
                    }
                }
            }
            else if (state.Current.IsProcessingDictionary())
            {
                string key = state.Current.KeyName;
                Debug.Assert(!string.IsNullOrEmpty(key));

                if (state.Current.TempDictionaryValues != null)
                {
                    ((IDictionary<string, TProperty>)state.Current.TempDictionaryValues)[key] = value;
                }
                else
                {
                    Debug.Assert(state.Current.ReturnValue != null);

                    object currentDictionary = state.Current.JsonPropertyInfo.GetValueAsObject(state.Current.ReturnValue);

                    if (currentDictionary is IDictionary<string, TProperty> genericDict)
                    {
                        Debug.Assert(!genericDict.IsReadOnly);
                        genericDict[key] = value;
                    }
                    else if (currentDictionary is IDictionary dict)
                    {
                        Debug.Assert(!dict.IsReadOnly);
                        dict[key] = value;
                    }
                    else
                    {
                        throw ThrowHelper.GetNotSupportedException_SerializationNotSupportedCollection(currentDictionary.GetType(), parentType: null, memberInfo: null);
                    }
                }
            }
            else
            {
                Debug.Assert(state.Current.JsonPropertyInfo != null);
                state.Current.JsonPropertyInfo.SetValueAsObject(state.Current.ReturnValue, value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddValueToEnumerable<TProperty>(ref ReadStack state, object target, TProperty value)
        {
            if (target is IList<TProperty> genericList)
            {
                Debug.Assert(!genericList.IsReadOnly);
                genericList.Add(value);
            }
            else if (target is IList list)
            {
                Debug.Assert(!list.IsReadOnly);
                list.Add(value);
            }
            else
            {
                Debug.Assert(state.Current.AddObjectToEnumerable != null);
                ((Action<TProperty>)state.Current.AddObjectToEnumerable)(value);
            }
        }
    }
}
