// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    // FlushIntentionFlush merely means that all the pending commands have been passed to
    // the hardware, and that the final results will be shown as soon as the hardware finishes
    // its rendering.  FlushIntentionSync means to wait for the hardware to actually finish its
    // rendering before returning - this is important for animation and timing loops.
    public enum FlushIntention
    {
        // Flush all batched rendering operations
        Flush = 0,

        // Flush all batched rendering operations and wait for them to complete
        Sync = 1
    }
}
