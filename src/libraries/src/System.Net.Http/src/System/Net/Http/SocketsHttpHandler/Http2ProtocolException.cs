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

        protected static string GetName(Http2ProtocolErrorCode code) =>
            // These strings are the names used in the HTTP2 spec and should not be localized.
            code switch
            {
                Http2ProtocolErrorCode.NoError => "NO_ERROR",
                Http2ProtocolErrorCode.ProtocolError => "PROTOCOL_ERROR",
                Http2ProtocolErrorCode.InternalError => "INTERNAL_ERROR",
                Http2ProtocolErrorCode.FlowControlError => "FLOW_CONTROL_ERROR",
                Http2ProtocolErrorCode.SettingsTimeout => "SETTINGS_TIMEOUT",
                Http2ProtocolErrorCode.StreamClosed => "STREAM_CLOSED",
                Http2ProtocolErrorCode.FrameSizeError => "FRAME_SIZE_ERROR",
                Http2ProtocolErrorCode.RefusedStream => "REFUSED_STREAM",
                Http2ProtocolErrorCode.Cancel => "CANCEL",
                Http2ProtocolErrorCode.CompressionError => "COMPRESSION_ERROR",
                Http2ProtocolErrorCode.ConnectError => "CONNECT_ERROR",
                Http2ProtocolErrorCode.EnhanceYourCalm => "ENHANCE_YOUR_CALM",
                Http2ProtocolErrorCode.InadequateSecurity => "INADEQUATE_SECURITY",
                Http2ProtocolErrorCode.Http11Required => "HTTP_1_1_REQUIRED",
                _ => "(unknown error)",
            };
    }
}
