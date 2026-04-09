// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;

namespace Microsoft.DotNet.RemoteExecutor;

internal static class RemoteExecutorExtensions
{
    /// <summary>
    /// Dispose the RemoteInvokeHandle synchronously can take considerable time, and cause other unrelated tests to fail on timeout
    /// because of depletion of xUnit synchronization context threads.
    /// Running dispose in a separate task on the thread pool can help alleviate this issue.
    /// </summary>
    /// <example>
    /// Executes the ServerCode in separate process and awaits its completion in a separate task outside of current synchronization context.
    /// <code>
    /// await RemoteExecutor.Invoke(ServerCode).DisposeAsync();
    /// </code>
    /// </example>
    public static async ValueTask DisposeAsync(this RemoteInvokeHandle handle)
    {
        await Task.Run(handle.Dispose);
    }
}
