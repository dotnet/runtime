// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for building a <see cref="ServiceProvider"/> from an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionContainerBuilderExtensions
    {
        /// <summary>
        /// Creates a <see cref="ServiceProvider"/> containing services from the provided <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> containing service descriptors.</param>
        /// <returns>The <see cref="ServiceProvider"/>.</returns>
        public static ServiceProvider BuildServiceProvider(this IServiceCollection services)
        {
            return BuildServiceProvider(services, ServiceProviderOptions.Default);
        }

        /// <summary>
        /// Creates a <see cref="ServiceProvider"/> containing services from the provided <see cref="IServiceCollection"/>
        /// optionally enabling scope validation.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> containing service descriptors.</param>
        /// <param name="validateScopes">
        /// <see langword="true" /> to perform check verifying that scoped services never gets resolved from root provider; otherwise <see langword="false" />.
        /// </param>
        /// <returns>The <see cref="ServiceProvider"/>.</returns>
        public static ServiceProvider BuildServiceProvider(this IServiceCollection services, bool validateScopes)
        {
            return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = validateScopes });
        }

        /// <summary>
        /// Creates a <see cref="ServiceProvider"/> containing services from the provided <see cref="IServiceCollection"/>
        /// optionally enabling scope validation.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> containing service descriptors.</param>
        /// <param name="options">
        /// Configures various service provider behaviors.
        /// </param>
        /// <returns>The <see cref="ServiceProvider"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// One or more registered <see cref="IServiceCollectionValidator"/> instances reported validation errors.
        /// </exception>
        public static ServiceProvider BuildServiceProvider(this IServiceCollection services, ServiceProviderOptions options)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(options);

            var provider = new ServiceProvider(services, options);

            RunValidators(provider, services);

            return provider;
        }

        private static void RunValidators(ServiceProvider provider, IServiceCollection services)
        {
            // Fast path: avoid resolution overhead and EventSource noise when no validators are registered.
            bool hasValidators = false;
            foreach (ServiceDescriptor descriptor in services)
            {
                if (descriptor.ServiceType == typeof(IServiceCollectionValidator))
                {
                    hasValidators = true;
                    break;
                }
            }

            if (!hasValidators)
            {
                return;
            }

            List<IServiceCollectionValidator>? validators = null;
            foreach (IServiceCollectionValidator validator in provider.GetServices<IServiceCollectionValidator>())
            {
                validators ??= new List<IServiceCollectionValidator>();
                validators.Add(validator);
            }

            if (validators is null)
            {
                return;
            }

            IReadOnlyList<ServiceDescriptor> descriptors = services is IReadOnlyList<ServiceDescriptor> readOnly
                ? readOnly
                : new ReadOnlyCollection<ServiceDescriptor>((IList<ServiceDescriptor>)services);

            List<string>? errors = null;
            try
            {
                foreach (IServiceCollectionValidator validator in validators)
                {
                    ValidationResult result = validator.Validate(descriptors);
                    if (!result.IsSuccess)
                    {
                        errors ??= new List<string>();
                        errors.AddRange(result.Errors);
                    }
                }
            }
            catch
            {
                provider.Dispose();
                throw;
            }

            if (errors is not null)
            {
                provider.Dispose();
                throw new InvalidOperationException(
                    SR.Format(SR.ValidatorsFailedWithErrors, string.Join(Environment.NewLine, errors)));
            }
        }
    }
}
