// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Groups related service configurations.
    /// </summary>
    public interface IServiceCollectionModule
    {
        /// <summary>
        /// Adds the service configurations of this module to the provided <see cref="IServiceCollection"/> instance.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the service configurations of this module to.</param>
        /// <returns>A reference to the provided <see cref="IServiceCollection"/> instance after the operation has completed.</returns>
        IServiceCollection ConfigureServices(IServiceCollection services);
    }
}
