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
        { }

        public HttpRequestException(string? message)
            : base(message)
        { }

        public HttpRequestException(string? message, Exception? inner)
            : base(message, inner)
        {
            if (inner != null)
            {
                HResult = inner.HResult;
            }
        }

        public HttpRequestException(string? message, HttpRequestError? httpRequestError)
            : this(message, null, httpRequestError)
        {
        }

        public HttpRequestException(string? message, Exception? inner, HttpRequestError? httpRequestError)
            : this(message, inner)
        {
            HttpRequestError = httpRequestError;
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

        public HttpRequestException(string? message, Exception? inner, HttpStatusCode? statusCode, HttpRequestError? httpRequestError)
            : this(message, inner, statusCode)
        {
            HttpRequestError = httpRequestError;
        }

        /// <summary>
        /// Gets the HTTP status code to be returned with the exception.
        /// </summary>
        /// <value>
        /// An HTTP status code if the exception represents a non-successful result, otherwise <c>null</c>.
        /// </value>
        public HttpStatusCode? StatusCode { get; }

        public HttpRequestError? HttpRequestError { get; }

        // This constructor is used internally to indicate that a request was not successfully sent due to an IOException,
        // and the exception occurred early enough so that the request may be retried on another connection.
        internal HttpRequestException(string? message, Exception? inner, RequestRetryType allowRetry, HttpRequestError? httpRequestError = null)
            : this(message, inner)
        {
            AllowRetry = allowRetry;
            HttpRequestError = httpRequestError;
        }
    }
}
