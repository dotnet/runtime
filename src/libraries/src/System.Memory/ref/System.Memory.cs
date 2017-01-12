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
        public static System.ReadOnlySpan<T> Empty { get { throw null; } }
        public ReadOnlySpan(T[] array) { throw null;}
        public ReadOnlySpan(T[] array, int start) { throw null;}
        public ReadOnlySpan(T[] array, int start, int length) { throw null;}
        public unsafe ReadOnlySpan(void* pointer, int length) { throw null;}
        public bool IsEmpty { get { throw null; } }
        public T this[int index] { get { throw null; }}
        public int Length { get { throw null; } }
        public void CopyTo(System.Span<T> destination) { }
        public static System.ReadOnlySpan<T> DangerousCreate(object obj, ref T objectData, int length) { throw null; }
        public ref T DangerousGetPinnableReference() { throw null; }
        [System.ObsoleteAttribute("Equals() on ReadOnlySpan will always throw an exception. Use == instead.")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool Equals(object obj) { throw null; }
        [System.ObsoleteAttribute("GetHashCode() on ReadOnlySpan will always throw an exception.")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.ReadOnlySpan<T> left, System.ReadOnlySpan<T> right) { throw null; }
        public static implicit operator System.ReadOnlySpan<T> (T[] array) { throw null; }
        public static implicit operator System.ReadOnlySpan<T> (System.ArraySegment<T> arraySegment) { throw null; }
        public static bool operator !=(System.ReadOnlySpan<T> left, System.ReadOnlySpan<T> right) { throw null; }
        public System.ReadOnlySpan<T> Slice(int start) { throw null; }
        public System.ReadOnlySpan<T> Slice(int start, int length) { throw null; }
        public T[] ToArray() { throw null; }
        public bool TryCopyTo(System.Span<T> destination) { throw null; }
    }

    public static class ReadOnlySpanExtensions
    {
        public static ReadOnlySpan<char> Slice(this string text) { throw null; }
        public static ReadOnlySpan<char> Slice(this string text, int start) { throw null; }
        public static ReadOnlySpan<char> Slice(this string text, int start, int length) { throw null; }
    }

    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Span<T>
    {
        public static System.Span<T> Empty { get { throw null; } }
        public Span(T[] array) { throw null;}
        public Span(T[] array, int start) { throw null;}
        public Span(T[] array, int start, int length) { throw null;}
        public unsafe Span(void* pointer, int length) { throw null;}
        public bool IsEmpty { get { throw null; } }
        public T this[int index] { get { throw null; } set { throw null; }}
        public ref T GetItem(int index) { throw null; }
        public int Length { get { throw null; } }
        public void CopyTo(System.Span<T> destination) { }
        public static System.Span<T> DangerousCreate(object obj, ref T objectData, int length) { throw null; }
        public ref T DangerousGetPinnableReference() { throw null; }
        [System.ObsoleteAttribute("Equals() on Span will always throw an exception. Use == instead.")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool Equals(object obj) { throw null; }
        [System.ObsoleteAttribute("GetHashCode() on Span will always throw an exception.")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Span<T> left, System.Span<T> right) { throw null; }
        public static implicit operator System.Span<T> (T[] array) { throw null; }
        public static implicit operator System.Span<T> (System.ArraySegment<T> arraySegment) { throw null; }
        public static implicit operator System.ReadOnlySpan<T> (Span<T> span) { throw null; }
        public static bool operator !=(System.Span<T> left, System.Span<T> right) { throw null; }
        public System.Span<T> Slice(int start) { throw null; }
        public System.Span<T> Slice(int start, int length) { throw null; }
        public T[] ToArray() { throw null; }
        public bool TryCopyTo(System.Span<T> destination) { throw null; }
    }

    public static class SpanExtensions
    {
         public static System.Span<byte> AsBytes<T>(this Span<T> source) where T : struct { throw null; }
         public static System.ReadOnlySpan<byte> AsBytes<T>(this ReadOnlySpan<T> source) where T : struct { throw null; }
         public static System.Span<TTo> NonPortableCast<TFrom, TTo>(this System.Span<TFrom> source) where TFrom : struct where TTo : struct { throw null; }
         public static System.ReadOnlySpan<TTo> NonPortableCast<TFrom, TTo>(this System.ReadOnlySpan<TFrom> source) where TFrom : struct where TTo : struct { throw null; }

         public static int IndexOf<T>(this Span<T> span, T value) where T:struct, IEquatable<T> { throw null; }
         public static int IndexOf(this Span<byte> span, byte value) { throw null; }
         public static int IndexOf(this Span<char> span, char value) { throw null; }

         public static bool SequenceEqual<T>(this Span<T> first, ReadOnlySpan<T> second) where T:struct, IEquatable<T> { throw null; }
         public static bool SequenceEqual(this Span<byte> first, ReadOnlySpan<byte> second) { throw null; }
         public static bool SequenceEqual(this Span<char> first, ReadOnlySpan<char> second) { throw null; }

         public static int IndexOf<T>(this Span<T> span, ReadOnlySpan<T> value) where T : struct, IEquatable<T> { throw null; }
         public static int IndexOf(this Span<byte> span, ReadOnlySpan<byte> value) { throw null; }
         public static int IndexOf(this Span<char> span, ReadOnlySpan<char> value) { throw null; }
    }
}

