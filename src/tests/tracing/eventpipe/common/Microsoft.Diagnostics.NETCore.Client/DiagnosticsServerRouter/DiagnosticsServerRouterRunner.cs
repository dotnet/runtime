// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.NETCore.Client
{
    /// <summary>
    /// Class used to run different flavours of Diagnostics Server routers.
    /// </summary>
    internal class DiagnosticsServerRouterRunner
    {
        internal interface Callbacks
        {
            void OnRouterStarted(string tcpAddress);
            void OnRouterStopped();
        }

        public static async Task<int> runIpcClientTcpServerRouter(CancellationToken token, string ipcClient, string tcpServer, int runtimeTimeoutMs, TcpServerRouterFactory.CreateInstanceDelegate tcpServerRouterFactory, ILogger logger, Callbacks callbacks)
        {
            return await runRouter(token, new IpcClientTcpServerRouterFactory(ipcClient, tcpServer, runtimeTimeoutMs, tcpServerRouterFactory, logger), callbacks).ConfigureAwait(false);
        }

        public static async Task<int> runIpcServerTcpServerRouter(CancellationToken token, string ipcServer, string tcpServer, int runtimeTimeoutMs, TcpServerRouterFactory.CreateInstanceDelegate tcpServerRouterFactory, ILogger logger, Callbacks callbacks)
        {
            return await runRouter(token, new IpcServerTcpServerRouterFactory(ipcServer, tcpServer, runtimeTimeoutMs, tcpServerRouterFactory, logger), callbacks).ConfigureAwait(false);
        }

        public static async Task<int> runIpcServerTcpClientRouter(CancellationToken token, string ipcServer, string tcpClient, int runtimeTimeoutMs, TcpClientRouterFactory.CreateInstanceDelegate tcpClientRouterFactory, ILogger logger, Callbacks callbacks)
        {
            return await runRouter(token, new IpcServerTcpClientRouterFactory(ipcServer, tcpClient, runtimeTimeoutMs, tcpClientRouterFactory, logger), callbacks).ConfigureAwait(false);
        }

        public static async Task<int> runIpcClientTcpClientRouter(CancellationToken token, string ipcClient, string tcpClient, int runtimeTimeoutMs, TcpClientRouterFactory.CreateInstanceDelegate tcpClientRouterFactory, ILogger logger, Callbacks callbacks)
        {
            return await runRouter(token, new IpcClientTcpClientRouterFactory(ipcClient, tcpClient, runtimeTimeoutMs, tcpClientRouterFactory, logger), callbacks).ConfigureAwait(false);
        }

        public static bool isLoopbackOnly(string address)
        {
            bool isLooback = false;

            try
            {
                var value = new IpcTcpSocketEndPoint(address);
                isLooback = IPAddress.IsLoopback(value.EndPoint.Address);
            }
            catch { }

            return isLooback;
        }

        async static Task<int> runRouter(CancellationToken token, DiagnosticsServerRouterFactory routerFactory, Callbacks callbacks)
        {
            List<Task> runningTasks = new List<Task>();
            List<Router> runningRouters = new List<Router>();

            try
            {
                await routerFactory.Start(token);
                if (!token.IsCancellationRequested)
                    callbacks?.OnRouterStarted(routerFactory.TcpAddress);

                while (!token.IsCancellationRequested)
                {
                    Task<Router> routerTask = null;
                    Router router = null;

                    try
                    {
                        routerTask = routerFactory.CreateRouterAsync(token);

                        do
                        {
                            // Search list and clean up dead router instances before continue waiting on new instances.
                            runningRouters.RemoveAll(IsRouterDead);

                            runningTasks.Clear();
                            foreach (var runningRouter in runningRouters)
                                runningTasks.Add(runningRouter.RouterTaskCompleted.Task);
                            runningTasks.Add(routerTask);
                        }
                        while (await Task.WhenAny(runningTasks.ToArray()).ConfigureAwait(false) != routerTask);

                        if (routerTask.IsFaulted || routerTask.IsCanceled)
                        {
                            //Throw original exception.
                            routerTask.GetAwaiter().GetResult();
                        }

                        if (routerTask.IsCompleted)
                        {
                            router = routerTask.Result;
                            router.Start();

                            // Add to list of running router instances.
                            runningRouters.Add(router);
                            router = null;
                        }

                        routerTask.Dispose();
                        routerTask = null;
                    }
                    catch (Exception ex)
                    {
                        router?.Dispose();
                        router = null;

                        routerTask?.Dispose();
                        routerTask = null;

                        // Timing out on accepting new streams could mean that either the frontend holds an open connection
                        // alive (but currently not using it), or we have a dead backend. If there are no running
                        // routers we assume a dead backend. Reset current backend endpoint and see if we get
                        // reconnect using same or different runtime instance.
                        if (ex is BackendStreamTimeoutException && runningRouters.Count == 0)
                        {
                            routerFactory.Logger?.LogDebug("No backend stream available before timeout.");
                            routerFactory.Reset();
                        }

                        // Timing out on accepting a new runtime connection means there is no runtime alive.
                        // Shutdown router to prevent instances to outlive runtime process (if auto shutdown is enabled).
                        if (ex is RuntimeTimeoutException)
                        {
                            routerFactory.Logger?.LogInformation("No runtime connected before timeout.");
                            routerFactory.Logger?.LogInformation("Starting automatic shutdown.");
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                routerFactory.Logger?.LogInformation($"Shutting down due to error: {ex.Message}");
            }
            finally
            {
                if (token.IsCancellationRequested)
                    routerFactory.Logger?.LogInformation("Shutting down due to cancelation request.");

                runningRouters.RemoveAll(IsRouterDead);
                runningRouters.Clear();

                await routerFactory?.Stop();
                callbacks?.OnRouterStopped();

                routerFactory.Logger?.LogInformation("Router stopped.");
            }
            return 0;
        }

        static bool IsRouterDead(Router router)
        {
            bool isRunning = router.IsRunning && !router.RouterTaskCompleted.Task.IsCompleted;
            if (!isRunning)
                router.Dispose();
            return !isRunning;
        }
    }
}
