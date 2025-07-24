// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Defines an alias for <see cref="ILoggerProvider"/> implementation to be used in filtering rules.
    /// </summary>
    /// <remarks>
    /// <para>By default, filtering rules are defined using the logging provider type's <see cref="System.Reflection.Assembly.FullName"/> as configuration section name.</para>
    /// <para>The <see cref="ProviderAliasAttribute"/> provides for specifying a second, additional, more concise and user-friendly configuration section name for specifying filter rules.</para>
    /// <para>The logging provider type's <see cref="System.Reflection.Assembly.FullName"/> can still be used as configuration section name when the <see cref="ProviderAliasAttribute"/> is specified for the provider, and its configuration section filtering rules have priority over the filtering rules specified for the alias.
    /// </para>
    /// </remarks>
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
        /// Gets the alias of the provider.
        /// </summary>
        public string Alias { get; }

    }
}
