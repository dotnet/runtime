// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionModuleExtensions
    {
        /// <summary>
        /// Adds the service configurations of the provided <see cref="IServiceCollectionModule"/> instances to the <see cref="IServiceCollection"/> instance.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the service configurations of the modules to.</param>
        /// <param name="modules">The list of <see cref="IServiceCollectionModule"/> instances.</param>
        /// <returns>A reference to the provided <see cref="IServiceCollection"/> instance after the operation has completed.</returns>
        public static IServiceCollection AddModules(this IServiceCollection services, params IServiceCollectionModule[] modules)
        {
            foreach (var module in modules)
            {
                module.ConfigureServices(services);
            }

            return services;
        }
    }
}
