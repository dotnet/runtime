// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Configures an option instance by using <see cref="ConfigurationBinder.Bind(IConfiguration, object)"/> against an <see cref="IConfiguration"/>.
    /// </summary>
    /// <typeparam name="TOptions">The type of options to bind.</typeparam>
    public class NamedConfigureFromConfigurationOptions<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions> : ConfigureNamedOptions<TOptions>
        where TOptions : class
    {
        /// <summary>
        /// Constructor that takes the <see cref="IConfiguration"/> instance to bind against.
        /// </summary>
        /// <param name="name">The name of the options instance.</param>
        /// <param name="config">The <see cref="IConfiguration"/> instance.</param>
        [RequiresUnreferencedCode(OptionsBuilderConfigurationExtensions.TrimmingRequiredUnreferencedCodeMessage)]
        public NamedConfigureFromConfigurationOptions(string? name, IConfiguration config)
            : this(name, config, _ => { })
        { }

        /// <summary>
        /// Constructor that takes the <see cref="IConfiguration"/> instance to bind against.
        /// </summary>
        /// <param name="name">The name of the options instance.</param>
        /// <param name="config">The <see cref="IConfiguration"/> instance.</param>
        /// <param name="configureBinder">Used to configure the <see cref="BinderOptions"/>.</param>
        [RequiresUnreferencedCode(OptionsBuilderConfigurationExtensions.TrimmingRequiredUnreferencedCodeMessage)]
        public NamedConfigureFromConfigurationOptions(string? name, IConfiguration config, Action<BinderOptions>? configureBinder)
            : base(name, options => BindFromOptions(options, config, configureBinder))
        {
            ThrowHelper.ThrowIfNull(config);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The only call to this method is the constructor which is already annotated as RequiresUnreferencedCode.")]
        private static void BindFromOptions(TOptions options, IConfiguration config, Action<BinderOptions>? configureBinder) => config.Bind(options, configureBinder);
    }
}
