// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Http.Logging
{
    public interface IHttpClientLogger
    {
        ValueTask<object?> LogRequestStartAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);

        ValueTask LogRequestStopAsync(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed, CancellationToken cancellationToken = default);

        ValueTask LogRequestFailedAsync(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception, TimeSpan elapsed, CancellationToken cancellationToken = default);
    }
}
