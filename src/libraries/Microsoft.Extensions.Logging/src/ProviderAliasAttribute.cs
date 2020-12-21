// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Defines alias for <see cref="ILoggerProvider"/> implementation to be used in filtering rules.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ProviderAliasAttribute : Attribute
    {
        /// <summary>
        /// Creates a new <see cref="ProviderAliasAttribute"/> instance.
        /// </summary>
        /// <param name="alias">The alias to set.</param>
        public ProviderAliasAttribute(string alias)
        {
            Alias = alias;
        }

        /// <summary>
        /// The alias of the provider.
        /// </summary>
        public string Alias { get; }

    }
}
