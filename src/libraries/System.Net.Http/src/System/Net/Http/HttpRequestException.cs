// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace System.Net.Http
{
    public class HttpRequestException : Exception
    {
        internal RequestRetryType AllowRetry { get; } = RequestRetryType.NoRetry;

        public HttpRequestException()
            : this(null, null)
        { }

        public HttpRequestException(string? message)
            : this(message, null)
        { }

        public HttpRequestException(string? message, Exception? inner)
            : base(message, inner)
        {
            if (inner != null)
            {
                HResult = inner.HResult;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRequestException" /> class with a specific message that describes the current exception, an inner exception, and an HTTP status code.
        /// </summary>
        /// <param name="message">A message that describes the current exception.</param>
        /// <param name="inner">The inner exception.</param>
        /// <param name="statusCode">The HTTP status code.</param>
        public HttpRequestException(string? message, Exception? inner, HttpStatusCode? statusCode)
            : this(message, inner)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Gets the HTTP status code to be returned with the exception.
        /// </summary>
        /// <value>
        /// An HTTP status code if the exception represents a non-successful result, otherwise <c>null</c>.
        /// </value>
        public HttpStatusCode? StatusCode { get; }

        // This constructor is used internally to indicate that a request was not successfully sent due to an IOException,
        // and the exception occurred early enough so that the request may be retried on another connection.
        internal HttpRequestException(string? message, Exception? inner, RequestRetryType allowRetry)
            : this(message, inner)
        {
            AllowRetry = allowRetry;
        }
    }
}
