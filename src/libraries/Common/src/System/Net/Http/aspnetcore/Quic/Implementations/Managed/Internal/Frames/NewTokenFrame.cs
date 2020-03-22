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

        internal static bool Read(QuicReader reader, out NewTokenFrame frame)
        {
            if (!reader.TryReadLengthPrefixedSpan(out var token) ||
                token.Length == 0) // token must be nonempty
            {
                frame = default;
                return false;
            }

            frame = new NewTokenFrame(token);
            return true;
        }
    }
}
