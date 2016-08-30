// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO.Compression
{
    // This class represents a match in the history window
    internal class Match
    {
        private MatchState _state;
        private int _pos;
        private int _len;
        private byte _symbol;

        internal MatchState State
        {
            get { return _state; }
            set { _state = value; }
        }

        internal int Position
        {
            get { return _pos; }
            set { _pos = value; }
        }

        internal int Length
        {
            get { return _len; }
            set { _len = value; }
        }

        internal byte Symbol
        {
            get { return _symbol; }
            set { _symbol = value; }
        }
    }
}
