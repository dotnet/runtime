// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.WebAssembly.Diagnostics;

internal abstract class AbstractConnection : IDisposable
{
    public abstract Task<string?> ReadOne(TaskCompletionSource client_initiated_close, CancellationToken token);
    public abstract Task SendAsync(byte[] bytes, CancellationToken token);

    public abstract Task Shutdown(CancellationToken cancellationToken);

    public virtual void Dispose()
    {}
}
