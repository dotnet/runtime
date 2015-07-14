// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net.Http;

namespace System.Net
{
    public class WebException : InvalidOperationException
    {
        private const WebExceptionStatus DefaultStatus = WebExceptionStatus.UnknownError;

        private WebExceptionStatus _status = DefaultStatus;
        private WebResponse _response = null;

        public WebException()
        {
        }

        public WebException(string message) :
            base(message)
        {
        }

        public WebException(string message, Exception inner) :
            this(message, inner, DefaultStatus, null)
        {
        }

        public WebException(string message, WebExceptionStatus status) :
            this(message, null, status, null)
        {
        }

        public WebException(string message,
                            Exception inner,
                            WebExceptionStatus status,
                            WebResponse response) :
            base(message, inner)
        {
            _status = status;
            _response = response;

            if (inner != null)
            {
                HResult = inner.HResult;
            }
        }

        public WebExceptionStatus Status
        {
            get
            {
                return _status;
            }
        }

        public WebResponse Response
        {
            get
            {
                return _response;
            }
        }

        internal static WebExceptionStatus GetStatusFromException(HttpRequestException ex)
        {
            WebExceptionStatus status;

            // Issue 2384: update WebException.GetStatusFromException after System.Net.Http API changes
            //
            // For now, we use the .HResult of the exception to help us map to a suitable
            // WebExceptionStatus enum value.  The .HResult is set into this exception by
            // the underlying .NET Core and .NET Native versions of the System.Net.Http stack.
            // In the future, the HttpRequestException will have its own .Status property that is
            // an enum type that is more compatible directly with the WebExceptionStatus enum.
            switch (ex.HResult)
            {
                case Interop.WININET_E_NAME_NOT_RESOLVED:
                    status = WebExceptionStatus.NameResolutionFailure;
                    break;
                default:
                    status = WebExceptionStatus.UnknownError;
                    break;
            }

            return status;
        }

        internal static Exception CreateCompatibleException(Exception exception)
        {
            Debug.Assert(exception != null);
            if (exception is HttpRequestException)
            {
                Exception inner = exception.InnerException;
                string message;

                if (inner != null)
                {
                    message = string.Format("{0} {1}", exception.Message, inner.Message);
                }
                else
                {
                    message = string.Format("{0}", exception.Message);
                }

                return new WebException(
                    message,
                    exception,
                    GetStatusFromException(exception as HttpRequestException),
                    null);
            }

            return exception;
        }
    }
}
