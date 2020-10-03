// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    [DebuggerDisplay("ClassType.{JsonClassInfo.ClassType}, {JsonClassInfo.Type.Name}")]
    internal struct WriteStackFrame
    {
        /// <summary>
        /// The enumerator for resumable collections.
        /// </summary>
        public IEnumerator? CollectionEnumerator;

        /// <summary>
        /// The original JsonPropertyInfo that is not changed. It contains all properties.
        /// </summary>
        /// <remarks>
        /// For objects, it is either the actual (real) JsonPropertyInfo or the <see cref="JsonClassInfo.PropertyInfoForClassInfo"/> for the class.
        /// For collections, it is the <see cref="JsonClassInfo.PropertyInfoForClassInfo"/> for the class and current element.
        /// </remarks>
        public JsonPropertyInfo? DeclaredJsonPropertyInfo;

        /// <summary>
        /// Used when processing extension data dictionaries.
        /// </summary>
        public bool IgnoreDictionaryKeyPolicy;

        /// <summary>
        /// The class (POCO or IEnumerable) that is being populated.
        /// </summary>
        public JsonClassInfo JsonClassInfo;

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
        /// The run-time JsonPropertyInfo that contains the ClassInfo and ConverterBase for polymorphic scenarios.
        /// </summary>
        /// <remarks>
        /// For objects, it is the <see cref="JsonClassInfo.PropertyInfoForClassInfo"/> for the class and current property.
        /// For collections, it is the <see cref="JsonClassInfo.PropertyInfoForClassInfo"/> for the class and current element.
        /// </remarks>
        public JsonPropertyInfo? PolymorphicJsonPropertyInfo;

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
            PolymorphicJsonPropertyInfo = null;
            PropertyState = StackFramePropertyState.None;
        }

        /// <summary>
        /// Return the property that contains the correct polymorphic properties including
        /// the ClassType and ConverterBase.
        /// </summary>
        public JsonPropertyInfo GetPolymorphicJsonPropertyInfo()
        {
            return PolymorphicJsonPropertyInfo ?? DeclaredJsonPropertyInfo!;
        }

        /// <summary>
        /// Initializes the state for polymorphic or re-entry cases.
        /// </summary>
        public JsonConverter InitializeReEntry(Type type, JsonSerializerOptions options, string? propertyName = null)
        {
            JsonClassInfo classInfo = options.GetOrAddClass(type);

            // Set for exception handling calculation of JsonPath.
            JsonPropertyNameAsString = propertyName;

            PolymorphicJsonPropertyInfo = classInfo.PropertyInfoForClassInfo;
            return PolymorphicJsonPropertyInfo.ConverterBase;
        }

        public void Reset()
        {
            CollectionEnumerator = null;
            EnumeratorIndex = 0;
            IgnoreDictionaryKeyPolicy = false;
            JsonClassInfo = null!;
            OriginalDepth = 0;
            ProcessedStartToken = false;
            ProcessedEndToken = false;

            EndProperty();
        }
    }
}
