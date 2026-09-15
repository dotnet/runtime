// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Provides extension methods for opting an <see cref="IConfigurationBuilder"/> into value
    /// expansion (<c>ref(...)</c> reference resolution) performed by the built
    /// <see cref="IConfigurationRoot"/>.
    /// </summary>
    /// <remarks>
    /// When expansions are allowed, every value read through the root is inspected for a
    /// top-level <c>ref(key1, key2, ...)</c> expression. The first listed key that resolves to
    /// a non-null value supplies the result; resolution recurses on the resolved value.
    /// Unresolved or malformed expressions are returned verbatim — the engine never throws.
    /// </remarks>
    public static class ConfigurationBuilderExtensions
    {
        // Stored in IConfigurationBuilder.Properties as a boxed bool. Reading happens at Build
        // time (or on each SwapEngine call for ConfigurationManager) so the flag may be toggled
        // multiple times during builder configuration without affecting the providers themselves.
        internal const string AllowExpansionsPropertyName = "Microsoft.Extensions.Configuration.AllowExpansions";

        /// <summary>
        /// Allows or disallows value expansions (<c>ref(...)</c> reference resolution) for the
        /// configuration root built from this <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to configure.</param>
        /// <param name="allow">
        /// <see langword="true"/> to scan every provider's values for <c>ref(...)</c>
        /// expressions; <see langword="false"/> to disable scanning. Defaults to <see langword="true"/>.
        /// </param>
        /// <returns>The same <paramref name="builder"/> instance so that calls can be chained.</returns>
        public static IConfigurationBuilder AllowExpansions(this IConfigurationBuilder builder, bool allow = true)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Properties[AllowExpansionsPropertyName] = allow;
            return builder;
        }

        // Reads the flag from a builder's Properties bag. Treats a missing entry, a non-bool
        // entry, or an explicit `false` value as "disallowed". Used by ConfigurationBuilder.Build
        // and ConfigurationManager.SwapEngine to decide whether to attach the engine.
        internal static bool IsAllowed(IDictionary<string, object> properties)
        {
            return properties is not null
                && properties.TryGetValue(AllowExpansionsPropertyName, out object? raw)
                && raw is bool flag
                && flag;
        }
    }
}
