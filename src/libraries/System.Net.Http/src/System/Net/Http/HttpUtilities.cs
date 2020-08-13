// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace System.Net.Http
{
    internal static class HttpUtilities
    {
        internal static Version DefaultRequestVersion => HttpVersion.Version11;

        internal static Version DefaultResponseVersion => HttpVersion.Version11;

        internal static HttpVersionPolicy DefaultVersionPolicy => HttpVersionPolicy.RequestVersionOrLower;

        internal static bool IsHttpUri(Uri uri)
        {
            Debug.Assert(uri != null);
            return IsSupportedScheme(uri.Scheme);
        }

        internal static bool IsSupportedScheme(string scheme) =>
            IsSupportedNonSecureScheme(scheme) ||
            IsSupportedSecureScheme(scheme);

        internal static bool IsSupportedNonSecureScheme(string scheme) =>
            string.Equals(scheme, "http", StringComparison.OrdinalIgnoreCase) || IsNonSecureWebSocketScheme(scheme);

        internal static bool IsSupportedSecureScheme(string scheme) =>
            string.Equals(scheme, "https", StringComparison.OrdinalIgnoreCase) || IsSecureWebSocketScheme(scheme);

        internal static bool IsNonSecureWebSocketScheme(string scheme) =>
            string.Equals(scheme, "ws", StringComparison.OrdinalIgnoreCase);

        internal static bool IsSecureWebSocketScheme(string scheme) =>
            string.Equals(scheme, "wss", StringComparison.OrdinalIgnoreCase);

        // Always specify TaskScheduler.Default to prevent us from using a user defined TaskScheduler.Current.
        //
        // Since we're not doing any CPU and/or I/O intensive operations, continue on the same thread.
        // This results in better performance since the continuation task doesn't get scheduled by the
        // scheduler and there are no context switches required.
        internal static Task ContinueWithStandard<T>(this Task<T> task, object state, Action<Task<T>, object?> continuation)
        {
            return task.ContinueWith(continuation, state, CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }
}
