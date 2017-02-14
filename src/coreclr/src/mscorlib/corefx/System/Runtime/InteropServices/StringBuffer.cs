// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Buffer that deals in char size increments. Dispose to free memory. Always makes ordinal
    /// comparisons. Not thread safe.
    ///
    /// A more performant replacement for StringBuilder when performing native interop.
    /// 
    /// "No copy" valuetype. Has to be passed as "ref".
    /// 
    /// </summary>
    /// <remarks>
    /// Suggested use through P/Invoke: define DllImport arguments that take a character buffer as SafeHandle and pass StringBuffer.GetHandle().
    /// </remarks>
    internal struct StringBuffer
    {
        private char[] _buffer;
        private int _length;

        /// <summary>
        /// Instantiate the buffer with capacity for at least the specified number of characters. Capacity
        /// includes the trailing null character.
        /// </summary>
        public StringBuffer(int initialCapacity)
        {
            _buffer = ArrayPool<char>.Shared.Rent(initialCapacity);
            _length = 0;
        }

        /// <summary>
        /// Get/set the character at the given index.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if attempting to index outside of the buffer length.</exception>
        public char this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (index >= _length) throw new ArgumentOutOfRangeException(nameof(index));
                return _buffer[index];
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (index >= _length) throw new ArgumentOutOfRangeException(nameof(index));
                _buffer[index] = value;
            }
        }

        /// <summary>
        /// Underlying storage of the buffer. Used for interop.
        /// </summary>
        public char[] UnderlyingArray => _buffer;

        /// <summary>
        /// Character capacity of the buffer. Includes the count for the trailing null character.
        /// </summary>
        public int Capacity => _buffer.Length;

        /// <summary>
        /// Ensure capacity in characters is at least the given minimum.
        /// </summary>
        /// <exception cref="OutOfMemoryException">Thrown if unable to allocate memory when setting.</exception>
        public void EnsureCapacity(int minCapacity)
        {
            if (minCapacity > Capacity)
            {
                char[] oldBuffer = _buffer;
                _buffer = ArrayPool<char>.Shared.Rent(minCapacity);
                Array.Copy(oldBuffer, 0, _buffer, 0, oldBuffer.Length);
                ArrayPool<char>.Shared.Return(oldBuffer);
            }
        }

        /// <summary>
        /// The logical length of the buffer in characters. (Does not include the final null.) Will automatically attempt to increase capacity.
        /// This is where the usable data ends.
        /// </summary>
        /// <exception cref="OutOfMemoryException">Thrown if unable to allocate memory when setting.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the set size in bytes is int.MaxValue (as space is implicitly reserved for the trailing null).</exception>
        public int Length
        {
            get { return _length; }
            set
            {
                // Null terminate
                EnsureCapacity(checked(value + 1));
                _buffer[value] = '\0';

                _length = value;
            }
        }

        /// <summary>
        /// True if the buffer contains the given character.
        /// </summary>
        public unsafe bool Contains(char value)
        {
            fixed (char* start = _buffer)
            {
                int length = _length;
                for (int i = 0; i < length; i++)
                {
                    if (start[i] == value) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the buffer starts with the given string.
        /// </summary>
        public bool StartsWith(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (_length < value.Length) return false;
            return SubstringEquals(value, startIndex: 0, count: value.Length);
        }

        /// <summary>
        /// Returns true if the specified StringBuffer substring equals the given value.
        /// </summary>
        /// <param name="value">The value to compare against the specified substring.</param>
        /// <param name="startIndex">Start index of the sub string.</param>
        /// <param name="count">Length of the substring, or -1 to check all remaining.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="startIndex"/> or <paramref name="count"/> are outside the range
        /// of the buffer's length.
        /// </exception>
        public unsafe bool SubstringEquals(string value, int startIndex = 0, int count = -1)
        {
            if (value == null) return false;
            if (count < -1) throw new ArgumentOutOfRangeException(nameof(count));
            if (startIndex > _length) throw new ArgumentOutOfRangeException(nameof(startIndex));

            int realCount = count == -1 ? _length - startIndex : (int)count;
            if (checked(startIndex + realCount) > _length) throw new ArgumentOutOfRangeException(nameof(count));

            int length = value.Length;

            // Check the substring length against the input length
            if (realCount != length) return false;

            fixed (char* valueStart = value)
            fixed (char* bufferStart = _buffer)
            {
                char* subStringStart = bufferStart + startIndex;

                for (int i = 0; i < length; i++)
                {
                    if (subStringStart[i] != valueStart[i]) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Append the given buffer.
        /// </summary>
        /// <param name="value">The buffer to append.</param>
        /// <param name="startIndex">The index in the input buffer to start appending from.</param>
        /// <param name="count">The count of characters to copy from the buffer string.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="startIndex"/> or <paramref name="count"/> are outside the range
        /// of <paramref name="value"/> characters.
        /// </exception>
        public void Append(ref StringBuffer value, int startIndex = 0)
        {
            if (value.Length == 0) return;

            value.CopyTo(
                bufferIndex: startIndex,
                destination: ref this,
                destinationIndex: _length,
                count: value.Length);
        }

        /// <summary>
        /// Append the given buffer.
        /// </summary>
        /// <param name="value">The buffer to append.</param>
        /// <param name="startIndex">The index in the input buffer to start appending from.</param>
        /// <param name="count">The count of characters to copy from the buffer string.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="startIndex"/> or <paramref name="count"/> are outside the range
        /// of <paramref name="value"/> characters.
        /// </exception>
        public void Append(ref StringBuffer value, int startIndex, int count)
        {
            if (count == 0) return;

            value.CopyTo(
                bufferIndex: startIndex,
                destination: ref this,
                destinationIndex: _length,
                count: count);
        }

        /// <summary>
        /// Copy contents to the specified buffer. Destination index must be within current destination length.
        /// Will grow the destination buffer if needed.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="bufferIndex"/> or <paramref name="destinationIndex"/> or <paramref name="count"/> are outside the range
        /// of <paramref name="value"/> characters.
        /// </exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="destination"/> is null.</exception>
        public void CopyTo(int bufferIndex, ref StringBuffer destination, int destinationIndex, int count)
        {
            if (destinationIndex > destination._length) throw new ArgumentOutOfRangeException(nameof(destinationIndex));
            if (bufferIndex >= _length) throw new ArgumentOutOfRangeException(nameof(bufferIndex));
            if (_length < checked(bufferIndex + count)) throw new ArgumentOutOfRangeException(nameof(count));

            if (count == 0) return;
            int lastIndex = checked(destinationIndex + count);
            if (destination.Length < lastIndex) destination.Length = lastIndex;

            Array.Copy(UnderlyingArray, bufferIndex, destination.UnderlyingArray, destinationIndex, count);
        }

        /// <summary>
        /// Copy contents from the specified string into the buffer at the given index. Start index must be within the current length of
        /// the buffer, will grow as necessary.
        /// </summary>
        public void CopyFrom(int bufferIndex, string source, int sourceIndex = 0, int count = -1)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (bufferIndex > _length) throw new ArgumentOutOfRangeException(nameof(bufferIndex));
            if (sourceIndex < 0 || sourceIndex > source.Length) throw new ArgumentOutOfRangeException(nameof(sourceIndex));
            if (count == -1) count = source.Length - sourceIndex;
            if (count < 0 || source.Length - count < sourceIndex) throw new ArgumentOutOfRangeException(nameof(count));

            if (count == 0) return;
            int lastIndex = bufferIndex + (int)count;
            if (_length < lastIndex) Length = lastIndex;

            source.CopyTo(sourceIndex, UnderlyingArray, bufferIndex, count);
        }

        /// <summary>
        /// Trim the specified values from the end of the buffer. If nothing is specified, nothing is trimmed.
        /// </summary>
        public void TrimEnd(char[] values)
        {
            if (values == null || values.Length == 0 || _length == 0) return;

            while (_length > 0 && Array.IndexOf(values, _buffer[_length - 1]) >= 0)
            {
                Length = _length - 1;
            }
        }

        /// <summary>
        /// String representation of the entire buffer. If the buffer is larger than the maximum size string (int.MaxValue) this will throw.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the buffer is too big to fit into a string.</exception>
        public override string ToString()
        {
            return new string(_buffer, startIndex: 0, length: _length);
        }

        /// <summary>
        /// Get the given substring in the buffer.
        /// </summary>
        /// <param name="count">Count of characters to take, or remaining characters from <paramref name="startIndex"/> if -1.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="startIndex"/> or <paramref name="count"/> are outside the range of the buffer's length
        /// or count is greater than the maximum string size (int.MaxValue).
        /// </exception>
        public string Substring(int startIndex, int count = -1)
        {
            if (startIndex > (_length == 0 ? 0 : _length - 1)) throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (count < -1) throw new ArgumentOutOfRangeException(nameof(count));

            int realCount = count == -1 ? _length - startIndex : (int)count;
            if (realCount > int.MaxValue || checked(startIndex + realCount) > _length) throw new ArgumentOutOfRangeException(nameof(count));

            // The buffer could be bigger than will fit into a string, but the substring might fit. As the starting
            // index might be bigger than int we need to index ourselves.
            return new string(_buffer, startIndex: startIndex, length: realCount);
        }

        public void Free()
        {
            ArrayPool<char>.Shared.Return(_buffer);
            _buffer = null;
            _length = 0;
        }
    }
}
