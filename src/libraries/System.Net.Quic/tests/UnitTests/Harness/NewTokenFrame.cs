// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Frames;

namespace System.Net.Quic.Tests.Harness
{
    using ImplFrame = Implementations.Managed.Internal.Frames.NewTokenFrame;

    /// <summary>
    ///     Provides client with a token to send in the header of an Initial packet for a future connection.
    /// </summary>
    internal class NewTokenFrame : FrameBase
    {
        /// <summary>
        ///     Opaque blob that the client must use with a future Initial packet.
        /// </summary>
        internal byte[] Token;

        internal override FrameType FrameType => FrameType.NewToken;

        internal override void Serialize(QuicWriter writer)
        {
            ImplFrame.Write(writer, new ImplFrame(Token));
        }

        internal override bool Deserialize(QuicReader reader)
        {
            if (!ImplFrame.Read(reader, out var frame))
                return false;

            Token = frame.Token.ToArray();

            return true;
        }
    }
}
