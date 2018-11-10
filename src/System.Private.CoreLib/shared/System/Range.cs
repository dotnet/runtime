// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    public readonly struct Range : IEquatable<Range>
    {
        public Index Start { get; }
        public Index End { get; }

        private Range(Index start, Index end)
        {
            Start = start;
            End = end;
        }

        public override bool Equals(object value)
        {
            if (value is Range)
            {
                Range r = (Range)value;
                return r.Start.Equals(Start) && r.End.Equals(End);
            }

            return false;
        }

        public bool Equals (Range other) => other.Start.Equals(Start) && other.End.Equals(End);

        public override int GetHashCode()
        {
            return HashCode.Combine(Start.GetHashCode(), End.GetHashCode());
        }

        public override string ToString()
        {
            return Start + ".." + End;
        }

        public static Range Create(Index start, Index end) => new Range(start, end);
        public static Range FromStart(Index start) => new Range(start, new Index(0, fromEnd: true));
        public static Range ToEnd(Index end) => new Range(new Index(0, fromEnd: false), end);
        public static Range All() => new Range(new Index(0, fromEnd: false), new Index(0, fromEnd: true));
    }
}
