// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for registering <see cref="IServiceCollectionValidator"/> instances with an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionValidationExtensions
    {
        /// <summary>
        /// Registers <typeparamref name="TValidator"/> as an <see cref="IServiceCollectionValidator"/> singleton.
        /// </summary>
        /// <typeparam name="TValidator">The validator type. It will be instantiated by the DI container, allowing constructor injection.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the validator to.</param>
        /// <returns>The <paramref name="services"/> to allow chaining.</returns>
        public static IServiceCollection AddValidator<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TValidator>(this IServiceCollection services)
            where TValidator : class, IServiceCollectionValidator
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddSingleton<IServiceCollectionValidator, TValidator>();
            return services;
        }

        /// <summary>
        /// Registers the given <paramref name="validator"/> instance as an <see cref="IServiceCollectionValidator"/> singleton.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the validator to.</param>
        /// <param name="validator">The validator instance to register.</param>
        /// <returns>The <paramref name="services"/> to allow chaining.</returns>
        public static IServiceCollection AddValidator(this IServiceCollection services, IServiceCollectionValidator validator)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(validator);

            services.AddSingleton<IServiceCollectionValidator>(validator);
            return services;
        }

        /// <summary>
        /// Registers a delegate as an <see cref="IServiceCollectionValidator"/> singleton.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the validator to.</param>
        /// <param name="validator">
        /// A delegate that performs the validation. The first parameter is the built <see cref="IServiceProvider"/>,
        /// which can be used to resolve services; the second parameter is a read-only view of the registered
        /// <see cref="ServiceDescriptor"/> instances.
        /// </param>
        /// <returns>The <paramref name="services"/> to allow chaining.</returns>
        public static IServiceCollection AddValidator(
            this IServiceCollection services,
            Func<IServiceProvider, IReadOnlyList<ServiceDescriptor>, ValidationResult> validator)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(validator);

            services.AddSingleton<IServiceCollectionValidator>(
                sp => new DelegateValidator(sp, validator));
            return services;
        }

        private sealed class DelegateValidator : IServiceCollectionValidator
        {
            private readonly IServiceProvider _serviceProvider;
            private readonly Func<IServiceProvider, IReadOnlyList<ServiceDescriptor>, ValidationResult> _validator;

            public DelegateValidator(
                IServiceProvider serviceProvider,
                Func<IServiceProvider, IReadOnlyList<ServiceDescriptor>, ValidationResult> validator)
            {
                _serviceProvider = serviceProvider;
                _validator = validator;
            }

            public ValidationResult Validate(IReadOnlyList<ServiceDescriptor> services)
                => _validator(_serviceProvider, services);
        }
    }
}
