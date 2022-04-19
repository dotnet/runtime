// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.WebAssembly.Diagnostics;

internal abstract class WasmDebuggerConnection : IDisposable
{
    public string Id { get; init; }

    protected WasmDebuggerConnection(string id) => Id = id;

    public abstract Task<string?> ReadOne(TaskCompletionSource client_initiated_close,
                                          TaskCompletionSource<Exception> side_exception,
                                          CancellationToken token);
    public abstract Task SendAsync(byte[] bytes, CancellationToken token);
    public abstract Task ShutdownAsync(CancellationToken cancellationToken);
    public virtual void Dispose()
    {}
}
