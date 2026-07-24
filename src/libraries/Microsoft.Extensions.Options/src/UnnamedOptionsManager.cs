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
            // For an async-validated type, read through the shared cache: when startup validation has seeded
            // the validated instance it is returned as-is, otherwise it is created
            // For a genuinely asynchronous validator the synchronous Create fails fast with an exception.
            if (_validatedCache is not null)
            {
                return _validatedCache.GetOrAdd(Options.DefaultName, () => _factory.Create(Options.DefaultName));
            }

            return _factory.Create(Options.DefaultName);
        }
    }
}
