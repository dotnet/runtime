// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// The stage at the end of the pipeline: the one that actually reads the providers. Everything a configuration root
    /// reports comes from here, and every other stage is an interpretation of it.
    /// </summary>
    internal sealed class ProviderEngine : ConfigurationEngine
    {
        /// <summary>
        /// The single instance. Reading providers depends on nothing but the providers handed to each call, so there is
        /// never a reason for a second one.
        /// </summary>
        internal static ProviderEngine Instance { get; } = new ProviderEngine();

        private ProviderEngine()
            // Nothing follows this stage, and both methods that would read Next are overridden below.
            : base(null!)
        {
        }

        /// <summary>
        /// Reads a key from the providers, highest precedence first, and takes the first answer.
        /// </summary>
        internal override ConfigurationValue? Get(IList<IConfigurationProvider> providers, string key)
        {
            for (int i = providers.Count - 1; i >= 0; i--)
            {
                try
                {
                    if (providers[i].TryGet(key, out string? text))
                    {
                        return ConfigurationValue.FromProvider(text, i);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // ConfigurationManager disposes providers when its sources are modified, so a read running
                    // concurrently with a change can reach one that has already gone. It is reading a list that has
                    // been replaced, and the value it wants is in the new one, so passing over the dead provider is
                    // the whole of the fix.
                }
            }

            return null;
        }

        /// <summary>
        /// Collects the child keys every provider declares under a path.
        /// </summary>
        /// <remarks>
        /// Each provider is handed the keys gathered so far, which is how a provider that stores keys in a shape of its
        /// own gets to reconcile them with the rest, so this accumulates rather than concatenates.
        /// <para>
        /// The result is a fresh <see cref="List{T}"/> rather than a lazy sequence, so a stage may cast it and add or
        /// remove keys in place. Reading eagerly also keeps every provider access inside the read: a
        /// <see cref="ConfigurationManager"/> pins its provider list only for the duration of the call, and a lazy
        /// result would be walked after it had let go.
        /// </para>
        /// </remarks>
        internal override IEnumerable<string> GetChildKeys(IList<IConfigurationProvider> providers, string? parentPath) =>
            providers
                .Aggregate(Enumerable.Empty<string>(), (seed, source) => source.GetChildKeys(seed, parentPath))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
    }
}
