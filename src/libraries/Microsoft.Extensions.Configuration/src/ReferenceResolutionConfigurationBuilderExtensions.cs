// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Provides extension methods for configuring how individual <see cref="IConfigurationSource"/>
    /// instances participate in <c>ref(...) / fmt(...)</c> reference resolution performed by the
    /// <see cref="IConfigurationRoot"/> built from the containing
    /// <see cref="IConfigurationBuilder"/>.
    /// </summary>
    /// <remarks>
    /// Sources default to <see cref="ReferenceMode.Read"/>. The reference-resolution engine is
    /// attached to the built root only when at least one source is marked
    /// <see cref="ReferenceMode.Scan"/>; otherwise the built root behaves as a plain
    /// <see cref="IConfigurationRoot"/> with no reference interpretation.
    /// </remarks>
    public static class ReferenceResolutionConfigurationBuilderExtensions
    {
        // Signal placed in IConfigurationBuilder.Properties that carries the per-source mode
        // overrides. The value is a Dictionary<IConfigurationSource, ReferenceMode>
        // (reference-equality keys); at Build time it is correlated with the produced providers
        // so the engine can apply the correct mode per provider without wrapping them. Sources
        // without an entry default to ReferenceMode.Read.
        internal const string SourceModesPropertyName = "Microsoft.Extensions.Configuration.ReferenceResolution.SourceModes";

        /// <summary>
        /// Sets the <see cref="ReferenceMode"/> for the most recently added source on the
        /// builder.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> whose last source should be configured.</param>
        /// <param name="mode">The mode to apply to the source.</param>
        /// <returns>The same <see cref="IConfigurationBuilder"/>.</returns>
        /// <exception cref="InvalidOperationException">The builder has no sources to configure.</exception>
        public static IConfigurationBuilder SetReferenceMode(this IConfigurationBuilder configurationBuilder, ReferenceMode mode)
        {
            ArgumentNullException.ThrowIfNull(configurationBuilder);

            IList<IConfigurationSource> sources = configurationBuilder.Sources;
            if (sources.Count == 0)
            {
                throw new InvalidOperationException(SR.ReferenceResolution_NoSourceToConfigure);
            }

            SetSourceMode(configurationBuilder, sources[sources.Count - 1], mode);
            return configurationBuilder;
        }

        /// <summary>
        /// Sets the <see cref="ReferenceMode"/> for the specified source. Use this to configure
        /// sources added by a host or other caller after they were registered.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> containing the source.</param>
        /// <param name="source">The source to configure. Must already be present in <see cref="IConfigurationBuilder.Sources"/>.</param>
        /// <param name="mode">The mode to apply to the source.</param>
        /// <returns>The same <see cref="IConfigurationBuilder"/>.</returns>
        /// <exception cref="ArgumentException">The specified source is not present in the builder.</exception>
        public static IConfigurationBuilder SetReferenceMode(this IConfigurationBuilder configurationBuilder, IConfigurationSource source, ReferenceMode mode)
        {
            ArgumentNullException.ThrowIfNull(configurationBuilder);
            ArgumentNullException.ThrowIfNull(source);

            if (!configurationBuilder.Sources.Contains(source))
            {
                throw new ArgumentException(SR.ReferenceResolution_SourceNotFound, nameof(source));
            }

            SetSourceMode(configurationBuilder, source, mode);
            return configurationBuilder;
        }

        /// <summary>
        /// Sets the <see cref="ReferenceMode"/> for a batch of sources. Each source must already
        /// be present in <see cref="IConfigurationBuilder.Sources"/>; validation runs before any
        /// change is applied, so the operation is atomic.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> containing the sources.</param>
        /// <param name="sources">The sources to configure. A <see langword="null"/> enumerable is treated as empty.</param>
        /// <param name="mode">The mode to apply to every source in <paramref name="sources"/>.</param>
        /// <returns>The same <see cref="IConfigurationBuilder"/>.</returns>
        /// <exception cref="ArgumentException">A source in <paramref name="sources"/> is <see langword="null"/> or not present in the builder.</exception>
        public static IConfigurationBuilder SetReferenceMode(this IConfigurationBuilder configurationBuilder, IEnumerable<IConfigurationSource> sources, ReferenceMode mode)
        {
            ArgumentNullException.ThrowIfNull(configurationBuilder);

            if (sources is null)
            {
                return configurationBuilder;
            }

            // Materialize once and validate up-front so partial application is impossible.
            var materialized = new List<IConfigurationSource>();
            foreach (IConfigurationSource source in sources)
            {
                if (source is null)
                {
                    throw new ArgumentException(SR.ReferenceResolution_SourceNotFound, nameof(sources));
                }
                if (!configurationBuilder.Sources.Contains(source))
                {
                    throw new ArgumentException(SR.ReferenceResolution_SourceNotFound, nameof(sources));
                }
                materialized.Add(source);
            }

            foreach (IConfigurationSource source in materialized)
            {
                SetSourceMode(configurationBuilder, source, mode);
            }

            return configurationBuilder;
        }

        internal static Dictionary<IConfigurationSource, ReferenceMode>? TryGetSourceModes(IDictionary<string, object> properties)
        {
            if (properties is not null && properties.TryGetValue(SourceModesPropertyName, out object? raw))
            {
                return raw as Dictionary<IConfigurationSource, ReferenceMode>;
            }

            return null;
        }

        // Projects the per-source mode overrides onto the produced providers by positional
        // correspondence. Sources and providers share order: ConfigurationBuilder.Build iterates
        // sources to produce providers, and ConfigurationManager tracks source additions one-to-one
        // with provider additions. Providers whose source has no override are omitted — callers
        // treat a missing key as ReferenceMode.Read. Mismatched counts return null.
        internal static Dictionary<IConfigurationProvider, ReferenceMode>? ResolveProviderModes(
            IDictionary<string, object> properties,
            IList<IConfigurationSource> sources,
            IReadOnlyList<IConfigurationProvider> providers)
        {
            Dictionary<IConfigurationSource, ReferenceMode>? overrides = TryGetSourceModes(properties);
            if (overrides is null || overrides.Count == 0 || sources.Count != providers.Count)
            {
                return null;
            }

            Dictionary<IConfigurationProvider, ReferenceMode>? result = null;
            for (int i = 0; i < sources.Count; i++)
            {
                if (overrides.TryGetValue(sources[i], out ReferenceMode mode))
                {
                    result ??= new Dictionary<IConfigurationProvider, ReferenceMode>();
                    result[providers[i]] = mode;
                }
            }

            return result;
        }

        // Checks whether at least one source in the builder is marked ReferenceMode.Scan.
        // Used by the builder/manager to decide whether to attach the engine at Build time;
        // when no source is Scan the feature is dormant and the root falls through to the
        // normal provider walk.
        internal static bool HasAnyScanSource(IDictionary<string, object> properties)
        {
            Dictionary<IConfigurationSource, ReferenceMode>? overrides = TryGetSourceModes(properties);
            if (overrides is null)
            {
                return false;
            }

            foreach (ReferenceMode mode in overrides.Values)
            {
                if (mode == ReferenceMode.Scan)
                {
                    return true;
                }
            }

            return false;
        }

        private static void SetSourceMode(IConfigurationBuilder configurationBuilder, IConfigurationSource source, ReferenceMode mode)
        {
            // Dictionary is written back to Properties whether newly created or not so that
            // builders whose Properties dictionary reacts to writes (e.g., ConfigurationManager)
            // trigger a rebuild of the engine with the new mode map.
            Dictionary<IConfigurationSource, ReferenceMode> map = TryGetSourceModes(configurationBuilder.Properties)
                ?? new Dictionary<IConfigurationSource, ReferenceMode>();
            map[source] = mode;
            configurationBuilder.Properties[SourceModesPropertyName] = map;
        }
    }
}
