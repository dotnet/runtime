// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// When placed on a type, property, or field, indicates what <see cref="JsonNumberHandling"/>
    /// settings should be used when serializing or deserializing numbers.
    /// </summary>
    /// <remarks>
    /// For contracts originating from <see cref="DefaultJsonTypeInfoResolver"/> or <see cref="JsonSerializerContext"/>,
    /// when placed on a property or field this attribute will be mapped to <see cref="JsonPropertyInfo.NumberHandling"/>,
    /// and when placed on a type it will be mapped to <see cref="JsonTypeInfo.NumberHandling"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class JsonNumberHandlingAttribute : JsonAttribute
    {
        /// <summary>
        /// Indicates what settings should be used when serializing or deserializing numbers.
        /// </summary>
        public JsonNumberHandling Handling { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="JsonNumberHandlingAttribute"/>.
        /// </summary>
        public JsonNumberHandlingAttribute(JsonNumberHandling handling)
        {
            if (!JsonSerializer.IsValidNumberHandlingValue(handling))
            {
                throw new ArgumentOutOfRangeException(nameof(handling));
            }
            Handling = handling;
        }
    }
}
