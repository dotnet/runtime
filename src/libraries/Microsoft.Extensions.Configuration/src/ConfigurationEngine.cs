// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// A value a read produced, and where it came from.
    /// </summary>
    /// <remarks>
    /// A read that produced nothing has no <see cref="ConfigurationValue"/> at all, so one that exists always names
    /// the provider that declared it. A stage that rewrites a value keeps the index of the text it started from,
    /// since that is where the key was declared, which is the question worth answering: a value built from several
    /// keys has no single provider of its own.
    /// </remarks>
    internal readonly struct ConfigurationValue
    {
        private ConfigurationValue(string? value, int providerIndex)
        {
            Value = value;
            ProviderIndex = providerIndex;
        }

        /// <summary>The text produced, which may be <see langword="null"/> for a key a provider holds as null.</summary>
        internal string? Value { get; }

        /// <summary>The position in the provider list of the provider that declared this value.</summary>
        internal int ProviderIndex { get; }

        /// <summary>A value the provider at <paramref name="providerIndex"/> holds.</summary>
        internal static ConfigurationValue FromProvider(string? value, int providerIndex) => new(value, providerIndex);

        /// <summary>The same declaration, holding <paramref name="value"/> in place of what was read there.</summary>
        internal ConfigurationValue WithValue(string? value) => new(value, ProviderIndex);
    }

    /// <summary>
    /// One stage of the pipeline a configuration root reads through. The stage at the end of the pipeline reads the
    /// providers; every stage before it interprets what those providers hold.
    /// </summary>
    /// <remarks>
    /// A stage holds no state about the configuration it serves: the providers are given to it on each call, so one
    /// pipeline serves a root for its whole life however often its sources change, and a stage can be shared.
    /// <para>
    /// Both methods read straight through to <see cref="Next"/> by default, so a stage overrides only what it changes.
    /// Reference resolution reinterprets a value and leaves enumeration alone; a stage that hides a key would have to do
    /// both, since the key has to stop being readable and stop being listed.
    /// </para>
    /// </remarks>
    internal abstract class ConfigurationEngine
    {
        /// <summary>
        /// The pipeline a configuration root reads through.
        /// </summary>
        /// <remarks>
        /// The one place the pipeline is composed, so its order is stated once. Reference resolution reads its targets
        /// through the stage after it rather than from the front of the pipeline, so a stage placed before it cannot
        /// change what a reference resolves to, and a stage placed after it can.
        /// <para>
        /// Turning references off leaves nothing here but the providers, which is what lets the trimmer drop reference
        /// resolution outright rather than merely leaving it unreachable.
        /// </para>
        /// </remarks>
        internal static ConfigurationEngine Default { get; } = ReferenceEngine.Disabled
            ? ProviderEngine.Instance
            : new ReferenceEngine(ProviderEngine.Instance);

        protected ConfigurationEngine(ConfigurationEngine next) => Next = next;

        /// <summary>
        /// The stage this one reads through. The stage at the end of the pipeline has none, and overrides everything
        /// that would read it.
        /// </summary>
        protected ConfigurationEngine Next { get; }

        /// <summary>
        /// Produces the value of <paramref name="key"/>, or <see langword="null"/> when there is none.
        /// </summary>
        /// <param name="providers">The providers to read.</param>
        /// <param name="key">The key to read.</param>
        internal virtual ConfigurationValue? Get(IList<IConfigurationProvider> providers, string key)
        {
            return Next.Get(providers, key);
        }

        /// <summary>
        /// Produces the keys of the immediate children of <paramref name="parentPath"/>, or of the root when it is
        /// <see langword="null"/>.
        /// </summary>
        internal virtual IEnumerable<string> GetChildKeys(IList<IConfigurationProvider> providers, string? parentPath)
        {
            return Next.GetChildKeys(providers, parentPath);
        }
    }
}
