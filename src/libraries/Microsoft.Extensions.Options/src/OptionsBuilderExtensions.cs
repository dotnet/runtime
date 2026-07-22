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
                    // Sync path (custom sync-only IStartupValidator): force evaluation through the
                    // monitor, which runs every validator (including an async validator's fail-fast
                    // synchronous Validate) and populates the cache.
                    vo._validators[(typeof(TOptions), name)] = () => monitor.Get(name);

                    // Async path: run the complete validation (both sync and async validators) for
                    // this (type, name) and seed the monitor cache with the validated instance so the
                    // first Get after startup returns it instead of re-running Create.
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
                            // so fall back to synchronous evaluation (also populates the cache).
                            monitor.Get(name);
                        }
                    };
                });

            return optionsBuilder;
        }

        /// <summary>
        /// Enables eager asynchronous revalidation of the options whenever their configuration reloads, instead of the
        /// default lazy revalidation on next access.
        /// </summary>
        /// <typeparam name="TOptions">The type of options.</typeparam>
        /// <param name="optionsBuilder">The <see cref="OptionsBuilder{TOptions}"/> to configure.</param>
        /// <param name="behavior">How reads are served when revalidation of a reloaded configuration fails.</param>
        /// <param name="onError">An optional callback invoked with the options name and the exception when revalidation of a reloaded configuration fails.</param>
        /// <returns>The <see cref="OptionsBuilder{TOptions}"/> so that additional calls can be chained.</returns>
        /// <remarks>
        /// On reload the options are re-created and validated in the background (running every registered validator,
        /// awaiting asynchronous ones); the last successfully validated value keeps being served until the new value
        /// validates, at which point it is swapped in atomically. This does not run startup validation; combine it with
        /// <see cref="ValidateOnStart{TOptions}(OptionsBuilder{TOptions})"/> to also validate the initial value.
        /// </remarks>
        public static OptionsBuilder<TOptions> ValidateOnChange<TOptions>(
            this OptionsBuilder<TOptions> optionsBuilder,
            OptionsReloadValidationBehavior behavior = OptionsReloadValidationBehavior.KeepLastGood,
            Action<string?, Exception>? onError = null)
            where TOptions : class
        {
            ArgumentNullException.ThrowIfNull(optionsBuilder);

            optionsBuilder.Services.AddOptions();
            optionsBuilder.Services.AddSingleton(new ReloadValidationConfiguration<TOptions>(optionsBuilder.Name, behavior, onError));

            return optionsBuilder;
        }
    }
}
