// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http
{
    public class HttpRequestException : Exception
    {
        public HttpRequestException()
            : this(null, null)
        { }

        public HttpRequestException(string message)
            : this(message, null)
        { }

        public HttpRequestException(string message, Exception inner)
            : base(message, inner)
        {
            if (inner != null)
            {
                HResult = inner.HResult;
            }
        }
    }
}
