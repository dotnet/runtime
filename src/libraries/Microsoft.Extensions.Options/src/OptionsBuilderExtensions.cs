// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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

            // Register async validator entries if any IAsyncValidateOptions<TOptions> are registered
            optionsBuilder.Services.AddOptions<StartupValidatorOptions>()
                .Configure<IOptionsMonitor<TOptions>, IEnumerable<IAsyncValidateOptions<TOptions>>>((vo, options, asyncValidators) =>
                {
                    // Materialize the validators into a list to check if any are registered
                    var validators = new List<IAsyncValidateOptions<TOptions>>(asyncValidators);
                    if (validators.Count > 0)
                    {
                        vo._asyncValidators[(typeof(TOptions), optionsBuilder.Name)] = async (CancellationToken ct) =>
                        {
                            // Retrieve the options value (already created by sync Validate() call)
                            TOptions optionsValue = options.Get(optionsBuilder.Name);

                            // Run async validators
                            List<string>? failures = null;
                            foreach (IAsyncValidateOptions<TOptions> validator in validators)
                            {
                                ValidateOptionsResult result = await validator.ValidateAsync(optionsBuilder.Name, optionsValue, ct).ConfigureAwait(false);
                                if (result is not null && result.Failed)
                                {
                                    failures ??= new List<string>();
                                    failures.AddRange(result.Failures);
                                }
                            }

                            if (failures is not null && failures.Count > 0)
                            {
                                throw new OptionsValidationException(optionsBuilder.Name, typeof(TOptions), failures);
                            }
                        };
                    }
                });

            return optionsBuilder;
        }
    }
}
