// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Unicode;

#if SYSTEM_PRIVATE_CORELIB
using Internal.Runtime.CompilerServices;
#endif

#pragma warning disable 0809  //warning CS0809: Obsolete member 'Utf8Span.Equals(object)' overrides non-obsolete member 'object.Equals(object)'

namespace System.Text
{
    [StructLayout(LayoutKind.Auto)]
    public readonly ref partial struct Utf8Span
    {
        /// <summary>
        /// Creates a <see cref="Utf8Span"/> from an existing <see cref="Utf8String"/> instance.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8Span(Utf8String? value)
        {
            Bytes = Utf8Extensions.AsBytes(value);
        }

        /// <summary>
        /// Ctor for internal use only. Caller _must_ validate both invariants hold:
        /// (a) the buffer represents well-formed UTF-8 data, and
        /// (b) the buffer is immutable.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Utf8Span(ReadOnlySpan<byte> rawData)
        {
            // In debug builds, we want to ensure that the callers really did validate
            // the buffer for well-formedness. The entire line below is removed when
            // compiling release builds.

            Debug.Assert(Utf8Utility.GetIndexOfFirstInvalidUtf8Sequence(rawData, out _) == -1);

            Bytes = rawData;
        }

        public ReadOnlySpan<byte> Bytes { get; }

        public static Utf8Span Empty => default;

        public bool IsEmpty => Bytes.IsEmpty;

        /// <summary>
        /// Returns the length (in UTF-8 code units, or <see cref="byte"/>s) of this instance.
        /// </summary>
        public int Length => Bytes.Length;

        public Utf8Span this[Range range]
        {
            get
            {
                (int offset, int length) = range.GetOffsetAndLength(Length);

                // Check for a split across a multi-byte subsequence on the way out.
                // Reminder: Unlike Utf8String, we can't safely dereference past the end of the span.

                ref byte newRef = ref DangerousGetMutableReference(offset);
                if (length > 0 && Utf8Utility.IsUtf8ContinuationByte(newRef))
                {
                    Utf8String.ThrowImproperStringSplit();
                }

                int endIdx = offset + length;
                if (endIdx < Length && Utf8Utility.IsUtf8ContinuationByte(DangerousGetMutableReference(endIdx)))
                {
                    Utf8String.ThrowImproperStringSplit();
                }

#if SYSTEM_PRIVATE_CORELIB
                return UnsafeCreateWithoutValidation(new ReadOnlySpan<byte>(ref newRef, length));
#else
                return UnsafeCreateWithoutValidation(Bytes.Slice(offset, length));
#endif
            }
        }

        /// <summary>
        /// Returns a <em>mutable</em> reference to the first byte of this <see cref="Utf8Span"/>
        /// (or, if this <see cref="Utf8Span"/> is empty, to where the first byte would be).
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref byte DangerousGetMutableReference() => ref MemoryMarshal.GetReference(Bytes);

        /// <summary>
        /// Returns a <em>mutable</em> reference to the element at index <paramref name="index"/>
        /// of this <see cref="Utf8Span"/> instance. The index is not bounds-checked.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref byte DangerousGetMutableReference(int index)
        {
            Debug.Assert(index >= 0, "Caller should've performed bounds checking.");
            return ref DangerousGetMutableReference((uint)index);
        }

        /// <summary>
        /// Returns a <em>mutable</em> reference to the element at index <paramref name="index"/>
        /// of this <see cref="Utf8Span"/> instance. The index is not bounds-checked.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref byte DangerousGetMutableReference(nuint index)
        {
            // Allow retrieving references to just past the end of the span (but shouldn't dereference this).

            Debug.Assert(index <= (uint)Length, "Caller should've performed bounds checking.");
#if SYSTEM_PRIVATE_CORELIB
            return ref Unsafe.AddByteOffset(ref DangerousGetMutableReference(), index);
#else
            return ref Unsafe.AddByteOffset(ref DangerousGetMutableReference(), (nint)index);
#endif
        }

        public bool IsEmptyOrWhiteSpace() => (Utf8Utility.GetIndexOfFirstNonWhiteSpaceChar(Bytes) == Length);

        /// <summary>
        /// This method is not supported as spans cannot be boxed. To compare two spans, use operator==.
        /// <exception cref="System.NotSupportedException">
        /// Always thrown by this method.
        /// </exception>
        /// </summary>
        [Obsolete("Equals(object) on Utf8Span will always throw an exception. Use Equals(Utf8Span) or operator == instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object? obj)
        {
            throw new NotSupportedException(SR.Utf8Span_CannotCallEqualsObject);
        }

        public bool Equals(Utf8Span other) => Equals(this, other);

        public bool Equals(Utf8Span other, StringComparison comparison) => Equals(this, other, comparison);

        public static bool Equals(Utf8Span left, Utf8Span right) => left.Bytes.SequenceEqual(right.Bytes);

        public static bool Equals(Utf8Span left, Utf8Span right, StringComparison comparison)
        {
            // TODO_UTF8STRING: This perf can be improved, including removing
            // the virtual dispatch by putting the switch directly in this method.

            return Utf8StringComparer.FromComparison(comparison).Equals(left, right);
        }

        public override int GetHashCode()
        {
            // TODO_UTF8STRING: Consider whether this should use a different seed than String.GetHashCode.
            // This method should only be called to calculate the hash code over spans that represent
            // UTF-8 textual data, not over arbitrary binary sequences.

            ulong seed = Marvin.DefaultSeed;
#if SYSTEM_PRIVATE_CORELIB
            return Marvin.ComputeHash32(ref MemoryMarshal.GetReference(Bytes), (uint)Length /* in bytes */, (uint)seed, (uint)(seed >> 32));
#else
            return Marvin.ComputeHash32(Bytes, seed);
#endif
        }

        public int GetHashCode(StringComparison comparison)
        {
            // TODO_UTF8STRING: This perf can be improved, including removing
            // the virtual dispatch by putting the switch directly in this method.

            return Utf8StringComparer.FromComparison(comparison).GetHashCode(this);
        }

        /// <summary>
        /// Returns <see langword="true"/> if this UTF-8 text consists of all-ASCII data,
        /// <see langword="false"/> if there is any non-ASCII data within this UTF-8 text.
        /// </summary>
        /// <remarks>
        /// ASCII text is defined as text consisting only of scalar values in the range [ U+0000..U+007F ].
        /// Empty spans are considered to be all-ASCII. The runtime of this method is O(n).
        /// </remarks>
        public bool IsAscii()
        {
            // TODO_UTF8STRING: Use an API that takes 'ref byte' instead of a 'byte*' as a parameter.

            unsafe
            {
                fixed (byte* pData = &MemoryMarshal.GetReference(Bytes))
                {
                    return (ASCIIUtility.GetIndexOfFirstNonAsciiByte(pData, (uint)Length) == (uint)Length);
                }
            }
        }

        /// <summary>
        /// Returns a value stating whether this <see cref="Utf8Span"/> instance is normalized
        /// using the specified Unicode normalization form.
        /// </summary>
        /// <param name="normalizationForm">The <see cref="NormalizationForm"/> to check.</param>
        /// <returns><see langword="true"/> if this <see cref="Utf8Span"/> instance represents text
        /// normalized under <paramref name="normalizationForm"/>, otherwise <see langword="false"/>.</returns>
        public bool IsNormalized(NormalizationForm normalizationForm = NormalizationForm.FormC)
        {
            // TODO_UTF8STRING: Avoid allocations in this code path.

            return ToString().IsNormalized(normalizationForm);
        }

        /// <summary>
        /// Gets an immutable reference that can be used in a <see langword="fixed"/> statement. Unlike
        /// <see cref="Utf8String"/>, the resulting reference is not guaranteed to be null-terminated.
        /// </summary>
        /// <remarks>
        /// If this <see cref="Utf8Span"/> instance is empty, returns <see langword="null"/>. Dereferencing
        /// such a reference will result in a <see cref="NullReferenceException"/> being generated.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref readonly byte GetPinnableReference()
        {
            // This returns null if the underlying span is empty. The reason for this is that unlike
            // Utf8String, these buffers are not guaranteed to be null-terminated, so it's not always
            // safe or meaningful to dereference the element just past the end of the buffer.

            return ref Bytes.GetPinnableReference();
        }

        public override string ToString()
        {
            // TODO_UTF8STRING: Since we know the underlying data is immutable, well-formed UTF-8,
            // we can perform transcoding using an optimized code path that skips all safety checks.

#if (!NETSTANDARD2_0 && !NETFRAMEWORK)
            return Encoding.UTF8.GetString(Bytes);
#else
            if (IsEmpty)
            {
                return string.Empty;
            }

            unsafe
            {
                fixed (byte* pBytes = Bytes)
                {
                    return Encoding.UTF8.GetString(pBytes, Length);
                }
            }
#endif
        }

        /// <summary>
        /// Converts this <see cref="Utf8Span"/> instance to a <see cref="string"/>.
        /// </summary>
        /// <remarks>
        /// This routine throws <see cref="InvalidOperationException"/> if the underlying instance
        /// contains invalid UTF-8 data.
        /// </remarks>
        internal unsafe string ToStringNoReplacement()
        {
            // TODO_UTF8STRING: Optimize the call below, potentially by avoiding the two-pass.

            fixed (byte* pData = &MemoryMarshal.GetReference(Bytes))
            {
                byte* pFirstInvalidByte = Utf8Utility.GetPointerToFirstInvalidByte(pData, Length, out int utf16CodeUnitCountAdjustment, out _);
                if (pFirstInvalidByte != pData + (uint)Length)
                {
                    // Saw bad UTF-8 data.
                    // TODO_UTF8STRING: Throw a better exception below?

                    ThrowHelper.ThrowInvalidOperationException();
                }

                int utf16CharCount = Length + utf16CodeUnitCountAdjustment;
                Debug.Assert(utf16CharCount <= Length && utf16CharCount >= 0);

#if (!NETSTANDARD2_0 && !NETFRAMEWORK)
                // TODO_UTF8STRING: Can we call string.FastAllocate directly?
                return string.Create(utf16CharCount, (pbData: (IntPtr)pData, cbData: Length), static (chars, state) =>
                {
                    OperationStatus status = Utf8.ToUtf16(new ReadOnlySpan<byte>((byte*)state.pbData, state.cbData), chars, out _, out _, replaceInvalidSequences: false);
                    Debug.Assert(status == OperationStatus.Done, "Did somebody mutate this Utf8String instance unexpectedly?");
                });
#else
                char[] buffer = ArrayPool<char>.Shared.Rent(utf16CharCount);
                try
                {
                    fixed (char* pBuffer = buffer)
                    {
                        Encoding.UTF8.GetChars(pData, Length, pBuffer, utf16CharCount);
                        return new string(pBuffer, 0, utf16CharCount);
                    }
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(buffer);
                }
#endif
            }
        }

        public Utf8String ToUtf8String()
        {
            // TODO_UTF8STRING: Since we know the underlying data is immutable, well-formed UTF-8,
            // we can perform transcoding using an optimized code path that skips all safety checks.

            return Utf8String.UnsafeCreateWithoutValidation(Bytes);
        }

        /// <summary>
        /// Wraps a <see cref="Utf8Span"/> instance around the provided <paramref name="buffer"/>,
        /// skipping validation of the input data.
        /// </summary>
        /// <remarks>
        /// Callers must uphold the following two invariants:
        ///
        /// (a) <paramref name="buffer"/> consists only of well-formed UTF-8 data and does
        ///     not contain invalid or incomplete UTF-8 subsequences; and
        /// (b) the contents of <paramref name="buffer"/> will not change for the duration
        ///     of the returned <see cref="Utf8Span"/>'s existence.
        ///
        /// If these invariants are not maintained, the runtime may exhibit undefined behavior.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Utf8Span UnsafeCreateWithoutValidation(ReadOnlySpan<byte> buffer)
        {
            return new Utf8Span(buffer);
        }
    }
}
