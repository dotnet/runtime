// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Implementation of <see cref="IOptionsSnapshot{TOptions}"/>.
    /// </summary>
    /// <typeparam name="TOptions">Options type.</typeparam>
    internal sealed class OptionsSnapshot<[DynamicallyAccessedMembers(Options.DynamicallyAccessedMembers)] TOptions> :
        IOptionsSnapshot<TOptions>
        where TOptions : class
    {
        private readonly IOptionsMonitor<TOptions> _optionsMonitor;

        private volatile ConcurrentDictionary<string, TOptions> _cache;
        private volatile TOptions _unnamedOptionsValue;

        /// <summary>
        /// Initializes a new instance with the specified options configurations.
        /// </summary>
        /// <param name="optionsMonitor">The options monitor to use to provide options.</param>
        public OptionsSnapshot(IOptionsMonitor<TOptions> optionsMonitor)
        {
            _optionsMonitor = optionsMonitor;
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
            if (name == null || name == Options.DefaultName)
            {
                if (_unnamedOptionsValue is TOptions value)
                {
                    return value;
                }

                return _unnamedOptionsValue = _optionsMonitor.Get(Options.DefaultName);
            }

            var cache = _cache ?? Interlocked.CompareExchange(ref _cache, new(concurrencyLevel: 1, capacity: 5, StringComparer.Ordinal), null) ?? _cache;

#if NETSTANDARD2_1
            TOptions options = cache.GetOrAdd(name, static (name, optionsMonitor) => optionsMonitor.Get(name), _optionsMonitor);
#else
            if (!cache.TryGetValue(name, out TOptions options))
            {
                options = cache.GetOrAdd(name, _optionsMonitor.Get(name));
            }
#endif
            return options;
        }
    }
}
