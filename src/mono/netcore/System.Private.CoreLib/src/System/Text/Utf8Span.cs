// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text
{
    [StructLayout(LayoutKind.Auto)]
    public readonly ref partial struct Utf8Span
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8Span(Utf8String? value)
        {
            throw new PlatformNotSupportedException();
        }

        public ReadOnlySpan<byte> Bytes { get; }

        public static Utf8Span Empty => default;

        public bool IsEmpty => throw new PlatformNotSupportedException();

        public int Length => throw new PlatformNotSupportedException();

        public Utf8Span this[Range range]
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public bool IsEmptyOrWhiteSpace() => throw new PlatformNotSupportedException();

        [Obsolete("Equals(object) on Utf8Span will always throw an exception. Use Equals(Utf8Span) or operator == instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable 0809  // Obsolete member 'Utf8Span.Equals(object)' overrides non-obsolete member 'object.Equals(object)'
        public override bool Equals(object? obj)
#pragma warning restore 0809
        {
            throw new NotSupportedException(SR.Utf8Span_CannotCallEqualsObject);
        }

        public bool Equals(Utf8Span other) => throw new PlatformNotSupportedException();

        public bool Equals(Utf8Span other, StringComparison comparison) => throw new PlatformNotSupportedException();

        public static bool Equals(Utf8Span left, Utf8Span right) => throw new PlatformNotSupportedException();

        public static bool Equals(Utf8Span left, Utf8Span right, StringComparison comparison)
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

        public bool IsAscii()
        {
            throw new PlatformNotSupportedException();
        }

        public bool IsNormalized(NormalizationForm normalizationForm = NormalizationForm.FormC)
        {
            throw new PlatformNotSupportedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref readonly byte GetPinnableReference()
        {
            throw new PlatformNotSupportedException();
        }

        public override string ToString()
        {
            throw new PlatformNotSupportedException();
        }

        internal unsafe string ToStringNoReplacement()
        {
            throw new PlatformNotSupportedException();
        }

        public Utf8String ToUtf8String()
        {
            throw new PlatformNotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Utf8Span UnsafeCreateWithoutValidation(ReadOnlySpan<byte> buffer)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
