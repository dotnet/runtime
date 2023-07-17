// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Net.Http
{
    /// <summary>
    /// An exception thrown when an error occurs while reading the response content stream.
    /// </summary>
    public class HttpIOException : IOException
    {
        /// <summary>
        /// Gets the <see cref="Http.HttpRequestError"/> that caused the exception.
        /// </summary>
        public HttpRequestError HttpRequestError { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpIOException"/> class.
        /// </summary>
        /// <param name="httpRequestError">The <see cref="Http.HttpRequestError"/> that caused the exception.</param>
        /// <param name="message">The message string describing the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public HttpIOException(HttpRequestError httpRequestError, string? message = null, Exception? innerException = null)
            : base(message, innerException)
        {
            HttpRequestError = httpRequestError;
        }
    }
}
