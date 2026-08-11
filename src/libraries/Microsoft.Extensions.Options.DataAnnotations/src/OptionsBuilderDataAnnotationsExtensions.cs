// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        /// Synchronous validation runs when an options instance is created. When targeting .NET 11 or later,
        /// asynchronous validation (including <c>AsyncValidationAttribute</c>-derived attributes)
        /// runs during startup when <c>ValidateOnStart()</c> is also called.
        /// If <c>ValidateOnStart()</c> is not called, attributes deriving from
        /// <c>AsyncValidationAttribute</c> are never evaluated asynchronously: runtime options access triggers only
        /// synchronous validation, which invokes the attribute's synchronous fallback instead.
        /// The built-in <see cref="IOptionsSnapshot{TOptions}"/> implementation always uses synchronous attribute
        /// validation and never calls the asynchronous <c>IsValidAsync</c> method. Options created before startup
        /// validation or recreated by the built-in <see cref="IOptionsMonitor{TOptions}"/> implementation after a
        /// change also use synchronous validation. To support these paths, ensure an
        /// <c>AsyncValidationAttribute</c>-derived attribute provides a synchronous <c>IsValid</c> fallback that
        /// does not throw.
        /// </remarks>
        /// <typeparam name="TOptions">The options type to be configured.</typeparam>
        /// <param name="optionsBuilder">The options builder to add the services to.</param>
        /// <returns>The <see cref="OptionsBuilder{TOptions}"/> so that additional calls can be chained.</returns>
        [RequiresUnreferencedCode("Uses DataAnnotationValidateOptions which is unsafe given that the options type passed in when calling Validate cannot be statically analyzed so its" +
            " members may be trimmed.")]
        public static OptionsBuilder<TOptions> ValidateDataAnnotations<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] TOptions>(this OptionsBuilder<TOptions> optionsBuilder) where TOptions : class
        {
#if NET11_0_OR_GREATER
            foreach (ServiceDescriptor descriptor in optionsBuilder.Services)
            {
                if (descriptor.ImplementationInstance is DataAnnotationValidateOptionsRegistration<TOptions> registration &&
                    registration.Name == optionsBuilder.Name)
                {
                    return optionsBuilder;
                }
            }

            optionsBuilder.Services.AddSingleton(new DataAnnotationValidateOptionsRegistration<TOptions>(optionsBuilder.Name));
            optionsBuilder.Services.TryAddSingleton<DataAnnotationValidateOptionsAdapter<TOptions>>();
            return optionsBuilder.Validate<DataAnnotationValidateOptionsAdapter<TOptions>>();
#else
            var instance = new DataAnnotationValidateOptions<TOptions>(optionsBuilder.Name);
            optionsBuilder.Services.AddSingleton<IValidateOptions<TOptions>>(instance);
            return optionsBuilder;
#endif
        }
    }

#if NET11_0_OR_GREATER
    internal sealed class DataAnnotationValidateOptionsAdapter<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] TOptions> :
        IAsyncValidateOptions<TOptions>
        where TOptions : class
    {
        private readonly IEnumerable<DataAnnotationValidateOptionsRegistration<TOptions>> _registrations;

        public DataAnnotationValidateOptionsAdapter(IEnumerable<DataAnnotationValidateOptionsRegistration<TOptions>> registrations) =>
            _registrations = registrations;

        public ValidateOptionsResult Validate(string? name, TOptions options)
        {
            foreach (DataAnnotationValidateOptionsRegistration<TOptions> registration in _registrations)
            {
                if (registration.Name == name)
                {
                    return registration.Validator.Validate(name, options);
                }
            }

            return ValidateOptionsResult.Skip;
        }

        public Task<ValidateOptionsResult> ValidateAsync(
            string? name,
            TOptions options,
            CancellationToken cancellationToken = default)
        {
            foreach (DataAnnotationValidateOptionsRegistration<TOptions> registration in _registrations)
            {
                if (registration.Name == name)
                {
                    return registration.Validator.ValidateAsync(name, options, cancellationToken);
                }
            }

            return Task.FromResult(ValidateOptionsResult.Skip);
        }
    }

    internal sealed class DataAnnotationValidateOptionsRegistration<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] TOptions>
        where TOptions : class
    {
        [RequiresUnreferencedCode("Uses DataAnnotationValidateOptions which is unsafe given that the options type cannot be statically analyzed so its members may be trimmed.")]
        public DataAnnotationValidateOptionsRegistration(string name)
        {
            Name = name;
            Validator = new DataAnnotationValidateOptions<TOptions>(name);
        }

        public string Name { get; }

        public DataAnnotationValidateOptions<TOptions> Validator { get; }
    }
#endif
}
