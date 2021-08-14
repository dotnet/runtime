// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Represents a section of application configuration values.
    /// </summary>
    public interface IConfigurationSection : IConfiguration
    {
        /// <summary>
        /// Gets the key this section occupies in its parent.
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Gets the full path to this section within the <see cref="IConfiguration"/>.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Gets or sets the section value.
        /// </summary>
        string? Value { get; set; }
    }
}
