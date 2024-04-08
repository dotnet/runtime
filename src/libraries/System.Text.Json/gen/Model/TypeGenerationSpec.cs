﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using SourceGenerators;

namespace System.Text.Json.SourceGeneration
{
    /// <summary>
    /// Models a generated type.
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
    [DebuggerDisplay("Type = {TypeRef.Name}, ClassType = {ClassType}")]
    public sealed record TypeGenerationSpec
    {
        /// <summary>
        /// The type being generated.
        /// </summary>
        public required TypeRef TypeRef { get; init; }

        /// <summary>
        /// The name of the public <c>JsonTypeInfo&lt;T&gt;</c> property for this type on the generated context class.
        /// For example, if the context class is named MyJsonContext, and the value of this property is JsonMessage;
        /// then users will call MyJsonContext.JsonMessage to access generated metadata for the type.
        /// </summary>
        public required string TypeInfoPropertyName { get; init; }

        public required JsonSourceGenerationMode GenerationMode { get; init; }

        public required JsonPrimitiveTypeKind? PrimitiveTypeKind { get; init; }

        public required ClassType ClassType { get; init; }

        public required bool ImplementsIJsonOnSerialized { get; init; }
        public required bool ImplementsIJsonOnSerializing { get; init; }

        public required bool IsPolymorphic { get; init; }

        public required bool IsValueTuple { get; init; }

        public required JsonNumberHandling? NumberHandling { get; init; }
        public required JsonUnmappedMemberHandling? UnmappedMemberHandling { get; init; }
        public required JsonObjectCreationHandling? PreferredPropertyObjectCreationHandling { get; init; }

        /// <summary>
        /// List of all properties without conflict resolution or sorting to be generated for the metadata-based serializer.
        /// </summary>
        public required ImmutableEquatableArray<PropertyGenerationSpec> PropertyGenSpecs { get; init; }

        /// <summary>
        /// List of properties with compile-time conflict resolution and sorting to be generated for the fast-path serializer.
        /// Contains indices pointing to <see cref="PropertyGenSpecs"/>. A <see cref="null"/> value in the case of object types
        /// indicates that a naming conflict was found and that an exception throwing stub should be emitted in the fast-path method.
        /// </summary>
        public required ImmutableEquatableArray<int>? FastPathPropertyIndices { get; init; }

        public required ImmutableEquatableArray<ParameterGenerationSpec> CtorParamGenSpecs { get; init; }

        public required ImmutableEquatableArray<PropertyInitializerGenerationSpec> PropertyInitializerSpecs { get; init; }

        public required CollectionType CollectionType { get; init; }

        public required TypeRef? CollectionKeyType { get; init; }

        public required TypeRef? CollectionValueType { get; init; }

        public required ObjectConstructionStrategy ConstructionStrategy { get; init; }

        public required bool ConstructorSetsRequiredParameters { get; init; }

        public required TypeRef? NullableUnderlyingType { get; init; }

        /// <summary>
        /// Supports deserialization of extension data dictionaries typed as <c>I[ReadOnly]Dictionary&lt;string, object/JsonElement&gt;</c>.
        /// Specifies a concrete type to instantiate, which would be <c>Dictionary&lt;string, object/JsonElement&gt;</c>.
        /// </summary>
        public required TypeRef? RuntimeTypeRef { get; init; }

        public required bool HasExtensionDataPropertyType { get; init; }

        public required TypeRef? ConverterType { get; init; }

        public required string? ImmutableCollectionFactoryMethod { get; init; }

        public bool IsFastPathSupported()
        {
            if (IsPolymorphic)
            {
                return false;
            }

            if (JsonHelpers.RequiresSpecialNumberHandlingOnWrite(NumberHandling))
            {
                return false;
            }

            switch (ClassType)
            {
                case ClassType.Object:
                    if (HasExtensionDataPropertyType)
                    {
                        return false;
                    }

                    foreach (PropertyGenerationSpec property in PropertyGenSpecs)
                    {
                        if (property.PropertyType.SpecialType is SpecialType.System_Object ||
                            property.NumberHandling != null ||
                            property.ConverterType != null)
                        {
                            return false;
                        }
                    }

                    return true;

                case ClassType.Enumerable:
                    return CollectionType != CollectionType.IAsyncEnumerableOfT &&
                           CollectionValueType!.SpecialType is not SpecialType.System_Object;

                case ClassType.Dictionary:
                    return CollectionKeyType!.SpecialType is SpecialType.System_String &&
                           CollectionValueType!.SpecialType is not SpecialType.System_Object;

                default:
                    return false;
            }
        }
    }
}
