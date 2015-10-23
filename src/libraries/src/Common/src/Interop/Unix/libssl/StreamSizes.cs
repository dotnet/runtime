// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net
{
    internal class StreamSizes
    {
        private static readonly int s_header;
        private static readonly int s_trailer;
        private static readonly int s_maximumMessage;

        public int header;
        public int trailer;
        public int maximumMessage;

        static StreamSizes()
        {
            // TODO (Issue #3362) : Trailer size requirement is changing based on protocol
            //       SSL3/TLS1.0 - 68, TLS1.1 - 37 and TLS1.2 - 24
            //       Current usage is only to compute max input buffer size for
            //       encryption and so the native code returns the max

            Interop.Ssl.GetStreamSizes(
                out s_header,
                out s_trailer,
                out s_maximumMessage);
        }

        internal StreamSizes()
        {
            header = s_header;
            trailer = s_trailer;
            maximumMessage = s_maximumMessage;
        }
    }
}
