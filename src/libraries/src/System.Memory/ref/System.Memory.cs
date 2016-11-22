#pragma warning disable 0809  //warning CS0809: Obsolete member 'Span<T>.Equals(object)' overrides non-obsolete member 'object.Equals(object)'
namespace System
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ReadOnlySpan<T>
    {
        public static readonly System.ReadOnlySpan<T> Empty;
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public ReadOnlySpan(T[] array) { throw null;}
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public ReadOnlySpan(T[] array, int start) { throw null;}
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public ReadOnlySpan(T[] array, int start, int length) { throw null;}
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public unsafe ReadOnlySpan(void* pointer, int length) { throw null;}
        public bool IsEmpty { get { throw null; } }
        public T this[int index] { [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]get { throw null; }}
        public int Length { get { throw null; } }
        public void CopyTo(System.Span<T> destination) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static System.ReadOnlySpan<T> DangerousCreate(object obj, ref T objectData, int length) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public ref T DangerousGetPinnableReference() { throw null; }
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public System.ReadOnlySpan<T> Slice(int start) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public System.ReadOnlySpan<T> Slice(int start, int length) { throw null; }
        public T[] ToArray() { throw null; }
        public bool TryCopyTo(System.Span<T> destination) { throw null; }
    }

    public static class ReadOnlySpanExtensions
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static ReadOnlySpan<char> Slice(this string text) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static ReadOnlySpan<char> Slice(this string text, int start) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static ReadOnlySpan<char> Slice(this string text, int start, int length) { throw null; }
    }

    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Span<T>
    {
        public static readonly System.Span<T> Empty;
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public Span(T[] array) { throw null;}
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public Span(T[] array, int start) { throw null;}
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public Span(T[] array, int start, int length) { throw null;}
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public unsafe Span(void* pointer, int length) { throw null;}
        public bool IsEmpty { get { throw null; } }
        public T this[int index] { [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]get { throw null; } [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]set { throw null; }}
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public ref T GetItem(int index) { throw null; }
        public int Length { get { throw null; } }
        public void CopyTo(System.Span<T> destination) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static System.Span<T> DangerousCreate(object obj, ref T objectData, int length) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public ref T DangerousGetPinnableReference() { throw null; }
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public System.Span<T> Slice(int start) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public System.Span<T> Slice(int start, int length) { throw null; }
        public T[] ToArray() { throw null; }
        public bool TryCopyTo(System.Span<T> destination) { throw null; }
    }

    public static class SpanExtensions
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)] public static System.Span<byte> AsBytes<T>(this Span<T> source) where T : struct { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)] public static System.ReadOnlySpan<byte> AsBytes<T>(this ReadOnlySpan<T> source) where T : struct { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)] public static System.Span<TTo> NonPortableCast<TFrom, TTo>(this System.Span<TFrom> source) where TFrom : struct where TTo : struct { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)] public static System.ReadOnlySpan<TTo> NonPortableCast<TFrom, TTo>(this System.ReadOnlySpan<TFrom> source) where TFrom : struct where TTo : struct { throw null; }
    }
}

