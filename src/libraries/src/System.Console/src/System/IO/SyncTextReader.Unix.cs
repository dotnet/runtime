// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.IO
{
    /* SyncTextReader intentionally locks on itself rather than a private lock object.
     * This is done to synchronize different console readers(Issue#2855).
     */
    internal sealed partial class SyncTextReader : TextReader
    {
        internal StdInStreamReader Inner
        {
            get
            {
                var inner = _in as StdInStreamReader;
                Debug.Assert(inner != null);
                return inner;
            }
        }

        public ConsoleKeyInfo ReadKey()
        {
            lock (this)
            {
                return Inner.ReadKey();
            }
        }

        public bool KeyAvailable
        {
            get
            {
                lock (this)
                {
                    StdInStreamReader r = Inner;
                    return !r.IsExtraBufferEmpty() || r.StdinReady;
                }
            }
        }
    }
}
