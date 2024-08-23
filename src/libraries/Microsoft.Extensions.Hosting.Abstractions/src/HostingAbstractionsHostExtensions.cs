// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Provides extension methods for the <see cref="IHost"/> from the hosting abstractions package.
    /// </summary>
    public static class HostingAbstractionsHostExtensions
    {
        /// <summary>
        /// Starts the host synchronously.
        /// </summary>
        /// <param name="host">The <see cref="IHost"/> to start.</param>
        public static void Start(this IHost host)
        {
            host.StartAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Attempts to gracefully stop the host with the given timeout.
        /// </summary>
        /// <param name="host">The <see cref="IHost"/> to stop.</param>
        /// <param name="timeout">The timeout for stopping gracefully. Once expired the
        /// server may terminate any remaining active connections.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public static async Task StopAsync(this IHost host, TimeSpan timeout)
        {
            using CancellationTokenSource cts = new CancellationTokenSource(timeout);
            await host.StopAsync(cts.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Block the calling thread until shutdown is triggered via Ctrl+C or SIGTERM.
        /// </summary>
        /// <param name="host">The running <see cref="IHost"/>.</param>
        public static void WaitForShutdown(this IHost host)
        {
            host.WaitForShutdownAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Runs an application and block the calling thread until host shutdown.
        /// </summary>
        /// <param name="host">The <see cref="IHost"/> to run.</param>
        public static void Run(this IHost host)
        {
            host.RunAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Runs an application and returns a <see cref="Task"/> that only completes when the token is triggered or shutdown is triggered.
        /// The <paramref name="host"/> instance is disposed of after running.
        /// </summary>
        /// <param name="host">The <see cref="IHost"/> to run.</param>
        /// <param name="token">The token to trigger shutdown.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public static async Task RunAsync(this IHost host, CancellationToken token = default)
        {
            try
            {
                await host.StartAsync(token).ConfigureAwait(false);

                await host.WaitForShutdownAsync(token).ConfigureAwait(false);
            }
            finally
            {
                if (host is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }
                else
                {
                    host.Dispose();
                }
            }
        }

        /// <summary>
        /// Returns a Task that completes when shutdown is triggered via the given token.
        /// </summary>
        /// <param name="host">The running <see cref="IHost"/>.</param>
        /// <param name="token">The token to trigger shutdown.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public static async Task WaitForShutdownAsync(this IHost host, CancellationToken token = default)
        {
            IHostApplicationLifetime applicationLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

            token.Register(state =>
            {
                ((IHostApplicationLifetime)state!).StopApplication();
            },
            applicationLifetime);

#if NET8_0_OR_GREATER
            await Task.Delay(Timeout.Infinite, applicationLifetime.ApplicationStopping).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
#else
            var waitForStop = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            applicationLifetime.ApplicationStopping.Register(obj =>
            {
                var tcs = (TaskCompletionSource<object?>)obj!;
                tcs.TrySetResult(null);
            }, waitForStop);

            await waitForStop.Task.ConfigureAwait(false);
#endif

            // Host will use its default ShutdownTimeout if none is specified.
            // The cancellation token may have been triggered to unblock waitForStop. Don't pass it here because that would trigger an abortive shutdown.
            await host.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }
}
