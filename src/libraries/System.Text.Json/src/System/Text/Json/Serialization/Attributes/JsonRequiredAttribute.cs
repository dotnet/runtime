// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Specifies the property is required when deserializing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class JsonRequiredAttribute : JsonAttribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="JsonRequiredAttribute"/>.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> token in JSON will not trigger a validation error.
        /// </remarks>
        public JsonRequiredAttribute()
        {
        }
    }
}
