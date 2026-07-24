// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Microsoft.Extensions.Options
{
    internal sealed class UnnamedOptionsManager<[DynamicallyAccessedMembers(Options.DynamicallyAccessedMembers)] TOptions> :
        IOptions<TOptions>
        where TOptions : class
    {
        private readonly IOptionsFactory<TOptions> _factory;
        private readonly IOptionsMonitorCache<TOptions>? _validatedCache;
        private object? _syncObj;
        private volatile TOptions? _value;

        public UnnamedOptionsManager(IOptionsFactory<TOptions> factory) => _factory = factory;

        public UnnamedOptionsManager(IOptionsFactory<TOptions> factory, IOptionsMonitorCache<TOptions> cache)
        {
            _factory = factory;

            if (factory is OptionsFactory<TOptions> optionsFactory && optionsFactory.HasAsyncValidators)
            {
                _validatedCache = cache;
            }
        }

        public TOptions Value
        {
            get
            {
                if (_value is TOptions value)
                {
                    return value;
                }

                lock (_syncObj ?? Interlocked.CompareExchange(ref _syncObj, new object(), null) ?? _syncObj)
                {
                    return _value ??= CreateValue();
                }
            }
        }

        private TOptions CreateValue()
        {
            // For an async-validated type, prefer the value validated during startup (seeded into the shared cache) so a
            // synchronous access returns the last validated value instead of re-running the throwing synchronous Validate.
            if (_validatedCache is OptionsCache<TOptions> optionsCache && optionsCache.TryGetValue(Options.DefaultName, out TOptions? validated))
            {
                return validated;
            }

            return _factory.Create(Options.DefaultName);
        }
    }
}
