// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.Primitives
{
    public partial class CancellationChangeToken : Microsoft.Extensions.Primitives.IChangeToken
    {
        public CancellationChangeToken(System.Threading.CancellationToken cancellationToken) { }
        public bool ActiveChangeCallbacks { get { throw null; } }
        public bool HasChanged { get { throw null; } }
        public System.IDisposable RegisterChangeCallback(System.Action<object?> callback, object? state) { throw null; }
    }
    public static partial class ChangeToken
    {
        public static System.IDisposable OnChange(System.Func<Microsoft.Extensions.Primitives.IChangeToken?> changeTokenProducer, System.Action changeTokenConsumer) { throw null; }
        public static System.IDisposable OnChange<TState>(System.Func<Microsoft.Extensions.Primitives.IChangeToken?> changeTokenProducer, System.Action<TState> changeTokenConsumer, TState state) { throw null; }
    }
    public partial class CompositeChangeToken : Microsoft.Extensions.Primitives.IChangeToken
    {
        public CompositeChangeToken(System.Collections.Generic.IReadOnlyList<Microsoft.Extensions.Primitives.IChangeToken> changeTokens) { }
        public bool ActiveChangeCallbacks { get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<Microsoft.Extensions.Primitives.IChangeToken> ChangeTokens { get { throw null; } }
        public bool HasChanged { get { throw null; } }
        public System.IDisposable RegisterChangeCallback(System.Action<object?> callback, object? state) { throw null; }
    }
    public static partial class Extensions
    {
        public static System.Text.StringBuilder Append(this System.Text.StringBuilder builder, Microsoft.Extensions.Primitives.StringSegment segment) { throw null; }
    }
    public partial interface IChangeToken
    {
        bool ActiveChangeCallbacks { get; }
        bool HasChanged { get; }
        System.IDisposable RegisterChangeCallback(System.Action<object?> callback, object? state);
    }
    public readonly partial struct StringSegment : System.IEquatable<Microsoft.Extensions.Primitives.StringSegment>, System.IEquatable<string>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public static readonly Microsoft.Extensions.Primitives.StringSegment Empty;
        public StringSegment(string buffer) { throw null; }
        public StringSegment(string buffer, int offset, int length) { throw null; }
        public string Buffer { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.MemberNotNullWhen(true, nameof(Buffer))]
        public bool HasValue { get { throw null; } }
        public char this[int index] { get { throw null; } }
        public int Length { get { throw null; } }
        public int Offset { get { throw null; } }
        public string? Value { get { throw null; } }
        public System.ReadOnlyMemory<char> AsMemory() { throw null; }
        public System.ReadOnlySpan<char> AsSpan() { throw null; }
        public System.ReadOnlySpan<char> AsSpan(int start) { throw null; }
        public System.ReadOnlySpan<char> AsSpan(int start, int length) { throw null; }
        public static int Compare(Microsoft.Extensions.Primitives.StringSegment a, Microsoft.Extensions.Primitives.StringSegment b, System.StringComparison comparisonType) { throw null; }
        public bool EndsWith(string text, System.StringComparison comparisonType) { throw null; }
        public bool Equals(Microsoft.Extensions.Primitives.StringSegment other) { throw null; }
        public static bool Equals(Microsoft.Extensions.Primitives.StringSegment a, Microsoft.Extensions.Primitives.StringSegment b, System.StringComparison comparisonType) { throw null; }
        public bool Equals(Microsoft.Extensions.Primitives.StringSegment other, System.StringComparison comparisonType) { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj) { throw null; }
        public bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] string? text) { throw null; }
        public bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] string? text, System.StringComparison comparisonType) { throw null; }
        public override int GetHashCode() { throw null; }
        public int IndexOf(char c) { throw null; }
        public int IndexOf(char c, int start) { throw null; }
        public int IndexOf(char c, int start, int count) { throw null; }
        public int IndexOfAny(char[] anyOf) { throw null; }
        public int IndexOfAny(char[] anyOf, int startIndex) { throw null; }
        public int IndexOfAny(char[] anyOf, int startIndex, int count) { throw null; }
        public static bool IsNullOrEmpty(Microsoft.Extensions.Primitives.StringSegment value) { throw null; }
        public int LastIndexOf(char value) { throw null; }
        public static bool operator ==(Microsoft.Extensions.Primitives.StringSegment left, Microsoft.Extensions.Primitives.StringSegment right) { throw null; }
        public static implicit operator System.ReadOnlyMemory<char>(Microsoft.Extensions.Primitives.StringSegment segment) { throw null; }
        public static implicit operator System.ReadOnlySpan<char>(Microsoft.Extensions.Primitives.StringSegment segment) { throw null; }
        public static implicit operator Microsoft.Extensions.Primitives.StringSegment(string value) { throw null; }
        public static bool operator !=(Microsoft.Extensions.Primitives.StringSegment left, Microsoft.Extensions.Primitives.StringSegment right) { throw null; }
        public Microsoft.Extensions.Primitives.StringTokenizer Split(char[] chars) { throw null; }
        public bool StartsWith(string text, System.StringComparison comparisonType) { throw null; }
        public Microsoft.Extensions.Primitives.StringSegment Subsegment(int offset) { throw null; }
        public Microsoft.Extensions.Primitives.StringSegment Subsegment(int offset, int length) { throw null; }
        public string Substring(int offset) { throw null; }
        public string Substring(int offset, int length) { throw null; }
        public override string ToString() { throw null; }
        public Microsoft.Extensions.Primitives.StringSegment Trim() { throw null; }
        public Microsoft.Extensions.Primitives.StringSegment TrimEnd() { throw null; }
        public Microsoft.Extensions.Primitives.StringSegment TrimStart() { throw null; }
    }
    public partial class StringSegmentComparer : System.Collections.Generic.IComparer<Microsoft.Extensions.Primitives.StringSegment>, System.Collections.Generic.IEqualityComparer<Microsoft.Extensions.Primitives.StringSegment>
    {
        internal StringSegmentComparer() { }
        public static Microsoft.Extensions.Primitives.StringSegmentComparer Ordinal { get { throw null; } }
        public static Microsoft.Extensions.Primitives.StringSegmentComparer OrdinalIgnoreCase { get { throw null; } }
        public int Compare(Microsoft.Extensions.Primitives.StringSegment x, Microsoft.Extensions.Primitives.StringSegment y) { throw null; }
        public bool Equals(Microsoft.Extensions.Primitives.StringSegment x, Microsoft.Extensions.Primitives.StringSegment y) { throw null; }
        public int GetHashCode(Microsoft.Extensions.Primitives.StringSegment obj) { throw null; }
    }
    public readonly partial struct StringTokenizer : System.Collections.Generic.IEnumerable<Microsoft.Extensions.Primitives.StringSegment>, System.Collections.IEnumerable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public StringTokenizer(Microsoft.Extensions.Primitives.StringSegment value, char[] separators) { throw null; }
        public StringTokenizer(string value, char[] separators) { throw null; }
        public Microsoft.Extensions.Primitives.StringTokenizer.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<Microsoft.Extensions.Primitives.StringSegment> System.Collections.Generic.IEnumerable<Microsoft.Extensions.Primitives.StringSegment>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<Microsoft.Extensions.Primitives.StringSegment>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public Enumerator(ref Microsoft.Extensions.Primitives.StringTokenizer tokenizer) { throw null; }
            public readonly Microsoft.Extensions.Primitives.StringSegment Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            public void Reset() { }
        }
    }
    public readonly partial struct StringValues : System.Collections.Generic.ICollection<string?>, System.Collections.Generic.IEnumerable<string?>, System.Collections.Generic.IList<string?>, System.Collections.Generic.IReadOnlyCollection<string?>, System.Collections.Generic.IReadOnlyList<string?>, System.Collections.IEnumerable, System.IEquatable<Microsoft.Extensions.Primitives.StringValues>, System.IEquatable<string?>, System.IEquatable<string?[]>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public static readonly Microsoft.Extensions.Primitives.StringValues Empty;
        public StringValues(string? value) { throw null; }
        public StringValues(string?[]? values) { throw null; }
        public int Count { get { throw null; } }
        public string? this[int index] { get { throw null; } }
        bool System.Collections.Generic.ICollection<System.String?>.IsReadOnly { get { throw null; } }
        string? System.Collections.Generic.IList<System.String?>.this[int index] { get { throw null; } set { } }
        public static Microsoft.Extensions.Primitives.StringValues Concat(Microsoft.Extensions.Primitives.StringValues values1, Microsoft.Extensions.Primitives.StringValues values2) { throw null; }
        public static Microsoft.Extensions.Primitives.StringValues Concat(in Microsoft.Extensions.Primitives.StringValues values, string? value) { throw null; }
        public static Microsoft.Extensions.Primitives.StringValues Concat(string? value, in Microsoft.Extensions.Primitives.StringValues values) { throw null; }
        public bool Equals(Microsoft.Extensions.Primitives.StringValues other) { throw null; }
        public static bool Equals(Microsoft.Extensions.Primitives.StringValues left, Microsoft.Extensions.Primitives.StringValues right) { throw null; }
        public static bool Equals(Microsoft.Extensions.Primitives.StringValues left, string? right) { throw null; }
        public static bool Equals(Microsoft.Extensions.Primitives.StringValues left, string?[]? right) { throw null; }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(string? other) { throw null; }
        public static bool Equals(string? left, Microsoft.Extensions.Primitives.StringValues right) { throw null; }
        public bool Equals(string?[]? other) { throw null; }
        public static bool Equals(string?[]? left, Microsoft.Extensions.Primitives.StringValues right) { throw null; }
        public Microsoft.Extensions.Primitives.StringValues.Enumerator GetEnumerator() { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool IsNullOrEmpty(Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        public static bool operator ==(Microsoft.Extensions.Primitives.StringValues left, Microsoft.Extensions.Primitives.StringValues right) { throw null; }
        public static bool operator ==(Microsoft.Extensions.Primitives.StringValues left, object? right) { throw null; }
        public static bool operator ==(Microsoft.Extensions.Primitives.StringValues left, string? right) { throw null; }
        public static bool operator ==(Microsoft.Extensions.Primitives.StringValues left, string?[]? right) { throw null; }
        public static bool operator ==(object? left, Microsoft.Extensions.Primitives.StringValues right) { throw null; }
        public static bool operator ==(string? left, Microsoft.Extensions.Primitives.StringValues right) { throw null; }
        public static bool operator ==(string?[]? left, Microsoft.Extensions.Primitives.StringValues right) { throw null; }
        public static implicit operator string?(Microsoft.Extensions.Primitives.StringValues values) { throw null; }
        public static implicit operator string?[]?(Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        public static implicit operator Microsoft.Extensions.Primitives.StringValues(string? value) { throw null; }
        public static implicit operator Microsoft.Extensions.Primitives.StringValues(string?[]? values) { throw null; }
        public static bool operator !=(Microsoft.Extensions.Primitives.StringValues left, Microsoft.Extensions.Primitives.StringValues right) { throw null; }
        public static bool operator !=(Microsoft.Extensions.Primitives.StringValues left, object? right) { throw null; }
        public static bool operator !=(Microsoft.Extensions.Primitives.StringValues left, string? right) { throw null; }
        public static bool operator !=(Microsoft.Extensions.Primitives.StringValues left, string?[]? right) { throw null; }
        public static bool operator !=(object? left, Microsoft.Extensions.Primitives.StringValues right) { throw null; }
        public static bool operator !=(string? left, Microsoft.Extensions.Primitives.StringValues right) { throw null; }
        public static bool operator !=(string?[]? left, Microsoft.Extensions.Primitives.StringValues right) { throw null; }
        void System.Collections.Generic.ICollection<System.String?>.Add(string? item) { }
        void System.Collections.Generic.ICollection<System.String?>.Clear() { }
        bool System.Collections.Generic.ICollection<System.String?>.Contains(string? item) { throw null; }
        void System.Collections.Generic.ICollection<System.String?>.CopyTo(string?[] array, int arrayIndex) { }
        bool System.Collections.Generic.ICollection<System.String?>.Remove(string? item) { throw null; }
        System.Collections.Generic.IEnumerator<string?> System.Collections.Generic.IEnumerable<System.String?>.GetEnumerator() { throw null; }
        int System.Collections.Generic.IList<System.String?>.IndexOf(string? item) { throw null; }
        void System.Collections.Generic.IList<System.String?>.Insert(int index, string? item) { }
        void System.Collections.Generic.IList<System.String?>.RemoveAt(int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public string?[] ToArray() { throw null; }
        public override string ToString() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<string?>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public Enumerator(ref Microsoft.Extensions.Primitives.StringValues values) { throw null; }
            public string? Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
        }
    }
}
