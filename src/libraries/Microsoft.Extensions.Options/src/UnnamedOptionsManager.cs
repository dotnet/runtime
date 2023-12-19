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
        private volatile object? _syncObj;
        private volatile object? _factoryOrValue;

        public UnnamedOptionsManager(IOptionsFactory<TOptions> factory) => _factoryOrValue = factory;

        public TOptions Value
        {
            get
            {
                if (_factoryOrValue is TOptions value)
                {
                    return value;
                }

                lock (_syncObj ?? Interlocked.CompareExchange(ref _syncObj, new object(), null) ?? _syncObj)
                {
                    return _factoryOrValue ??= ((IOptionsFactory<TOptions>)_factoryOrValue).Create(Options.DefaultName);
                }
            }
        }
    }
}
