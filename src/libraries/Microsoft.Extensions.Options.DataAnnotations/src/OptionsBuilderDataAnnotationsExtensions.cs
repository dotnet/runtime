// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for adding configuration related options services to the DI container via <see cref="OptionsBuilder{TOptions}"/>.
    /// </summary>
    public static class OptionsBuilderDataAnnotationsExtensions
    {
        /// <summary>
        /// Register this options instance for validation of its DataAnnotations.
        /// </summary>
        /// <typeparam name="TOptions">The options type to be configured.</typeparam>
        /// <param name="optionsBuilder">The options builder to add the services to.</param>
        /// <returns>The <see cref="OptionsBuilder{TOptions}"/> so that additional calls can be chained.</returns>
        [RequiresUnreferencedCode("Uses DataAnnotationValidateOptions which is unsafe given that the options type passed in when calling Validate cannot be statically analyzed so its" +
            " members may be trimmed.")]
        public static OptionsBuilder<TOptions> ValidateDataAnnotations<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] TOptions>(this OptionsBuilder<TOptions> optionsBuilder) where TOptions : class
        {
            optionsBuilder.Services.AddSingleton<IValidateOptions<TOptions>>(new DataAnnotationValidateOptions<TOptions>(optionsBuilder.Name));
            return optionsBuilder;
        }

#if NET11_0_OR_GREATER
        /// <summary>
        /// Register this options instance for asynchronous validation of its DataAnnotations.
        /// </summary>
        /// <typeparam name="TOptions">The options type to be configured.</typeparam>
        /// <param name="optionsBuilder">The options builder to add the services to.</param>
        /// <returns>The <see cref="OptionsBuilder{TOptions}"/> so that additional calls can be chained.</returns>
        /// <remarks>
        /// Async validators run only at startup when used with <c>ValidateOnStart</c>.
        /// <see cref="IOptionsMonitor{TOptions}"/> reload validation uses only synchronous validators.
        /// </remarks>
        [RequiresUnreferencedCode("Uses DataAnnotationValidateOptionsAsync which is unsafe given that the options type passed in when calling Validate cannot be statically analyzed so its" +
            " members may be trimmed.")]
        public static OptionsBuilder<TOptions> ValidateDataAnnotationsAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] TOptions>(this OptionsBuilder<TOptions> optionsBuilder) where TOptions : class
        {
            optionsBuilder.Services.AddSingleton<IAsyncValidateOptions<TOptions>>(new DataAnnotationValidateOptionsAsync<TOptions>(optionsBuilder.Name));
            return optionsBuilder;
        }
#endif
    }
}
