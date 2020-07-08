// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Unicode;
using Internal.Runtime.CompilerServices;

namespace System
{
    public sealed partial class Utf8String
    {
        /*
         * INSTANCE FIELDS
         * Do not reorder these fields. They must match the layout of Utf8StringObject in object.h.
         */

        private readonly int _length;
        private readonly byte _firstByte;

        /*
         * INSTANCE PROPERTIES
         */

        /// <summary>
        /// Returns the length (in UTF-8 code units, or <see cref="byte"/>s) of this instance.
        /// </summary>
        public int Length => _length;

        /*
         * CONSTRUCTORS
         *
         * Defining a new constructor for string-like types (like Utf8String) requires changes both
         * to the managed code below and to the native VM code. See the comment at the top of
         * src/vm/ecall.cpp for instructions on how to add new overloads.
         *
         * These ctors validate their input, throwing ArgumentException if the input does not represent
         * well-formed UTF-8 data. (In the case of transcoding ctors, the ctors throw if the input does
         * not represent well-formed UTF-16 data.) There are Create* factory methods which allow the caller
         * to control this behavior with finer granularity, including performing U+FFFD replacement instead
         * of throwing, or even suppressing validation altogether if the caller knows the input to be well-
         * formed.
         *
         * The reason a throwing behavior was chosen by default is that we don't want to surprise developers
         * if ill-formed data loses fidelity while being round-tripped through this type. Developers should
         * perform an explicit gesture to opt-in to lossy behavior, such as calling the factories explicitly
         * documented as performing such replacement.
         */

        /// <summary>
        /// Creates a <see cref="Utf8String"/> instance from existing UTF-8 data.
        /// </summary>
        /// <param name="value">The existing UTF-8 data from which to create a new <see cref="Utf8String"/>.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="value"/> does not represent well-formed UTF-8 data.
        /// </exception>
        /// <remarks>
        /// The UTF-8 data in <paramref name="value"/> is validated for well-formedness upon construction,
        /// and an exception is thrown if the input is ill-formed. To avoid this exception, consider using
        /// <see cref="TryCreateFrom(ReadOnlySpan{byte}, out Utf8String)"/> or <see cref="CreateFromRelaxed(ReadOnlySpan{byte})"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern Utf8String(ReadOnlySpan<byte> value);

#if !CORECLR
        static
#endif
        private Utf8String Ctor(ReadOnlySpan<byte> value)
        {
            if (value.IsEmpty)
            {
                return Empty;
            }

            // Create and populate the Utf8String instance.

            Utf8String newString = FastAllocateSkipZeroInit(value.Length);
            Buffer.Memmove(ref newString.DangerousGetMutableReference(), ref MemoryMarshal.GetReference(value), (uint)value.Length);

            // Now perform validation.
            // Reminder: Perform validation over the copy, not over the source.

            if (!Utf8Utility.IsWellFormedUtf8(newString.AsBytes()))
            {
                throw new ArgumentException(
                    message: SR.Utf8String_InputContainedMalformedUtf8,
                    paramName: nameof(value));
            }

            return newString;
        }

        /// <summary>
        /// Creates a <see cref="Utf8String"/> instance from existing UTF-8 data.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="value"/> does not represent well-formed UTF-8 data.
        /// </exception>
        /// <remarks>
        /// The UTF-8 data in <paramref name="value"/> is validated for well-formedness upon construction,
        /// and an exception is thrown if the input is ill-formed. To avoid this exception, consider using
        /// <see cref="TryCreateFrom(ReadOnlySpan{byte}, out Utf8String)"/> or <see cref="CreateFromRelaxed(ReadOnlySpan{byte})"/>.
        /// </remarks>

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern Utf8String(byte[] value, int startIndex, int length);

#if !CORECLR
        static
#endif
        private Utf8String Ctor(byte[] value, int startIndex, int length)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            return Ctor(new ReadOnlySpan<byte>(value, startIndex, length));
        }

        /// <summary>
        /// Creates a <see cref="Utf8String"/> instance from existing null-terminated UTF-8 data.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="value"/> does not represent well-formed UTF-8 data.
        /// </exception>
        /// <remarks>
        /// The UTF-8 data in <paramref name="value"/> is validated for well-formedness upon construction,
        /// and an exception is thrown if the input is ill-formed. To avoid this exception, consider using
        /// <see cref="TryCreateFrom(ReadOnlySpan{byte}, out Utf8String)"/> or <see cref="CreateFromRelaxed(ReadOnlySpan{byte})"/>.
        /// </remarks>

        [MethodImpl(MethodImplOptions.InternalCall)]
        [CLSCompliant(false)]
        public extern unsafe Utf8String(byte* value);

#if !CORECLR
        static
#endif
        private unsafe Utf8String Ctor(byte* value)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            return Ctor(new ReadOnlySpan<byte>(value, string.strlen(value)));
        }

        /// <summary>
        /// Creates a <see cref="Utf8String"/> instance from existing UTF-16 data, transcoding the
        /// existing data to UTF-8 upon creation.
        /// </summary>
        /// <param name="value">The existing UTF-16 data from which to create a new <see cref="Utf8String"/>.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="value"/> does not represent well-formed UTF-16 data.
        /// </exception>
        /// <remarks>
        /// The UTF-16 data in <paramref name="value"/> is validated for well-formedness upon construction,
        /// and an exception is thrown if the input is ill-formed. To avoid this exception, consider using
        /// <see cref="TryCreateFrom(ReadOnlySpan{char}, out Utf8String)"/> or <see cref="CreateFromRelaxed(ReadOnlySpan{char})"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern Utf8String(ReadOnlySpan<char> value);

