// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;

namespace Microsoft.Extensions.Http.Logging
{
    /// <summary>
    /// An abstraction for custom HTTP request logging for a named <see cref="HttpClient"/> instances returned by <see cref="IHttpClientFactory"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It is up to the user implementing the interface to decide where (to <see cref="Microsoft.Extensions.Logging.ILogger"/>, or anything else) and what exactly to log.
    /// However, the implementation should be mindful about potential adverse side effects of accessing some of the <see cref="HttpRequestMessage"/> or
    /// <see cref="HttpResponseMessage"/> properties, such as reading from a content stream; if possible, such behavior should be avoided.
    /// </para>
    /// <para>
    /// Logging implementation also should not throw any exceptions, as an unhandled exception in logging would fail the request.
    /// </para>
    /// </remarks>
    public interface IHttpClientLogger
    {
        /// <summary>
        /// Logs before sending an HTTP request.
        /// </summary>
        /// <param name="request">The HTTP request message that will be sent.</param>
        /// <returns>A context object that will be passed to a corresponding <see cref="LogRequestStop"/> or <see cref="LogRequestFailed"/>. Can be `null`
        /// if no context object is needed by the implementation.</returns>
        object? LogRequestStart(HttpRequestMessage request);

        /// <summary>
        /// Logs after receiving an HTTP response.
        /// </summary>
        /// <param name="context">The context object that was previously returned by <see cref="LogRequestStart"/>.</param>
        /// <param name="request">The HTTP request message that was sent.</param>
        /// <param name="response">The HTTP response message that was received.</param>
        /// <param name="elapsed">Time elapsed since calling <see cref="LogRequestStart"/>.</param>
        void LogRequestStop(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed);

        /// <summary>
        /// Logs the exception happened while sending an HTTP request.
        /// </summary>
        /// <param name="context">The context object that was previously returned by <see cref="LogRequestStart"/>.</param>
        /// <param name="request">The HTTP request message that was sent.</param>
        /// <param name="response">If available, the HTTP response message that was received, and `null` otherwise.</param>
        /// <param name="exception">Exception that happened during processing the HTTP request.</param>
        /// <param name="elapsed">Time elapsed since calling <see cref="LogRequestStart"/>.</param>
        void LogRequestFailed(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception, TimeSpan elapsed);
    }
}
