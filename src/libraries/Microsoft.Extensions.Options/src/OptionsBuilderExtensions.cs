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
        /// <para>
        /// With the built-in <see cref="IOptionsFactory{TOptions}"/>, asynchronous validation runs during startup and
        /// seeds the built-in <see cref="IOptions{TOptions}"/> and <see cref="IOptionsMonitor{TOptions}"/> instances
        /// when their caches do not already contain a value. Options requiring asynchronous validation cannot be
        /// accessed synchronously before startup completes. A value successfully created synchronously before or
        /// during startup remains the cache winner, and a faulted monitor cache entry causes startup validation to
        /// fail rather than being replaced.
        /// </para>
        /// <para>
        /// A derived or replacement <see cref="IOptionsFactory{TOptions}"/> uses synchronous startup validation and
        /// does not invoke <see cref="IAsyncValidateOptions{TOptions}.ValidateAsync"/>. Default-name asynchronous
        /// validation requires the built-in <see cref="IOptions{TOptions}"/> implementation; startup throws
        /// <see cref="InvalidOperationException"/> when a custom implementation is registered. The built-in
        /// <see cref="IOptionsSnapshot{TOptions}"/> validates synchronously in per-scope caches that startup validation
        /// does not populate. The built-in options monitor also reloads synchronously and does not invoke asynchronous
        /// validation. The built-in asynchronous validators therefore cause reload to fail and prevent change
        /// listeners from being notified; no asynchronous last-known-good guarantee is provided.
        /// </para>
        /// <para>
        /// Publication uses <see cref="IOptionsMonitorCache{TOptions}.GetOrAdd"/> so an existing monitor value is not
        /// replaced. For compatibility, this method exposes the built-in startup validator through
        /// <see cref="IStartupValidator"/> and <see cref="IAsyncStartupValidator"/> as the same singleton. A custom
        /// validator registered only as <see cref="IStartupValidator"/> takes precedence and suppresses all
        /// asynchronous startup validators. New custom startup validators should register only
        /// <see cref="IAsyncStartupValidator"/>.
        /// </para>
        /// </remarks>
        /// <typeparam name="TOptions">The type of options.</typeparam>
        /// <param name="optionsBuilder">The <see cref="OptionsBuilder{TOptions}"/> to configure options instance.</param>
        /// <returns>The <see cref="OptionsBuilder{TOptions}"/> so that additional calls can be chained.</returns>
        public static OptionsBuilder<TOptions> ValidateOnStart<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TOptions>(this OptionsBuilder<TOptions> optionsBuilder)
            where TOptions : class
        {
            ArgumentNullException.ThrowIfNull(optionsBuilder);

            string name = optionsBuilder.Name;

            // Both contracts alias one instance so the host can distinguish the built-in compatibility registration
            // from independent custom validators without inferring registration identity from implementation type.
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
                    // and seed empty caches so the first synchronous access after startup does not re-run the throwing
                    // synchronous Validate. Values already materialized through a synchronous path remain the winners.
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
                            // A successfully created pre-start IOptions value is used when the monitor cache is empty.
                            // Otherwise, the existing monitor value remains the winner and seeds IOptions if necessary.
                            TOptions winner = sharedCache.GetOrAdd(
                                name,
                                () => optionsManager?.GetOrSetValue(validated) ?? validated);

                            if (optionsManager is not null)
                            {
                                optionsManager.GetOrSetValue(winner);
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
