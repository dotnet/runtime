// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization;

namespace System.Text.Json.SourceGeneration
{
    [DebuggerDisplay("Name={Name}, Type={TypeMetadata}")]
    internal sealed class PropertyGenerationSpec
    {
        /// <summary>
        /// The CLR name of the property.
        /// </summary>
        public string ClrName { get; init; }

        /// <summary>
        /// Is this a property or a field?
        /// </summary>
        public bool IsProperty { get; init; }

        /// <summary>
        /// The property name specified via JsonPropertyNameAttribute, if available.
        /// </summary>
        public string? JsonPropertyName { get; init; }

        /// <summary>
        /// Whether the property has a set method.
        /// </summary>
        public bool IsReadOnly { get; init; }

        /// <summary>
        /// Whether the property has a public or internal (only usable when JsonIncludeAttribute is specified)
        /// getter that can be referenced in generated source code.
        /// </summary>
        public bool CanUseGetter { get; init; }

        /// <summary>
        /// Whether the property has a public or internal (only usable when JsonIncludeAttribute is specified)
        /// setter that can be referenced in generated source code.
        /// </summary>
        public bool CanUseSetter { get; init; }

        public bool GetterIsVirtual { get; init; }

        public bool SetterIsVirtual { get; init; }

        /// <summary>
        /// The <see cref="JsonIgnoreCondition"/> for the property.
        /// </summary>
        public JsonIgnoreCondition? DefaultIgnoreCondition { get; init; }

        /// <summary>
        /// The <see cref="JsonNumberHandling"/> for the property.
        /// </summary>
        public JsonNumberHandling? NumberHandling { get; init; }

        /// <summary>
        /// Whether the property has the JsonIncludeAttribute. If so, non-public accessors can be used for (de)serialziation.
        /// </summary>
        public bool HasJsonInclude { get; init; }

        /// <summary>
        /// Generation specification for the property's type.
        /// </summary>
        public TypeGenerationSpec TypeGenerationSpec { get; init; }

        /// <summary>
        /// Compilable name of the property's declaring type.
        /// </summary>
        public string DeclaringTypeRef { get; init; }

        /// <summary>
        /// Source code to instantiate design-time specified custom converter.
        /// </summary>
        public string? ConverterInstantiationLogic { get; init; }
    }
}
