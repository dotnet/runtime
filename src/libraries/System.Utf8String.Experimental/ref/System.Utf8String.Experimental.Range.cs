// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System
{
    public readonly partial struct Index : System.IEquatable<System.Index>
    {
        private readonly int _dummyPrimitive;
        public Index(int value, bool fromEnd = false) { throw null; }
        public static System.Index End { get { throw null; } }
        public bool IsFromEnd { get { throw null; } }
        public static System.Index Start { get { throw null; } }
        public int Value { get { throw null; } }
        public bool Equals(System.Index other) { throw null; }
        public override bool Equals(object? value) { throw null; }
        public static System.Index FromEnd(int value) { throw null; }
        public static System.Index FromStart(int value) { throw null; }
        public override int GetHashCode() { throw null; }
        public int GetOffset(int length) { throw null; }
        public static implicit operator System.Index(int value) { throw null; }
        public override string ToString() { throw null; }
    }
    public readonly partial struct Range : System.IEquatable<System.Range>
    {
        private readonly int _dummyPrimitive;
        public Range(System.Index start, System.Index end) { throw null; }
        public static System.Range All { get { throw null; } }
        public System.Index End { get { throw null; } }
        public System.Index Start { get { throw null; } }
        public static System.Range EndAt(System.Index end) { throw null; }
        public override bool Equals(object? value) { throw null; }
        public bool Equals(System.Range other) { throw null; }
        public override int GetHashCode() { throw null; }
        public (int Offset, int Length) GetOffsetAndLength(int length) { throw null; }
        public static System.Range StartAt(System.Index start) { throw null; }
        public override string ToString() { throw null; }
    }
}
