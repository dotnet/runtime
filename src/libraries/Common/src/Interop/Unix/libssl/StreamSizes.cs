// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    internal class StreamSizes
    {
        public readonly int header;
        public readonly int trailer;
        public readonly int maximumMessage;

        internal StreamSizes()
        {
            Interop.Ssl.GetStreamSizes(
                out header,
                out trailer,
                out maximumMessage);
        }
    }
}
