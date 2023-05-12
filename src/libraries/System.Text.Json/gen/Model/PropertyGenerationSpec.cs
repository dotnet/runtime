// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization;

namespace System.Text.Json.SourceGeneration
{
    /// <summary>
    /// Models a property for a generated type.
    /// </summary>
    /// <remarks>
    /// Type needs to be cacheable as a Roslyn incremental value so it must be
    ///
    /// 1) immutable and
    /// 2) implement structural (pointwise) equality comparison.
    ///
    /// We can get these properties for free provided that we
    ///
    /// a) define the type as an immutable C# record and
    /// b) ensure all nested members are also immutable and implement structural equality.
    ///
    /// When adding new members to the type, please ensure that these properties
    /// are satisfied otherwise we risk breaking incremental caching in the source generator!
    /// </remarks>
    [DebuggerDisplay("Name={Name}, Type={TypeMetadata}")]
    public sealed record PropertyGenerationSpec
    {
        /// <summary>
        /// The exact name specified in the source code. This might be different
        /// from the <see cref="MemberName"/> because source code might be decorated
        /// with '@' for reserved keywords, e.g. public string @event { get; set; }
        /// </summary>
        public required string NameSpecifiedInSourceCode { get; init; }

        public required string MemberName { get; init; }

        /// <summary>
        /// Is this a property or a field?
        /// </summary>
        public required bool IsProperty { get; init; }

        /// <summary>
        /// If representing a property, returns true if either the getter or setter are public.
        /// </summary>
        public required bool IsPublic { get; init; }

        public required bool IsVirtual { get; init; }

        /// <summary>
        /// The property name specified via JsonPropertyNameAttribute, if available.
        /// </summary>
        public required string? JsonPropertyName { get; init; }

        /// <summary>
        /// The pre-determined JSON property name, accounting for <see cref="JsonNamingPolicy"/>
        /// specified ahead-of-time via <see cref="JsonSourceGenerationOptionsAttribute"/>.
        /// Only used in fast-path serialization logic.
        /// </summary>
        public required string RuntimePropertyName { get; init; }

        public required string PropertyNameVarName { get; init; }

        /// <summary>
        /// Whether the property has a set method.
        /// </summary>
        public required bool IsReadOnly { get; init; }

        /// <summary>
        /// Whether the property is marked `required`.
        /// </summary>
        public required bool IsRequired { get; init; }

        /// <summary>
        /// The property is marked with JsonRequiredAttribute.
        /// </summary>
        public required bool HasJsonRequiredAttribute { get; init; }

        /// <summary>
        /// Whether the property has an init-only set method.
        /// </summary>
        public required bool IsInitOnlySetter { get; init; }

        /// <summary>
        /// Whether the property has a public or internal (only usable when JsonIncludeAttribute is specified)
        /// getter that can be referenced in generated source code.
        /// </summary>
        public required bool CanUseGetter { get; init; }

        /// <summary>
        /// Whether the property has a public or internal (only usable when JsonIncludeAttribute is specified)
        /// setter that can be referenced in generated source code.
        /// </summary>
        public required bool CanUseSetter { get; init; }

        public required bool GetterIsVirtual { get; init; }

        public required bool SetterIsVirtual { get; init; }

        /// <summary>
        /// The <see cref="JsonIgnoreCondition"/> for the property.
        /// </summary>
        public required JsonIgnoreCondition? DefaultIgnoreCondition { get; init; }

        /// <summary>
        /// The <see cref="JsonNumberHandling"/> for the property.
        /// </summary>
        public required JsonNumberHandling? NumberHandling { get; init; }

        /// <summary>
        /// The <see cref="JsonObjectCreationHandling"/> for the property.
        /// </summary>
        public required JsonObjectCreationHandling? ObjectCreationHandling { get; init; }

        /// <summary>
        /// The serialization order of the property.
        /// </summary>
        public required int Order { get; init; }

        /// <summary>
        /// Whether the property has the JsonIncludeAttribute. If so, non-public accessors can be used for (de)serialziation.
        /// </summary>
        public required bool HasJsonInclude { get; init; }

        /// <summary>
        /// Whether the property has the JsonExtensionDataAttribute.
        /// </summary>
        public required bool IsExtensionData { get; init; }

        /// <summary>
        /// Generation specification for the property's type.
        /// </summary>
        public required TypeRef PropertyType { get; init; }

        /// <summary>
        /// Compilable name of the property's declaring type.
        /// </summary>
        public required string DeclaringTypeRef { get; init; }

        /// <summary>
        /// Source code to instantiate design-time specified custom converter.
        /// </summary>
        public required string? ConverterInstantiationLogic { get; init; }

        public required bool HasFactoryConverter { get; init; }
    }
}
