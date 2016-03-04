// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Native buffer that deals in char size increments. Dispose to free memory. Allows buffers larger
    /// than a maximum size string to enable working with very large string arrays.  Always makes ordinal
    /// comparisons.
    ///
    /// A more performant replacement for StringBuilder when performing native interop.
    /// </summary>
    /// <remarks>
    /// Suggested use through P/Invoke: define DllImport arguments that take a character buffer as SafeHandle and pass StringBuffer.GetHandle().
    /// </remarks>
    internal class StringBuffer : NativeBuffer
    {
        private uint _length;

        /// <summary>
        /// Instantiate the buffer with capacity for at least the specified number of characters. Capacity
        /// includes the trailing null character.
        /// </summary>
        public StringBuffer(uint initialCapacity = 0)
            : base(initialCapacity * (ulong)sizeof(char))
        {
        }

        /// <summary>
        /// Instantiate the buffer with a copy of the specified string.
        /// </summary>
        public unsafe StringBuffer(string initialContents)
            : base(0)
        {
            // We don't pass the count of bytes to the base constructor, appending will
            // initialize to the correct size for the specified initial contents.
            if (initialContents != null)
            {
                Append(initialContents);
            }
        }

        /// <summary>
        /// Get/set the character at the given index.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if attempting to index outside of the buffer length.</exception>
        public unsafe char this[uint index]
        {
            get
            {
                if (index >= _length) throw new ArgumentOutOfRangeException(nameof(index));
                return CharPointer[index];
            }
            set
            {
                if (index >= _length) throw new ArgumentOutOfRangeException(nameof(index));
                CharPointer[index] = value;
            }
        }

        /// <summary>
        /// Character capacity of the buffer. Includes the count for the trailing null character.
        /// </summary>
        public uint CharCapacity
        {
            get
            {
                ulong byteCapacity = ByteCapacity;
                ulong charCapacity = byteCapacity == 0 ? 0 : byteCapacity / sizeof(char);
                return charCapacity > uint.MaxValue ? uint.MaxValue : (uint)charCapacity;
            }
        }

        /// <summary>
        /// Ensure capacity in characters is at least the given minimum.
        /// </summary>
        /// <exception cref="OutOfMemoryException">Thrown if unable to allocate memory when setting.</exception>
        public void EnsureCharCapacity(uint minCapacity)
        {
            EnsureByteCapacity(minCapacity * (ulong)sizeof(char));
        }

        /// <summary>
        /// The logical length of the buffer in characters. (Does not include the final null.) Will automatically attempt to increase capacity.
        /// This is where the usable data ends.
        /// </summary>
        /// <exception cref="OutOfMemoryException">Thrown if unable to allocate memory when setting.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the set size in bytes is uint.MaxValue (as space is implicitly reservced for the trailing null).</exception>
        public unsafe uint Length
        {
            get { return _length; }
            set
            {
                if (value == uint.MaxValue) throw new ArgumentOutOfRangeException("Length");

                // Null terminate
                EnsureCharCapacity(value + 1);
                CharPointer[value] = '\0';

                _length = value;
            }
        }

        /// <summary>
        /// For use when the native api null terminates but doesn't return a length.
        /// If no null is found, the length will not be changed.
        /// </summary>
        public unsafe void SetLengthToFirstNull()
        {
            char* buffer = CharPointer;
            uint capacity = CharCapacity;
            for (uint i = 0; i < capacity; i++)
            {
                if (buffer[i] == '\0')
                {
                    _length = i;
                    break;
                }
            }
        }

        internal unsafe char* CharPointer
        {
            get
            {
                return (char*)VoidPointer;
            }
        }

        /// <summary>
        /// Returns true if the buffer starts with the given string.
        /// </summary>
        public bool StartsWith(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (_length < (uint)value.Length) return false;
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
        public unsafe bool SubstringEquals(string value, uint startIndex = 0, int count = -1)
        {
            if (value == null) return false;
            if (count < -1) throw new ArgumentOutOfRangeException(nameof(count));
            if (startIndex > _length) throw new ArgumentOutOfRangeException(nameof(startIndex));

            uint realCount = count == -1 ? _length - startIndex : (uint)count;
            if (checked(startIndex + realCount) > _length) throw new ArgumentOutOfRangeException(nameof(count));

            int length = value.Length;

            // Check the substring length against the input length
            if (realCount != (uint)length) return false;

            fixed (char* valueStart = value)
            {
                char* bufferStart = CharPointer + startIndex;
                for (int i = 0; i < length; i++)
                {
                    // Note that indexing in this case generates faster code than trying to copy the pointer and increment it
                    if (*bufferStart++ != valueStart[i]) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Append the given string.
        /// </summary>
        /// <param name="value">The string to append.</param>
        /// <param name="startIndex">The index in the input string to start appending from.</param>
        /// <param name="count">The count of characters to copy from the input string, or -1 for all remaining.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="startIndex"/> or <paramref name="count"/> are outside the range
        /// of <paramref name="value"/> characters.
        /// </exception>
        public void Append(string value, int startIndex = 0, int count = -1)
        {
            CopyFrom(
                bufferIndex: _length,
                source: value,
                sourceIndex: startIndex,
                count: count);
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
        public void Append(StringBuffer value, uint startIndex = 0)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (value.Length == 0) return;

            value.CopyTo(
                bufferIndex: startIndex,
                destination: this,
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
        public void Append(StringBuffer value, uint startIndex, uint count)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (count == 0) return;

            value.CopyTo(
                bufferIndex: startIndex,
                destination: this,
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
        public unsafe void CopyTo(uint bufferIndex, StringBuffer destination, uint destinationIndex, uint count)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (destinationIndex > destination._length) throw new ArgumentOutOfRangeException(nameof(destinationIndex));
            if (bufferIndex >= _length) throw new ArgumentOutOfRangeException(nameof(bufferIndex));
            if (_length < checked(bufferIndex + count)) throw new ArgumentOutOfRangeException(nameof(count));

            if (count == 0) return;
            uint lastIndex = checked(destinationIndex + count);
            if (destination._length < lastIndex) destination.Length = lastIndex;

            Buffer.MemoryCopy(
                source: CharPointer + bufferIndex,
                destination: destination.CharPointer + destinationIndex,
                destinationSizeInBytes: checked((long)(destination.ByteCapacity - (destinationIndex * sizeof(char)))),
                sourceBytesToCopy: checked((long)count * sizeof(char)));
        }

        /// <summary>
        /// Copy contents from the specified string into the buffer at the given index. Start index must be within the current length of
        /// the buffer, will grow as necessary.
        /// </summary>
        public unsafe void CopyFrom(uint bufferIndex, string source, int sourceIndex = 0, int count = -1)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (bufferIndex > _length) throw new ArgumentOutOfRangeException(nameof(bufferIndex));
            if (sourceIndex < 0 || sourceIndex > source.Length) throw new ArgumentOutOfRangeException(nameof(sourceIndex));
            if (count == -1) count = source.Length - sourceIndex;
            if (count < 0 || source.Length - count < sourceIndex) throw new ArgumentOutOfRangeException(nameof(count));

            if (count == 0) return;
            uint lastIndex = bufferIndex + (uint)count;
            if (_length < lastIndex) Length = lastIndex;

            fixed (char* content = source)
            {
                Buffer.MemoryCopy(
                    source: content + sourceIndex,
                    destination: CharPointer + bufferIndex,
                    destinationSizeInBytes: checked((long)(ByteCapacity - (bufferIndex * sizeof(char)))),
                    sourceBytesToCopy: (long)count * sizeof(char));
            }
        }

        /// <summary>
        /// Trim the specified values from the end of the buffer. If nothing is specified, nothing is trimmed.
        /// </summary>
        public unsafe void TrimEnd(char[] values)
        {
            if (values == null || values.Length == 0 || _length == 0) return;

            char* end = CharPointer + _length - 1;

            while (_length > 0 && Array.IndexOf(values, *end) >= 0)
            {
                Length = _length - 1;
                end--;
            }
        }

        /// <summary>
        /// String representation of the entire buffer. If the buffer is larger than the maximum size string (int.MaxValue) this will throw.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the buffer is too big to fit into a string.</exception>
        public unsafe override string ToString()
        {
            if (_length == 0) return string.Empty;
            if (_length > int.MaxValue) throw new InvalidOperationException();
            return new string(CharPointer, startIndex: 0, length: (int)_length);
        }

        /// <summary>
        /// Get the given substring in the buffer.
        /// </summary>
        /// <param name="count">Count of characters to take, or remaining characters from <paramref name="startIndex"/> if -1.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="startIndex"/> or <paramref name="count"/> are outside the range of the buffer's length
        /// or count is greater than the maximum string size (int.MaxValue).
        /// </exception>
        public unsafe string Substring(uint startIndex, int count = -1)
        {
            if (startIndex > (_length == 0 ? 0 : _length - 1)) throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (count < -1) throw new ArgumentOutOfRangeException(nameof(count));

            uint realCount = count == -1 ? _length - startIndex : (uint)count;
            if (realCount > int.MaxValue || checked(startIndex + realCount) > _length) throw new ArgumentOutOfRangeException(nameof(count));
            if (realCount == 0) return string.Empty;

            // The buffer could be bigger than will fit into a string, but the substring might fit. As the starting
            // index might be bigger than int we need to index ourselves.
            return new string(value: CharPointer + startIndex, startIndex: 0, length: (int)realCount);
        }

        public override void Free()
        {
            base.Free();
            _length = 0;
        }
    }
}
