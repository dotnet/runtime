// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Provides extension methods for enabling configuration references on an <see cref="IConfigurationBuilder"/>.
    /// </summary>
    public static class ReferenceConfigurationBuilderExtensions
    {
        /// <summary>
        /// Enables references for the sources registered so far. Keys in those sources may hold a reference (by
        /// default the <c>ref(target)</c> marker) that the configuration root resolves to another key's value or
        /// subtree, within the envelope declared by <paramref name="configure"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add references to.</param>
        /// <param name="configure">A callback that declares the allowed references and, optionally, a custom parser.</param>
        /// <returns>The same <see cref="IConfigurationBuilder"/> so calls can be chained.</returns>
        public static IConfigurationBuilder AllowReferences(this IConfigurationBuilder builder, Action<ConfigurationReferenceBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(configure);

            var references = new ConfigurationReferenceBuilder();
            configure(references);

            builder.Add(new ReferenceConfigurationSource(references.ConcreteRules, references.TemplateRules, references.Parser));
            return builder;
        }
    }
}
