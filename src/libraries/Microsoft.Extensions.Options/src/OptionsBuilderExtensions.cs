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
        /// <remarks>
        /// When the built-in <see cref="IOptionsFactory{TOptions}"/> implementation is used, asynchronous validation
        /// runs during startup and seeds the built-in <see cref="IOptions{TOptions}"/> and
        /// <see cref="IOptionsMonitor{TOptions}"/> instances for subsequent synchronous access. If an options value
        /// was successfully created synchronously before startup, that instance retains the singleton slot and is
        /// published to the monitor cache while asynchronous validation runs against a separate startup candidate.
        /// A derived or replacement <see cref="IOptionsFactory{TOptions}"/> uses synchronous startup validation and
        /// does not invoke <see cref="IAsyncValidateOptions{TOptions}.ValidateAsync"/>.
        /// Options that require asynchronous validation cannot be accessed synchronously before startup validation
        /// completes. Default-name asynchronous validation requires the built-in <see cref="IOptions{TOptions}"/>
        /// implementation so the validated value can be installed safely; startup fails when a custom implementation
        /// is registered. The built-in <see cref="IOptionsSnapshot{TOptions}"/> implementation validates instances
        /// synchronously in per-scope caches that startup validation does not populate. The built-in options monitor
        /// also reloads synchronously and provides no asynchronous last-known-good guarantee. Publication to the
        /// built-in monitor cache is atomic. The <see cref="IOptionsMonitorCache{TOptions}"/> contract has no atomic
        /// replacement operation, so applications using a custom or derived cache must avoid concurrent cache access
        /// during startup validation if atomic publication is required. Startup validation throws
        /// <see cref="InvalidOperationException"/> if publication to a custom or derived cache does not succeed.
        /// A custom validator that implements both <see cref="IStartupValidator"/> and
        /// <see cref="IAsyncStartupValidator"/> should register one instance under both service contracts. A custom
        /// <see cref="IStartupValidator"/> that is not also registered by identity as
        /// <see cref="IAsyncStartupValidator"/> takes precedence and suppresses asynchronous startup validators.
        /// </remarks>
        /// <typeparam name="TOptions">The type of options.</typeparam>
        /// <param name="optionsBuilder">The <see cref="OptionsBuilder{TOptions}"/> to configure options instance.</param>
        /// <returns>The <see cref="OptionsBuilder{TOptions}"/> so that additional calls can be chained.</returns>
        public static OptionsBuilder<TOptions> ValidateOnStart<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TOptions>(this OptionsBuilder<TOptions> optionsBuilder)
            where TOptions : class
        {
            ArgumentNullException.ThrowIfNull(optionsBuilder);

            string name = optionsBuilder.Name;

            // Both contracts alias one instance so the host can distinguish the built-in dual registration from
            // independent custom validators of the same runtime type.
            optionsBuilder.Services.TryAddSingleton<StartupValidator>();
#pragma warning disable SYSLIB0066 // IStartupValidator is obsolete but retained for compatibility.
            optionsBuilder.Services.TryAddSingleton<IStartupValidator>(
                static sp => sp.GetRequiredService<StartupValidator>());
#pragma warning restore SYSLIB0066
            optionsBuilder.Services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IAsyncStartupValidator, StartupValidator>(
                    static sp => sp.GetRequiredService<StartupValidator>()));
            optionsBuilder.Services.AddOptions<StartupValidatorOptions>()
                .Configure<IOptions<TOptions>, IOptionsMonitor<TOptions>, IOptionsFactory<TOptions>, IOptionsMonitorCache<TOptions>>((vo, options, monitor, factory, sharedCache) =>
                {
                    // Sync path (custom sync-only IStartupValidator): force evaluation through the monitor,
                    // which runs every validator, including an async validator's fail-fast synchronous Validate.
                    vo._validators[(typeof(TOptions), name)] = () => monitor.Get(name);

                    // Async path: run the complete validation (both sync and async validators) for this (type, name)
                    // and seed the monitor cache with the validated instance so the first synchronous access after
                    // startup returns it instead of re-running the throwing synchronous Validate.
                    vo._asyncValidators[(typeof(TOptions), name)] = async (CancellationToken ct) =>
                    {
                        if (factory is OptionsFactory<TOptions> asyncFactory &&
                            asyncFactory.GetType() == typeof(OptionsFactory<TOptions>) &&
                            asyncFactory.HasAsyncValidators(name))
                        {
                            UnnamedOptionsManager<TOptions>? optionsManager = null;

                            if (name == Microsoft.Extensions.Options.Options.DefaultName)
                            {
                                optionsManager =
                                    options as UnnamedOptionsManager<TOptions> ??
                                    throw new InvalidOperationException(
                                        SR.Format(
                                            SR.AsyncValidationUnsupportedIOptions,
                                            typeof(TOptions),
                                            options.GetType()));
                            }

                            TOptions validated = await asyncFactory.CreateAsync(name, ct).ConfigureAwait(false);
                            // A successfully created pre-start IOptions value owns the singleton slot, even though
                            // asynchronous startup validation ran against this separately created candidate.
                            TOptions winner = optionsManager?.GetOrSetValue(validated) ?? validated;

                            if (!OptionsCache<TOptions>.TryAddOrReplace(sharedCache, name, winner))
                            {
                                throw new InvalidOperationException(
                                    SR.Format(
                                        SR.AsyncValidationCachePublicationFailed,
                                        typeof(TOptions),
                                        name,
                                        sharedCache.GetType()));
                            }
                        }
                        else
                        {
                            // Sync-only validation and custom factories use the monitor so an existing cached
                            // instance is preserved and configuration does not run again.
                            monitor.Get(name);
                        }
                    };
                });

            return optionsBuilder;
        }
    }
}
