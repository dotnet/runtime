// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net.Http;

namespace System.Net
{
    public partial class WebException : InvalidOperationException
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
