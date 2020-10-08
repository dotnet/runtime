namespace System.Net.Quic.Implementations.Managed.Internal
{
    /// <summary>
    ///     State of the sending part of a stream.
    /// </summary>
    internal enum SendStreamState
    {
        /// <summary>
        ///     Initial state. Stream was created, but nothing was sent yet (peer does not know about it).
        /// </summary>
        Ready,

        /// <summary>
        ///    Sending and retransmitting stream data, respecting control flow limits.
        /// </summary>
        Send,

        /// <summary>
        ///     All data has been sent, only retransmissions are done, no control flow updates needed.
        /// </summary>
        DataSent,

        /// <summary>
        ///     Intermediate state that stream reset is wanted, but it was not sent yet. No data transmission occurs
        ///     there.
        /// </summary>
        WantReset,

        /// <summary>
        ///     Sending of the stream has been aborted by this endpoint and RESET_STREAM was sent. No data are sent.
        /// </summary>
        ResetSent,

        /// <summary>
        ///     Terminal state, all data has been acknowledged by the peer.
        /// </summary>
        DataReceived,

        /// <summary>
        ///     Terminal state, RESET_STREAM frame has been acknowledged by the peer.
        /// </summary>
        ResetReceived
    }
}
