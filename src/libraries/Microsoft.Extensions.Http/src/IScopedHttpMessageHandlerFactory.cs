// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace System.Net.Http
{
    /// <summary>
    /// A factory abstraction for a component that can create <see cref="HttpMessageHandler"/> instances with custom
    /// configuration for a given logical name within an existing DI Scope.
    /// </summary>
    /// <remarks>
    /// A default <see cref="IScopedHttpMessageHandlerFactory"/> can be registered in an <see cref="IServiceCollection"/>
    /// by calling <see cref="HttpClientFactoryServiceCollectionExtensions.AddHttpClient(IServiceCollection)"/>.
    /// The default <see cref="IScopedHttpMessageHandlerFactory"/> will be registered in the service collection as a scoped service.
    /// To use a configuration with a default <see cref="IScopedHttpMessageHandlerFactory"/>, it should have
    /// <see cref="HttpClientFactoryOptions.PreserveExistingScope"/> option set to `true` by calling
    /// <see cref="HttpClientBuilderExtensions.SetPreserveExistingScope(IHttpClientBuilder, bool)"/> upon registration.
    /// </remarks>
    public interface IScopedHttpMessageHandlerFactory
    {
        /// <summary>
        /// Creates and configures an <see cref="HttpMessageHandler"/> instance using the configuration that corresponds
        /// to the logical name specified by <paramref name="name"/>. The configuration should have
        /// <see cref="HttpClientFactoryOptions.PreserveExistingScope"/> set to `true`.
        /// </summary>
        /// <param name="name">The logical name of the message handler to create.</param>
        /// <returns>A new <see cref="HttpMessageHandler"/> instance.</returns>
        /// <remarks>
        /// <para>
        /// The default <see cref="IScopedHttpMessageHandlerFactory"/> implementation may cache the underlying
        /// <see cref="HttpMessageHandler"/> instances to improve performance.
        /// </para>
        /// <para>
        /// The default <see cref="IScopedHttpMessageHandlerFactory"/> implementation also manages the lifetime of the
        /// handler created, so disposing of the <see cref="HttpMessageHandler"/> returned by this method may
        /// have no effect.
        /// </para>
        /// </remarks>
        HttpMessageHandler CreateHandler(string name);
    }
}
