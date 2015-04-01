// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO.Compression
{
    // This class represents a match in the history window
    internal class Match
    {
        private MatchState state;
        private int pos;
        private int len;
        private byte symbol;

        internal MatchState State
        {
            get { return state; }
            set { state = value; }
        }

        internal int Position
        {
            get { return pos; }
            set { pos = value; }
        }

        internal int Length
        {
            get { return len; }
            set { len = value; }
        }

        internal byte Symbol
        {
            get { return symbol; }
            set { symbol = value; }
        }
    }
}