// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Net.Quic.Implementations.Managed.Internal.Frames
{
    /// <summary>
    ///     Provides client with a token to send in the header of an Initial packet for a future connection.
    /// </summary>
    internal readonly ref struct NewTokenFrame
    {
        /// <summary>
        ///     Opaque blob that the client must use with a future Initial packet.
        /// </summary>
        internal readonly ReadOnlySpan<byte> Token;

        internal NewTokenFrame(ReadOnlySpan<byte> token)
        {
            Token = token;
        }

        internal int GetSerializedLength()
        {
            return 1 +
                   QuicPrimitives.GetVarIntLength(Token.Length) +
                   Token.Length;
        }

        internal static bool Read(QuicReader reader, out NewTokenFrame frame)
        {
            var type = reader.ReadFrameType();
            Debug.Assert(type == FrameType.NewToken);

            if (!reader.TryReadLengthPrefixedSpan(out var token) ||
                token.Length == 0) // token must be nonempty
            {
                frame = default;
                return false;
            }

            frame = new NewTokenFrame(token);
            return true;
        }

        internal static void Write(QuicWriter writer, in NewTokenFrame frame)
        {
            Debug.Assert(writer.BytesAvailable >= frame.GetSerializedLength());

            writer.WriteFrameType(FrameType.NewToken);

            writer.WriteLengthPrefixedSpan(frame.Token);
        }
    }
}
