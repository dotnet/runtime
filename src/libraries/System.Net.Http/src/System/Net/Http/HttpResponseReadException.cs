// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Net.Http
{
    public class HttpResponseReadException : IOException
    {
        public HttpRequestError? HttpRequestError { get; }

        public HttpResponseReadException(HttpRequestError? httpRequestError, string? message, Exception? innerException = null)
            : base(message, innerException)
        {
            HttpRequestError = httpRequestError;
        }
    }
}
