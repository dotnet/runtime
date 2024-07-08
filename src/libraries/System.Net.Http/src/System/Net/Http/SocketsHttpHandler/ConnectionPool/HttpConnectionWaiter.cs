// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed class HttpConnectionWaiter<T> : TaskCompletionSourceWithCancellation<T>
        where T : HttpConnectionBase?
    {
        // When a connection attempt is pending, reference the connection's CTS, so we can tear it down if the initiating request is cancelled
        // or completes on a different connection.
        public CancellationTokenSource? ConnectionCancellationTokenSource;

        // Distinguish connection cancellation that happens because the initiating request is cancelled or completed on a different connection.
        public bool CancelledByOriginatingRequestCompletion { get; set; }

        public ValueTask<T> WaitForConnectionAsync(HttpRequestMessage request, HttpConnectionPool pool, bool async, CancellationToken requestCancellationToken)
        {
            return HttpTelemetry.Log.IsEnabled() || pool.Settings._metrics!.RequestsQueueDuration.Enabled || Activity.Current?.OperationName is DiagnosticsHandlerLoggingStrings.RequestActivityName
                ? WaitForConnectionWithTelemetryAsync(request, pool, async, requestCancellationToken)
                : WaitWithCancellationAsync(async, requestCancellationToken);
        }

        private async ValueTask<T> WaitForConnectionWithTelemetryAsync(HttpRequestMessage request, HttpConnectionPool pool, bool async, CancellationToken requestCancellationToken)
        {
            Debug.Assert(typeof(T) == typeof(HttpConnection) || typeof(T) == typeof(Http2Connection));

            long startingTimestamp = Stopwatch.GetTimestamp();
            using Activity? waitForConnectionActivity = DiagnosticsHelper.WaitForConnectionActivitySource.StartActivity(DiagnosticsHandlerLoggingStrings.WaitForConnectionActivityName);
            try
            {
                T connection = await WaitWithCancellationAsync(async, requestCancellationToken).ConfigureAwait(false);
                if (waitForConnectionActivity is not null && connection?.ConnectionSetupActivity is Activity connectionSetupActivity)
                {
                    waitForConnectionActivity.AddLink(new ActivityLink(connectionSetupActivity.Context));
                }
                return connection;
            }
            catch (Exception ex) when (waitForConnectionActivity is not null)
            {
                waitForConnectionActivity.SetStatus(ActivityStatusCode.Error);
                string? errorType = null;
                if (!DiagnosticsHelper.TryGetErrorType(null, ex, out errorType))
                {
                    errorType = ex.GetType().FullName;
                }
                waitForConnectionActivity.SetTag("error.type", errorType);
                throw;
            }
            finally
            {
                TimeSpan duration = Stopwatch.GetElapsedTime(startingTimestamp);
                int versionMajor = typeof(T) == typeof(HttpConnection) ? 1 : 2;

                pool.Settings._metrics!.RequestLeftQueue(request, pool, duration, versionMajor);

                if (HttpTelemetry.Log.IsEnabled())
                {
                    HttpTelemetry.Log.RequestLeftQueue(versionMajor, duration);
                }
            }
        }

        public bool TrySignal(T connection)
        {
            Debug.Assert(connection is not null);

            if (TrySetResult(connection))
            {
                if (NetEventSource.Log.IsEnabled()) connection.Trace("Dequeued waiting request.");
                return true;
            }
            else
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    connection.Trace(Task.IsCanceled
                        ? "Discarding canceled request from queue."
                        : "Discarding signaled request waiter from queue.");
                }
                return false;
            }
        }

        public void CancelIfNecessary(HttpConnectionPool pool, bool requestCancelled)
        {
            int timeout = GlobalHttpSettings.SocketsHttpHandler.PendingConnectionTimeoutOnRequestCompletion;
            if (ConnectionCancellationTokenSource is null ||
                timeout == Timeout.Infinite ||
                pool.Settings._connectTimeout != Timeout.InfiniteTimeSpan && timeout > (int)pool.Settings._connectTimeout.TotalMilliseconds) // Do not override shorter ConnectTimeout
            {
                return;
            }

            lock (this)
            {
                if (ConnectionCancellationTokenSource is null)
                {
                    return;
                }

                if (NetEventSource.Log.IsEnabled())
                {
                    pool.Trace($"Initiating cancellation of a pending connection attempt with delay of {timeout} ms, " +
                        $"Reason: {(requestCancelled ? "Request cancelled" : "Request served by another connection")}.");
                }

                CancelledByOriginatingRequestCompletion = true;
                if (timeout > 0)
                {
                    // Cancel after the specified timeout. This cancellation will not fire if the connection
                    // succeeds within the delay and the CTS becomes disposed.
                    ConnectionCancellationTokenSource.CancelAfter(timeout);
                }
                else
                {
                    // Cancel immediately if no timeout specified.
                    ConnectionCancellationTokenSource.Cancel();
                }
            }
        }
    }
}
