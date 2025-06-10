// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Provides configuration key/values for an application.
    /// </summary>
    public interface IConfigurationProvider
    {
        /// <summary>
        /// Tries to get a configuration value for the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">When this method returns, contains the value for the specified key.</param>
        /// <returns><see langword="true" /> if a value for the specified key was found, otherwise <see langword="false" />.</returns>
        bool TryGet(string key, out string? value);

        /// <summary>
        /// Sets a configuration value for the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        void Set(string key, string? value);

        /// <summary>
        /// Attempts to get an <see cref="IChangeToken"/> for change tracking.
        /// </summary>
        /// <returns>An <see cref="IChangeToken"/> token if this provider supports change tracking, <see langword="null"/> otherwise.</returns>
        IChangeToken GetReloadToken();

        /// <summary>
        /// Loads configuration values from the source represented by this <see cref="IConfigurationProvider"/>.
        /// </summary>
        void Load();

        /// <summary>
        /// Returns the immediate descendant configuration keys for a given parent path based on the data of this
        /// <see cref="IConfigurationProvider"/> and the set of keys returned by all the preceding
        /// <see cref="IConfigurationProvider"/> providers.
        /// </summary>
        /// <param name="earlierKeys">The child keys returned by the preceding providers for the same parent path.</param>
        /// <param name="parentPath">The parent path.</param>
        /// <returns>The child keys.</returns>
        IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath);
    }
}
