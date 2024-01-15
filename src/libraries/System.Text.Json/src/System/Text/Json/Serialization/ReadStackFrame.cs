// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json
{
    [StructLayout(LayoutKind.Auto)]
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal struct ReadStackFrame
    {
        // Current property values.
        public JsonPropertyInfo? JsonPropertyInfo;
        public StackFramePropertyState PropertyState;
        public bool UseExtensionProperty;

        // Support JSON Path on exceptions and non-string Dictionary keys.
        // This is Utf8 since we don't want to convert to string until an exception is thrown.
        // For dictionary keys we don't want to convert to TKey until we have both key and value when parsing the dictionary elements on stream cases.
        public byte[]? JsonPropertyName;
        public string? JsonPropertyNameAsString; // This is used for string dictionary keys and re-entry cases that specify a property name.

        // Stores the non-string dictionary keys for continuation.
        public object? DictionaryKey;

        /// <summary>
        /// Records the Utf8JsonReader Depth at the start of the current value.
        /// </summary>
        public int OriginalDepth;
#if DEBUG
        /// <summary>
        /// Records the Utf8JsonReader TokenType at the start of the current value.
        /// Only used to validate debug builds.
        /// </summary>
        public JsonTokenType OriginalTokenType;
#endif

        // Current object (POCO or IEnumerable).
        public object? ReturnValue; // The current return value used for re-entry.
        public JsonTypeInfo JsonTypeInfo;
        public StackFrameObjectState ObjectState; // State tracking the current object.

        // Current object can contain metadata
        public bool CanContainMetadata;
        public MetadataPropertyName LatestMetadataPropertyName;
        public MetadataPropertyName MetadataPropertyNames;

        // Serialization state for value serialized by the current frame.
        public PolymorphicSerializationState PolymorphicSerializationState;

        // Holds any entered polymorphic JsonTypeInfo metadata.
        public JsonTypeInfo? PolymorphicJsonTypeInfo;

        // Gets the initial JsonTypeInfo metadata used when deserializing the current value.
        public JsonTypeInfo BaseJsonTypeInfo
            => PolymorphicSerializationState == PolymorphicSerializationState.PolymorphicReEntryStarted
                ? PolymorphicJsonTypeInfo!
                : JsonTypeInfo;

        // For performance, we order the properties by the first deserialize and PropertyIndex helps find the right slot quicker.
        public int PropertyIndex;
        public List<PropertyRef>? PropertyRefCache;

        // Holds relevant state when deserializing objects with parameterized constructors.
        public ArgumentState? CtorArgumentState;

        // Whether to use custom number handling.
        public JsonNumberHandling? NumberHandling;

        // Represents required properties which have value assigned.
        // Each bit corresponds to a required property.
        // False means that property is not set (not yet occurred in the payload).
        // Length of the BitArray is equal to number of required properties.
        // Every required JsonPropertyInfo has RequiredPropertyIndex property which maps to an index in this BitArray.
        public BitArray? RequiredPropertiesSet;

        // Tracks state related to property population.
        public bool HasParentObject;
        public bool IsPopulating;

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
            return JsonTypeInfo.Kind is JsonTypeInfoKind.Dictionary;
        }

        /// <summary>
        /// Is the current object an Enumerable.
        /// </summary>
        public bool IsProcessingEnumerable()
        {
            return JsonTypeInfo.Kind is JsonTypeInfoKind.Enumerable;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MarkRequiredPropertyAsRead(JsonPropertyInfo propertyInfo)
        {
            if (propertyInfo.IsRequired)
            {
                Debug.Assert(RequiredPropertiesSet != null);
                RequiredPropertiesSet[propertyInfo.RequiredPropertyIndex] = true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InitializeRequiredPropertiesValidationState(JsonTypeInfo typeInfo)
        {
            Debug.Assert(RequiredPropertiesSet == null);

            if (typeInfo.NumberOfRequiredProperties > 0)
            {
                RequiredPropertiesSet = new BitArray(typeInfo.NumberOfRequiredProperties);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ValidateAllRequiredPropertiesAreRead(JsonTypeInfo typeInfo)
        {
            if (typeInfo.NumberOfRequiredProperties > 0)
            {
                Debug.Assert(RequiredPropertiesSet != null);

                if (!RequiredPropertiesSet.HasAllSet())
                {
                    ThrowHelper.ThrowJsonException_JsonRequiredPropertyMissing(typeInfo, RequiredPropertiesSet);
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay => $"ConverterStrategy.{JsonTypeInfo?.Converter.ConverterStrategy}, {JsonTypeInfo?.Type.Name}";
    }
}
