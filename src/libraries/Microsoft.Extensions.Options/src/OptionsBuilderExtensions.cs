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

            string name = optionsBuilder.Name;

            // Register the built-in validator as a single IStartupValidator (for back-compatibility)
            // and as an enumerable IAsyncStartupValidator so the host can run it alongside any custom async validators.
            optionsBuilder.Services.TryAddTransient<IStartupValidator, StartupValidator>();
            optionsBuilder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IAsyncStartupValidator, StartupValidator>());
            optionsBuilder.Services.AddOptions<StartupValidatorOptions>()
                .Configure<IOptionsMonitor<TOptions>, IOptionsFactory<TOptions>, IOptionsMonitorCache<TOptions>>((vo, monitor, factory, cache) =>
                {
                    // Sync path (custom sync-only IStartupValidator): force evaluation through the monitor,
                    // which runs every validator, including an async validator's fail-fast synchronous Validate.
                    vo._validators[(typeof(TOptions), name)] = () => monitor.Get(name);

                    // Async path: run the complete validation (both sync and async validators) for this (type, name)
                    // and seed the monitor cache with the validated instance so the first synchronous access after
                    // startup returns it instead of re-running the throwing synchronous Validate.
                    vo._asyncValidators[(typeof(TOptions), name)] = async (CancellationToken ct) =>
                    {
                        if (factory is OptionsFactory<TOptions> asyncFactory)
                        {
                            TOptions validated = await asyncFactory.CreateAsync(name, ct).ConfigureAwait(false);
                            if (cache is OptionsCache<TOptions> optionsCache)
                            {
                                optionsCache.AddOrReplace(name, validated);
                            }
                            else
                            {
                                cache.TryRemove(name);
                                cache.TryAdd(name, validated);
                            }
                        }
                        else
                        {
                            // Custom IOptionsFactory<TOptions>: no async validation path is available,
                            // so fall back to synchronous evaluation.
                            monitor.Get(name);
                        }
                    };
                });

            return optionsBuilder;
        }
    }
}
