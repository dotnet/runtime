// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Optional contract for <see cref="IServiceProviderFactory{TContainerBuilder}"/> implementations
    /// that handle <see cref="ServiceDecoration"/> entries natively, using their container's built-in
    /// decoration support.
    /// </summary>
    /// <typeparam name="TContainerBuilder">The type of the container builder.</typeparam>
    /// <remarks>
    /// When a factory implements this interface, the hosting layer will call
    /// <see cref="ApplyDecorations"/> after <see cref="IServiceProviderFactory{TContainerBuilder}.CreateBuilder"/>
    /// instead of materializing decorations into standard service descriptors.
    /// </remarks>
    public interface ISupportServiceDecoration<TContainerBuilder> where TContainerBuilder : notnull
    {
        /// <summary>
        /// Applies decorations to the container builder using the container's native decoration support.
        /// </summary>
        /// <param name="builder">The container builder returned by <see cref="IServiceProviderFactory{TContainerBuilder}.CreateBuilder"/>.</param>
        /// <param name="services">The service collection containing the decorations to apply.</param>
        void ApplyDecorations(TContainerBuilder builder, IDecorationServiceCollection services);
    }
}
