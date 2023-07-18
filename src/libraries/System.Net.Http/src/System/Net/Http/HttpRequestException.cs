// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        /// Initializes a new instance of the <see cref="HttpRequestException" /> class with a specific message an inner exception, and an HTTP status code and an <see cref="HttpRequestError"/>.
        /// </summary>
        /// <param name="message">A message that describes the current exception.</param>
        /// <param name="inner">The inner exception.</param>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <param name="httpRequestError">The <see cref="HttpRequestError"/> that caused the exception.</param>
        public HttpRequestException(string? message, Exception? inner = null, HttpStatusCode? statusCode = null, HttpRequestError? httpRequestError = null)
            : this(message, inner, statusCode)
        {
            HttpRequestError = httpRequestError;
        }

        /// <summary>
        /// Gets the <see cref="Http.HttpRequestError"/> that caused the exception.
        /// </summary>
        /// <value>
        /// The <see cref="Http.HttpRequestError"/> or <see langword="null"/> if the underlying <see cref="HttpMessageHandler"/> did not provide it.
        /// </value>
        public HttpRequestError? HttpRequestError { get; }

        /// <summary>
        /// Gets the HTTP status code to be returned with the exception.
        /// </summary>
        /// <value>
        /// An HTTP status code if the exception represents a non-successful result, otherwise <c>null</c>.
        /// </value>
        public HttpStatusCode? StatusCode { get; }

        // This constructor is used internally to indicate that a request was not successfully sent due to an IOException,
        // and the exception occurred early enough so that the request may be retried on another connection.
        internal HttpRequestException(string? message, Exception? inner, RequestRetryType allowRetry, HttpRequestError? httpRequestError = null)
            : this(message, inner, httpRequestError: httpRequestError)
        {
            AllowRetry = allowRetry;
        }
    }
}
