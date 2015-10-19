// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net
{
    internal class StreamSizes
    {
        public int header;
        public int trailer;
        public int maximumMessage;

        internal StreamSizes(int headerSize, int trailerSize, int maxMessageSize)
        {
            header = headerSize;
            trailer = trailerSize;
            maximumMessage = maxMessageSize;
        }
    }
}
