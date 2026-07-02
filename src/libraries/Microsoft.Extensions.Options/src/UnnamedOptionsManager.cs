// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Options
{
    internal sealed class UnnamedOptionsManager<[DynamicallyAccessedMembers(Options.DynamicallyAccessedMembers)] TOptions> :
        IOptions<TOptions>
        where TOptions : class
    {
        private readonly IOptionsFactory<TOptions> _factory;
        private object? _syncObj;
        private volatile TOptions? _value;

        public UnnamedOptionsManager(IOptionsFactory<TOptions> factory) => _factory = factory;

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
                    return _value ??= _factory.Create(Options.DefaultName);
                }
            }
        }
    }

    internal sealed class UnnamedOptionsManagerWithAsyncValidation<[DynamicallyAccessedMembers(Options.DynamicallyAccessedMembers)] TOptions> :
        IOptions<TOptions>
        where TOptions : class
    {
        private readonly IOptionsFactory<TOptions> _factory;
        private readonly OptionsAsyncValidationCoordinator<TOptions>? _asyncValidationCoordinator;
        private object? _syncObj;
        private volatile TOptions? _value;

        public UnnamedOptionsManagerWithAsyncValidation(IOptionsFactory<TOptions> factory, IServiceProvider serviceProvider)
        {
            _factory = factory;
            _asyncValidationCoordinator = OptionsAsyncValidation.GetCoordinator<TOptions>(serviceProvider);
        }

        public TOptions Value
        {
            get
            {
                OptionsAsyncValidationCoordinator<TOptions>? asyncValidationCoordinator = _asyncValidationCoordinator;
                if (asyncValidationCoordinator is not null &&
                    asyncValidationCoordinator.HasApplicableAsyncOnlyValidators(Options.DefaultName))
                {
                    return asyncValidationCoordinator.GetValidatedValueOrThrow(Options.DefaultName);
                }

                if (_value is TOptions value)
                {
                    return value;
                }

                lock (_syncObj ?? Interlocked.CompareExchange(ref _syncObj, new object(), null) ?? _syncObj)
                {
                    return _value ??= _factory.Create(Options.DefaultName);
                }
            }
        }

    }
}
