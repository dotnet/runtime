// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;

namespace System.Net.Quic.Tests.Harness
{
    using ImplFrame = Implementations.Managed.Internal.Frames.ResetStreamFrame;

    /// <summary>
    ///     Informs endpoint that the peer abruptly terminates their sending part of the stream.
    /// </summary>
    internal class ResetStreamFrame : FrameBase
    {
        /// <summary>
        ///     Stream ID of the stream being terminated.
        /// </summary>
        internal long StreamId;

        /// <summary>
        ///     Application-level error code indicating why the stream is being closed.
        /// </summary>
        internal long ApplicationErrorCode;

        /// <summary>
        ///     Final size of the stream reset by this frame in bytes.
        /// </summary>
        internal long FinalSize;

        internal override FrameType FrameType => FrameType.ResetStream;

        protected override string GetAdditionalInfo() =>
            $"[StreamId={StreamId}, ErrorCode={ApplicationErrorCode}, FinalSize={FinalSize}]";

        internal override void Serialize(QuicWriter writer)
        {
            ImplFrame.Write(writer, new ImplFrame(StreamId, ApplicationErrorCode, FinalSize));
        }

        internal override bool Deserialize(QuicReader reader)
        {
            if (!ImplFrame.Read(reader, out var frame))
                return false;

            StreamId = frame.StreamId;
            ApplicationErrorCode = frame.ApplicationErrorCode;
            FinalSize = frame.FinalSize;

            return true;
        }
    }
}
