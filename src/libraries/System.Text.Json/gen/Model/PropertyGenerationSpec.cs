// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization;
using SourceGenerators;

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
    [DebuggerDisplay("Name = {MemberName}, Type = {PropertyType.Name}")]
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
        public required string EffectiveJsonPropertyName { get; init; }

        /// <summary>
        /// The field identifier used for storing JsonEncodedText for use by the fast-path serializer.
        /// </summary>
        public required string PropertyNameFieldName { get; init; }

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
        /// Gets a reference to the property type.
        /// </summary>
        public required TypeRef PropertyType { get; init; }

        /// <summary>
        /// Gets a reference to the declaring type of the property.
        /// </summary>
        public required TypeRef DeclaringType { get; init; }

        /// <summary>
        /// Design-time specified custom converter type.
        /// </summary>
        public required TypeRef? ConverterType { get; init; }

        /// <summary>
        /// Determines if the specified property should be included in the fast-path method body.
        /// </summary>
        public bool ShouldIncludePropertyForFastPath(ContextGenerationSpec contextSpec)
        {
            // Discard ignored properties
            if (DefaultIgnoreCondition is JsonIgnoreCondition.Always)
            {
                return false;
            }

            // Discard properties without getters
            if (!CanUseGetter)
            {
                return false;
            }

            // Discard fields when JsonInclude or IncludeFields aren't enabled.
            if (!IsProperty && !HasJsonInclude && contextSpec.GeneratedOptionsSpec?.IncludeFields != true)
            {
                return false;
            }

            // Ignore read-only properties/fields if enabled in configuration.
            if (IsReadOnly)
            {
                if (IsProperty)
                {
                    if (contextSpec.GeneratedOptionsSpec?.IgnoreReadOnlyProperties == true)
                    {
                        return false;
                    }
                }
                else if (contextSpec.GeneratedOptionsSpec?.IgnoreReadOnlyFields == true)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
