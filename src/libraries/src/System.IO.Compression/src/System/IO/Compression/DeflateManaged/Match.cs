// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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