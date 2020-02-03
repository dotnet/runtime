// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Quic
{
    public class QuicNotSupportedException : QuicException
    {
        internal QuicNotSupportedException() : base(SR.net_quic_notsupported)
        {
        }

        public QuicNotSupportedException(string message) : base(message)
        {
        }
    }
}
