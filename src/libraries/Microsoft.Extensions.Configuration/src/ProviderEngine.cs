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
        internal override bool Get(IList<IConfigurationProvider> providers, string key, out string? value, out int providerIndex)
        {
            for (int i = providers.Count - 1; i >= 0; i--)
            {
                try
                {
                    if (providers[i].TryGet(key, out value))
                    {
                        providerIndex = i;
                        return true;
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

            value = null;
            providerIndex = -1;
            return false;
        }

        /// <summary>
        /// Collects the child keys every provider declares under a path.
        /// </summary>
        /// <remarks>
        /// Each provider is handed the keys gathered so far, which is how a provider that stores keys in a shape of its
        /// own gets to reconcile them with the rest, so this accumulates rather than concatenates.
        /// <para>
        /// The providers are all consulted before this returns, since the fold is eager, but a provider is free to hand
        /// back a sequence of its own that is not. So a caller that has borrowed its provider list has to gather the
        /// result before letting go of it.
        /// </para>
        /// </remarks>
        internal override IEnumerable<string> GetChildKeys(IList<IConfigurationProvider> providers, string? parentPath) =>
            providers
                .Aggregate(Enumerable.Empty<string>(), (seed, source) => source.GetChildKeys(seed, parentPath))
                .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
