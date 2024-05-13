// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// A builder for configuring <see cref="System.Net.Http.SocketsHttpHandler"/> for a named
    /// <see cref="System.Net.Http.HttpClient"/> instances returned by <see cref="System.Net.Http.IHttpClientFactory"/>.
    /// </summary>
    public interface ISocketsHttpHandlerBuilder
    {
        /// <summary>
        /// Gets the name of the client for a handler configured by this builder.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the application service collection.
        /// </summary>
        IServiceCollection Services { get; }
    }
}
#endif
