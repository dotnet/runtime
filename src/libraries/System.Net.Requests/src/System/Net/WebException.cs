// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace System.Net
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public partial class WebException : InvalidOperationException, ISerializable
    {
        private const WebExceptionStatus DefaultStatus = WebExceptionStatus.UnknownError;

        private readonly WebExceptionStatus _status = DefaultStatus;
        private readonly WebResponse? _response;

        public WebException()
        {
        }

        public WebException(string? message) :
            base(message)
        {
        }

        public WebException(string? message, Exception? innerException) :
            this(message, innerException, DefaultStatus, null)
        {
        }

        public WebException(string? message, WebExceptionStatus status) :
            this(message, null, status, null)
        {
        }

        public WebException(string? message,
                            Exception? innerException,
                            WebExceptionStatus status,
                            WebResponse? response) :
            base(message, innerException)
        {
            _status = status;
            _response = response;

            if (innerException != null)
            {
                HResult = innerException.HResult;
            }
        }

        protected WebException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }

        public WebExceptionStatus Status => _status;

        public WebResponse? Response => _response;

        void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            base.GetObjectData(serializationInfo, streamingContext);
        }

        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            base.GetObjectData(serializationInfo, streamingContext);
        }

        internal static Exception CreateCompatibleException(Exception exception)
        {
            Debug.Assert(exception != null);
            if (exception is HttpRequestException hre)
            {
                return new WebException(
                    exception.Message,
                    exception,
                    GetStatusFromException(hre),
                    null);
            }
            else if (exception is TaskCanceledException)
            {
                return new WebException(
                    SR.net_webstatus_Timeout,
                    null,
                    WebExceptionStatus.Timeout,
                    null);
            }

            return exception;
        }

        private static WebExceptionStatus GetStatusFromExceptionHelper(HttpRequestException ex)
        {
            NetworkException? networkException = ex.InnerException as NetworkException;

            if (networkException is null)
            {
                return WebExceptionStatus.UnknownError;
            }

            WebExceptionStatus status;
            switch (networkException.NetworkError)
            {
                case NetworkError.HostNotFound:
                    status = WebExceptionStatus.NameResolutionFailure;
                    break;
                default:
                    status = WebExceptionStatus.UnknownError;
                    break;
            }

            return status;
        }
    }
}
