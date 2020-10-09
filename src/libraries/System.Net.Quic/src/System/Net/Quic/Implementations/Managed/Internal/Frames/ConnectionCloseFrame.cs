// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using System.Text;

namespace System.Net.Quic.Implementations.Managed.Internal.Frames
{
    /// <summary>
    ///     Used to notify the peer that the connection is being closed.
    /// </summary>
    internal readonly ref struct ConnectionCloseFrame
    {
        /// <summary>
        ///     Error code which indicates the reason for closing this connection. If <see cref="IsQuicError" /> is true, then the
        ///     value is one of the <see cref="TransportErrorCode" />. Otherwise it is the application defined error.
        /// </summary>
        internal readonly long ErrorCode;

        /// <summary>
        ///     If true, the error reported comes from the QUIC layer, otherwise from the application layer.
        /// </summary>
        internal readonly bool IsQuicError;

        /// <summary>
        ///     Frame type that triggered the error. Value of <see cref="FrameType.Padding" /> is used when frame type is unknown.
        ///     Only used if <see cref="IsQuicError" /> is true.
        /// </summary>
        internal readonly FrameType FrameType;

        /// <summary>
        ///     Human readable explanation why the connection was closed.
        /// </summary>
        internal readonly string? ReasonPhrase;

        public ConnectionCloseFrame(long errorCode, bool isQuicError, FrameType frameType, string? reasonPhrase)
        {
            ErrorCode = errorCode;
            IsQuicError = isQuicError;
            FrameType = frameType;
            ReasonPhrase = reasonPhrase;
        }

        public int GetSerializedLength()
        {
            int reasonPhraseLength = Encoding.UTF8.GetByteCount(ReasonPhrase ?? string.Empty);

            return 1 +
                   QuicPrimitives.GetVarIntLength(ErrorCode) +
                   (IsQuicError ? QuicPrimitives.GetVarIntLength((long)FrameType) : 0) +
                   QuicPrimitives.GetVarIntLength(reasonPhraseLength) +
                   reasonPhraseLength;
        }

        internal static bool Read(QuicReader reader, out ConnectionCloseFrame frame)
        {
            var type = reader.ReadFrameType();
            Debug.Assert(type == FrameType.ConnectionCloseApplication || type == FrameType.ConnectionCloseQuic);

            FrameType frameType = default;
            if (!reader.TryReadVarInt(out long error) ||
                type == FrameType.ConnectionCloseQuic && !reader.TryReadFrameType(out frameType) ||
                !reader.TryReadVarInt(out long length) ||
                !reader.TryReadSpan((int)length, out var reason))
            {
                frame = default;
                return false;
            }

            frame = new ConnectionCloseFrame(error, type == FrameType.ConnectionCloseQuic, frameType,
                Encoding.UTF8.GetString(reason));
            return true;
        }

        internal static void Write(QuicWriter writer, in ConnectionCloseFrame frame)
        {
            Debug.Assert(writer.BytesAvailable >= frame.GetSerializedLength());

            writer.WriteFrameType(frame.IsQuicError
                ? FrameType.ConnectionCloseQuic
                : FrameType.ConnectionCloseApplication);

            writer.WriteVarInt(frame.ErrorCode);
            if (frame.IsQuicError)
                writer.WriteFrameType(frame.FrameType);

            int length = Encoding.UTF8.GetByteCount(frame.ReasonPhrase ?? string.Empty);
            writer.WriteVarInt(length);
            if (length > 0)
                Encoding.UTF8.GetBytes(frame.ReasonPhrase, writer.GetWritableSpan(length));
        }
    }
}
