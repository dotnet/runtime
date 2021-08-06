// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Provides information about a constructor parameter required for JSON deserialization.
    /// </summary>
    public sealed class JsonParameterInfoValues
    {
        /// <summary>
        /// The name of the parameter.
        /// </summary>
        public string Name { get; init; } = null!;

        /// <summary>
        /// The type of the parameter.
        /// </summary>
        public Type ParameterType { get; init; } = null!;

        /// <summary>
        /// The zero-based position of the parameter in the formal parameter list.
        /// </summary>
        public int Position { get; init; }

        /// <summary>
        /// Whether a default value was specified for the parameter.
        /// </summary>
        public bool HasDefaultValue { get; init; }

        /// <summary>
        /// The default value of the parameter.
        /// </summary>
        public object? DefaultValue { get; init; }
    }
}
