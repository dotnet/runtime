// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration
{
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
        /// Turning transformations off leaves nothing here but the providers, which is what lets the trimmer drop
        /// reference resolution outright rather than merely leaving it unreachable.
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
        /// Reads <paramref name="key"/>, and reports whether anything declared it.
        /// </summary>
        /// <param name="providers">The providers to read.</param>
        /// <param name="key">The key to read.</param>
        /// <param name="value">
        /// The text produced, which may be <see langword="null"/> for a key a provider holds as null. A read that
        /// produced nothing leaves this <see langword="null"/> as well, so the two are told apart by the result.
        /// </param>
        /// <param name="providerIndex">
        /// The position in <paramref name="providers"/> of the provider that declared the key, or -1 when none did.
        /// A stage that rewrites a value reports the provider of the text it started from, since that is where the
        /// key was declared, which is the question worth answering: a value built from several keys has no single
        /// provider of its own.
        /// </param>
        internal virtual bool Get(IList<IConfigurationProvider> providers, string key, out string? value, out int providerIndex)
        {
            return Next.Get(providers, key, out value, out providerIndex);
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
