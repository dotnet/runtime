// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Options
{
    // REVIEW: consider deleting/obsoleting, not used by Configure anymore (in favor of name), left for breaking change)

    /// <summary>
    /// Configures an option instance by using <see cref="ConfigurationBinder.Bind(IConfiguration, object)"/> against an <see cref="IConfiguration"/>.
    /// </summary>
    /// <typeparam name="TOptions">The type of options to bind.</typeparam>
    public class ConfigureFromConfigurationOptions<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions> : ConfigureOptions<TOptions>
        where TOptions : class
    {
        /// <summary>
        /// Constructor that takes the <see cref="IConfiguration"/> instance to bind against.
        /// </summary>
        /// <param name="config">The <see cref="IConfiguration"/> instance.</param>
        //Even though TOptions is annotated, we need to annotate as RUC as we can't guarantee properties on referenced types are preserved.
        [RequiresUnreferencedCode(OptionsBuilderConfigurationExtensions.TrimmingRequiredUnreferencedCodeMessage)]
        public ConfigureFromConfigurationOptions(IConfiguration config)
            : base(options => BindFromOptions(options, config))
        {
            ThrowHelper.ThrowIfNull(config);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The only call to this method is the constructor which is already annotated as RequiresUnreferencedCode.")]
        private static void BindFromOptions(TOptions options, IConfiguration config) => ConfigurationBinder.Bind(config, options);
    }
}
