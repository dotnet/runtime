// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Unicode;

namespace System
{
    public sealed partial class Utf8String
    {
        private readonly byte[] _bytes;

        /// <summary>
        /// Returns the length (in UTF-8 code units, or <see cref="byte"/>s) of this instance.
        /// </summary>
        public int Length => _bytes.Length - 1; // -1 because the bytes are always null-terminated

        public Utf8String(ReadOnlySpan<byte> value)
        {
            _bytes = InitializeBuffer(value);
        }

        public Utf8String(byte[] value, int startIndex, int length)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            _bytes = InitializeBuffer(new ReadOnlySpan<byte>(value, startIndex, length));
        }

        [CLSCompliant(false)]
        public unsafe Utf8String(byte* value)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            _bytes = InitializeBuffer(new ReadOnlySpan<byte>(value, strlen(value)));
        }

        public Utf8String(ReadOnlySpan<char> value)
        {
            _bytes = InitializeBuffer(value);
        }

        public Utf8String(char[] value, int startIndex, int length)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            _bytes = InitializeBuffer(new ReadOnlySpan<char>(value, startIndex, length));
        }

        [CLSCompliant(false)]
        public unsafe Utf8String(char* value)
        {
            if (value == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            _bytes = InitializeBuffer(new ReadOnlySpan<char>(value, wcslen(value)));
        }

        public Utf8String(string value)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            _bytes = InitializeBuffer(value.AsSpan());
        }

        private static byte[] InitializeBuffer(ReadOnlySpan<byte> value)
        {
            if (value.IsEmpty)
            {
                return Empty._bytes;
            }

            // Create and populate the Utf8String buffer.

            byte[] newBuffer = AllocateBuffer(value.Length);
            value.CopyTo(newBuffer);

            // Now perform validation.
            // Reminder: Perform validation over the copy, not over the source.

            if (!Utf8Utility.IsWellFormedUtf8(newBuffer))
            {
                throw new ArgumentException(
                    message: SR.Utf8String_InputContainedMalformedUtf8,
                    paramName: nameof(value));
            }

            return newBuffer;
        }

        private static byte[] InitializeBuffer(ReadOnlySpan<char> value)
        {
            byte[]? newBuffer = CreateBufferFromUtf16Common(value, replaceInvalidSequences: false);

            if (newBuffer is null)
            {
                // Input buffer contained invalid UTF-16 data.

                throw new ArgumentException(
                    message: SR.Utf8String_InputContainedMalformedUtf16,
                    paramName: nameof(value));
            }

            return newBuffer;
        }

        // This should only be called from FastAllocate
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
            ref MemoryMarshal.GetReference(_bytes.AsSpan());

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
        internal ReadOnlySpan<byte> GetSpan() => _bytes.AsSpan(0, Length);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlyMemory<byte> CreateMemoryBytes(int start, int length) =>
            _bytes.AsMemory(start, length);

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
            return new Utf8String(AllocateBuffer(length));
        }

        private static byte[] AllocateBuffer(int length)
        {
            Debug.Assert(length > 0);

            if (length == int.MaxValue)
            {
                // Ensure we don't overflow below. The VM will throw an OutOfMemoryException
                // if we try to create a byte[] this large anyway.
                length = int.MaxValue - 1;
            }

            // Actual storage allocated is "length + 1" bytes because instances are null-terminated.
            return new byte[length + 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe int wcslen(char* ptr)
        {
            // IndexOf processes memory in aligned chunks, and thus it won't crash even if it accesses memory beyond the null terminator.
            int length = new ReadOnlySpan<char>(ptr, int.MaxValue).IndexOf('\0');
            if (length < 0)
            {
                ThrowMustBeNullTerminatedString();
            }

            return length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int strlen(byte* ptr)
        {
            // IndexOf processes memory in aligned chunks, and thus it won't crash even if it accesses memory beyond the null terminator.
            int length = new ReadOnlySpan<byte>(ptr, int.MaxValue).IndexOf((byte)'\0');
            if (length < 0)
            {
                ThrowMustBeNullTerminatedString();
            }

            return length;
        }

        [DoesNotReturn]
        private static void ThrowMustBeNullTerminatedString()
        {
            throw new ArgumentException(SR.Arg_MustBeNullTerminatedString);
        }
    }
}
