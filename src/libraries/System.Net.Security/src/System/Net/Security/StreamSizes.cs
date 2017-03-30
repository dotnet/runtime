// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    internal partial class StreamSizes
    {
        public int Header
        {
            get;
            private set;
        }

        public int Trailer
        {
            get;
            private set;
        }

        public int MaximumMessage
        {
            get;
            private set;
        }
    }
}
