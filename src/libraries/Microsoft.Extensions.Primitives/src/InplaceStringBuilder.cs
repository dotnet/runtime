// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Primitives
{
    /// <summary>
    /// Provides a mechanism for fast, non-allocating string concatenation.
    /// </summary>
    [DebuggerDisplay("Value = {_value}")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("This type is retained only for compatibility. The recommended alternative is string.Create<TState> (int length, TState state, System.Buffers.SpanAction<char,TState> action).", error: true)]
    public struct InplaceStringBuilder
    {
        private int _offset;
        private int _capacity;
        private string? _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="InplaceStringBuilder"/> class.
        /// </summary>
        /// <param name="capacity">The suggested starting size of the <see cref="InplaceStringBuilder"/> instance.</param>
        public InplaceStringBuilder(int capacity) : this()
        {
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity);
            }

            _capacity = capacity;
        }

        /// <summary>
        /// Gets the number of characters that the current <see cref="InplaceStringBuilder"/> object can contain.
        /// </summary>
        public int Capacity
        {
            get => _capacity;
            set
            {
                if (value < 0)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
                }

                // _offset > 0 indicates writing state
                if (_offset > 0)
                {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.Capacity_CannotChangeAfterWriteStarted);
                }

                _capacity = value;
            }
        }

        /// <summary>
        /// Appends a string to the end of the current <see cref="InplaceStringBuilder"/> instance.
        /// </summary>
        /// <param name="value">The string to append.</param>
        public void Append(string? value)
        {
            if (value == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            Append(value, 0, value.Length);
        }

        /// <summary>
        /// Appends a string segment to the end of the current <see cref="InplaceStringBuilder"/> instance.
        /// </summary>
        /// <param name="segment">The string segment to append.</param>
        public void Append(StringSegment segment)
        {
            Append(segment.Buffer, segment.Offset, segment.Length);
        }

        /// <summary>
        /// Appends a substring to the end of the current <see cref="InplaceStringBuilder"/> instance.
        /// </summary>
        /// <param name="value">The string that contains the substring to append.</param>
        /// <param name="offset">The starting position of the substring within value.</param>
        /// <param name="count">The number of characters in value to append.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Append(string? value, int offset, int count)
        {
            EnsureValueIsInitialized();

            if (value == null
                || offset < 0
                || value.Length - offset < count
                || Capacity - _offset < count)
            {
                ThrowValidationError(value, offset, count);
            }

            fixed (char* destination = _value)
            fixed (char* source = value)
            {
                Unsafe.CopyBlockUnaligned(destination + _offset, source + offset, (uint)count * 2);
                _offset += count;
            }
        }

        /// <summary>
        /// Appends a character to the end of the current <see cref="InplaceStringBuilder"/> instance.
        /// </summary>
        /// <param name="c">The character to append.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Append(char c)
        {
            EnsureValueIsInitialized();

            if (_offset >= Capacity)
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.Capacity_NotEnough, 1, Capacity - _offset);
            }

            fixed (char* destination = _value)
            {
                destination[_offset++] = c;
            }
        }

        /// <summary>
        /// Converts the value of this instance to a String.
        /// </summary>
        /// <returns>A string whose value is the same as this instance.</returns>
        public override string? ToString()
        {
            if (Capacity != _offset)
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.Capacity_NotUsedEntirely, Capacity, _offset);
            }

            return _value;
        }

        private void EnsureValueIsInitialized()
        {
            _value ??= new string('\0', _capacity);
        }

        private void ThrowValidationError(string? value, int offset, int count)
        {
            if (value == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            if (offset < 0 || value.Length - offset < count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.offset);
            }

            if (Capacity - _offset < count)
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.Capacity_NotEnough, value.Length, Capacity - _offset);
            }
        }
    }
}
