// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization;

/// <summary>
/// When placed on a field or property, indicates if member will replaced or populated.
/// When placed on a type with <see cref="JsonObjectCreationHandling.Populate"/> indicates that all members which are capable of being populated will be.
/// </summary>
/// <remarks>
/// For attributes placed on fields and properties with default resolvers this will be reflected with <see cref="JsonPropertyInfo.ObjectCreationHandling"/>.
/// For attributes placed on types this will be reflected with <see cref="JsonTypeInfo.PreferredPropertyObjectCreationHandling"/>.
/// Note the attribute corresponds only to the preferred values of properties.
/// Only property type is taken into consideration. For example if property is of type
/// <see cref="IEnumerable{T}"/> but it is assigned <see cref="List{T}"/> it will not be populated
/// because <see cref="IEnumerable{T}"/> is not capable of populating.
/// Additionally value types require a setter to be populated in which case populating means
/// that deserialization will happen on the copy of the value type and assigned back with setter when deserialization is finished.
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false)]
public sealed class JsonObjectCreationHandlingAttribute : JsonAttribute
{
    /// <summary>
    /// Indicates what settings should be used when serializing or deserializing members.
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
