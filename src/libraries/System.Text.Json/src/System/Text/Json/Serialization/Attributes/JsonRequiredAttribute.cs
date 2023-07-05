// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Indicates that the annotated member must bind to a JSON property on deserialization.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> token in JSON will not trigger a validation error.
    /// For contracts originating from <see cref="DefaultJsonTypeInfoResolver"/> or <see cref="JsonSerializerContext"/>,
    /// this attribute will be mapped to <see cref="JsonPropertyInfo.IsRequired"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class JsonRequiredAttribute : JsonAttribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="JsonRequiredAttribute"/>.
        /// </summary>
        public JsonRequiredAttribute() { }
    }
}
