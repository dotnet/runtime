// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    internal class JsonPreservedReference<T>
    {
        public T Values { get; set; }
    }

    public static partial class JsonSerializer
    {
        [PreserveDependency("get_Values", "System.Text.Json.JsonPreservedReference`1")]
        [PreserveDependency("set_Values", "System.Text.Json.JsonPreservedReference`1")]
        [PreserveDependency(".ctor()", "System.Text.Json.JsonPreservedReference`1")]
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
                    if (options.ReferenceHandling.ShouldReadPreservedReferences())
                    {
                        HandlePreservedArray(ref state, options);
                    }
                    else
                    {
                        // We have bad JSON: enumerable element appeared without preceding StartArray token.
                        ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(state.Current.JsonPropertyInfo.DeclaredPropertyType);
                    }
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
            // Only allow dictionaries to be processed here if this is the DataExtensionProperty or if it was a preserved reference.
            Debug.Assert(!state.Current.IsProcessingDictionary() || state.Current.JsonClassInfo.DataExtensionProperty == state.Current.JsonPropertyInfo || state.Current.ShouldHandleReference);

            // Check if we are trying to build the sorted cache.
            if (state.Current.PropertyRefCache != null)
            {
                state.Current.JsonClassInfo.UpdateSortedPropertyCache(ref state.Current);
            }

            object value;
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
                state.Pop();

                ApplyObjectToEnumerable(value, ref state);
            }
        }

        private static object GetPreservedArrayValue(ref ReadStack state)
        {
            // Preserved JSON arrays are wrapped into JsonPreservedReference<T> where T is the original type of the enumerable
            // and Values is the actual enumerable instance being preserved.
            JsonPropertyInfo info = state.Current.JsonClassInfo.PropertyCache["Values"];
            object value = info.GetValueAsObject(state.Current.ReturnValue);

            if (value == null)
            {
                ThrowHelper.ThrowJsonException_MetadataPreservedArrayValuesNotFound(info.DeclaredPropertyType);
            }

            return value;
        }

        private static void HandlePreservedArray(ref ReadStack state, JsonSerializerOptions options)
        {
            // Check we are not parsing into immutable or array.
            if (state.Current.JsonPropertyInfo.EnumerableConverter != null)
            {
                ThrowHelper.ThrowJsonException_MetadataCannotParsePreservedObjectIntoImmutable(state.Current.JsonPropertyInfo.DeclaredPropertyType);
            }
            Type preservedObjType = state.Current.JsonPropertyInfo.GetJsonPreservedReferenceType();
            if (state.Current.IsProcessingProperty(ClassType.Enumerable))
            {
                state.Push();
                state.Current.Initialize(preservedObjType, options);
            }
            else
            {
                // Re-Initialize the current frame.
                state.Current.Initialize(preservedObjType, options);
            }

            state.Current.IsPreservedArray = true;
        }
    }
}
