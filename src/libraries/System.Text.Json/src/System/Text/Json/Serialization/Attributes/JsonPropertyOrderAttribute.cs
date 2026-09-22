// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Specifies the property order that is present in the JSON when serializing. Lower values are serialized first.
    /// If the attribute is not specified, the default value is 0.
    /// </summary>
    /// <remarks>
    /// If multiple properties have the same value, the ordering is undefined between them.
    /// For contracts originating from <see cref="DefaultJsonTypeInfoResolver"/> or <see cref="JsonSerializerContext"/>,
    /// this attribute will be mapped to <see cref="JsonPropertyInfo.Order"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class JsonPropertyOrderAttribute : JsonAttribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="JsonPropertyOrderAttribute"/> with the specified order.
        /// </summary>
        /// <param name="order">The order of the property.</param>
        public JsonPropertyOrderAttribute(int order)
        {
            Order = order;
        }

        /// <summary>
        /// The serialization order of the property.
        /// </summary>
        public int Order { get; }
    }
}
