// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for adding configuration related options services to the DI container via <see cref="OptionsBuilder{TOptions}"/>.
    /// </summary>
    public static class OptionsBuilderValidationExtensions
    {
        /// <summary>
        /// Enforces options validation check in startup time rather then in runtime.
        /// </summary>
        /// <typeparam name="TOptions">The type of options.</typeparam>
        /// <param name="optionsBuilder">The <see cref="OptionsBuilder{TOptions}"/> to configure options instance.</param>
        /// <returns>The <see cref="OptionsBuilder{TOptions}"/> so that additional calls can be chained.</returns>
        public static OptionsBuilder<TOptions> ValidateOnStart<TOptions>(this OptionsBuilder<TOptions> optionsBuilder)
            where TOptions : class, new()
        {
            _ = optionsBuilder ?? throw new ArgumentNullException(nameof(optionsBuilder));

            // This will only add the hosted service once
            _ = optionsBuilder.Services.AddHostedService<ValidationHostedService>();

                _ = optionsBuilder
                        .Services
                        .AddOptions<ValidatorOptions>()
                        .Configure<IOptionsMonitor<TOptions>>((vo, options) =>
                        {
                            // This adds an action that resolves the options value to force evaluation
                            // We don't care about the result as duplicates aren't important
                            _ = vo.Validators.TryAdd(typeof(TOptions), () => _ = options.Get(optionsBuilder.Name));
                        });

            return optionsBuilder;
        }
    }
}