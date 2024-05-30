// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;

namespace Microsoft.DotNet.RemoteExecutor;

internal static class RemoteExecutorExtensions
{
    public static Task DisposeAsync(this RemoteInvokeHandle handle) =>
        Task.Run(handle.Dispose);
}
