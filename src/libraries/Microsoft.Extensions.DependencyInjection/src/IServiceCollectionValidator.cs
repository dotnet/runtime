// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Defines a mechanism to validate service registrations when building a service provider.
    /// </summary>
    /// <remarks>
    /// Implementations are registered in the <see cref="IServiceCollection"/> via
    /// <see cref="ServiceCollectionValidationExtensions.AddValidator{TValidator}(IServiceCollection)"/> or one of its overloads,
    /// and are resolved and invoked automatically just before the built <see cref="System.IServiceProvider"/> is returned to the caller.
    /// Because validators are resolved from the container, constructor injection of any registered service is fully supported.
    /// </remarks>
    public interface IServiceCollectionValidator
    {
        /// <summary>
        /// Validates the service registrations described by <paramref name="services"/>.
        /// </summary>
        /// <param name="services">A read-only view of the service descriptors registered in the container.</param>
        /// <returns>
        /// A <see cref="ValidationResult"/> that is <see cref="ValidationResult.Success"/> when validation passed,
        /// or contains one or more error messages when validation failed.
        /// </returns>
        ValidationResult Validate(IReadOnlyList<ServiceDescriptor> services);
    }
}
