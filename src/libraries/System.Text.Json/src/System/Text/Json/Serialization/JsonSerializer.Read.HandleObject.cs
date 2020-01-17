// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
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
                HandleStartObjectInEnumerable(ref state, options, state.Current.JsonPropertyInfo!.DeclaredPropertyType);
            }
            else if (state.Current.JsonPropertyInfo != null)
            {
                // Nested object within an object.
                Debug.Assert(state.Current.IsProcessingObject(ClassType.Object));

                Type objType = state.Current.JsonPropertyInfo.RuntimePropertyType;
                state.Push();
                state.Current.Initialize(objType, options);
            }

            JsonClassInfo classInfo = state.Current.JsonClassInfo!;

            Debug.Assert(state.Current.IsProcessingObject(ClassType.Dictionary) || state.Current.IsProcessingObject(ClassType.Object) || state.Current.IsProcessingObject(ClassType.Enumerable));

            if (state.Current.IsProcessingObject(ClassType.Dictionary))
            {
                object? value = ReadStackFrame.CreateDictionaryValue(ref state);

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
            else if (state.Current.IsProcessingObject(ClassType.Enumerable))
            {
                // Nested array with metadata within another array with metadata.
                HandleStartObjectInEnumerable(ref state, options, classInfo.Type);

                Debug.Assert(options.ReferenceHandling.ShouldReadPreservedReferences());
                Debug.Assert(state.Current.JsonClassInfo!.Type.GetGenericTypeDefinition() == typeof(JsonPreservableArrayReference<>));

                state.Current.ReturnValue = state.Current.JsonClassInfo.CreateObject!();
                state.Current.IsNestedPreservedArray = true;
            }
        }

        private static void HandleEndObject(ref ReadStack state)
        {
            Debug.Assert(state.Current.JsonClassInfo != null);

            // Only allow dictionaries to be processed here if this is the DataExtensionProperty or if the dictionary is a preserved reference.
            Debug.Assert(!state.Current.IsProcessingDictionary() ||
                state.Current.JsonClassInfo.DataExtensionProperty == state.Current.JsonPropertyInfo ||
                (state.Current.IsProcessingObject(ClassType.Dictionary) && state.Current.ReferenceId != null));

            // Check if we are trying to build the sorted cache.
            if (state.Current.PropertyRefCache != null)
            {
                state.Current.JsonClassInfo.UpdateSortedPropertyCache(ref state.Current);
            }

            object? value;
            // Used for ReferenceHandling.Preserve
            if (state.Current.IsPreservedArray)
            {
                value = GetPreservedArrayValue(ref state);
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
                // Set directly when handling non-nested preserved array
                bool setPropertyDirectly = state.Current.IsPreservedArray && !state.Current.IsNestedPreservedArray;
                state.Pop();

                ApplyObjectToEnumerable(value, ref state, setPropertyDirectly);
            }
        }

        private static object GetPreservedArrayValue(ref ReadStack state)
        {
            JsonPropertyInfo info = GetValuesPropertyInfoFromJsonPreservableArrayRef(ref state.Current);
            object? value = info.GetValueAsObject(state.Current.ReturnValue);

            if (value == null)
            {
                ThrowHelper.ThrowJsonException_MetadataPreservedArrayValuesNotFound(info.DeclaredPropertyType);
            }

            return value;
        }

        private static void HandleStartPreservedArray(ref ReadStack state, JsonSerializerOptions options)
        {
            // Check we are not parsing into an immutable list or array.
            if (state.Current.JsonPropertyInfo!.EnumerableConverter != null)
            {
                ThrowHelper.ThrowJsonException_MetadataCannotParsePreservedObjectIntoImmutable(state.Current.JsonPropertyInfo.DeclaredPropertyType);
            }
            Type preservedObjType = state.Current.JsonPropertyInfo.GetJsonPreservableArrayReferenceType();
            if (state.Current.IsProcessingProperty(ClassType.Enumerable))
            {
                state.Push();
                state.Current.Initialize(preservedObjType, options);
            }
            else
            {
                // For array objects, we don't need to Push a new frame to the stack,
                // so we just call Initialize again passing the wrapper class
                // since we are going to handle the array at the moment we step into JsonPreservableArrayReference<T>.Values.
                state.Current.Initialize(preservedObjType, options);
            }

            state.Current.IsPreservedArray = true;
        }

        [PreserveDependency("get_Values", "System.Text.Json.JsonPreservableArrayReference`1")]
        [PreserveDependency("set_Values", "System.Text.Json.JsonPreservableArrayReference`1")]
        [PreserveDependency(".ctor()", "System.Text.Json.JsonPreservableArrayReference`1")]
        private static void HandleStartObjectInEnumerable(ref ReadStack state, JsonSerializerOptions options, Type type)
        {
            if (!state.Current.CollectionPropertyInitialized)
            {
                if (options.ReferenceHandling.ShouldReadPreservedReferences())
                {
                    HandleStartPreservedArray(ref state, options);
                }
                else
                {
                    // We have bad JSON: enumerable element appeared without preceding StartArray token.
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(type);
                }
            }
            else
            {
                Type objType = state.Current.GetElementType();
                state.Push();
                state.Current.Initialize(objType, options);
            }
        }
    }
}
