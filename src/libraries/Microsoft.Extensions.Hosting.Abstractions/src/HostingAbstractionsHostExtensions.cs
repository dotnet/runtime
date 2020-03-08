// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting
{
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
        public static Task StopAsync(this IHost host, TimeSpan timeout)
        {
            return host.StopAsync(new CancellationTokenSource(timeout).Token);
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
        /// Runs an application and returns a Task that only completes when the token is triggered or shutdown is triggered.
        /// </summary>
        /// <param name="host">The <see cref="IHost"/> to run.</param>
        /// <param name="token">The token to trigger shutdown.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public static async Task RunAsync(this IHost host, CancellationToken token = default)
        {
            try
            {
                await host.StartAsync(token);

                await host.WaitForShutdownAsync(token);
            }
            finally
            {
                if (host is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
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
            var applicationLifetime = host.Services.GetService<IHostApplicationLifetime>();

            token.Register(state =>
            {
                ((IHostApplicationLifetime)state).StopApplication();
            },
            applicationLifetime);

            var waitForStop = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            applicationLifetime.ApplicationStopping.Register(obj =>
            {
                var tcs = (TaskCompletionSource<object>)obj;
                tcs.TrySetResult(null);
            }, waitForStop);

            await waitForStop.Task;

            // Host will use its default ShutdownTimeout if none is specified.
            await host.StopAsync();
        }
    }
}
