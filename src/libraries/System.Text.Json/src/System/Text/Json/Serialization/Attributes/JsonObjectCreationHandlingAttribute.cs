// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization;

/// <summary>
/// Determines how deserialization will handle object creation for fields or properties.
/// </summary>
/// <remarks>
/// When placed on a field or property, indicates if member will replaced or populated.
/// When default resolvers are used this will be mapped to <see cref="JsonPropertyInfo.ObjectCreationHandling"/>.
///
/// When placed on a type with <see cref="JsonObjectCreationHandling.Populate"/> indicates that all members
/// that support population will be populated. When default resolvers are used this will be mapped to <see cref="JsonTypeInfo.PreferredPropertyObjectCreationHandling"/>.
///
/// Note that the attribute corresponds only to the preferred values of creation handling for properties when placed on a type.
/// For example when <see cref="JsonObjectCreationHandlingAttribute"/> with <see cref="JsonObjectCreationHandling.Populate"/> is placed on a class
/// and property is not capable of being populated, it will be replaced.
/// That may be true if i.e. value type doesn't have a setter or property is of type <see cref="IEnumerable{T}"/>.
///
/// Only the property type is taken into consideration. For example if property is of type
/// <see cref="IEnumerable{T}"/> has runtime value of type <see cref="List{T}"/> it will not be populated
/// because <see cref="IEnumerable{T}"/> is not capable of populating.
///
/// Value types require a setter to support population; in such cases
/// deserialization will use a copy of the property value which will get assigned back to the setter once finished.
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false)]
public sealed class JsonObjectCreationHandlingAttribute : JsonAttribute
{
    /// <summary>
    /// Indicates what configuration should be used when deserializing members.
    /// </summary>
    public JsonObjectCreationHandling Handling { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="JsonObjectCreationHandlingAttribute"/>.
    /// </summary>
    public JsonObjectCreationHandlingAttribute(JsonObjectCreationHandling handling)
    {
        if (!JsonSerializer.IsValidCreationHandlingValue(handling))
        {
            throw new ArgumentOutOfRangeException(nameof(handling));
        }

        Handling = handling;
    }
}
