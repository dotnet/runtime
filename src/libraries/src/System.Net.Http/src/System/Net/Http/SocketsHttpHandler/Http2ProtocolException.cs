// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.Net.Http
{
    [Serializable]
    internal abstract class Http2ProtocolException : Exception
    {
        public Http2ProtocolException(string message, Http2ProtocolErrorCode protocolError)
            : base(message)
        {
            ProtocolError = protocolError;
        }

        protected Http2ProtocolException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ProtocolError = (Http2ProtocolErrorCode)info.GetInt32(nameof(ProtocolError));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(ProtocolError), (int)ProtocolError);
            base.GetObjectData(info, context);
        }

        internal Http2ProtocolErrorCode ProtocolError { get; }

        protected static string GetName(Http2ProtocolErrorCode code)
        {
            // These strings are the names used in the HTTP2 spec and should not be localized.
            switch (code)
            {
                case Http2ProtocolErrorCode.NoError:
                    return "NO_ERROR";
                case Http2ProtocolErrorCode.ProtocolError:
                    return "PROTOCOL_ERROR";
                case Http2ProtocolErrorCode.InternalError:
                    return "INTERNAL_ERROR";
                case Http2ProtocolErrorCode.FlowControlError:
                    return "FLOW_CONTROL_ERROR";
                case Http2ProtocolErrorCode.SettingsTimeout:
                    return "SETTINGS_TIMEOUT";
                case Http2ProtocolErrorCode.StreamClosed:
                    return "STREAM_CLOSED";
                case Http2ProtocolErrorCode.FrameSizeError:
                    return "FRAME_SIZE_ERROR";
                case Http2ProtocolErrorCode.RefusedStream:
                    return "REFUSED_STREAM";
                case Http2ProtocolErrorCode.Cancel:
                    return "CANCEL";
                case Http2ProtocolErrorCode.CompressionError:
                    return "COMPRESSION_ERROR";
                case Http2ProtocolErrorCode.ConnectError:
                    return "CONNECT_ERROR";
                case Http2ProtocolErrorCode.EnhanceYourCalm:
                    return "ENHANCE_YOUR_CALM";
                case Http2ProtocolErrorCode.InadequateSecurity:
                    return "INADEQUATE_SECURITY";
                case Http2ProtocolErrorCode.Http11Required:
                    return "HTTP_1_1_REQUIRED";
                default:
                    return "(unknown error)";
            }
        }
    }
}
