// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json
{
    [DebuggerDisplay("ConverterStrategy.{JsonTypeInfo.PropertyInfoForTypeInfo.ConverterStrategy}, {JsonTypeInfo.Type.Name}")]
    internal struct ReadStackFrame
    {
        // Current property values.
        public JsonPropertyInfo? JsonPropertyInfo;
        public StackFramePropertyState PropertyState;
        public bool UseExtensionProperty;

        // Support JSON Path on exceptions and non-string Dictionary keys.
        // This is Utf8 since we don't want to convert to string until an exception is thown.
        // For dictionary keys we don't want to convert to TKey until we have both key and value when parsing the dictionary elements on stream cases.
        public byte[]? JsonPropertyName;
        public string? JsonPropertyNameAsString; // This is used for string dictionary keys and re-entry cases that specify a property name.

        // Stores the non-string dictionary keys for continuation.
        public object? DictionaryKey;

        // Validation state.
        public int OriginalDepth;
        public JsonTokenType OriginalTokenType;

        // Current object (POCO or IEnumerable).
        public object? ReturnValue; // The current return value used for re-entry.
        public JsonTypeInfo JsonTypeInfo;
        public StackFrameObjectState ObjectState; // State tracking the current object.

        private JsonPropertyInfo? CachedPolymorphicJsonPropertyInfo;

        /// <summary>
        /// Dictates how <see cref="CachedPolymorphicJsonPropertyInfo"/> is to be consumed.
        /// </summary>
        /// <remarks>
        /// If true we are dispatching serialization to a polymorphic converter that should consume it.
        /// If false it is simply a value we are caching for performance.
        /// </remarks>
        public PolymorphicSerializationState PolymorphicSerializationState;

        // Validate EndObject token on array with preserve semantics.
        public bool ValidateEndTokenOnArray;

        // For performance, we order the properties by the first deserialize and PropertyIndex helps find the right slot quicker.
        public int PropertyIndex;
        public List<PropertyRef>? PropertyRefCache;

        // Holds relevant state when deserializing objects with parameterized constructors.
        public int CtorArgumentStateIndex;
        public ArgumentState? CtorArgumentState;

        // Whether to use custom number handling.
        public JsonNumberHandling? NumberHandling;

        /// <summary>
        /// Return the property that contains the correct polymorphic properties including
        /// the ConverterStrategy and ConverterBase.
        /// </summary>
        public JsonTypeInfo GetPolymorphicJsonTypeInfo()
        {
            JsonTypeInfo? jsonTypeInfo;

            if (PolymorphicSerializationState == PolymorphicSerializationState.PolymorphicReEntryStarted)
            {
                Debug.Assert(CachedPolymorphicJsonPropertyInfo is not null);
                jsonTypeInfo = CachedPolymorphicJsonPropertyInfo.RuntimeTypeInfo;
            }
            else
            {
                ConverterStrategy converterStrategy = JsonTypeInfo.PropertyInfoForTypeInfo.ConverterStrategy;

                if (converterStrategy == ConverterStrategy.Object)
                {
                    if (JsonPropertyInfo is not null)
                    {
                        jsonTypeInfo = JsonPropertyInfo.RuntimeTypeInfo;
                    }
                    else
                    {
                        jsonTypeInfo = CtorArgumentState!.JsonParameterInfo!.RuntimeTypeInfo;
                    }
                }
                else if (converterStrategy == ConverterStrategy.Value)
                {
                    // Although ConverterStrategy.Value doesn't push, a custom custom converter may re-enter serialization.
                    Debug.Assert(JsonPropertyInfo is not null);
                    jsonTypeInfo = JsonPropertyInfo.RuntimeTypeInfo;
                }
                else
                {
                    Debug.Assert(((ConverterStrategy.Enumerable | ConverterStrategy.Dictionary) & converterStrategy) != 0);
                    Debug.Assert(JsonTypeInfo.ElementTypeInfo is not null);
                    jsonTypeInfo = JsonTypeInfo.ElementTypeInfo;
                }
            }

            return jsonTypeInfo;
        }

        /// <summary>
        /// Initializes the state for polymorphic cases and returns the appropriate converter.
        /// </summary>
        public JsonConverter InitializePolymorphicReEntry(Type type, JsonSerializerOptions options)
        {
            Debug.Assert(PolymorphicSerializationState == PolymorphicSerializationState.None);

            // For perf, avoid the dictionary lookup in GetOrAddClass() for every element of a collection
            // if the current element is the same type as the previous element.
            if (CachedPolymorphicJsonPropertyInfo?.RuntimePropertyType != type)
            {
                JsonTypeInfo typeInfo = options.GetOrAddClass(type);
                CachedPolymorphicJsonPropertyInfo = typeInfo.PropertyInfoForTypeInfo;
            }

            PolymorphicSerializationState = PolymorphicSerializationState.PolymorphicReEntryStarted;
            return CachedPolymorphicJsonPropertyInfo.ConverterBase;
        }

        public JsonConverter ResumePolymorphicReEntry()
        {
            Debug.Assert(PolymorphicSerializationState == PolymorphicSerializationState.PolymorphicReEntrySuspended);
            Debug.Assert(CachedPolymorphicJsonPropertyInfo is not null);

            PolymorphicSerializationState = PolymorphicSerializationState.PolymorphicReEntryStarted;
            return CachedPolymorphicJsonPropertyInfo.ConverterBase;
        }

        public void EndConstructorParameter()
        {
            CtorArgumentState!.JsonParameterInfo = null;
            JsonPropertyName = null;
            PropertyState = StackFramePropertyState.None;
        }

        public void EndProperty()
        {
            JsonPropertyInfo = null!;
            JsonPropertyName = null;
            JsonPropertyNameAsString = null;
            PropertyState = StackFramePropertyState.None;
            ValidateEndTokenOnArray = false;
            CachedPolymorphicJsonPropertyInfo = null;
            PolymorphicSerializationState = PolymorphicSerializationState.None;

            // No need to clear these since they are overwritten each time:
            //  NumberHandling
            //  UseExtensionProperty
        }

        public void EndElement()
        {
            JsonPropertyNameAsString = null;
            PropertyState = StackFramePropertyState.None;
        }

        /// <summary>
        /// Is the current object a Dictionary.
        /// </summary>
        public bool IsProcessingDictionary()
        {
            return (JsonTypeInfo.PropertyInfoForTypeInfo.ConverterStrategy & ConverterStrategy.Dictionary) != 0;
        }

        /// <summary>
        /// Is the current object an Enumerable.
        /// </summary>
        public bool IsProcessingEnumerable()
        {
            return (JsonTypeInfo.PropertyInfoForTypeInfo.ConverterStrategy & ConverterStrategy.Enumerable) != 0;
        }

        //public void Reset()
        //{
        //    CtorArgumentStateIndex = 0;
        //    CtorArgumentState = null;
        //    JsonTypeInfo = null!;
        //    ObjectState = StackFrameObjectState.None;
        //    OriginalDepth = 0;
        //    OriginalTokenType = JsonTokenType.None;
        //    PropertyIndex = 0;
        //    PropertyRefCache = null;
        //    ReturnValue = null;
        //    DictionaryKey = null;
        //    UseExtensionProperty = false;
        //    NumberHandling = null;

        //    EndProperty();

        //    Debug.Assert(EqualityComparer<ReadStackFrame>.Default.Equals(this, default));
        //}
    }
}
