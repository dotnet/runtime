// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System
{
    public readonly partial struct Char8 : IComparable<Char8>, IEquatable<Char8>
    {
        private readonly int _dummy;
        public static bool operator ==(Char8 left, Char8 right) => throw null;
        public static bool operator !=(Char8 left, Char8 right) => throw null;
        public static bool operator <(Char8 left, Char8 right) => throw null;
        public static bool operator <=(Char8 left, Char8 right) => throw null;
        public static bool operator >(Char8 left, Char8 right) => throw null;
        public static bool operator >=(Char8 left, Char8 right) => throw null;
        public static implicit operator byte(Char8 value) => throw null;
        [CLSCompliant(false)]
        public static explicit operator sbyte(Char8 value) => throw null;
        public static explicit operator char(Char8 value) => throw null;
        public static implicit operator short(Char8 value) => throw null;
        [CLSCompliant(false)]
        public static implicit operator ushort(Char8 value) => throw null;
        public static implicit operator int(Char8 value) => throw null;
        [CLSCompliant(false)]
        public static implicit operator uint(Char8 value) => throw null;
        public static implicit operator long(Char8 value) => throw null;
        [CLSCompliant(false)]
        public static implicit operator ulong(Char8 value) => throw null;
        public static implicit operator Char8(byte value) => throw null;
        [CLSCompliant(false)]
        public static explicit operator Char8(sbyte value) => throw null;
        public static explicit operator Char8(char value) => throw null;
        public static explicit operator Char8(short value) => throw null;
        [CLSCompliant(false)]
        public static explicit operator Char8(ushort value) => throw null;
        public static explicit operator Char8(int value) => throw null;
        [CLSCompliant(false)]
        public static explicit operator Char8(uint value) => throw null;
        public static explicit operator Char8(long value) => throw null;
        [CLSCompliant(false)]
        public static explicit operator Char8(ulong value) => throw null;
        public int CompareTo(Char8 other) => throw null;
        public override bool Equals(object obj) => throw null;
        public bool Equals(Char8 other) => throw null;
        public override int GetHashCode() => throw null;
        public override string ToString() => throw null;
    }
    public static partial class Utf8Extensions
    {
        public static ReadOnlySpan<byte> AsBytes(this ReadOnlySpan<Char8> text) => throw null;
        public static ReadOnlySpan<byte> AsBytes(this Utf8String text) => throw null;
        public static ReadOnlySpan<byte> AsBytes(this Utf8String text, int start) => throw null;
        public static ReadOnlySpan<byte> AsBytes(this Utf8String text, int start, int length) => throw null;
        public static ReadOnlySpan<Char8> AsSpan(this Utf8String text) => throw null;
        public static ReadOnlySpan<Char8> AsSpan(this Utf8String text, int start) => throw null;
        public static ReadOnlySpan<Char8> AsSpan(this Utf8String text, int start, int length) => throw null;
        public static ReadOnlyMemory<Char8> AsMemory(this Utf8String text) => throw null;
        public static ReadOnlyMemory<Char8> AsMemory(this Utf8String text, int start) => throw null;
        public static ReadOnlyMemory<Char8> AsMemory(this Utf8String text, Index startIndex) => throw null;
        public static ReadOnlyMemory<Char8> AsMemory(this Utf8String text, int start, int length) => throw null;
        public static ReadOnlyMemory<Char8> AsMemory(this Utf8String text, Range range) => throw null;
        public static ReadOnlyMemory<byte> AsMemoryBytes(this Utf8String text) => throw null;
        public static ReadOnlyMemory<byte> AsMemoryBytes(this Utf8String text, int start) => throw null;
        public static ReadOnlyMemory<byte> AsMemoryBytes(this Utf8String text, Index startIndex) => throw null;
        public static ReadOnlyMemory<byte> AsMemoryBytes(this Utf8String text, int start, int length) => throw null;
        public static ReadOnlyMemory<byte> AsMemoryBytes(this Utf8String text, Range range) => throw null;
    }
    public sealed partial class Utf8String : IEquatable<Utf8String>
    {
        public static readonly Utf8String Empty;
        public Utf8String(ReadOnlySpan<byte> value) { }
        public Utf8String(byte[] value, int startIndex, int length) { }
        [CLSCompliant(false)]
        public unsafe Utf8String(byte* value) { }
        public Utf8String(ReadOnlySpan<char> value) { }
        public Utf8String(char[] value, int startIndex, int length) { }
        [CLSCompliant(false)]
        public unsafe Utf8String(char* value) { }
        public Utf8String(string value) { }
        public static explicit operator ReadOnlySpan<byte>(Utf8String value) => throw null;
        public static implicit operator ReadOnlySpan<Char8>(Utf8String value) => throw null;
        public static bool operator ==(Utf8String left, Utf8String right) => throw null;
        public static bool operator !=(Utf8String left, Utf8String right) => throw null;
        public Char8 this[int index] => throw null;
        public int Length => throw null;
        public bool Contains(char value) => throw null;
        public bool Contains(System.Text.Rune value) => throw null;
        public bool EndsWith(char value) => throw null;
        public bool EndsWith(System.Text.Rune value) => throw null;
        public override bool Equals(object obj) => throw null;
        public bool Equals(Utf8String value) => throw null;
        public static bool Equals(Utf8String left, Utf8String right) => throw null;
        public override int GetHashCode() => throw null;
        [ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)] // for compiler use only
        public ref readonly byte GetPinnableReference() => throw null;
        public int IndexOf(char value) => throw null;
        public int IndexOf(System.Text.Rune value) => throw null;
        public static bool IsNullOrEmpty(Utf8String value) => throw null;
        [ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)] // for compiler use only
        public Utf8String Slice(int startIndex, int length) => throw null;
        public bool StartsWith(char value) => throw null;
        public bool StartsWith(System.Text.Rune value) => throw null;
        public Utf8String Substring(int startIndex) => throw null;
        public Utf8String Substring(int startIndex, int length) => throw null;
        public byte[] ToByteArray() => throw null;
        public byte[] ToByteArray(int startIndex, int length) => throw null;
        public override string ToString() => throw null;
    }
}
namespace System.Net.Http
{
    public sealed partial class Utf8StringContent : System.Net.Http.HttpContent
    {
        public Utf8StringContent(Utf8String content) { }
        public Utf8StringContent(Utf8String content, string mediaType) { }
        protected override System.Threading.Tasks.Task<System.IO.Stream> CreateContentReadStreamAsync() => throw null;
        protected override System.Threading.Tasks.Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext context) => throw null;
        protected override bool TryComputeLength(out long length) => throw null;
    }
}
