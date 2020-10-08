namespace System.Net.Quic.Implementations.Managed.Internal
{
    /// <summary>
    ///     State pf the receiving part of a stream.
    /// </summary>
    internal enum RecvStreamState
    {
        /// <summary>
        ///     Initial state. Data is received, respecting the control flow updates.
        /// </summary>
        Receive,

        /// <summary>
        ///     Final size of the stream is known, still receiving data, but no control flow updates are needed.
        /// </summary>
        SizeKnown,

        /// <summary>
        ///     All data received, but not delivered to the application.
        /// </summary>
        DataReceived,

        /// <summary>
        ///     Terminal state. All data has been delivered to the application.
        /// </summary>
        DataRead,

        /// <summary>
        ///     Application requested stopping sending on this stream, but the STOP_SENDING frame was not sent yet.
        /// </summary>
        WantStopSending,

        /// <summary>
        ///     STOP_SENDING frame has been sent, possible even acknowledged. Waiting for RESET_STREAM
        /// </summary>
        StopSendingSent,

        /// <summary>
        ///     Peer aborted sending on the stream using RESET_STREAM frame, no data is sent.
        /// </summary>
        ResetReceived,
    }
}
