// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting
{
    public static class HostingAbstractionsHostBuilderExtensions
    {
        /// <summary>
        /// Builds and starts the host.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IHostBuilder"/> to start.</param>
        /// <returns>The started <see cref="IHost"/>.</returns>
        public static IHost Start(this IHostBuilder hostBuilder)
        {
            return hostBuilder.StartAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Builds and starts the host.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IHostBuilder"/> to start.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the start.</param>
        /// <returns>The started <see cref="IHost"/>.</returns>
        public static async Task<IHost> StartAsync(this IHostBuilder hostBuilder, CancellationToken cancellationToken = default)
        {
            IHost host = hostBuilder.Build();
            await host.StartAsync(cancellationToken).ConfigureAwait(false);
            return host;
        }
    }
}
