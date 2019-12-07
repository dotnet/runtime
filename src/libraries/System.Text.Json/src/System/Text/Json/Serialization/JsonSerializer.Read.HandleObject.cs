// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Text.Json
{
    internal class JsonPreservedReference<T>
    {
        public JsonPreservedReference()
        {
            Values = default;
            T copy = Values;
        }

        public T Values { get; set; }
    }

    public static partial class JsonSerializer
    {
        private static void HandleStartObject(JsonSerializerOptions options, ref ReadStack state)
        {
            Debug.Assert(!state.Current.IsProcessingDictionary());

            // Note: unless we are a root object, we are going to push a property onto the ReadStack
            // in the if/else if check below.

            if (state.Current.IsProcessingEnumerable())
            {
                // A nested object within an enumerable (non-dictionary).

                if (!state.Current.CollectionPropertyInitialized)
                {
                    // We have bad JSON: enumerable element appeared without preceding StartArray token.
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(state.Current.JsonPropertyInfo.DeclaredPropertyType);
                }

                Type objType = state.Current.GetElementType();
                state.Push();
                state.Current.Initialize(objType, options);
            }
            else if (state.Current.JsonPropertyInfo != null)
            {
                // Nested object within an object.
                Debug.Assert(state.Current.IsProcessingObject(ClassType.Object));

                Type objType = state.Current.JsonPropertyInfo.RuntimePropertyType;
                state.Push();
                state.Current.Initialize(objType, options);
            }

            JsonClassInfo classInfo = state.Current.JsonClassInfo;

            if (state.Current.IsProcessingObject(ClassType.Dictionary))
            {
                object value = ReadStackFrame.CreateDictionaryValue(ref state);

                // If value is not null, then we don't have a converter so apply the value.
                if (value != null)
                {
                    state.Current.ReturnValue = value;
                    state.Current.DetermineIfDictionaryCanBePopulated(state.Current.ReturnValue);
                }

                state.Current.CollectionPropertyInitialized = true;
            }
            else if (state.Current.IsProcessingObject(ClassType.Object))
            {
                if (classInfo.CreateObject == null)
                {
                    ThrowHelper.ThrowNotSupportedException_DeserializeCreateObjectDelegateIsNull(classInfo.Type);
                }

                state.Current.ReturnValue = classInfo.CreateObject();
            }
            else
            {
                // Only dictionaries or objects are valid given the `StartObject` token.
                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(classInfo.Type);
            }
        }

        private static void HandleStartObjectRef(JsonSerializerOptions options, ref ReadStack state)
        {
            Debug.Assert(!state.Current.IsProcessingDictionary());

            // Note: unless we are a root object, we are going to push a property onto the ReadStack
            // in the if/else if check below.

            if (state.Current.IsProcessingEnumerable())
            {
                // A potential Preserved Array - Hit an StartObject while enumerable has not been initialized.
                if (!state.Current.CollectionPropertyInitialized)
                {
                    //dummy code to force linking of Values
                    new JsonPreservedReference<object>();

                    //Check we are not dealing with an immutable collection or fixed size array.
                    if (state.Current.JsonPropertyInfo.EnumerableConverter != null)
                    {
                        throw new JsonException("Immutable types and fixed size arrays cannot be preserved.");
                    }

                    // Is property.
                    if (state.Current.IsProcessingProperty(ClassType.Enumerable))
                    {
                        Type preservedObjType = typeof(JsonPreservedReference<>).MakeGenericType(state.Current.JsonPropertyInfo.RuntimePropertyType); // is this the right property?
                        state.Push();
                        state.Current.Initialize(preservedObjType, options);
                    }
                    // Is root.
                    else
                    {
                        Type preservedObjType = typeof(JsonPreservedReference<>).MakeGenericType(state.Current.JsonClassInfo.Type); // is this the right property?
                        // Re-Initialize the current frame.
                        state.Current.Initialize(preservedObjType, options);
                    }

                    state.Current.IsPreservedArray = true;
                }
                else
                {
                    Type objType = state.Current.GetElementType();
                    state.Push();
                    state.Current.Initialize(objType, options);
                }
            }
            else if (state.Current.JsonPropertyInfo != null)
            {
                // Nested object within an object.
                Debug.Assert(state.Current.IsProcessingObject(ClassType.Object));

                Type objType = state.Current.JsonPropertyInfo.RuntimePropertyType;
                state.Push();
                state.Current.Initialize(objType, options);
            }

            JsonClassInfo classInfo = state.Current.JsonClassInfo;

            if (state.Current.IsProcessingObject(ClassType.Dictionary))
            {
                object value = ReadStackFrame.CreateDictionaryValue(ref state);

                // If value is not null, then we don't have a converter so apply the value.
                if (value != null)
                {
                    state.Current.ReturnValue = value;
                    state.Current.DetermineIfDictionaryCanBePopulated(state.Current.ReturnValue);
                }

                state.Current.CollectionPropertyInitialized = true;
            }
            else if (state.Current.IsProcessingObject(ClassType.Object))
            {
                if (classInfo.CreateObject == null)
                {
                    ThrowHelper.ThrowNotSupportedException_DeserializeCreateObjectDelegateIsNull(classInfo.Type);
                }

                state.Current.ReturnValue = classInfo.CreateObject();
            }
            else
            {
                // Only dictionaries or objects are valid given the `StartObject` token.
                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(classInfo.Type);
            }
        }

        private static void HandleEndObject(ref ReadStack state)
        {
            // Only allow dictionaries to be processed here if this is the DataExtensionProperty or a reference object evaluated as null and is now finishing the dictionary object.
            Debug.Assert(!state.Current.IsProcessingDictionary() || state.Current.JsonClassInfo.DataExtensionProperty == state.Current.JsonPropertyInfo || state.Current.ShouldHandleReference);

            // Check if we are trying to build the sorted cache.
            if (state.Current.PropertyRefCache != null)
            {
                state.Current.JsonClassInfo.UpdateSortedPropertyCache(ref state.Current);
            }

            object value = state.Current.ReturnValue;

            if (state.IsLastFrame)
            {
                state.Current.Reset();
                state.Current.ReturnValue = value;
            }
            else
            {
                state.Pop();

                ApplyObjectToEnumerable(value, ref state);
            }
        }

        private static void HandleEndObjectRef(ref ReadStack state)
        {
            // Only allow dictionaries to be processed here if this is the DataExtensionProperty or a reference object evaluated as null and is now finishing the dictionary object.
            Debug.Assert(!state.Current.IsProcessingDictionary() || state.Current.JsonClassInfo.DataExtensionProperty == state.Current.JsonPropertyInfo || state.Current.ShouldHandleReference);

            // Check if we are trying to build the sorted cache.
            if (state.Current.PropertyRefCache != null)
            {
                state.Current.JsonClassInfo.UpdateSortedPropertyCache(ref state.Current);
            }

            object value;
            if (state.Current.IsPreservedArray)
            {
                JsonPropertyInfo info = state.Current.JsonClassInfo.PropertyCache["Values"]; //Well-known property.
                value = info.GetValueAsObject(state.Current.ReturnValue);

                if (value == null)
                {
                    throw new JsonException(
                            "Deserializaiton failed for one of these reasons:\n" +
                                "1. $values property was not present in preserved array.\n" +
                                "2. " + SR.Format(SR.DeserializeUnableToConvertValue, info.DeclaredPropertyType));
                }
            }
            else
            {
                value = state.Current.ReturnValue;
            }

            if (state.IsLastFrame)
            {
                state.Current.Reset();
                state.Current.ReturnValue = value;
            }
            else
            {
                state.Pop();

                ApplyObjectToEnumerable(value, ref state);
            }
        }
    }
}
