// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.IO
{
    /* SyncTextReader intentionally locks on itself rather than a private lock object.
     * This is done to synchronize different console readers(Issue#2855).
     */
    internal sealed partial class SyncTextReader : TextReader
    {
        internal StdInReader Inner
        {
            get
            {
                var inner = _in as StdInReader;
                Debug.Assert(inner != null);
                return inner;
            }
        }

        public ConsoleKeyInfo ReadKey(out bool previouslyProcessed)
        {
            lock (this)
            {
                return Inner.ReadKey(out previouslyProcessed);
            }
        }

        public bool KeyAvailable
        {
            get
            {
                lock (this)
                {
                    StdInReader r = Inner;
                    return !r.IsUnprocessedBufferEmpty() || r.StdinReady;
                }
            }
        }
    }
}
