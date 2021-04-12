// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net
{
    internal partial struct StreamSizes
    {
        public int Header
        {
            readonly get;
            private set;
        }

        public int Trailer
        {
            readonly get;
            private set;
        }

        public int MaximumMessage
        {
            readonly get;
            private set;
        }
    }
}
