﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Provides information about a constructor parameter required for JSON deserialization.
    /// </summary>
    public struct JsonParameterClrInfo
    {
        /// <summary>
        /// The name of the parameter.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The type of the parameter.
        /// </summary>
        public Type ParameterType { get; set; }

        /// <summary>
        /// The zero-based position of the parameter in the formal parameter list.
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// Whether a default value was specified for the parameter.
        /// </summary>
        public bool HasDefaultValue { get; set; }

        /// <summary>
        /// The default value of the parameter.
        /// </summary>
        public object? DefaultValue { get; set; }
    }
}
