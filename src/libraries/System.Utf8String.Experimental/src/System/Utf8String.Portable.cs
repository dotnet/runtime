// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public sealed partial class Utf8String
    {
        private static ReadOnlySpan<byte> s_EmptyRef => new byte[] { 0x00 };
        private readonly byte[] _bytes;

        /// <summary>
        /// Returns the length (in UTF-8 code units, or <see cref="byte"/>s) of this instance.
        /// </summary>
        public int Length => _bytes.Length == 0 ? 0 : _bytes.Length - 1; // -1 because the bytes are always null-terminated

        public Utf8String(ReadOnlySpan<byte> value)
        {
            _bytes = Array.Empty<byte>(); //TODO: eerhardt
        }

        public Utf8String(byte[] value, int startIndex, int length)
        {
            _bytes = Array.Empty<byte>(); //TODO: eerhardt
        }

        [CLSCompliant(false)]
        public unsafe Utf8String(byte* value)
        {
            _bytes = Array.Empty<byte>(); //TODO: eerhardt
        }

        public Utf8String(ReadOnlySpan<char> value)
        {
            _bytes = Array.Empty<byte>(); //TODO: eerhardt
        }

        public Utf8String(char[] value, int startIndex, int length)
        {
            _bytes = Array.Empty<byte>(); //TODO: eerhardt
        }

        [CLSCompliant(false)]
        public unsafe Utf8String(char* value)
        {
            _bytes = Array.Empty<byte>(); //TODO: eerhardt
        }

        public Utf8String(string value)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            _bytes = CreateBufferFromUtf16Common(value.AsSpan(), replaceInvalidSequences: false);

            if (_bytes is null)
            {
                // Input buffer contained invalid UTF-16 data.

                throw new ArgumentException(
                    message: SR.Utf8String_InputContainedMalformedUtf16,
                    paramName: nameof(value));
            }
        }

        private Utf8String(byte[] bytes)
        {
            _bytes = bytes;
        }

        /// <summary>
        /// Returns a <em>mutable</em> reference to the first byte of this <see cref="Utf8String"/>
        /// (or the null terminator if the string is empty).
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref byte DangerousGetMutableReference() =>
            ref MemoryMarshal.GetReference(Length > 0 ? _bytes.AsSpan() : s_EmptyRef);

        /// <summary>
        /// Returns a <em>mutable</em> <see cref="Span{Byte}"/> that can be used to populate this
        /// <see cref="Utf8String"/> instance. Only to be used during construction.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Span<byte> DangerousGetMutableSpan()
        {
            Debug.Assert(Length > 0, $"This should only ever be called on a non-empty {nameof(Utf8String)}.");
            return _bytes.AsSpan(0, Length);
        }

        /// <summary>
        /// Returns a <see cref="ReadOnlySpan{Byte}"/> for this
        /// <see cref="Utf8String"/> instance.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpan<byte> GetSpan() => Length > 0 ? _bytes.AsSpan(0, Length) : s_EmptyRef.Slice(0, 0);

        /// <summary>
        /// Gets an immutable reference that can be used in a <see langword="fixed"/> statement. The resulting
        /// reference can be pinned and used as a null-terminated <em>LPCUTF8STR</em>.
        /// </summary>
        /// <remarks>
        /// If this <see cref="Utf8String"/> instance is empty, returns a reference to the null terminator.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)] // for compiler use only
        public ref readonly byte GetPinnableReference() => ref _bytes.AsSpan().GetPinnableReference();

        /// <summary>
        /// Similar to <see cref="Utf8Extensions.AsBytes(Utf8String)"/>, but skips the null check on the input.
        /// Throws a <see cref="NullReferenceException"/> if the input is null.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpan<byte> AsBytesSkipNullCheck() => GetSpan();

        /// <summary>
        /// Creates a new zero-initialized instance of the specified length. Actual storage allocated is "length + 1" bytes
        /// because instances are null-terminated.
        /// </summary>
        /// <remarks>
        /// The implementation of this method checks its input argument for overflow.
        /// </remarks>
        private static Utf8String FastAllocate(int length)
        {
            // just simulate a "fast allocate", since this is portable
            return new Utf8String(new byte[length + 1]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlyMemory<byte> CreateMemoryBytes(int start, int length) =>
            _bytes.AsMemory(start, length);
    }
}