#if !CORECLR
        static
#endif
        private Utf8String Ctor(ReadOnlySpan<char> value)
        {
            Utf8String? newString = CreateFromUtf16Common(value, replaceInvalidSequences: false);

            if (newString is null)
            {
                // Input buffer contained invalid UTF-16 data.

                throw new ArgumentException(
                    message: SR.Utf8String_InputContainedMalformedUtf16,
                    paramName: nameof(value));
            }

            return newString;
        }

        /// <summary>
        /// Creates a <see cref="Utf8String"/> instance from existing UTF-16 data.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="value"/> does not represent well-formed UTF-16 data.
        /// </exception>
        /// <remarks>
        /// The UTF-16 data in <paramref name="value"/> is validated for well-formedness upon construction,
        /// and an exception is thrown if the input is ill-formed. To avoid this exception, consider using
        /// <see cref="TryCreateFrom(ReadOnlySpan{char}, out Utf8String)"/> or <see cref="CreateFromRelaxed(ReadOnlySpan{char})"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern Utf8String(char[] value, int startIndex, int length);

#if !CORECLR
        static
#endif
        private Utf8String Ctor(char[] value, int startIndex, int length)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            return Ctor(new ReadOnlySpan<char>(value, startIndex, length));
        }

        /// <summary>
        /// Creates a <see cref="Utf8String"/> instance from existing null-terminated UTF-16 data.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="value"/> does not represent well-formed UTF-16 data.
        /// </exception>
        /// <remarks>
        /// The UTF-16 data in <paramref name="value"/> is validated for well-formedness upon construction,
        /// and an exception is thrown if the input is ill-formed. To avoid this exception, consider using
        /// <see cref="TryCreateFrom(ReadOnlySpan{char}, out Utf8String)"/> or <see cref="CreateFromRelaxed(ReadOnlySpan{char})"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.InternalCall)]
        [CLSCompliant(false)]
        public extern unsafe Utf8String(char* value);

#if !CORECLR
        static
#endif
        private unsafe Utf8String Ctor(char* value)
        {
            if (value == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            return Ctor(new ReadOnlySpan<char>(value, string.wcslen(value)));
        }

        /// <summary>
        /// Creates a <see cref="Utf8String"/> instance from existing UTF-16 data.
        /// </summary>
        /// <remarks>
        /// The UTF-16 data in <paramref name="value"/> is validated for well-formedness upon construction,
        /// and an exception is thrown if the input is ill-formed. To avoid this exception, consider using
        /// <see cref="TryCreateFrom(ReadOnlySpan{char}, out Utf8String)"/> or <see cref="CreateFromRelaxed(ReadOnlySpan{char})"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern Utf8String(string value);

#if !CORECLR
        static
#endif
        private Utf8String Ctor(string value)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            return Ctor(value.AsSpan());
        }

        /*
         * METHODS
         */

        /// <summary>
        /// Similar to <see cref="Utf8Extensions.AsBytes(Utf8String)"/>, but skips the null check on the input.
        /// Throws a <see cref="NullReferenceException"/> if the input is null.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpan<byte> AsBytesSkipNullCheck()
        {
            // By dereferencing Length first, the JIT will skip the null check that normally precedes
            // most instance method calls, and it'll use the field dereference as the null check.

            int length = Length;
            return new ReadOnlySpan<byte>(ref DangerousGetMutableReference(), length);
        }

        /// <summary>
        /// Returns a <em>mutable</em> <see cref="Span{Byte}"/> that can be used to populate this
        /// <see cref="Utf8String"/> instance. Only to be used during construction.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Span<byte> DangerousGetMutableSpan()
        {
            // By dereferencing Length first, the JIT will skip the null check that normally precedes
            // most instance method calls, and it'll use the field dereference as the null check.

            int length = Length;
            return new Span<byte>(ref DangerousGetMutableReference(), length);
        }

        /// <summary>
        /// Returns a <em>mutable</em> reference to the first byte of this <see cref="Utf8String"/>
        /// (or the null terminator if the string is empty).
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref byte DangerousGetMutableReference() => ref Unsafe.AsRef(in _firstByte);

        /// <summary>
        /// Gets an immutable reference that can be used in a <see langword="fixed"/> statement. The resulting
        /// reference can be pinned and used as a null-terminated <em>LPCUTF8STR</em>.
        /// </summary>
        /// <remarks>
        /// If this <see cref="Utf8String"/> instance is empty, returns a reference to the null terminator.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)] // for compiler use only
        public ref readonly byte GetPinnableReference() => ref _firstByte;

        /*
         * HELPER METHODS
         */

        /// <summary>
        /// Creates a new zero-initialized instance of the specified length. Actual storage allocated is "length + 1" bytes
        /// because instances are null-terminated.
        /// </summary>
        /// <remarks>
        /// The implementation of this method checks its input argument for overflow.
        /// </remarks>
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern Utf8String FastAllocate(int length);
    }
}
