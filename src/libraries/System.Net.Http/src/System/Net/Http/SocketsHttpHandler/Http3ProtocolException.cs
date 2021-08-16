// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Runtime.Versioning;

namespace System.Net.Http
{
    [Serializable]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    internal class Http3ProtocolException : Exception
    {
        public Http3ErrorCode ErrorCode { get; }

        protected Http3ProtocolException(string message, Http3ErrorCode errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        protected Http3ProtocolException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ErrorCode = (Http3ErrorCode)info.GetUInt32(nameof(ErrorCode));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(ErrorCode), (uint)ErrorCode);
            base.GetObjectData(info, context);
        }

        protected static string GetName(Http3ErrorCode errorCode) =>
            // These strings come from the H3 spec and should not be localized.
            errorCode switch
            {
                Http3ErrorCode.NoError => "H3_NO_ERROR (0x100)",
                Http3ErrorCode.ProtocolError => "H3_GENERAL_PROTOCOL_ERROR (0x101)",
                Http3ErrorCode.InternalError => "H3_INTERNAL_ERROR (0x102)",
                Http3ErrorCode.StreamCreationError => "H3_STREAM_CREATION_ERROR (0x103)",
                Http3ErrorCode.ClosedCriticalStream => "H3_CLOSED_CRITICAL_STREAM (0x104)",
                Http3ErrorCode.UnexpectedFrame => "H3_FRAME_UNEXPECTED (0x105)",
                Http3ErrorCode.FrameError => "H3_FRAME_ERROR (0x106)",
                Http3ErrorCode.ExcessiveLoad => "H3_EXCESSIVE_LOAD (0x107)",
                Http3ErrorCode.IdError => "H3_ID_ERROR (0x108)",
                Http3ErrorCode.SettingsError => "H3_SETTINGS_ERROR (0x109)",
                Http3ErrorCode.MissingSettings => "H3_MISSING_SETTINGS (0x10A)",
                Http3ErrorCode.RequestRejected => "H3_REQUEST_REJECTED (0x10B)",
                Http3ErrorCode.RequestCancelled => "H3_REQUEST_CANCELLED (0x10C)",
                Http3ErrorCode.RequestIncomplete => "H3_REQUEST_INCOMPLETE (0x10D)",
                Http3ErrorCode.ConnectError => "H3_CONNECT_ERROR (0x10F)",
                Http3ErrorCode.VersionFallback => "H3_VERSION_FALLBACK (0x110)",
                _ => "(unknown error)"
            };
    }
}
