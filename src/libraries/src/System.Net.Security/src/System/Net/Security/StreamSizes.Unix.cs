// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    internal partial class StreamSizes
    {
        // Windows SChannel requires that you pass it a buffer big enough to hold
        // the header, the trailer, and the payload.  You're also required to do your
        // own chunking, not to exceed the maximum message size (which includes header+trailer).
        //
        // OpenSSL, in contrast, does all of this within the library, using their BIO structure
        // to grow to meet demands.  If OpenSSL wants to break the message apart into two, it
        // will. The only thing that maximumMessage does here is control the biggest amount of
        // data we'll ever push at OpenSSL in one call.
        //
        // 16k is the maximum frame size in Ssl3, Tls1, Tls11, and Tls12.
        // We could really set maximumMessage to int.MaxValue and have everything still work,
        // but using a bound of 32k means that if we were to switch from pointers to temporary
        // arrays, we'd be maintaining a reasonable upper bound.

        public StreamSizes()
        {
            Header = 0;
            Trailer = 0;
            MaximumMessage = 32 * 1024;
        }
    }
}
