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
        /// Registers this options instance for validation of its DataAnnotations.
        /// </summary>
        /// <remarks>
        /// Synchronous validation runs when the options instance is created or accessed. When targeting .NET 11 or later,
        /// asynchronous validation (including <c>AsyncValidationAttribute</c>-derived attributes) runs during startup
        /// validation when <c>ValidateOnStart()</c> is also called, and during runtime reloads observed by
        /// <c>IOptionsMonitor{TOptions}</c>. Monitor change notifications are published after asynchronous validation
        /// succeeds. If <c>ValidateOnStart()</c> is not called, synchronous options access
        /// triggers only synchronous validation, which invokes the attribute's synchronous fallback instead.
        /// When using <c>AsyncValidationAttribute</c>-derived attributes, ensure the synchronous
        /// <c>IsValid</c> fallback does not throw: synchronous validation still runs on every
        /// options access, so a throwing fallback surfaces as an exception on each access (for example
        /// when resolving <c>IOptions{TOptions}.Value</c>), even if startup validation succeeded.
        /// </remarks>
        /// <typeparam name="TOptions">The options type to be configured.</typeparam>
        /// <param name="optionsBuilder">The options builder to add the services to.</param>
        /// <returns>The <see cref="OptionsBuilder{TOptions}"/> so that additional calls can be chained.</returns>
        [RequiresUnreferencedCode("Uses DataAnnotationValidateOptions which is unsafe given that the options type passed in when calling Validate cannot be statically analyzed so its" +
            " members may be trimmed.")]
        public static OptionsBuilder<TOptions> ValidateDataAnnotations<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] TOptions>(this OptionsBuilder<TOptions> optionsBuilder) where TOptions : class
        {
            var instance = new DataAnnotationValidateOptions<TOptions>(optionsBuilder.Name);
            optionsBuilder.Services.AddSingleton<IValidateOptions<TOptions>>(instance);
#if NET11_0_OR_GREATER
            optionsBuilder.Services.AddSingleton<IAsyncValidateOptions<TOptions>>(instance);
#endif
            return optionsBuilder;
        }
    }
}
