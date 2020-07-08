// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace System
{
    /// <summary>
    /// Represents an immutable string of UTF-8 code units.
    /// </summary>
    public sealed partial class Utf8String : IComparable<Utf8String?>, IEquatable<Utf8String?>
    {
#pragma warning disable CS8618
        public static readonly Utf8String Empty;
#pragma warning restore CS8618

        public static bool operator ==(Utf8String? left, Utf8String? right) => throw new PlatformNotSupportedException();
        public static bool operator !=(Utf8String? left, Utf8String? right) => throw new PlatformNotSupportedException();
        public static implicit operator Utf8Span(Utf8String? value) => throw new PlatformNotSupportedException();

        public int Length => throw new PlatformNotSupportedException();
        public Utf8String this[Range range]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public int CompareTo(Utf8String? other)
        {
            throw new PlatformNotSupportedException();
        }

        public int CompareTo(Utf8String? other, StringComparison comparison)
        {
            throw new PlatformNotSupportedException();
        }

        public override bool Equals(object? obj)
        {
            throw new PlatformNotSupportedException();
        }

        public bool Equals(Utf8String? value)
        {
            throw new PlatformNotSupportedException();
        }

        public bool Equals(Utf8String? value, StringComparison comparison) => throw new PlatformNotSupportedException();

        public static bool Equals(Utf8String? left, Utf8String? right)
        {
            throw new PlatformNotSupportedException();
        }

        public static bool Equals(Utf8String? a, Utf8String? b, StringComparison comparison)
        {
            throw new PlatformNotSupportedException();
        }

        public override int GetHashCode()
        {
            throw new PlatformNotSupportedException();
        }

        public int GetHashCode(StringComparison comparison)
        {
            throw new PlatformNotSupportedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)] // for compiler use only
        public ref readonly byte GetPinnableReference() => throw new PlatformNotSupportedException();

        public bool IsAscii()
        {
            throw new PlatformNotSupportedException();
        }

        public static bool IsNullOrEmpty([NotNullWhen(false)] Utf8String? value)
        {
            throw new PlatformNotSupportedException();
        }

        public static bool IsNullOrWhiteSpace([NotNullWhen(false)] Utf8String? value)
        {
            throw new PlatformNotSupportedException();
        }

        public byte[] ToByteArray() => throw new PlatformNotSupportedException();

        public override string ToString()
        {
            throw new PlatformNotSupportedException();
        }
    }
}
