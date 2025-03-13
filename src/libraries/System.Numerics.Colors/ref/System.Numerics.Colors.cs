// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Numerics.Colors
{
#pragma warning disable CS3001 // Argument type is not CLS-compliant
#pragma warning disable CS3002 // Return type is not CLS-compliant

    public static class Argb
    {
        public static System.Numerics.Colors.Argb<byte> CreateBigEndian(uint color) { throw null; }
        public static System.Numerics.Colors.Argb<byte> CreateLittleEndian(uint color) { throw null; }
        public static uint ToUInt32BigEndian(this System.Numerics.Colors.Argb<byte> color) { throw null; }
        public static uint ToUInt32LittleEndian(this System.Numerics.Colors.Argb<byte> color) { throw null; }
    }

    public readonly struct Argb<T> : System.IEquatable<System.Numerics.Colors.Argb<T>>//, System.IFormattable, System.ISpanFormattable
        where T : struct
    {
        private readonly T _dummyT;
        public T A { get { throw null; } init { throw null; } }
        public T R { get { throw null; } init { throw null; } }
        public T G { get { throw null; } init { throw null; } }
        public T B { get { throw null; } init { throw null; } }
        public Argb(T a, T r, T g, T b) { throw null; }
        public Argb(System.ReadOnlySpan<T> values) { throw null; }
        public void CopyTo(System.Span<T> destination) { throw null; }
        public bool Equals(System.Numerics.Colors.Argb<T> other) { throw null; }
        //public string ToString(string format, IFormatProvider formatProvider) { throw null; }
        public System.Numerics.Colors.Rgba<T> ToRgba() { throw null; }
    }

    public static class Rgba
    {
        public static System.Numerics.Colors.Rgba<byte> CreateBigEndian(uint color) { throw null; }
        public static System.Numerics.Colors.Rgba<byte> CreateLittleEndian(uint color) { throw null; }
        public static uint ToUInt32BigEndian(this System.Numerics.Colors.Rgba<byte> color) { throw null; }
        public static uint ToUInt32LittleEndian(this System.Numerics.Colors.Rgba<byte> color) { throw null; }
    }

    public readonly struct Rgba<T> : System.IEquatable<System.Numerics.Colors.Rgba<T>>//, IFormattable, ISpanFormattable
        where T : struct
    {
        private readonly T _dummyT;
        public T R { get { throw null; } init { throw null; } }
        public T G { get { throw null; } init { throw null; } }
        public T B { get { throw null; } init { throw null; } }
        public T A { get { throw null; } init { throw null; } }
        public Rgba(T r, T g, T b, T a) { throw null; }
        public Rgba(System.ReadOnlySpan<T> values) { throw null; }
        public void CopyTo(System.Span<T> destination) { throw null; }
        public bool Equals(System.Numerics.Colors.Rgba<T> other) { throw null; }
        //public string ToString(string format, IFormatProvider formatProvider) { throw null; }
        public System.Numerics.Colors.Argb<T> ToArgb() { throw null; }
    }

#pragma warning restore CS3001 // Argument type is not CLS-compliant
#pragma warning restore CS3002 // Return type is not CLS-compliant
}
