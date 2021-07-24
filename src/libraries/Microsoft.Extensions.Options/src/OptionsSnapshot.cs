// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Implementation of <see cref="IOptions{TOptions}"/> and <see cref="IOptionsSnapshot{TOptions}"/>.
    /// </summary>
    /// <typeparam name="TOptions">Options type.</typeparam>
    internal class OptionsSnapshot<[DynamicallyAccessedMembers(Options.DynamicallyAccessedMembers)] TOptions> :
        IOptionsSnapshot<TOptions>
        where TOptions : class
    {
        private readonly IOptionsMonitor<TOptions> _factory;
        private readonly ConcurrentDictionary<string, TOptions> _cache = new(concurrencyLevel: 1, capacity: 5, StringComparer.Ordinal);

        /// <summary>
        /// Initializes a new instance with the specified options configurations.
        /// </summary>
        /// <param name="factory">The factory to use to create options.</param>
        public OptionsSnapshot(IOptionsMonitor<TOptions> factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// The default configured <typeparamref name="TOptions"/> instance, equivalent to Get(Options.DefaultName).
        /// </summary>
        public TOptions Value => Get(Options.DefaultName);

        /// <summary>
        /// Returns a configured <typeparamref name="TOptions"/> instance with the given <paramref name="name"/>.
        /// </summary>
        public TOptions Get(string name)
        {
            name ??= Options.DefaultName;

#if NETSTANDARD2_1
            TOptions options = _cache.GetOrAdd(name, static (name, factory) => factory.Get(name), _factory);
#else
            if (!_cache.TryGetValue(name, out TOptions options))
            {
                options = _cache.GetOrAdd(name, _factory.Get(name));
            }
#endif
            return options;
        }
    }
}
