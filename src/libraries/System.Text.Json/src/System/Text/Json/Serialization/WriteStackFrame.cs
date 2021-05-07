// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json
{
    [DebuggerDisplay("ConverterStrategy.{JsonTypeInfo.PropertyInfoForTypeInfo.ConverterStrategy}, {JsonTypeInfo.Type.Name}")]
    internal struct WriteStackFrame
    {
        /// <summary>
        /// The enumerator for resumable collections.
        /// </summary>
        public IEnumerator? CollectionEnumerator;

        /// <summary>
        /// The enumerator for resumable async disposables.
        /// </summary>
        public IAsyncDisposable? AsyncEnumerator;

        /// <summary>
        /// The current stackframe has suspended serialization due to a pending task,
        /// stored in the <see cref="WriteStack.PendingTask"/> property.
        /// </summary>
        public bool AsyncEnumeratorIsPendingCompletion;

        /// <summary>
        /// Flag indicating that the current value has been pushed for cycle detection.
        /// </summary>
        public bool IsCycleDetectionReferencePushed;

        /// <summary>
        /// The original JsonPropertyInfo that is not changed. It contains all properties.
        /// </summary>
        /// <remarks>
        /// For objects, it is either the actual (real) JsonPropertyInfo or the <see cref="JsonTypeInfo.PropertyInfoForTypeInfo"/> for the class.
        /// For collections, it is the <see cref="JsonTypeInfo.PropertyInfoForTypeInfo"/> for the class and current element.
        /// </remarks>
        public JsonPropertyInfo? DeclaredJsonPropertyInfo;

        /// <summary>
        /// Used when processing extension data dictionaries.
        /// </summary>
        public bool IgnoreDictionaryKeyPolicy;

        /// <summary>
        /// The class (POCO or IEnumerable) that is being populated.
        /// </summary>
        public JsonTypeInfo JsonTypeInfo;

        /// <summary>
        /// Validation state for a class.
        /// </summary>
        public int OriginalDepth;

        // Class-level state for collections.
        public bool ProcessedStartToken;
        public bool ProcessedEndToken;

        /// <summary>
        /// Property or Element state.
        /// </summary>
        public StackFramePropertyState PropertyState;

        /// <summary>
        /// The enumerator index for resumable collections.
        /// </summary>
        public int EnumeratorIndex;

        // This is used for re-entry cases for exception handling.
        public string? JsonPropertyNameAsString;

        // Preserve Reference
        public MetadataPropertyName MetadataPropertyName;

        /// <summary>
        /// The run-time JsonPropertyInfo that contains the TypeInfo and ConverterBase for polymorphic scenarios.
        /// </summary>
        /// <remarks>
        /// For objects, it is the <see cref="JsonTypeInfo.PropertyInfoForTypeInfo"/> for the class and current property.
        /// For collections, it is the <see cref="JsonTypeInfo.PropertyInfoForTypeInfo"/> for the class and current element.
        /// </remarks>
        private JsonPropertyInfo? CachedPolymorphicJsonPropertyInfo;

        /// <summary>
        /// Dictates how <see cref="CachedPolymorphicJsonPropertyInfo"/> is to be consumed.
        /// </summary>
        /// <remarks>
        /// If true we are dispatching serialization to a polymorphic converter that should consume it.
        /// If false it is simply a value we are caching for performance.
        /// </remarks>
        public PolymorphicSerializationState PolymorphicSerializationState;

        // Whether to use custom number handling.
        public JsonNumberHandling? NumberHandling;

        public void EndDictionaryElement()
        {
            PropertyState = StackFramePropertyState.None;
        }

        public void EndProperty()
        {
            DeclaredJsonPropertyInfo = null!;
            JsonPropertyNameAsString = null;
            CachedPolymorphicJsonPropertyInfo = null;
            PropertyState = StackFramePropertyState.None;
            PolymorphicSerializationState = PolymorphicSerializationState.None;
        }

        /// <summary>
        /// Return the property that contains the correct polymorphic properties including
        /// the ConverterStrategy and ConverterBase.
        /// </summary>
        public JsonPropertyInfo GetPolymorphicJsonPropertyInfo()
        {
            return PolymorphicSerializationState == PolymorphicSerializationState.PolymorphicReEntryStarted ? CachedPolymorphicJsonPropertyInfo! : DeclaredJsonPropertyInfo!;
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

        //public void Reset()
        //{
        //    CollectionEnumerator = null;
        //    EnumeratorIndex = 0;
        //    AsyncEnumerator = null;
        //    AsyncEnumeratorIsPendingCompletion = false;
        //    IgnoreDictionaryKeyPolicy = false;
        //    JsonTypeInfo = null!;
        //    OriginalDepth = 0;
        //    ProcessedStartToken = false;
        //    ProcessedEndToken = false;
        //    IsCycleDetectionReferencePushed = false;
        //    MetadataPropertyName = MetadataPropertyName.NoMetadata;
        //    NumberHandling = null;

        //    EndProperty();

        //    Debug.Assert(EqualityComparer<WriteStackFrame>.Default.Equals(this, default));
        //}
    }
}
