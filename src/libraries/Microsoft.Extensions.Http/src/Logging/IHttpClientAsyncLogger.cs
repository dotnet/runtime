// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Http.Logging
{
    /// <summary>
    /// An abstraction for asyncronous custom HTTP request logging for a named <see cref="HttpClient"/> instances returned by <see cref="IHttpClientFactory"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Asyncronous methods (such as <see cref="LogRequestStartAsync"/>) would be called from async code paths (such as
    /// <see cref="HttpClient.SendAsync(HttpRequestMessage, CancellationToken)"/>), and their
    /// syncronous counterparts inherited from <see cref="IHttpClientLogger"/> (such as <see cref="IHttpClientLogger.LogRequestStart"/>)
    /// would be called from the corresponding sync code paths.
    /// </para>
    /// <para>
    /// It is up to the user implementing the interface to decide where (to <see cref="Microsoft.Extensions.Logging.ILogger"/>, or anything else) and what exactly to log.
    /// However, the implementation should be mindful about potential adverse side effects of accessing some of the <see cref="HttpRequestMessage"/> or
    /// <see cref="HttpResponseMessage"/> properties, such as reading from a content stream; if possible, such behavior should be avoided.
    /// </para>
    /// <para>
    /// Logging implementation also should not throw any exceptions, as an unhandled exception in logging would fail the request.
    /// </para>
    /// </remarks>
    public interface IHttpClientAsyncLogger : IHttpClientLogger
    {
        /// <summary>
        /// Logs before sending an HTTP request.
        /// </summary>
        /// <param name="request">The HTTP request message that will be sent.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>The task object representing the asynchronous operation. The result of the operation is a context object that will
        /// be passed to a corresponding <see cref="LogRequestStopAsync"/> or <see cref="LogRequestFailedAsync"/>. Can be `null`
        /// if no context object is needed by the implementation.</returns>
        ValueTask<object?> LogRequestStartAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs after receiving an HTTP response.
        /// </summary>
        /// <param name="context">The context object that was previously returned by <see cref="LogRequestStartAsync"/>.</param>
        /// <param name="request">The HTTP request message that was sent.</param>
        /// <param name="response">The HTTP response message that was received.</param>
        /// <param name="elapsed">Time elapsed since calling <see cref="LogRequestStartAsync"/>.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        ValueTask LogRequestStopAsync(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed, CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs the exception happened while sending an HTTP request.
        /// </summary>
        /// <param name="context">The context object that was previously returned by <see cref="LogRequestStartAsync"/>.</param>
        /// <param name="request">The HTTP request message that was sent.</param>
        /// <param name="response">If available, the HTTP response message that was received, and `null` otherwise.</param>
        /// <param name="exception">Exception that happened during processing the HTTP request.</param>
        /// <param name="elapsed">Time elapsed since calling <see cref="LogRequestStartAsync"/>.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        ValueTask LogRequestFailedAsync(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception, TimeSpan elapsed, CancellationToken cancellationToken = default);
    }
}
