// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System
{
    public static partial class Utf8Extensions
    {
        public static System.ReadOnlyMemory<System.Char8> AsMemory(this System.Utf8String? text, System.Index startIndex) { throw null; }
        public static System.ReadOnlyMemory<System.Char8> AsMemory(this System.Utf8String? text, System.Range range) { throw null; }
        public static System.ReadOnlyMemory<byte> AsMemoryBytes(this System.Utf8String? text, System.Index startIndex) { throw null; }
        public static System.ReadOnlyMemory<byte> AsMemoryBytes(this System.Utf8String? text, System.Range range) { throw null; }
    }
    public sealed partial class Utf8String : System.IComparable<System.Utf8String?>, System.IEquatable<System.Utf8String?>
    {
        public static System.Utf8String Create<TState>(int length, TState state, System.Buffers.SpanAction<byte, TState> action) { throw null; }
        public static System.Utf8String CreateRelaxed<TState>(int length, TState state, System.Buffers.SpanAction<byte, TState> action) { throw null; }
        public System.Utf8String this[System.Range range] { get { throw null; } }
        public bool TryFind(char value, out System.Range range) { throw null; }
        public bool TryFind(char value, System.StringComparison comparisonType, out System.Range range) { throw null; }
        public bool TryFind(System.Text.Rune value, out System.Range range) { throw null; }
        public bool TryFind(System.Text.Rune value, System.StringComparison comparisonType, out System.Range range) { throw null; }
        public bool TryFind(System.Utf8String value, out System.Range range) { throw null; }
        public bool TryFind(System.Utf8String value, System.StringComparison comparisonType, out System.Range range) { throw null; }
        public bool TryFindLast(char value, out System.Range range) { throw null; }
        public bool TryFindLast(char value, System.StringComparison comparisonType, out System.Range range) { throw null; }
        public bool TryFindLast(System.Text.Rune value, out System.Range range) { throw null; }
        public bool TryFindLast(System.Text.Rune value, System.StringComparison comparisonType, out System.Range range) { throw null; }
        public bool TryFindLast(System.Utf8String value, out System.Range range) { throw null; }
        public bool TryFindLast(System.Utf8String value, System.StringComparison comparisonType, out System.Range range) { throw null; }
        public static System.Utf8String UnsafeCreateWithoutValidation<TState>(int length, TState state, System.Buffers.SpanAction<byte, TState> action) { throw null; }
    }
}
namespace System.Text
{
    public readonly ref partial struct Utf8Span
    {
        public System.Text.Utf8Span this[System.Range range] { get { throw null; } }
        public bool TryFind(char value, out System.Range range) { throw null; }
        public bool TryFind(char value, System.StringComparison comparisonType, out System.Range range) { throw null; }
        public bool TryFind(System.Text.Rune value, out System.Range range) { throw null; }
        public bool TryFind(System.Text.Rune value, System.StringComparison comparisonType, out System.Range range) { throw null; }
        public bool TryFind(System.Text.Utf8Span value, out System.Range range) { throw null; }
        public bool TryFind(System.Text.Utf8Span value, System.StringComparison comparisonType, out System.Range range) { throw null; }
        public bool TryFindLast(char value, out System.Range range) { throw null; }
        public bool TryFindLast(char value, System.StringComparison comparisonType, out System.Range range) { throw null; }
        public bool TryFindLast(System.Text.Rune value, out System.Range range) { throw null; }
        public bool TryFindLast(System.Text.Rune value, System.StringComparison comparisonType, out System.Range range) { throw null; }
        public bool TryFindLast(System.Text.Utf8Span value, out System.Range range) { throw null; }
        public bool TryFindLast(System.Text.Utf8Span value, System.StringComparison comparisonType, out System.Range range) { throw null; }
    }
}
