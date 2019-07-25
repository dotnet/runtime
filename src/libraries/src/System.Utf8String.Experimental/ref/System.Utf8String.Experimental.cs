// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System
{
    public readonly partial struct Char8 : System.IComparable<System.Char8>, System.IEquatable<System.Char8>
    {
        private readonly int _dummyPrimitive;
        public int CompareTo(System.Char8 other) { throw null; }
        public bool Equals(System.Char8 other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Char8 left, System.Char8 right) { throw null; }
        public static explicit operator System.Char8 (char value) { throw null; }
        public static explicit operator char (System.Char8 value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator sbyte (System.Char8 value) { throw null; }
        public static explicit operator System.Char8 (short value) { throw null; }
        public static explicit operator System.Char8 (int value) { throw null; }
        public static explicit operator System.Char8 (long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Char8 (sbyte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Char8 (ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Char8 (uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Char8 (ulong value) { throw null; }
        public static bool operator >(System.Char8 left, System.Char8 right) { throw null; }
        public static bool operator >=(System.Char8 left, System.Char8 right) { throw null; }
        public static implicit operator System.Char8 (byte value) { throw null; }
        public static implicit operator byte (System.Char8 value) { throw null; }
        public static implicit operator short (System.Char8 value) { throw null; }
        public static implicit operator int (System.Char8 value) { throw null; }
        public static implicit operator long (System.Char8 value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator ushort (System.Char8 value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator uint (System.Char8 value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator ulong (System.Char8 value) { throw null; }
        public static bool operator !=(System.Char8 left, System.Char8 right) { throw null; }
        public static bool operator <(System.Char8 left, System.Char8 right) { throw null; }
        public static bool operator <=(System.Char8 left, System.Char8 right) { throw null; }
        public override string ToString() { throw null; }
    }
    public static partial class Utf8Extensions
    {
        public static System.ReadOnlySpan<byte> AsBytes(this System.ReadOnlySpan<System.Char8> text) { throw null; }
        public static System.ReadOnlySpan<byte> AsBytes(this System.Utf8String text) { throw null; }
        public static System.ReadOnlySpan<byte> AsBytes(this System.Utf8String text, int start) { throw null; }
        public static System.ReadOnlySpan<byte> AsBytes(this System.Utf8String text, int start, int length) { throw null; }
        public static System.ReadOnlyMemory<System.Char8> AsMemory(this System.Utf8String text) { throw null; }
        public static System.ReadOnlyMemory<System.Char8> AsMemory(this System.Utf8String text, System.Index startIndex) { throw null; }
        public static System.ReadOnlyMemory<System.Char8> AsMemory(this System.Utf8String text, int start) { throw null; }
        public static System.ReadOnlyMemory<System.Char8> AsMemory(this System.Utf8String text, int start, int length) { throw null; }
        public static System.ReadOnlyMemory<System.Char8> AsMemory(this System.Utf8String text, System.Range range) { throw null; }
        public static System.ReadOnlyMemory<byte> AsMemoryBytes(this System.Utf8String text) { throw null; }
        public static System.ReadOnlyMemory<byte> AsMemoryBytes(this System.Utf8String text, System.Index startIndex) { throw null; }
        public static System.ReadOnlyMemory<byte> AsMemoryBytes(this System.Utf8String text, int start) { throw null; }
        public static System.ReadOnlyMemory<byte> AsMemoryBytes(this System.Utf8String text, int start, int length) { throw null; }
        public static System.ReadOnlyMemory<byte> AsMemoryBytes(this System.Utf8String text, System.Range range) { throw null; }
        public static System.ReadOnlySpan<System.Char8> AsSpan(this System.Utf8String text) { throw null; }
        public static System.ReadOnlySpan<System.Char8> AsSpan(this System.Utf8String text, int start) { throw null; }
        public static System.ReadOnlySpan<System.Char8> AsSpan(this System.Utf8String text, int start, int length) { throw null; }
    }
    public sealed partial class Utf8String : System.IEquatable<System.Utf8String>
    {
        public static readonly System.Utf8String Empty;
        [System.CLSCompliantAttribute(false)]
        public unsafe Utf8String(byte* value) { }
        public Utf8String(byte[] value, int startIndex, int length) { }
        [System.CLSCompliantAttribute(false)]
        public unsafe Utf8String(char* value) { }
        public Utf8String(char[] value, int startIndex, int length) { }
        public Utf8String(System.ReadOnlySpan<byte> value) { }
        public Utf8String(System.ReadOnlySpan<char> value) { }
        public Utf8String(string value) { }
        public System.Char8 this[int index] { get { throw null; } }
        public int Length { get { throw null; } }
        public bool Contains(char value) { throw null; }
        public bool Contains(System.Text.Rune value) { throw null; }
        public bool EndsWith(char value) { throw null; }
        public bool EndsWith(System.Text.Rune value) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Utf8String value) { throw null; }
        public static bool Equals(System.Utf8String left, System.Utf8String right) { throw null; }
        public override int GetHashCode() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public ref readonly byte GetPinnableReference() { throw null; }
        public int IndexOf(char value) { throw null; }
        public int IndexOf(System.Text.Rune value) { throw null; }
        public static bool IsNullOrEmpty(System.Utf8String value) { throw null; }
        public static bool operator ==(System.Utf8String left, System.Utf8String right) { throw null; }
        public static explicit operator System.ReadOnlySpan<byte> (System.Utf8String value) { throw null; }
        public static implicit operator System.ReadOnlySpan<System.Char8> (System.Utf8String value) { throw null; }
        public static bool operator !=(System.Utf8String left, System.Utf8String right) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public System.Utf8String Slice(int startIndex, int length) { throw null; }
        public bool StartsWith(char value) { throw null; }
        public bool StartsWith(System.Text.Rune value) { throw null; }
        public System.Utf8String Substring(int startIndex) { throw null; }
        public System.Utf8String Substring(int startIndex, int length) { throw null; }
        public byte[] ToByteArray() { throw null; }
        public byte[] ToByteArray(int startIndex, int length) { throw null; }
        public override string ToString() { throw null; }
    }
}
namespace System.Net.Http
{
    public sealed partial class Utf8StringContent : System.Net.Http.HttpContent
    {
        public Utf8StringContent(System.Utf8String content) { }
        public Utf8StringContent(System.Utf8String content, string mediaType) { }
        protected override System.Threading.Tasks.Task<System.IO.Stream> CreateContentReadStreamAsync() { throw null; }
        protected override System.Threading.Tasks.Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext context) { throw null; }
        protected override bool TryComputeLength(out long length) { throw null; }
    }
}
