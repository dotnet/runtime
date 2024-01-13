// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Specifies the key name for a configuration property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ConfigurationKeyNameAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ConfigurationKeyNameAttribute"/>.
        /// </summary>
        /// <param name="name">The key name.</param>
        public ConfigurationKeyNameAttribute([System.Runtime.CompilerServices.CallerMemberName] string? name = null) => Name = name ?? "";

        /// <summary>
        /// The key name for a configuration property.
        /// </summary>
        public string Name { get; }
    }
}
