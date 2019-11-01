// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Data.SqlClient.SNI
{
    /// <summary>
    /// Mars queued packet
    /// </summary>
    internal class SNIMarsQueuedPacket
    {
        private SNIPacket _packet;
        private SNIAsyncCallback _callback;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="packet">SNI packet</param>
        /// <param name="callback">Completion callback</param>
        public SNIMarsQueuedPacket(SNIPacket packet, SNIAsyncCallback callback)
        {
            _packet = packet;
            _callback = callback;
        }

        /// <summary>
        /// SNI packet
        /// </summary>
        public SNIPacket Packet
        {
            get
            {
                return _packet;
            }

            set
            {
                _packet = value;
            }
        }

        /// <summary>
        /// Completion callback
        /// </summary>
        public SNIAsyncCallback Callback
        {
            get
            {
                return _callback;
            }

            set
            {
                _callback = value;
            }
        }
    }
}
