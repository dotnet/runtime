// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for adding configuration-related options services to the DI container via <see cref="OptionsBuilder{TOptions}"/>.
    /// </summary>
    public static class OptionsBuilderExtensions
    {
        /// <summary>
        /// Enforces options validation check on start rather than at run time.
        /// </summary>
        /// <typeparam name="TOptions">The type of options.</typeparam>
        /// <param name="optionsBuilder">The <see cref="OptionsBuilder{TOptions}"/> to configure options instance.</param>
        /// <returns>The <see cref="OptionsBuilder{TOptions}"/> so that additional calls can be chained.</returns>
        public static OptionsBuilder<TOptions> ValidateOnStart<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TOptions>(this OptionsBuilder<TOptions> optionsBuilder)
            where TOptions : class
        {
            ArgumentNullException.ThrowIfNull(optionsBuilder);

            optionsBuilder.Services.TryAddTransient<IStartupValidator, StartupValidator>();
            optionsBuilder.Services.TryAddTransient<IAsyncStartupValidator, StartupValidator>();
            optionsBuilder.Services.AddOptions<StartupValidatorOptions>()
                .Configure<IOptionsMonitor<TOptions>>((vo, options) =>
                {
                    // This adds an action that resolves the options value to force evaluation
                    // We don't care about the result as duplicates are not important
                    vo._validators[(typeof(TOptions), optionsBuilder.Name)] = () => options.Get(optionsBuilder.Name);
                });

            // Register async validator entries if any IAsyncValidateOptions<TOptions> are registered.
            optionsBuilder.Services.AddOptions<StartupValidatorOptions>()
                .Configure<OptionsAsyncValidationCoordinator<TOptions>>((vo, asyncValidationCoordinator) =>
                {
                    if (asyncValidationCoordinator.HasApplicableAsyncValidators(optionsBuilder.Name))
                    {
                        var validationState = new StartupValidationState<TOptions>(optionsBuilder.Name, asyncValidationCoordinator);
                        vo._validators[(typeof(TOptions), optionsBuilder.Name)] = validationState.Validate;
                        vo._asyncValidators[(typeof(TOptions), optionsBuilder.Name)] = validationState.ValidateAsync;
                    }
                });

            return optionsBuilder;
        }

        private sealed class StartupValidationState<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TOptions>
            where TOptions : class
        {
            private readonly string _name;
            private readonly OptionsAsyncValidationCoordinator<TOptions> _asyncValidationCoordinator;
            private readonly object _syncObj = new object();
            private TOptions? _syncValidatedOptions;
            private bool _hasSyncValidatedOptions;

            public StartupValidationState(string name, OptionsAsyncValidationCoordinator<TOptions> asyncValidationCoordinator)
            {
                _name = name;
                _asyncValidationCoordinator = asyncValidationCoordinator;
            }

            public void Validate()
            {
                TOptions options = _asyncValidationCoordinator.ValidateSync(_name);
                lock (_syncObj)
                {
                    _syncValidatedOptions = options;
                    _hasSyncValidatedOptions = true;
                }
            }

            public async Task ValidateAsync(CancellationToken cancellationToken)
            {
                TOptions options = GetOrCreateSyncValidatedOptions();
                await _asyncValidationCoordinator.ValidateAndPublishAsync(_name, options, cancellationToken).ConfigureAwait(false);
            }

            private TOptions GetOrCreateSyncValidatedOptions()
            {
                lock (_syncObj)
                {
                    if (_hasSyncValidatedOptions)
                    {
                        return _syncValidatedOptions!;
                    }
                }

                TOptions options = _asyncValidationCoordinator.ValidateSync(_name);
                lock (_syncObj)
                {
                    if (!_hasSyncValidatedOptions)
                    {
                        _syncValidatedOptions = options;
                        _hasSyncValidatedOptions = true;
                    }

                    return _syncValidatedOptions!;
                }
            }
        }
    }
}
