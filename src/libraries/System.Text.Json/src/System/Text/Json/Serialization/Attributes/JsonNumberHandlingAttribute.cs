// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// When placed on a type, property, or field, indicates what <see cref="JsonNumberHandling"/>
    /// settings should be used when serializing or deserializing numbers.
    /// </summary>
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
