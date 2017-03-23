// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

#pragma warning disable 0809  //warning CS0809: Obsolete member 'Span<T>.Equals(object)' overrides non-obsolete member 'object.Equals(object)'
namespace System
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ReadOnlySpan<T>
    {
        public static ReadOnlySpan<T> Empty { get { throw null; } }
        public ReadOnlySpan(T[] array) { throw null;}
        public ReadOnlySpan(T[] array, int start) { throw null;}
        public ReadOnlySpan(T[] array, int start, int length) { throw null;}
        public unsafe ReadOnlySpan(void* pointer, int length) { throw null;}
        public bool IsEmpty { get { throw null; } }
        public T this[int index] { get { throw null; }}
        public int Length { get { throw null; } }
        public void CopyTo(Span<T> destination) { }
        public static ReadOnlySpan<T> DangerousCreate(object obj, ref T objectData, int length) { throw null; }
        public ref T DangerousGetPinnableReference() { throw null; }
        [System.ObsoleteAttribute("Equals() on ReadOnlySpan will always throw an exception. Use == instead.")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool Equals(object obj) { throw null; }
        [System.ObsoleteAttribute("GetHashCode() on ReadOnlySpan will always throw an exception.")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override int GetHashCode() { throw null; }
        public static bool operator ==(ReadOnlySpan<T> left, ReadOnlySpan<T> right) { throw null; }
        public static implicit operator ReadOnlySpan<T> (T[] array) { throw null; }
        public static implicit operator ReadOnlySpan<T> (ArraySegment<T> arraySegment) { throw null; }
        public static bool operator !=(ReadOnlySpan<T> left, ReadOnlySpan<T> right) { throw null; }
        public ReadOnlySpan<T> Slice(int start) { throw null; }
        public ReadOnlySpan<T> Slice(int start, int length) { throw null; }
        public T[] ToArray() { throw null; }
        public bool TryCopyTo(Span<T> destination) { throw null; }
    }

    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Span<T>
    {
        public static Span<T> Empty { get { throw null; } }
        public Span(T[] array) { throw null;}
        public Span(T[] array, int start) { throw null;}
        public Span(T[] array, int start, int length) { throw null;}
        public unsafe Span(void* pointer, int length) { throw null;}
        public bool IsEmpty { get { throw null; } }
        public ref T this[int index] { get { throw null; } }
        public int Length { get { throw null; } }
        public void Clear() { }
        public void Fill(T value) { }
        public void CopyTo(Span<T> destination) { }
        public static Span<T> DangerousCreate(object obj, ref T objectData, int length) { throw null; }
        public ref T DangerousGetPinnableReference() { throw null; }
        [System.ObsoleteAttribute("Equals() on Span will always throw an exception. Use == instead.")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool Equals(object obj) { throw null; }
        [System.ObsoleteAttribute("GetHashCode() on Span will always throw an exception.")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override int GetHashCode() { throw null; }
        public static bool operator ==(Span<T> left, Span<T> right) { throw null; }
        public static implicit operator Span<T> (T[] array) { throw null; }
        public static implicit operator Span<T> (ArraySegment<T> arraySegment) { throw null; }
        public static implicit operator ReadOnlySpan<T> (Span<T> span) { throw null; }
        public static bool operator !=(Span<T> left, Span<T> right) { throw null; }
        public Span<T> Slice(int start) { throw null; }
        public Span<T> Slice(int start, int length) { throw null; }
        public T[] ToArray() { throw null; }
        public bool TryCopyTo(Span<T> destination) { throw null; }
    }

    public static class Span
    {
        public static Span<byte> AsBytes<T>(this Span<T> source) where T : struct { throw null; }
        public static ReadOnlySpan<byte> AsBytes<T>(this ReadOnlySpan<T> source) where T : struct { throw null; }
        public static Span<TTo> NonPortableCast<TFrom, TTo>(this Span<TFrom> source) where TFrom : struct where TTo : struct { throw null; }
        public static ReadOnlySpan<TTo> NonPortableCast<TFrom, TTo>(this ReadOnlySpan<TFrom> source) where TFrom : struct where TTo : struct { throw null; }

        public static ReadOnlySpan<char> Slice(this string text) { throw null; }
        public static ReadOnlySpan<char> Slice(this string text, int start) { throw null; }
        public static ReadOnlySpan<char> Slice(this string text, int start, int length) { throw null; }
    }

    public static class SpanExtensions
    {
         public static int IndexOf<T>(this Span<T> span, T value) where T:struct, IEquatable<T> { throw null; }
         public static int IndexOf(this Span<byte> span, byte value) { throw null; }

         public static int IndexOf<T>(this ReadOnlySpan<T> span, T value) where T : struct, IEquatable<T> { throw null; }
         public static int IndexOf(this ReadOnlySpan<byte> span, byte value) { throw null; }

         public static int IndexOf<T>(this Span<T> span, ReadOnlySpan<T> value) where T : struct, IEquatable<T> { throw null; }
         public static int IndexOf(this Span<byte> span, ReadOnlySpan<byte> value) { throw null; }

        public static int IndexOf<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value) where T : struct, IEquatable<T> { throw null; }
        public static int IndexOf(this ReadOnlySpan<byte> span, ReadOnlySpan<byte> value) { throw null; }

        public static bool SequenceEqual<T>(this Span<T> first, ReadOnlySpan<T> second) where T:struct, IEquatable<T> { throw null; }
        public static bool SequenceEqual(this Span<byte> first, ReadOnlySpan<byte> second) { throw null; }

        public static bool SequenceEqual<T>(this ReadOnlySpan<T> first, ReadOnlySpan<T> second) where T : struct, IEquatable<T> { throw null; }
        public static bool SequenceEqual(this ReadOnlySpan<byte> first, ReadOnlySpan<byte> second) { throw null; }

        public static bool StartsWith<T>(this Span<T> span, ReadOnlySpan<T> value) where T : struct, IEquatable<T> { throw null; }
        public static bool StartsWith(this Span<byte> span, ReadOnlySpan<byte> value) { throw null; }

        public static bool StartsWith<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value) where T : struct, IEquatable<T> { throw null; }
        public static bool StartsWith(this ReadOnlySpan<byte> span, ReadOnlySpan<byte> value) { throw null; }
    }
}

