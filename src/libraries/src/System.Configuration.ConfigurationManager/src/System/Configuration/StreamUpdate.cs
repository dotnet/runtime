// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    internal class StreamUpdate
    {
        internal StreamUpdate(string newStreamname)
        {
            NewStreamname = newStreamname;
        }

        // desired new stream name
        internal string NewStreamname { get; }

        // indicates whether the change from the old stream name
        // to the new stream name has been completed.
        internal bool WriteCompleted { get; set; }
    }
}