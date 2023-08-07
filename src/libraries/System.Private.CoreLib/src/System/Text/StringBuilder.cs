// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Text
{
    // This class represents a mutable string.  It is convenient for situations in
    // which it is desirable to modify a string, perhaps by removing, replacing, or
    // inserting characters, without creating a new String subsequent to
    // each modification.
    //
    // The methods contained within this class do not return a new StringBuilder
    // object unless specified otherwise.  This class may be used in conjunction with the String
    // class to carry out modifications upon strings.
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed partial class StringBuilder : ISerializable
    {
        // A StringBuilder is internally represented as a linked list of blocks each of which holds
        // a chunk of the string.  It turns out string as a whole can also be represented as just a chunk,
        // so that is what we do.

        /// <summary>
        /// The character buffer for this chunk.
        /// </summary>
        internal char[] m_ChunkChars;

        /// <summary>
        /// The chunk that logically precedes this chunk.
        /// </summary>
        internal StringBuilder? m_ChunkPrevious;

        /// <summary>
        /// The number of characters in this chunk.
        /// This is the number of elements in <see cref="m_ChunkChars"/> that are in use, from the start of the buffer.
        /// </summary>
        internal int m_ChunkLength;

        /// <summary>
        /// The logical offset of this chunk's characters in the string it is a part of.
        /// This is the sum of the number of characters in preceding blocks.
        /// </summary>
        internal int m_ChunkOffset;

        /// <summary>
        /// The maximum capacity this builder is allowed to have.
        /// </summary>
        internal int m_MaxCapacity;

        /// <summary>
        /// The default capacity of a <see cref="StringBuilder"/>.
        /// </summary>
        internal const int DefaultCapacity = 16;

        private const string CapacityField = "Capacity"; // Do not rename (binary serialization)
        private const string MaxCapacityField = "m_MaxCapacity"; // Do not rename (binary serialization)
        private const string StringValueField = "m_StringValue"; // Do not rename (binary serialization)
        private const string ThreadIDField = "m_currentThread"; // Do not rename (binary serialization)

        // We want to keep chunk arrays out of large object heap (< 85K bytes ~ 40K chars) to be sure.
        // Making the maximum chunk size big means less allocation code called, but also more waste
        // in unused characters and slower inserts / replaces (since you do need to slide characters over
        // within a buffer).
        internal const int MaxChunkSize = 8000;

        /// <summary>
        /// Initializes a new instance of the <see cref="StringBuilder"/> class.
        /// </summary>
        public StringBuilder()
        {
            m_MaxCapacity = int.MaxValue;
            m_ChunkChars = new char[DefaultCapacity];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringBuilder"/> class.
        /// </summary>
        /// <param name="capacity">The initial capacity of this builder.</param>
        public StringBuilder(int capacity)
            : this(capacity, int.MaxValue)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringBuilder"/> class.
        /// </summary>
        /// <param name="value">The initial contents of this builder.</param>
        public StringBuilder(string? value)
            : this(value, DefaultCapacity)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringBuilder"/> class.
        /// </summary>
        /// <param name="value">The initial contents of this builder.</param>
        /// <param name="capacity">The initial capacity of this builder.</param>
        public StringBuilder(string? value, int capacity)
            : this(value, 0, value?.Length ?? 0, capacity)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringBuilder"/> class.
        /// </summary>
        /// <param name="value">The initial contents of this builder.</param>
        /// <param name="startIndex">The index to start in <paramref name="value"/>.</param>
        /// <param name="length">The number of characters to read in <paramref name="value"/>.</param>
        /// <param name="capacity">The initial capacity of this builder.</param>
        public StringBuilder(string? value, int startIndex, int length, int capacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(capacity);
            ArgumentOutOfRangeException.ThrowIfNegative(length);
            ArgumentOutOfRangeException.ThrowIfNegative(startIndex);

            value ??= string.Empty;

            if (startIndex > value.Length - length)
            {
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_IndexLength);
            }

            m_MaxCapacity = int.MaxValue;
            if (capacity == 0)
            {
                capacity = DefaultCapacity;
            }
            capacity = Math.Max(capacity, length);

            m_ChunkChars = GC.AllocateUninitializedArray<char>(capacity);
            m_ChunkLength = length;

            value.AsSpan(startIndex, length).CopyTo(m_ChunkChars);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringBuilder"/> class.
        /// </summary>
        /// <param name="capacity">The initial capacity of this builder.</param>
        /// <param name="maxCapacity">The maximum capacity of this builder.</param>
        public StringBuilder(int capacity, int maxCapacity)
        {
            if (capacity > maxCapacity)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), SR.ArgumentOutOfRange_Capacity);
            }
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxCapacity);
            ArgumentOutOfRangeException.ThrowIfNegative(capacity);

            if (capacity == 0)
            {
                capacity = Math.Min(DefaultCapacity, maxCapacity);
            }

            m_MaxCapacity = maxCapacity;
            m_ChunkChars = GC.AllocateUninitializedArray<char>(capacity);
        }

        private StringBuilder(SerializationInfo info, StreamingContext context)
        {
            ArgumentNullException.ThrowIfNull(info);

            int persistedCapacity = 0;
            string? persistedString = null;
            int persistedMaxCapacity = int.MaxValue;
            bool capacityPresent = false;

            // Get the data
            SerializationInfoEnumerator enumerator = info.GetEnumerator();
            while (enumerator.MoveNext())
            {
                switch (enumerator.Name)
                {
                    case MaxCapacityField:
                        persistedMaxCapacity = info.GetInt32(MaxCapacityField);
                        break;
                    case StringValueField:
                        persistedString = info.GetString(StringValueField);
                        break;
                    case CapacityField:
                        persistedCapacity = info.GetInt32(CapacityField);
                        capacityPresent = true;
                        break;
                    default:
                        // Ignore other fields for forwards-compatibility.
                        break;
                }
            }

            // Check values and set defaults
            persistedString ??= string.Empty;
            if (persistedMaxCapacity < 1 || persistedString.Length > persistedMaxCapacity)
            {
                throw new SerializationException(SR.Serialization_StringBuilderMaxCapacity);
            }

            if (!capacityPresent)
            {
                // StringBuilder in V1.X did not persist the Capacity, so this is a valid legacy code path.
                persistedCapacity = Math.Min(Math.Max(DefaultCapacity, persistedString.Length), persistedMaxCapacity);
            }

            if (persistedCapacity < 0 || persistedCapacity < persistedString.Length || persistedCapacity > persistedMaxCapacity)
            {
                throw new SerializationException(SR.Serialization_StringBuilderCapacity);
            }

            // Assign
            m_MaxCapacity = persistedMaxCapacity;
            m_ChunkChars = GC.AllocateUninitializedArray<char>(persistedCapacity);
            persistedString.CopyTo(0, m_ChunkChars, 0, persistedString.Length);
            m_ChunkLength = persistedString.Length;
            AssertInvariants();
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            ArgumentNullException.ThrowIfNull(info);

            AssertInvariants();
            info.AddValue(MaxCapacityField, m_MaxCapacity);
            info.AddValue(CapacityField, Capacity);
            info.AddValue(StringValueField, ToString());
            // Note: persist "m_currentThread" to be compatible with old versions
            info.AddValue(ThreadIDField, 0);
        }

        [Conditional("DEBUG")]
        private void AssertInvariants()
        {
            Debug.Assert(m_ChunkOffset + m_ChunkChars.Length >= m_ChunkOffset, "The length of the string is greater than int.MaxValue.");

            StringBuilder currentBlock = this;
            int maxCapacity = this.m_MaxCapacity;
            while (true)
            {
                // All blocks have the same max capacity.
                Debug.Assert(currentBlock.m_MaxCapacity == maxCapacity);
                Debug.Assert(currentBlock.m_ChunkChars != null);

                Debug.Assert(currentBlock.m_ChunkLength <= currentBlock.m_ChunkChars.Length);
                Debug.Assert(currentBlock.m_ChunkLength >= 0);
                Debug.Assert(currentBlock.m_ChunkOffset >= 0);

                StringBuilder? prevBlock = currentBlock.m_ChunkPrevious;
                if (prevBlock == null)
                {
                    Debug.Assert(currentBlock.m_ChunkOffset == 0);
                    break;
                }
                // There are no gaps in the blocks.
                Debug.Assert(currentBlock.m_ChunkOffset == prevBlock.m_ChunkOffset + prevBlock.m_ChunkLength);
                currentBlock = prevBlock;
            }
        }

        public int Capacity
        {
            get => m_ChunkChars.Length + m_ChunkOffset;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                if (value > MaxCapacity)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_Capacity);
                }
                if (value < Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_SmallCapacity);
                }

                if (Capacity != value)
                {
                    int newLen = value - m_ChunkOffset;
                    char[] newArray = GC.AllocateUninitializedArray<char>(newLen);
                    Array.Copy(m_ChunkChars, newArray, m_ChunkLength);
                    m_ChunkChars = newArray;
                }
            }
        }

        /// <summary>
        /// Gets the maximum capacity this builder is allowed to have.
        /// </summary>
        public int MaxCapacity => m_MaxCapacity;

        /// <summary>
        /// Ensures that the capacity of this builder is at least the specified value.
        /// </summary>
        /// <param name="capacity">The new capacity for this builder.</param>
        /// <remarks>
        /// If <paramref name="capacity"/> is less than or equal to the current capacity of
        /// this builder, the capacity remains unchanged.
        /// </remarks>
        public int EnsureCapacity(int capacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(capacity);

            if (Capacity < capacity)
            {
                Capacity = capacity;
            }
            return Capacity;
        }

        public override string ToString()
        {
            AssertInvariants();

            if (Length == 0)
            {
                return string.Empty;
            }

            string result = string.FastAllocateString(Length);
            StringBuilder? chunk = this;
            do
            {
                if (chunk.m_ChunkLength > 0)
                {
                    // Copy these into local variables so that they are stable even in the presence of race conditions
                    char[] sourceArray = chunk.m_ChunkChars;
                    int chunkOffset = chunk.m_ChunkOffset;
                    int chunkLength = chunk.m_ChunkLength;

                    // Check that we will not overrun our boundaries.
                    if ((uint)(chunkLength + chunkOffset) > (uint)result.Length || (uint)chunkLength > (uint)sourceArray.Length)
                    {
                        throw new ArgumentOutOfRangeException(nameof(chunkLength), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);
                    }

                    Buffer.Memmove(
                        ref Unsafe.Add(ref result.GetRawStringData(), chunkOffset),
                        ref MemoryMarshal.GetArrayDataReference(sourceArray),
                        (nuint)chunkLength);
                }
                chunk = chunk.m_ChunkPrevious;
            }
            while (chunk != null);

            return result;
        }

        /// <summary>
        /// Creates a string from a substring of this builder.
        /// </summary>
        /// <param name="startIndex">The index to start in this builder.</param>
        /// <param name="length">The number of characters to read in this builder.</param>
        public string ToString(int startIndex, int length)
        {
            int currentLength = this.Length;
            ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
            if (startIndex > currentLength)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_StartIndexLargerThanLength);
            }
            ArgumentOutOfRangeException.ThrowIfNegative(length);
            if (startIndex > currentLength - length)
            {
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_IndexLength);
            }

            AssertInvariants();
            string result = string.FastAllocateString(length);
            CopyTo(startIndex, new Span<char>(ref result.GetRawStringData(), result.Length), result.Length);
            return result;
        }

        public StringBuilder Clear()
        {
            this.Length = 0;
            return this;
        }

        /// <summary>
        /// Gets or sets the length of this builder.
        /// </summary>
        public int Length
        {
            get => m_ChunkOffset + m_ChunkLength;
            set
            {
                // If the new length is less than 0 or greater than our Maximum capacity, bail.
                ArgumentOutOfRangeException.ThrowIfNegative(value);

                if (value > MaxCapacity)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_SmallCapacity);
                }

                if (value == 0 && m_ChunkPrevious == null)
                {
                    m_ChunkLength = 0;
                    m_ChunkOffset = 0;
                    return;
                }

                int delta = value - Length;
                if (delta > 0)
                {
                    // Pad ourselves with null characters.
                    Append('\0', delta);
                }
                else
                {
                    StringBuilder chunk = FindChunkForIndex(value);
                    if (chunk != this)
                    {
                        // Avoid possible infinite capacity growth.  See https://github.com/dotnet/coreclr/pull/16926
                        int capacityToPreserve = Math.Min(Capacity, Math.Max(Length * 6 / 5, m_ChunkChars.Length));
                        int newLen = capacityToPreserve - chunk.m_ChunkOffset;
                        if (newLen > chunk.m_ChunkChars.Length)
                        {
                            // We crossed a chunk boundary when reducing the Length. We must replace this middle-chunk with a new larger chunk,
                            // to ensure the capacity we want is preserved.
                            char[] newArray = GC.AllocateUninitializedArray<char>(newLen);
                            Array.Copy(chunk.m_ChunkChars, newArray, chunk.m_ChunkLength);
                            m_ChunkChars = newArray;
                        }
                        else
                        {
                            // Special case where the capacity we want to keep corresponds exactly to the size of the content.
                            // Just take ownership of the array.
                            Debug.Assert(newLen == chunk.m_ChunkChars.Length, "The new chunk should be larger or equal to the one it is replacing.");
                            m_ChunkChars = chunk.m_ChunkChars;
                        }

                        m_ChunkPrevious = chunk.m_ChunkPrevious;
                        m_ChunkOffset = chunk.m_ChunkOffset;
                    }
                    m_ChunkLength = value - chunk.m_ChunkOffset;
                    AssertInvariants();
                }
                Debug.Assert(Length == value, "Something went wrong setting Length.");
            }
        }

        [IndexerName("Chars")]
        public char this[int index]
        {
            get
            {
                StringBuilder? chunk = this;
                while (true)
                {
                    int indexInBlock = index - chunk.m_ChunkOffset;
                    if (indexInBlock >= 0)
                    {
                        if (indexInBlock >= chunk.m_ChunkLength)
                        {
                            throw new IndexOutOfRangeException();
                        }
                        return chunk.m_ChunkChars[indexInBlock];
                    }
                    chunk = chunk.m_ChunkPrevious;
                    if (chunk == null)
                    {
                        throw new IndexOutOfRangeException();
                    }
                }
            }
            set
            {
                StringBuilder? chunk = this;
                while (true)
                {
                    int indexInBlock = index - chunk.m_ChunkOffset;
                    if (indexInBlock >= 0)
                    {
                        if (indexInBlock >= chunk.m_ChunkLength)
                        {
                            throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexMustBeLess);
                        }
                        chunk.m_ChunkChars[indexInBlock] = value;
                        return;
                    }
                    chunk = chunk.m_ChunkPrevious;
                    if (chunk == null)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexMustBeLess);
                    }
                }
            }
        }

        /// <summary>
        /// GetChunks returns ChunkEnumerator that follows the IEnumerable pattern and
        /// thus can be used in a C# 'foreach' statements to retrieve the data in the StringBuilder
        /// as chunks (ReadOnlyMemory) of characters.  An example use is:
        ///
        ///      foreach (ReadOnlyMemory&lt;char&gt; chunk in sb.GetChunks())
        ///         foreach (char c in chunk.Span)
        ///             { /* operation on c }
        ///
        /// It is undefined what happens if the StringBuilder is modified while the chunk
        /// enumeration is incomplete.  StringBuilder is also not thread-safe, so operating
        /// on it with concurrent threads is illegal.  Finally the ReadOnlyMemory chunks returned
        /// are NOT guaranteed to remain unchanged if the StringBuilder is modified, so do
        /// not cache them for later use either.  This API's purpose is efficiently extracting
        /// the data of a CONSTANT StringBuilder.
        ///
        /// Creating a ReadOnlySpan from a ReadOnlyMemory  (the .Span property) is expensive
        /// compared to the fetching of the character, so create a local variable for the SPAN
        /// if you need to use it in a nested for statement.  For example
        ///
        ///    foreach (ReadOnlyMemory&lt;char&gt; chunk in sb.GetChunks())
        ///    {
        ///         var span = chunk.Span;
        ///         for (int i = 0; i &lt; span.Length; i++)
        ///             { /* operation on span[i] */ }
        ///    }
        /// </summary>
        public ChunkEnumerator GetChunks() => new ChunkEnumerator(this);

        /// <summary>
        /// ChunkEnumerator supports both the IEnumerable and IEnumerator pattern so foreach
        /// works (see GetChunks).  It needs to be public (so the compiler can use it
        /// when building a foreach statement) but users typically don't use it explicitly.
        /// (which is why it is a nested type).
        /// </summary>
        public struct ChunkEnumerator
        {
            private readonly StringBuilder _firstChunk; // The first Stringbuilder chunk (which is the end of the logical string)
            private StringBuilder? _currentChunk;        // The chunk that this enumerator is currently returning (Current).
            private readonly ManyChunkInfo? _manyChunks; // Only used for long string builders with many chunks (see constructor)

            /// <summary>
            /// Implement IEnumerable.GetEnumerator() to return  'this' as the IEnumerator
            /// </summary>
            [EditorBrowsable(EditorBrowsableState.Never)] // Only here to make foreach work
            public ChunkEnumerator GetEnumerator() { return this; }

            /// <summary>
            /// Implements the IEnumerator pattern.
            /// </summary>
            public bool MoveNext()
            {
                if (_currentChunk == _firstChunk)
                {
                    return false;
                }


                if (_manyChunks != null)
                {
                    return _manyChunks.MoveNext(ref _currentChunk);
                }

                StringBuilder next = _firstChunk;
                while (next.m_ChunkPrevious != _currentChunk)
                {
                    Debug.Assert(next.m_ChunkPrevious != null);
                    next = next.m_ChunkPrevious;
                }
                _currentChunk = next;
                return true;
            }

            /// <summary>
            /// Implements the IEnumerator pattern.
            /// </summary>
            public ReadOnlyMemory<char> Current
            {
                get
                {
                    if (_currentChunk == null)
                    {
                        ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();
                    }

                    return new ReadOnlyMemory<char>(_currentChunk.m_ChunkChars, 0, _currentChunk.m_ChunkLength);
                }
            }

            #region private
            internal ChunkEnumerator(StringBuilder stringBuilder)
            {
                Debug.Assert(stringBuilder != null);
                _firstChunk = stringBuilder;
                _currentChunk = null;   // MoveNext will find the last chunk if we do this.
                _manyChunks = null;

                // There is a performance-vs-allocation tradeoff.   Because the chunks
                // are a linked list with each chunk pointing to its PREDECESSOR, walking
                // the list FORWARD is not efficient.   If there are few chunks (< 8) we
                // simply scan from the start each time, and tolerate the N*N behavior.
                // However above this size, we allocate an array to hold reference to all
                // the chunks and we can be efficient for large N.
                int chunkCount = ChunkCount(stringBuilder);
                if (8 < chunkCount)
                {
                    _manyChunks = new ManyChunkInfo(stringBuilder, chunkCount);
                }
            }

            private static int ChunkCount(StringBuilder? stringBuilder)
            {
                int ret = 0;
                while (stringBuilder != null)
                {
                    ret++;
                    stringBuilder = stringBuilder.m_ChunkPrevious;
                }
                return ret;
            }

            /// <summary>
            /// Used to hold all the chunks indexes when you have many chunks.
            /// </summary>
            private sealed class ManyChunkInfo
            {
                private readonly StringBuilder[] _chunks;    // These are in normal order (first chunk first)
                private int _chunkPos;

                public bool MoveNext(ref StringBuilder? current)
                {
                    int pos = ++_chunkPos;
                    if (_chunks.Length <= pos)
                    {
                        return false;
                    }
                    current = _chunks[pos];
                    return true;
                }

                public ManyChunkInfo(StringBuilder? stringBuilder, int chunkCount)
                {
                    _chunks = new StringBuilder[chunkCount];
                    while (0 <= --chunkCount)
                    {
                        Debug.Assert(stringBuilder != null);
                        _chunks[chunkCount] = stringBuilder;
                        stringBuilder = stringBuilder.m_ChunkPrevious;
                    }
                    _chunkPos = -1;
                }
            }
#endregion
        }

        /// <summary>
        /// Appends a character 0 or more times to the end of this builder.
        /// </summary>
        /// <param name="value">The character to append.</param>
        /// <param name="repeatCount">The number of times to append <paramref name="value"/>.</param>
        public StringBuilder Append(char value, int repeatCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(repeatCount);

            if (repeatCount == 0)
            {
                return this;
            }

            char[] chunkChars = m_ChunkChars;
            int chunkLength = m_ChunkLength;

            // Try to fit the whole repeatCount in the current chunk
            // Use the same check as Span<T>.Slice for 64-bit so it can be folded
            // Since repeatCount can't be negative, there's no risk for it to overflow on 32 bit
            if (((nuint)(uint)chunkLength + (nuint)(uint)repeatCount) <= (nuint)(uint)chunkChars.Length)
            {
                chunkChars.AsSpan(chunkLength, repeatCount).Fill(value);
                m_ChunkLength += repeatCount;
            }
            else
            {
                AppendWithExpansion(value, repeatCount);
            }

            AssertInvariants();
            return this;
        }

        private void AppendWithExpansion(char value, int repeatCount)
        {
            Debug.Assert(repeatCount > 0, "Invalid length; should have been validated by caller.");

            // Check if the repeatCount will put us over m_MaxCapacity
            if ((uint)(repeatCount + Length) > (uint)m_MaxCapacity)
            {
                throw new ArgumentOutOfRangeException(nameof(repeatCount), SR.ArgumentOutOfRange_LengthGreaterThanCapacity);
            }

            char[] chunkChars = m_ChunkChars;
            int chunkLength = m_ChunkLength;

            // Fill the rest of the current chunk
            int firstLength = chunkChars.Length - chunkLength;
            if (firstLength > 0)
            {
                chunkChars.AsSpan(chunkLength, firstLength).Fill(value);
                m_ChunkLength = chunkChars.Length;
            }

            // Expand the builder to add another chunk
            int restLength = repeatCount - firstLength;
            ExpandByABlock(restLength);
            Debug.Assert(m_ChunkLength == 0, "A new block was not created.");

            // Fill the new chunk with the remaining part of repeatCount
            m_ChunkChars.AsSpan(0, restLength).Fill(value);
            m_ChunkLength = restLength;
        }

        /// <summary>
        /// Appends a range of characters to the end of this builder.
        /// </summary>
        /// <param name="value">The characters to append.</param>
        /// <param name="startIndex">The index to start in <paramref name="value"/>.</param>
        /// <param name="charCount">The number of characters to read in <paramref name="value"/>.</param>
        public StringBuilder Append(char[]? value, int startIndex, int charCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
            ArgumentOutOfRangeException.ThrowIfNegative(charCount);

            if (value == null)
            {
                if (startIndex == 0 && charCount == 0)
                {
                    return this;
                }

                ArgumentNullException.Throw(nameof(value));
            }
            if (charCount > value.Length - startIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(charCount), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }

            if (charCount != 0)
            {
                Append(ref value[startIndex], charCount);
            }

            return this;
        }

        /// <summary>
        /// Appends a string to the end of this builder.
        /// </summary>
        /// <param name="value">The string to append.</param>
        public StringBuilder Append(string? value)
        {
            if (value is not null)
            {
                Append(ref value.GetRawStringData(), value.Length);
            }

            return this;
        }

        /// <summary>
        /// Appends part of a string to the end of this builder.
        /// </summary>
        /// <param name="value">The string to append.</param>
        /// <param name="startIndex">The index to start in <paramref name="value"/>.</param>
        /// <param name="count">The number of characters to read in <paramref name="value"/>.</param>
        public StringBuilder Append(string? value, int startIndex, int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            if (value == null)
            {
                if (startIndex == 0 && count == 0)
                {
                    return this;
                }
                ArgumentNullException.Throw(nameof(value));
            }

            if (count != 0)
            {
                if (startIndex > value.Length - count)
                {
                    throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);
                }

                Append(ref Unsafe.Add(ref value.GetRawStringData(), startIndex), count);
            }

            return this;
        }

        public StringBuilder Append(StringBuilder? value)
        {
            if (value != null && value.Length != 0)
            {
                return AppendCore(value, 0, value.Length);
            }
            return this;
        }

        public StringBuilder Append(StringBuilder? value, int startIndex, int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            if (value == null)
            {
                if (startIndex == 0 && count == 0)
                {
                    return this;
                }
                ArgumentNullException.Throw(nameof(value));
            }

            if (count == 0)
            {
                return this;
            }

            if (count > value.Length - startIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }

            return AppendCore(value, startIndex, count);
        }

        private StringBuilder AppendCore(StringBuilder value, int startIndex, int count)
        {
            if (value == this)
            {
                return Append(value.ToString(startIndex, count));
            }

            int newLength = Length + count;

            if ((uint)newLength > (uint)m_MaxCapacity)
            {
                throw new ArgumentOutOfRangeException(nameof(Capacity), SR.ArgumentOutOfRange_Capacity);
            }

            while (count > 0)
            {
                int length = Math.Min(m_ChunkChars.Length - m_ChunkLength, count);
                if (length == 0)
                {
                    ExpandByABlock(count);
                    length = Math.Min(m_ChunkChars.Length - m_ChunkLength, count);
                }
                value.CopyTo(startIndex, new Span<char>(m_ChunkChars, m_ChunkLength, length), length);

                m_ChunkLength += length;
                startIndex += length;
                count -= length;
            }

            return this;
        }

        public StringBuilder AppendLine() => Append(Environment.NewLine);

        public StringBuilder AppendLine(string? value)
        {
            Append(value);
            return Append(Environment.NewLine);
        }

        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            ArgumentNullException.ThrowIfNull(destination);

            ArgumentOutOfRangeException.ThrowIfNegative(destinationIndex);

            if (destinationIndex > destination.Length - count)
            {
                throw new ArgumentException(SR.ArgumentOutOfRange_OffsetOut);
            }

            CopyTo(sourceIndex, new Span<char>(destination).Slice(destinationIndex), count);
        }

        public void CopyTo(int sourceIndex, Span<char> destination, int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            if ((uint)sourceIndex > (uint)Length)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }

            if (sourceIndex > Length - count)
            {
                throw new ArgumentException(SR.Arg_LongerThanSrcString);
            }

            AssertInvariants();

            StringBuilder? chunk = this;
            int sourceEndIndex = sourceIndex + count;
            int curDestIndex = count;
            while (count > 0)
            {
                Debug.Assert(chunk != null);
                int chunkEndIndex = sourceEndIndex - chunk.m_ChunkOffset;
                if (chunkEndIndex >= 0)
                {
                    chunkEndIndex = Math.Min(chunkEndIndex, chunk.m_ChunkLength);

                    int chunkCount = count;
                    int chunkStartIndex = chunkEndIndex - count;
                    if (chunkStartIndex < 0)
                    {
                        chunkCount += chunkStartIndex;
                        chunkStartIndex = 0;
                    }
                    curDestIndex -= chunkCount;
                    count -= chunkCount;

                    new ReadOnlySpan<char>(chunk.m_ChunkChars, chunkStartIndex, chunkCount).CopyTo(destination.Slice(curDestIndex));
                }
                chunk = chunk.m_ChunkPrevious;
            }
        }

        /// <summary>
        /// Inserts a string 0 or more times into this builder at the specified position.
        /// </summary>
        /// <param name="index">The index to insert in this builder.</param>
        /// <param name="value">The string to insert.</param>
        /// <param name="count">The number of times to insert the string.</param>
        public StringBuilder Insert(int index, string? value, int count) => Insert(index, value.AsSpan(), count);

        private StringBuilder Insert(int index, ReadOnlySpan<char> value, int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            int currentLength = Length;
            if ((uint)index > (uint)currentLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }

            if (value.IsEmpty || count == 0)
            {
                return this;
            }

            // Ensure we don't insert more chars than we can hold, and we don't
            // have any integer overflow in our new length.
            long insertingChars = (long)value.Length * count;
            if (insertingChars > MaxCapacity - this.Length)
            {
                throw new OutOfMemoryException();
            }
            Debug.Assert(insertingChars + this.Length < int.MaxValue);

            MakeRoom(index, (int)insertingChars, out StringBuilder chunk, out int indexInChunk, false);

            while (count > 0)
            {
                ReplaceInPlaceAtChunk(ref chunk!, ref indexInChunk, ref MemoryMarshal.GetReference(value), value.Length);
                --count;
            }

            return this;
        }

        /// <summary>
        /// Removes a range of characters from this builder.
        /// </summary>
        /// <remarks>
        /// This method does not reduce the capacity of this builder.
        /// </remarks>
        public StringBuilder Remove(int startIndex, int length)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(length);
            ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
            if (length > Length - startIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }

            if (Length == length && startIndex == 0)
            {
                Length = 0;
                return this;
            }

            if (length > 0)
            {
                Remove(startIndex, length, out _, out _);
            }

            return this;
        }

#pragma warning disable CA1830 // Prefer strongly-typed Append and Insert method overloads on StringBuilder. No need to fix for the builder itself
        public StringBuilder Append(bool value) => Append(value.ToString());
#pragma warning restore CA1830

        public StringBuilder Append(char value)
        {
            int nextCharIndex = m_ChunkLength;
            char[] chars = m_ChunkChars;

            if ((uint)chars.Length > (uint)nextCharIndex)
            {
                chars[nextCharIndex] = value;
                m_ChunkLength++;
            }
            else
            {
                AppendWithExpansion(value);
            }

            return this;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AppendWithExpansion(char value)
        {
            ExpandByABlock(1);
            Debug.Assert(m_ChunkLength == 0, "A new block was not created.");
            m_ChunkChars[0] = value;
            m_ChunkLength++;
        }

        [CLSCompliant(false)]
        public StringBuilder Append(sbyte value) => AppendSpanFormattable(value);

        public StringBuilder Append(byte value) => AppendSpanFormattable(value);

        public StringBuilder Append(short value) => AppendSpanFormattable(value);

        public StringBuilder Append(int value) => AppendSpanFormattable(value);

        public StringBuilder Append(long value) => AppendSpanFormattable(value);

        public StringBuilder Append(float value) => AppendSpanFormattable(value);

        public StringBuilder Append(double value) => AppendSpanFormattable(value);

        public StringBuilder Append(decimal value) => AppendSpanFormattable(value);

        [CLSCompliant(false)]
        public StringBuilder Append(ushort value) => AppendSpanFormattable(value);

        [CLSCompliant(false)]
        public StringBuilder Append(uint value) => AppendSpanFormattable(value);

        [CLSCompliant(false)]
        public StringBuilder Append(ulong value) => AppendSpanFormattable(value);

        private StringBuilder AppendSpanFormattable<T>(T value) where T : ISpanFormattable
        {
            Debug.Assert(typeof(T).Assembly.Equals(typeof(object).Assembly), "Implementation trusts the results of TryFormat because T is expected to be something known");

            if (value.TryFormat(RemainingCurrentChunk, out int charsWritten, format: default, provider: null))
            {
                m_ChunkLength += charsWritten;
                return this;
            }

            return Append(value.ToString());
        }

        internal StringBuilder AppendSpanFormattable<T>(T value, string? format, IFormatProvider? provider) where T : ISpanFormattable
        {
            Debug.Assert(typeof(T).Assembly.Equals(typeof(object).Assembly), "Implementation trusts the results of TryFormat because T is expected to be something known");

            if (value.TryFormat(RemainingCurrentChunk, out int charsWritten, format, provider))
            {
                m_ChunkLength += charsWritten;
                return this;
            }

            return Append(value.ToString(format, provider));
        }

        public StringBuilder Append(object? value) => (value == null) ? this : Append(value.ToString());

        public StringBuilder Append(char[]? value)
        {
            if (value is not null)
            {
                Append(ref MemoryMarshal.GetArrayDataReference(value), value.Length);
            }

            return this;
        }

        public StringBuilder Append(ReadOnlySpan<char> value)
        {
            Append(ref MemoryMarshal.GetReference(value), value.Length);
            return this;
        }

        public StringBuilder Append(ReadOnlyMemory<char> value) => Append(value.Span);

        /// <summary>Appends the specified interpolated string to this instance.</summary>
        /// <param name="handler">The interpolated string to append.</param>
        /// <returns>A reference to this instance after the append operation has completed.</returns>
        public StringBuilder Append([InterpolatedStringHandlerArgument("")] ref AppendInterpolatedStringHandler handler) => this;

        /// <summary>Appends the specified interpolated string to this instance.</summary>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="handler">The interpolated string to append.</param>
        /// <returns>A reference to this instance after the append operation has completed.</returns>
        public StringBuilder Append(IFormatProvider? provider, [InterpolatedStringHandlerArgument("", nameof(provider))] ref AppendInterpolatedStringHandler handler) => this;

        /// <summary>Appends the specified interpolated string followed by the default line terminator to the end of the current StringBuilder object.</summary>
        /// <param name="handler">The interpolated string to append.</param>
        /// <returns>A reference to this instance after the append operation has completed.</returns>
        public StringBuilder AppendLine([InterpolatedStringHandlerArgument("")] ref AppendInterpolatedStringHandler handler) => AppendLine();

        /// <summary>Appends the specified interpolated string followed by the default line terminator to the end of the current StringBuilder object.</summary>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="handler">The interpolated string to append.</param>
        /// <returns>A reference to this instance after the append operation has completed.</returns>
        public StringBuilder AppendLine(IFormatProvider? provider, [InterpolatedStringHandlerArgument("", nameof(provider))] ref AppendInterpolatedStringHandler handler) => AppendLine();

        #region AppendJoin

        public StringBuilder AppendJoin(string? separator, params object?[] values)
        {
            separator ??= string.Empty;
            return AppendJoinCore(ref separator.GetRawStringData(), separator.Length, values);
        }

        public StringBuilder AppendJoin<T>(string? separator, IEnumerable<T> values)
        {
            separator ??= string.Empty;
            return AppendJoinCore(ref separator.GetRawStringData(), separator.Length, values);
        }

        public StringBuilder AppendJoin(string? separator, params string?[] values)
        {
            separator ??= string.Empty;
            return AppendJoinCore(ref separator.GetRawStringData(), separator.Length, values);
        }

        public StringBuilder AppendJoin(char separator, params object?[] values)
        {
            return AppendJoinCore(ref separator, 1, values);
        }

        public StringBuilder AppendJoin<T>(char separator, IEnumerable<T> values)
        {
            return AppendJoinCore(ref separator, 1, values);
        }

        public StringBuilder AppendJoin(char separator, params string?[] values)
        {
            return AppendJoinCore(ref separator, 1, values);
        }

        private StringBuilder AppendJoinCore<T>(ref char separator, int separatorLength, IEnumerable<T> values)
        {
            Debug.Assert(!Unsafe.IsNullRef(ref separator));
            Debug.Assert(separatorLength >= 0);

            if (values == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.values);
            }

            Debug.Assert(values != null);
            using (IEnumerator<T> en = values.GetEnumerator())
            {
                if (!en.MoveNext())
                {
                    return this;
                }

                T value = en.Current;
                if (value != null)
                {
                    Append(value.ToString());
                }

                while (en.MoveNext())
                {
                    Append(ref separator, separatorLength);
                    value = en.Current;
                    if (value != null)
                    {
                        Append(value.ToString());
                    }
                }
            }
            return this;
        }

        private StringBuilder AppendJoinCore<T>(ref char separator, int separatorLength, T[] values)
        {
            if (values == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.values);
            }

            Debug.Assert(values != null);
            if (values.Length == 0)
            {
                return this;
            }

            if (values[0] != null)
            {
                Append(values[0]!.ToString());
            }

            for (int i = 1; i < values.Length; i++)
            {
                Append(ref separator, separatorLength);
                if (values[i] != null)
                {
                    Append(values[i]!.ToString());
                }
            }
            return this;
        }

        #endregion

        public StringBuilder Insert(int index, string? value)
        {
            if ((uint)index > (uint)Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }

            if (value != null)
            {
                Insert(index, ref value.GetRawStringData(), value.Length);
            }

            return this;
        }

#pragma warning disable CA1830 // Prefer strongly-typed Append and Insert method overloads on StringBuilder. No need to fix for the builder itself
        // bool does not implement ISpanFormattable but its ToString override returns cached strings.
        public StringBuilder Insert(int index, bool value) => Insert(index, value.ToString().AsSpan(), 1);
#pragma warning restore CA1830

        [CLSCompliant(false)]
        public StringBuilder Insert(int index, sbyte value) => InsertSpanFormattable(index, value);

        public StringBuilder Insert(int index, byte value) => InsertSpanFormattable(index, value);

        public StringBuilder Insert(int index, short value) => InsertSpanFormattable(index, value);

        public StringBuilder Insert(int index, char value)
        {
            if ((uint)index > (uint)Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }

            Insert(index, ref value, 1);
            return this;
        }

        public StringBuilder Insert(int index, char[]? value)
        {
            if ((uint)index > (uint)Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }

            if (value != null)
            {
                Insert(index, ref MemoryMarshal.GetArrayDataReference(value), value.Length);
            }
            return this;
        }

        public StringBuilder Insert(int index, char[]? value, int startIndex, int charCount)
        {
            int currentLength = Length;
            if ((uint)index > (uint)currentLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }

            if (value == null)
            {
                if (startIndex == 0 && charCount == 0)
                {
                    return this;
                }
                ArgumentNullException.Throw(nameof(value));
            }

            ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
            ArgumentOutOfRangeException.ThrowIfNegative(charCount);
            if (startIndex > value.Length - charCount)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }

            if (charCount > 0)
            {
                Insert(index, ref value[startIndex], charCount);
            }

            return this;
        }

        public StringBuilder Insert(int index, int value) => InsertSpanFormattable(index, value);

        public StringBuilder Insert(int index, long value) => InsertSpanFormattable(index, value);

        public StringBuilder Insert(int index, float value) => InsertSpanFormattable(index, value);

        public StringBuilder Insert(int index, double value) => InsertSpanFormattable(index, value);

        public StringBuilder Insert(int index, decimal value) => InsertSpanFormattable(index, value);

        [CLSCompliant(false)]
        public StringBuilder Insert(int index, ushort value) => InsertSpanFormattable(index, value);

        [CLSCompliant(false)]
        public StringBuilder Insert(int index, uint value) => InsertSpanFormattable(index, value);

        [CLSCompliant(false)]
        public StringBuilder Insert(int index, ulong value) => InsertSpanFormattable(index, value);

        public StringBuilder Insert(int index, object? value) => (value == null) ? this : Insert(index, value.ToString(), 1);

        public StringBuilder Insert(int index, ReadOnlySpan<char> value)
        {
            if ((uint)index > (uint)Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }

            if (value.Length != 0)
            {
                Insert(index, ref MemoryMarshal.GetReference(value), value.Length);
            }

            return this;
        }

        private StringBuilder InsertSpanFormattable<T>(int index, T value) where T : ISpanFormattable
        {
            Debug.Assert(typeof(T).Assembly.Equals(typeof(object).Assembly), "Implementation trusts the results of TryFormat because T is expected to be something known");

            Span<char> buffer = stackalloc char[string.StackallocCharBufferSizeLimit];
            if (value.TryFormat(buffer, out int charsWritten, format: default, provider: null))
            {
                // We don't use Insert(int, ReadOnlySpan<char>) for exception compatibility;
                // we want exceeding the maximum capacity to throw an OutOfMemoryException.
                return Insert(index, buffer.Slice(0, charsWritten), 1);
            }

            return Insert(index, value.ToString(), 1);
        }

        public StringBuilder AppendFormat([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0)
        {
            return AppendFormatHelper(null, format, new ReadOnlySpan<object?>(in arg0));
        }

        public StringBuilder AppendFormat([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1)
        {
            TwoObjects two = new TwoObjects(arg0, arg1);
            return AppendFormatHelper(null, format, MemoryMarshal.CreateReadOnlySpan(ref two.Arg0, 2));
        }

        public StringBuilder AppendFormat([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1, object? arg2)
        {
            ThreeObjects three = new ThreeObjects(arg0, arg1, arg2);
            return AppendFormatHelper(null, format, MemoryMarshal.CreateReadOnlySpan(ref three.Arg0, 3));
        }

        public StringBuilder AppendFormat([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[] args)
        {
            if (args is null)
            {
                // To preserve the original exception behavior, throw an exception about format if both
                // args and format are null. The actual null check for format is in AppendFormatHelper.
                ArgumentNullException.Throw(format is null ? nameof(format) : nameof(args));
            }

            return AppendFormatHelper(null, format, args);
        }

        public StringBuilder AppendFormat(IFormatProvider? provider, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0)
        {
            return AppendFormatHelper(provider, format, new ReadOnlySpan<object?>(in arg0));
        }

        public StringBuilder AppendFormat(IFormatProvider? provider, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1)
        {
            TwoObjects two = new TwoObjects(arg0, arg1);
            return AppendFormatHelper(provider, format, MemoryMarshal.CreateReadOnlySpan(ref two.Arg0, 2));
        }

        public StringBuilder AppendFormat(IFormatProvider? provider, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1, object? arg2)
        {
            ThreeObjects three = new ThreeObjects(arg0, arg1, arg2);
            return AppendFormatHelper(provider, format, MemoryMarshal.CreateReadOnlySpan(ref three.Arg0, 3));
        }

        public StringBuilder AppendFormat(IFormatProvider? provider, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[] args)
        {
            if (args is null)
            {
                // To preserve the original exception behavior, throw an exception about format if both
                // args and format are null. The actual null check for format is in AppendFormatHelper.
                ArgumentNullException.Throw(format is null ? nameof(format) : nameof(args));
            }

            return AppendFormatHelper(provider, format, args);
        }

        internal StringBuilder AppendFormatHelper(IFormatProvider? provider, string format, ReadOnlySpan<object?> args)
        {
            ArgumentNullException.ThrowIfNull(format);

            // Undocumented exclusive limits on the range for Argument Hole Index and Argument Hole Alignment.
            const int IndexLimit = 1_000_000; // Note:            0 <= ArgIndex < IndexLimit
            const int WidthLimit = 1_000_000; // Note:  -WidthLimit <  ArgAlign < WidthLimit

            // Query the provider (if one was supplied) for an ICustomFormatter.  If there is one,
            // it needs to be used to transform all arguments.
            ICustomFormatter? cf = (ICustomFormatter?)provider?.GetFormat(typeof(ICustomFormatter));

            // Repeatedly find the next hole and process it.
            int pos = 0;
            char ch;
            while (true)
            {
                // Skip until either the end of the input or the first unescaped opening brace, whichever comes first.
                // Along the way we need to also unescape escaped closing braces.
                while (true)
                {
                    // Find the next brace.  If there isn't one, the remainder of the input is text to be appended, and we're done.
                    if ((uint)pos >= (uint)format.Length)
                    {
                        return this;
                    }

                    ReadOnlySpan<char> remainder = format.AsSpan(pos);
                    int countUntilNextBrace = remainder.IndexOfAny('{', '}');
                    if (countUntilNextBrace < 0)
                    {
                        Append(remainder);
                        return this;
                    }

                    // Append the text until the brace.
                    Append(remainder.Slice(0, countUntilNextBrace));
                    pos += countUntilNextBrace;

                    // Get the brace.  It must be followed by another character, either a copy of itself in the case of being
                    // escaped, or an arbitrary character that's part of the hole in the case of an opening brace.
                    char brace = format[pos];
                    ch = MoveNext(format, ref pos);
                    if (brace == ch)
                    {
                        Append(ch);
                        pos++;
                        continue;
                    }

                    // This wasn't an escape, so it must be an opening brace.
                    if (brace != '{')
                    {
                        ThrowHelper.ThrowFormatInvalidString(pos, ExceptionResource.Format_UnexpectedClosingBrace);
                    }

                    // Proceed to parse the hole.
                    break;
                }

                // We're now positioned just after the opening brace of an argument hole, which consists of
                // an opening brace, an index, an optional width preceded by a comma, and an optional format
                // preceded by a colon, with arbitrary amounts of spaces throughout.
                int width = 0;
                bool leftJustify = false;
                ReadOnlySpan<char> itemFormatSpan = default; // used if itemFormat is null

                // First up is the index parameter, which is of the form:
                //     at least on digit
                //     optional any number of spaces
                // We've already read the first digit into ch.
                Debug.Assert(format[pos - 1] == '{');
                Debug.Assert(ch != '{');
                int index = ch - '0';
                if ((uint)index >= 10u)
                {
                    ThrowHelper.ThrowFormatInvalidString(pos, ExceptionResource.Format_ExpectedAsciiDigit);
                }

                // Common case is a single digit index followed by a closing brace.  If it's not a closing brace,
                // proceed to finish parsing the full hole format.
                ch = MoveNext(format, ref pos);
                if (ch != '}')
                {
                    // Continue consuming optional additional digits.
                    while (char.IsAsciiDigit(ch) && index < IndexLimit)
                    {
                        index = index * 10 + ch - '0';
                        ch = MoveNext(format, ref pos);
                    }

                    // Consume optional whitespace.
                    while (ch == ' ')
                    {
                        ch = MoveNext(format, ref pos);
                    }

                    // Parse the optional alignment, which is of the form:
                    //     comma
                    //     optional any number of spaces
                    //     optional -
                    //     at least one digit
                    //     optional any number of spaces
                    if (ch == ',')
                    {
                        // Consume optional whitespace.
                        do
                        {
                            ch = MoveNext(format, ref pos);
                        }
                        while (ch == ' ');

                        // Consume an optional minus sign indicating left alignment.
                        if (ch == '-')
                        {
                            leftJustify = true;
                            ch = MoveNext(format, ref pos);
                        }

                        // Parse alignment digits. The read character must be a digit.
                        width = ch - '0';
                        if ((uint)width >= 10u)
                        {
                            ThrowHelper.ThrowFormatInvalidString(pos, ExceptionResource.Format_ExpectedAsciiDigit);
                        }
                        ch = MoveNext(format, ref pos);
                        while (char.IsAsciiDigit(ch) && width < WidthLimit)
                        {
                            width = width * 10 + ch - '0';
                            ch = MoveNext(format, ref pos);
                        }

                        // Consume optional whitespace
                        while (ch == ' ')
                        {
                            ch = MoveNext(format, ref pos);
                        }
                    }

                    // The next character needs to either be a closing brace for the end of the hole,
                    // or a colon indicating the start of the format.
                    if (ch != '}')
                    {
                        if (ch != ':')
                        {
                            // Unexpected character
                            ThrowHelper.ThrowFormatInvalidString(pos, ExceptionResource.Format_UnclosedFormatItem);
                        }

                        // Search for the closing brace; everything in between is the format,
                        // but opening braces aren't allowed.
                        int startingPos = pos;
                        while (true)
                        {
                            ch = MoveNext(format, ref pos);

                            if (ch == '}')
                            {
                                // Argument hole closed
                                break;
                            }

                            if (ch == '{')
                            {
                                // Braces inside the argument hole are not supported
                                ThrowHelper.ThrowFormatInvalidString(pos, ExceptionResource.Format_UnclosedFormatItem);
                            }
                        }

                        startingPos++;
                        itemFormatSpan = format.AsSpan(startingPos, pos - startingPos);
                    }
                }

                // Construct the output for this arg hole.
                Debug.Assert(format[pos] == '}');
                pos++;
                string? s = null;
                string? itemFormat = null;

                if ((uint)index >= (uint)args.Length)
                {
                    ThrowHelper.ThrowFormatIndexOutOfRange();
                }
                object? arg = args[index];

                if (cf != null)
                {
                    if (!itemFormatSpan.IsEmpty)
                    {
                        itemFormat = new string(itemFormatSpan);
                    }

                    s = cf.Format(itemFormat, arg, provider);
                }

                if (s == null)
                {
                    // If arg is ISpanFormattable and the beginning doesn't need padding,
                    // try formatting it into the remaining current chunk.
                    if ((leftJustify || width == 0) &&
                        arg is ISpanFormattable spanFormattableArg &&
                        spanFormattableArg.TryFormat(RemainingCurrentChunk, out int charsWritten, itemFormatSpan, provider))
                    {
                        if ((uint)charsWritten > (uint)RemainingCurrentChunk.Length)
                        {
                            // Untrusted ISpanFormattable implementations might return an erroneous charsWritten value,
                            // and m_ChunkLength might end up being used in Unsafe code, so fail if we get back an
                            // out-of-range charsWritten value.
                            ThrowHelper.ThrowFormatInvalidString();
                        }

                        m_ChunkLength += charsWritten;

                        // Pad the end, if needed.
                        if (leftJustify && width > charsWritten)
                        {
                            Append(' ', width - charsWritten);
                        }

                        // Continue to parse other characters.
                        continue;
                    }

                    // Otherwise, fallback to trying IFormattable or calling ToString.
                    if (arg is IFormattable formattableArg)
                    {
                        if (itemFormatSpan.Length != 0)
                        {
                            itemFormat ??= new string(itemFormatSpan);
                        }
                        s = formattableArg.ToString(itemFormat, provider);
                    }
                    else
                    {
                        s = arg?.ToString();
                    }

                    s ??= string.Empty;
                }

                // Append it to the final output of the Format String.
                if (width <= s.Length)
                {
                    Append(s);
                }
                else if (leftJustify)
                {
                    Append(s);
                    Append(' ', width - s.Length);
                }
                else
                {
                    Append(' ', width - s.Length);
                    Append(s);
                }

                // Continue parsing the rest of the format string.
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static char MoveNext(string format, ref int pos)
            {
                pos++;
                if ((uint)pos >= (uint)format.Length)
                {
                    ThrowHelper.ThrowFormatInvalidString(pos, ExceptionResource.Format_UnclosedFormatItem);
                }
                return format[pos];
            }
        }

        /// <summary>
        /// Appends the string returned by processing a composite format string, which contains zero or more format items, to this instance.
        /// Each format item is replaced by the string representation of any of the arguments using a specified format provider.
        /// </summary>
        /// <typeparam name="TArg0">The type of the first object to format.</typeparam>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="format">A <see cref="CompositeFormat"/>.</param>
        /// <param name="arg0">The first object to format.</param>
        /// <returns>A reference to this instance after the append operation has completed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="format"/> is null.</exception>
        /// <exception cref="FormatException">The index of a format item is greater than or equal to the number of supplied arguments.</exception>
        public StringBuilder AppendFormat<TArg0>(IFormatProvider? provider, CompositeFormat format, TArg0 arg0)
        {
            ArgumentNullException.ThrowIfNull(format);
            format.ValidateNumberOfArgs(1);
            return AppendFormat(provider, format, arg0, 0, 0, default);
        }

        /// <summary>
        /// Appends the string returned by processing a composite format string, which contains zero or more format items, to this instance.
        /// Each format item is replaced by the string representation of any of the arguments using a specified format provider.
        /// </summary>
        /// <typeparam name="TArg0">The type of the first object to format.</typeparam>
        /// <typeparam name="TArg1">The type of the second object to format.</typeparam>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="format">A <see cref="CompositeFormat"/>.</param>
        /// <param name="arg0">The first object to format.</param>
        /// <param name="arg1">The second object to format.</param>
        /// <returns>A reference to this instance after the append operation has completed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="format"/> is null.</exception>
        /// <exception cref="FormatException">The index of a format item is greater than or equal to the number of supplied arguments.</exception>
        public StringBuilder AppendFormat<TArg0, TArg1>(IFormatProvider? provider, CompositeFormat format, TArg0 arg0, TArg1 arg1)
        {
            ArgumentNullException.ThrowIfNull(format);
            format.ValidateNumberOfArgs(2);
            return AppendFormat(provider, format, arg0, arg1, 0, default);
        }

        /// <summary>
        /// Appends the string returned by processing a composite format string, which contains zero or more format items, to this instance.
        /// Each format item is replaced by the string representation of any of the arguments using a specified format provider.
        /// </summary>
        /// <typeparam name="TArg0">The type of the first object to format.</typeparam>
        /// <typeparam name="TArg1">The type of the second object to format.</typeparam>
        /// <typeparam name="TArg2">The type of the third object to format.</typeparam>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="format">A <see cref="CompositeFormat"/>.</param>
        /// <param name="arg0">The first object to format.</param>
        /// <param name="arg1">The second object to format.</param>
        /// <param name="arg2">The third object to format.</param>
        /// <returns>A reference to this instance after the append operation has completed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="format"/> is null.</exception>
        /// <exception cref="FormatException">The index of a format item is greater than or equal to the number of supplied arguments.</exception>
        public StringBuilder AppendFormat<TArg0, TArg1, TArg2>(IFormatProvider? provider, CompositeFormat format, TArg0 arg0, TArg1 arg1, TArg2 arg2)
        {
            ArgumentNullException.ThrowIfNull(format);
            format.ValidateNumberOfArgs(3);
            return AppendFormat(provider, format, arg0, arg1, arg2, default);
        }

        /// <summary>
        /// Appends the string returned by processing a composite format string, which contains zero or more format items, to this instance.
        /// Each format item is replaced by the string representation of any of the arguments using a specified format provider.
        /// </summary>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="format">A <see cref="CompositeFormat"/>.</param>
        /// <param name="args">An array of objects to format.</param>
        /// <returns>A reference to this instance after the append operation has completed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="format"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> is null.</exception>
        /// <exception cref="FormatException">The index of a format item is greater than or equal to the number of supplied arguments.</exception>
        public StringBuilder AppendFormat(IFormatProvider? provider, CompositeFormat format, params object?[] args)
        {
            ArgumentNullException.ThrowIfNull(format);
            ArgumentNullException.ThrowIfNull(args);
            return AppendFormat(provider, format, (ReadOnlySpan<object?>)args);
        }

        /// <summary>
        /// Appends the string returned by processing a composite format string, which contains zero or more format items, to this instance.
        /// Each format item is replaced by the string representation of any of the arguments using a specified format provider.
        /// </summary>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="format">A <see cref="CompositeFormat"/>.</param>
        /// <param name="args">A span of objects to format.</param>
        /// <returns>A reference to this instance after the append operation has completed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="format"/> is null.</exception>
        /// <exception cref="FormatException">The index of a format item is greater than or equal to the number of supplied arguments.</exception>
        public StringBuilder AppendFormat(IFormatProvider? provider, CompositeFormat format, ReadOnlySpan<object?> args)
        {
            ArgumentNullException.ThrowIfNull(format);
            format.ValidateNumberOfArgs(args.Length);
            return args.Length switch
            {
                0 => AppendFormat(provider, format, 0, 0, 0, args),
                1 => AppendFormat(provider, format, args[0], 0, 0, args),
                2 => AppendFormat(provider, format, args[0], args[1], 0, args),
                _ => AppendFormat(provider, format, args[0], args[1], args[2], args),
            };
        }

        private StringBuilder AppendFormat<TArg0, TArg1, TArg2>(IFormatProvider? provider, CompositeFormat format, TArg0 arg0, TArg1 arg1, TArg2 arg2, ReadOnlySpan<object?> args)
        {
            // Create the interpolated string handler.
            var handler = new AppendInterpolatedStringHandler(format._literalLength, format._formattedCount, this, provider);

            // Append each segment.
            foreach ((string? Literal, int ArgIndex, int Alignment, string? Format) segment in format._segments)
            {
                if (segment.Literal is string literal)
                {
                    handler.AppendLiteral(literal);
                }
                else
                {
                    int index = segment.ArgIndex;
                    switch (index)
                    {
                        case 0:
                            handler.AppendFormatted(arg0, segment.Alignment, segment.Format);
                            break;

                        case 1:
                            handler.AppendFormatted(arg1, segment.Alignment, segment.Format);
                            break;

                        case 2:
                            handler.AppendFormatted(arg2, segment.Alignment, segment.Format);
                            break;

                        default:
                            Debug.Assert(index > 2);
                            handler.AppendFormatted(args[index], segment.Alignment, segment.Format);
                            break;
                    }
                }
            }

            // Complete the operation.
            return Append(ref handler);
        }

        /// <summary>
        /// Replaces all instances of one string with another in this builder.
        /// </summary>
        /// <param name="oldValue">The string to replace.</param>
        /// <param name="newValue">The string to replace <paramref name="oldValue"/> with.</param>
        /// <remarks>
        /// If <paramref name="newValue"/> is <c>null</c>, instances of <paramref name="oldValue"/>
        /// are removed from this builder.
        /// </remarks>
        public StringBuilder Replace(string oldValue, string? newValue) => Replace(oldValue, newValue, 0, Length);

        /// <summary>
        /// Determines if the contents of this builder are equal to the contents of another builder.
        /// </summary>
        /// <param name="sb">The other builder.</param>
        public bool Equals([NotNullWhen(true)] StringBuilder? sb)
        {
            if (sb == null)
            {
                return false;
            }
            if (Length != sb.Length)
            {
                return false;
            }
            if (sb == this)
            {
                return true;
            }
            StringBuilder? thisChunk = this;
            int thisChunkIndex = thisChunk.m_ChunkLength;
            StringBuilder? sbChunk = sb;
            int sbChunkIndex = sbChunk.m_ChunkLength;
            while (true)
            {
                --thisChunkIndex;
                --sbChunkIndex;

                while (thisChunkIndex < 0)
                {
                    thisChunk = thisChunk.m_ChunkPrevious;
                    if (thisChunk == null)
                    {
                        break;
                    }
                    thisChunkIndex = thisChunk.m_ChunkLength + thisChunkIndex;
                }

                while (sbChunkIndex < 0)
                {
                    sbChunk = sbChunk.m_ChunkPrevious;
                    if (sbChunk == null)
                    {
                        break;
                    }
                    sbChunkIndex = sbChunk.m_ChunkLength + sbChunkIndex;
                }

                if (thisChunkIndex < 0)
                {
                    return sbChunkIndex < 0;
                }
                if (sbChunkIndex < 0)
                {
                    return false;
                }

                Debug.Assert(thisChunk != null && sbChunk != null);
                if (thisChunk.m_ChunkChars[thisChunkIndex] != sbChunk.m_ChunkChars[sbChunkIndex])
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Determines if the contents of this builder are equal to the contents of <see cref="ReadOnlySpan{Char}"/>.
        /// </summary>
        /// <param name="span">The <see cref="ReadOnlySpan{Char}"/>.</param>
        public bool Equals(ReadOnlySpan<char> span)
        {
            if (span.Length != Length)
            {
                return false;
            }

            StringBuilder? sbChunk = this;
            int offset = 0;

            do
            {
                int chunk_length = sbChunk.m_ChunkLength;
                offset += chunk_length;

                ReadOnlySpan<char> chunk = new ReadOnlySpan<char>(sbChunk.m_ChunkChars, 0, chunk_length);

                if (!chunk.EqualsOrdinal(span.Slice(span.Length - offset, chunk_length)))
                {
                    return false;
                }

                sbChunk = sbChunk.m_ChunkPrevious;
            } while (sbChunk != null);

            Debug.Assert(offset == Length);
            return true;
        }

        /// <summary>
        /// Replaces all instances of one string with another in part of this builder.
        /// </summary>
        /// <param name="oldValue">The string to replace.</param>
        /// <param name="newValue">The string to replace <paramref name="oldValue"/> with.</param>
        /// <param name="startIndex">The index to start in this builder.</param>
        /// <param name="count">The number of characters to read in this builder.</param>
        /// <remarks>
        /// If <paramref name="newValue"/> is <c>null</c>, instances of <paramref name="oldValue"/>
        /// are removed from this builder.
        /// </remarks>
        public StringBuilder Replace(string oldValue, string? newValue, int startIndex, int count)
        {
            int currentLength = Length;
            if ((uint)startIndex > (uint)currentLength)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }
            if (count < 0 || startIndex > currentLength - count)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }
            ArgumentException.ThrowIfNullOrEmpty(oldValue);

            newValue ??= string.Empty;

            var replacements = new ValueListBuilder<int>(stackalloc int[128]); // A list of replacement positions in a chunk to apply

            // Find the chunk, indexInChunk for the starting point
            StringBuilder chunk = FindChunkForIndex(startIndex);
            int indexInChunk = startIndex - chunk.m_ChunkOffset;
            while (count > 0)
            {
                Debug.Assert(chunk != null, "chunk was null in replace");

                // While the remaining search space is at least as large as the old value being replaced,
                // find all occurrences of it contained entirely within the chunk. We stop searching
                // once we're within oldValue.Length from the end of the chunk (or count limit), at which point
                // we need to consider a value that bridges between two chunks.
                ReadOnlySpan<char> remainingChunk = chunk.m_ChunkChars.AsSpan(indexInChunk, Math.Min(chunk.m_ChunkLength - indexInChunk, count));
                while (oldValue.Length <= remainingChunk.Length)
                {
                    // Find the next match.
                    int foundPos = remainingChunk.IndexOf(oldValue);
                    if (foundPos >= 0)
                    {
                        // We found one.  Add it as a location for the replacement.
                        indexInChunk += foundPos;
                        replacements.Append(indexInChunk);

                        // Move ahead to the next location.
                        remainingChunk = remainingChunk.Slice(foundPos + oldValue.Length);
                        indexInChunk += oldValue.Length;
                        count -= foundPos + oldValue.Length;

                        // If after accounting for moving past the match our count has
                        // gone to 0, break out to stop searching.
                        Debug.Assert(count >= 0, "count should never go negative");
                        if (count == 0)
                        {
                            break;
                        }
                    }
                    else
                    {
                        // No match found. Reposition to one character beyond the last starting
                        // location searched, which will be oldValue.Length - 1 from the end.
                        // Then break out so that we can start the cross-chunk matching from that location.
                        int move = remainingChunk.Length - (oldValue.Length - 1);
                        indexInChunk += move;
                        count -= move;
                        break;
                    }
                }

                Debug.Assert(oldValue.Length > Math.Min(count, chunk.m_ChunkLength - indexInChunk),
                    $"oldValue.Length = {oldValue.Length}, chunk.m_ChunkLength - indexInChunk = {chunk.m_ChunkLength - indexInChunk}, count == {count}");

                // Now do the more complicated cross-chunk matching.
                while (indexInChunk < chunk.m_ChunkLength && count > 0)
                {
                    if (StartsWith(chunk, indexInChunk, count, oldValue))
                    {
                        replacements.Append(indexInChunk);
                        indexInChunk += oldValue.Length;
                        count -= oldValue.Length;
                    }
                    else
                    {
                        indexInChunk++;
                        --count;
                    }
                }

                // We've either fully explored the chunk or we've reached our count limit.
                Debug.Assert(indexInChunk >= chunk.m_ChunkLength || count == 0,
                    $"indexInChunk = {indexInChunk}, chunk.m_ChunkLength == {chunk.m_ChunkLength}, count == {count}");

                // Replacing mutates the blocks, so we need to convert to a logical index and back afterwards.
                int index = indexInChunk + chunk.m_ChunkOffset;

                // Apply any replacements we accumulated.
                if (replacements.Length != 0)
                {
                    // Perform all replacements, and adjust the logical index if the new and old values
                    // have different lengths, such that the replacements would have impacted it.
                    ReplaceAllInChunk(replacements.AsSpan(), chunk, oldValue.Length, newValue);
                    index += (newValue.Length - oldValue.Length) * replacements.Length;
                    replacements.Length = 0;
                }

                chunk = FindChunkForIndex(index);
                indexInChunk = index - chunk.m_ChunkOffset;
                Debug.Assert(chunk != null || count == 0, "Chunks ended prematurely!");
            }

            replacements.Dispose();

            AssertInvariants();
            return this;
        }

        /// <summary>
        /// Replaces all instances of one character with another in this builder.
        /// </summary>
        /// <param name="oldChar">The character to replace.</param>
        /// <param name="newChar">The character to replace <paramref name="oldChar"/> with.</param>
        public StringBuilder Replace(char oldChar, char newChar)
        {
            return Replace(oldChar, newChar, 0, Length);
        }

        /// <summary>
        /// Replaces all instances of one character with another in this builder.
        /// </summary>
        /// <param name="oldChar">The character to replace.</param>
        /// <param name="newChar">The character to replace <paramref name="oldChar"/> with.</param>
        /// <param name="startIndex">The index to start in this builder.</param>
        /// <param name="count">The number of characters to read in this builder.</param>
        public StringBuilder Replace(char oldChar, char newChar, int startIndex, int count)
        {
            int currentLength = Length;
            if ((uint)startIndex > (uint)currentLength)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }

            if (count < 0 || startIndex > currentLength - count)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }

            int endIndex = startIndex + count;
            StringBuilder chunk = this;

            while (true)
            {
                int endIndexInChunk = endIndex - chunk.m_ChunkOffset;
                int startIndexInChunk = startIndex - chunk.m_ChunkOffset;
                if (endIndexInChunk >= 0)
                {
                    int curInChunk = Math.Max(startIndexInChunk, 0);
                    int endInChunk = Math.Min(chunk.m_ChunkLength, endIndexInChunk);

                    Span<char> span = chunk.m_ChunkChars.AsSpan(curInChunk, endInChunk - curInChunk);
                    span.Replace(oldChar, newChar);
                }

                if (startIndexInChunk >= 0)
                {
                    break;
                }

                Debug.Assert(chunk.m_ChunkPrevious != null);
                chunk = chunk.m_ChunkPrevious;
            }

            AssertInvariants();
            return this;
        }

        /// <summary>
        /// Appends a character buffer to this builder.
        /// </summary>
        /// <param name="value">The pointer to the start of the buffer.</param>
        /// <param name="valueCount">The number of characters in the buffer.</param>
        [CLSCompliant(false)]
        public unsafe StringBuilder Append(char* value, int valueCount)
        {
            // We don't check null value as this case will throw null reference exception anyway
            ArgumentOutOfRangeException.ThrowIfNegative(valueCount);

            Append(ref *value, valueCount);
            return this;
        }

        /// <summary>Appends a specified number of chars starting from the specified reference.</summary>
        private void Append(ref char value, int valueCount)
        {
            Debug.Assert(valueCount >= 0, "Invalid length; should have been validated by caller.");
            if (valueCount != 0)
            {
                char[] chunkChars = m_ChunkChars;
                int chunkLength = m_ChunkLength;

                if (((uint)chunkLength + (uint)valueCount) <= (uint)chunkChars.Length)
                {
                    ref char destination = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(chunkChars), chunkLength);
                    if (valueCount <= 2)
                    {
                        destination = value;
                        if (valueCount == 2)
                        {
                            Unsafe.Add(ref destination, 1) = Unsafe.Add(ref value, 1);
                        }
                    }
                    else
                    {
                        Buffer.Memmove(ref destination, ref value, (nuint)valueCount);
                    }

                    m_ChunkLength = chunkLength + valueCount;
                }
                else
                {
                    AppendWithExpansion(ref value, valueCount);
                }
            }
        }

        private void AppendWithExpansion(ref char value, int valueCount)
        {
            // Check if the valueCount will put us over m_MaxCapacity.
            // Doing the check here prevents corruption of the StringBuilder.
            int newLength = Length + valueCount;
            if (newLength > m_MaxCapacity || newLength < valueCount)
            {
                throw new ArgumentOutOfRangeException(nameof(valueCount), SR.ArgumentOutOfRange_LengthGreaterThanCapacity);
            }

            // Copy the first chunk
            int firstLength = m_ChunkChars.Length - m_ChunkLength;
            if (firstLength > 0)
            {
                new ReadOnlySpan<char>(ref value, firstLength).CopyTo(m_ChunkChars.AsSpan(m_ChunkLength));
                m_ChunkLength = m_ChunkChars.Length;
            }

            // Expand the builder to add another chunk.
            int restLength = valueCount - firstLength;
            ExpandByABlock(restLength);
            Debug.Assert(m_ChunkLength == 0, "A new block was not created.");

            // Copy the second chunk
            new ReadOnlySpan<char>(ref Unsafe.Add(ref value, firstLength), restLength).CopyTo(m_ChunkChars);
            m_ChunkLength = restLength;

            AssertInvariants();
        }

        /// <summary>
        /// Inserts a character buffer into this builder at the specified position.
        /// </summary>
        /// <param name="index">The index to insert in this builder.</param>
        /// <param name="value">The reference to the start of the buffer.</param>
        /// <param name="valueCount">The number of characters in the buffer.</param>
        private void Insert(int index, ref char value, int valueCount)
        {
            Debug.Assert((uint)index <= (uint)Length, "Callers should check that index is a legal value.");

            if (valueCount > 0)
            {
                MakeRoom(index, valueCount, out StringBuilder chunk, out int indexInChunk, false);
                ReplaceInPlaceAtChunk(ref chunk!, ref indexInChunk, ref value, valueCount);
            }
        }

        /// <summary>
        /// Replaces strings at specified indices with a new string in a chunk.
        /// </summary>
        /// <param name="replacements">The list of indices, relative to the beginning of the chunk, to remove at.</param>
        /// <param name="sourceChunk">The source chunk.</param>
        /// <param name="removeCount">The number of characters to remove at each replacement.</param>
        /// <param name="value">The string to insert at each replacement.</param>
        /// <remarks>
        /// This routine is very efficient because it does replacements in bulk.
        /// </remarks>
        private void ReplaceAllInChunk(ReadOnlySpan<int> replacements, StringBuilder sourceChunk, int removeCount, string value)
        {
            Debug.Assert(!replacements.IsEmpty);

            // calculate the total amount of extra space or space needed for all the replacements.
            long longDelta = (value.Length - removeCount) * (long)replacements.Length;
            int delta = (int)longDelta;
            if (delta != longDelta)
            {
                throw new OutOfMemoryException();
            }

            StringBuilder targetChunk = sourceChunk;        // the target as we copy chars down
            int targetIndexInChunk = replacements[0];

            // Make the room needed for all the new characters if needed.
            if (delta > 0)
            {
                MakeRoom(targetChunk.m_ChunkOffset + targetIndexInChunk, delta, out targetChunk, out targetIndexInChunk, true);
            }

            // We made certain that characters after the insertion point are not moved,
            int i = 0;
            while (true)
            {
                // Copy in the new string for the ith replacement
                ReplaceInPlaceAtChunk(ref targetChunk!, ref targetIndexInChunk, ref value.GetRawStringData(), value.Length);
                int gapStart = replacements[i] + removeCount;
                i++;
                if ((uint)i >= replacements.Length)
                {
                    break;
                }

                int gapEnd = replacements[i];
                Debug.Assert(gapStart < sourceChunk.m_ChunkChars.Length, "gap starts at end of buffer.  Should not happen");
                Debug.Assert(gapStart <= gapEnd, "negative gap size");
                Debug.Assert(gapEnd <= sourceChunk.m_ChunkLength, "gap too big");
                if (delta != 0)     // can skip the sliding of gaps if source an target string are the same size.
                {
                    // Copy the gap data between the current replacement and the next replacement
                    ReplaceInPlaceAtChunk(ref targetChunk!, ref targetIndexInChunk, ref sourceChunk.m_ChunkChars[gapStart], gapEnd - gapStart);
                }
                else
                {
                    targetIndexInChunk += gapEnd - gapStart;
                    Debug.Assert(targetIndexInChunk <= targetChunk.m_ChunkLength, "gap not in chunk");
                }
            }

            // Remove extra space if necessary.
            if (delta < 0)
            {
                Remove(targetChunk.m_ChunkOffset + targetIndexInChunk, -delta, out targetChunk, out targetIndexInChunk);
            }
        }

        /// <summary>
        /// Returns a value indicating whether a substring of a builder starts with a specified prefix.
        /// </summary>
        /// <param name="chunk">The chunk in which the substring starts.</param>
        /// <param name="indexInChunk">The index in <paramref name="chunk"/> at which the substring starts.</param>
        /// <param name="count">The logical count of the substring.</param>
        /// <param name="value">The prefix.</param>
        private bool StartsWith(StringBuilder chunk, int indexInChunk, int count, string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (count == 0)
                {
                    return false;
                }

                if (indexInChunk >= chunk.m_ChunkLength)
                {
                    chunk = Next(chunk)!;
                    if (chunk == null)
                    {
                        return false;
                    }
                    indexInChunk = 0;
                }

                if (value[i] != chunk.m_ChunkChars[indexInChunk])
                {
                    return false;
                }

                indexInChunk++;
                --count;
            }

            return true;
        }

        /// <summary>
        /// Replaces characters at a specified location with the contents of a character buffer.
        /// This function is the logical equivalent of memcpy.
        /// </summary>
        /// <param name="chunk">
        /// The chunk in which to start replacing characters.
        /// Receives the chunk in which character replacement ends.
        /// </param>
        /// <param name="indexInChunk">
        /// The index in <paramref name="chunk"/> to start replacing characters at.
        /// Receives the index at which character replacement ends.
        /// </param>
        /// <param name="value">The reference to the start of the character buffer.</param>
        /// <param name="count">The number of characters in the buffer.</param>
        private void ReplaceInPlaceAtChunk(ref StringBuilder? chunk, ref int indexInChunk, ref char value, int count)
        {
            if (count != 0)
            {
                while (true)
                {
                    Debug.Assert(chunk != null, "chunk should not be null at this point");
                    int lengthInChunk = chunk.m_ChunkLength - indexInChunk;
                    Debug.Assert(lengthInChunk >= 0, "Index isn't in the chunk.");

                    int lengthToCopy = Math.Min(lengthInChunk, count);
                    new ReadOnlySpan<char>(ref value, lengthToCopy).CopyTo(chunk.m_ChunkChars.AsSpan(indexInChunk));

                    // Advance the index.
                    indexInChunk += lengthToCopy;
                    if (indexInChunk >= chunk.m_ChunkLength)
                    {
                        chunk = Next(chunk);
                        indexInChunk = 0;
                    }
                    count -= lengthToCopy;
                    if (count == 0)
                    {
                        break;
                    }
                    value = ref Unsafe.Add(ref value, lengthToCopy);
                }
            }
        }

        /// <summary>
        /// Gets the chunk corresponding to the logical index in this builder.
        /// </summary>
        /// <param name="index">The logical index in this builder.</param>
        /// <remarks>
        /// After calling this method, you can obtain the actual index within the chunk by
        /// subtracting <see cref="m_ChunkOffset"/> from <paramref name="index"/>.
        /// </remarks>
        private StringBuilder FindChunkForIndex(int index)
        {
            Debug.Assert(0 <= index && index <= Length);

            StringBuilder result = this;
            while (result.m_ChunkOffset > index)
            {
                Debug.Assert(result.m_ChunkPrevious != null);
                result = result.m_ChunkPrevious;
            }

            Debug.Assert(result != null);
            return result;
        }

        /// <summary>Gets a span representing the remaining space available in the current chunk.</summary>
        private Span<char> RemainingCurrentChunk
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Span<char>(m_ChunkChars, m_ChunkLength, m_ChunkChars.Length - m_ChunkLength);
        }

        /// <summary>
        /// Finds the chunk that logically succeeds the specified chunk.
        /// </summary>
        /// <param name="chunk">The chunk whose successor should be found.</param>
        /// <remarks>
        /// Each chunk only stores the reference to its logical predecessor, so this routine has to start
        /// from the 'this' reference (which is assumed to represent the whole StringBuilder) and work its
        /// way down until it finds the specified chunk (which is O(n)). Thus, it is more expensive than
        /// a field fetch.
        /// </remarks>
        private StringBuilder? Next(StringBuilder chunk) => chunk == this ? null : FindChunkForIndex(chunk.m_ChunkOffset + chunk.m_ChunkLength);

        /// <summary>
        /// Transfers the character buffer from this chunk to a new chunk, and allocates a new buffer with a minimum size for this chunk.
        /// </summary>
        /// <param name="minBlockCharCount">The minimum size of the new buffer to be allocated for this chunk.</param>
        /// <remarks>
        /// This method requires that the current chunk is full. Otherwise, there's no point in shifting the characters over.
        /// It also assumes that 'this' is the last chunk in the linked list.
        /// </remarks>
        private void ExpandByABlock(int minBlockCharCount)
        {
            Debug.Assert(Capacity == Length, nameof(ExpandByABlock) + " should only be called when there is no space left.");
            Debug.Assert(minBlockCharCount > 0);

            AssertInvariants();

            if ((minBlockCharCount + Length) > m_MaxCapacity || minBlockCharCount + Length < minBlockCharCount)
            {
                throw new ArgumentOutOfRangeException("requiredLength", SR.ArgumentOutOfRange_SmallCapacity);
            }

            // - We always need to make the new chunk at least as big as was requested (`minBlockCharCount`).
            // - We'd also prefer to make it at least at big as the current length (thus doubling capacity).
            //   - But this is only up to a maximum, so we stay in the small object heap, and never allocate
            //     really big chunks even if the string gets really big.
            int newBlockLength = Math.Max(minBlockCharCount, Math.Min(Length, MaxChunkSize));

            // Check for integer overflow (logical buffer size > int.MaxValue)
            if (m_ChunkOffset + m_ChunkLength + newBlockLength < newBlockLength)
            {
                throw new OutOfMemoryException();
            }

            // Allocate the array before updating any state to avoid leaving inconsistent state behind in case of out of memory exception
            char[] chunkChars = GC.AllocateUninitializedArray<char>(newBlockLength);

            // Move all of the data from this chunk to a new one, via a few O(1) reference adjustments.
            // Then, have this chunk point to the new one as its predecessor.
            m_ChunkPrevious = new StringBuilder(this);
            m_ChunkOffset += m_ChunkLength;
            m_ChunkLength = 0;

            m_ChunkChars = chunkChars;

            AssertInvariants();
        }

        /// <summary>
        /// Creates a new chunk with fields copied from an existing chunk.
        /// </summary>
        /// <param name="from">The chunk from which to copy fields.</param>
        /// <remarks>
        /// <para>
        /// This method runs in O(1) time. It does not copy data within the character buffer
        /// <paramref name="from"/> holds, but copies the reference to the character buffer itself
        /// (plus a few other fields).
        /// </para>
        /// <para>
        /// Callers are expected to update <paramref name="from"/> subsequently to point to this
        /// chunk as its predecessor.
        /// </para>
        /// </remarks>
        private StringBuilder(StringBuilder from)
        {
            m_ChunkLength = from.m_ChunkLength;
            m_ChunkOffset = from.m_ChunkOffset;
            m_ChunkChars = from.m_ChunkChars;
            m_ChunkPrevious = from.m_ChunkPrevious;
            m_MaxCapacity = from.m_MaxCapacity;

            AssertInvariants();
        }

        /// <summary>
        /// Creates a gap at a logical index with the specified count.
        /// </summary>
        /// <param name="index">The logical index in this builder.</param>
        /// <param name="count">The number of characters in the gap.</param>
        /// <param name="chunk">Receives the chunk containing the gap.</param>
        /// <param name="indexInChunk">The index in <paramref name="chunk"/> that points to the gap.</param>
        /// <param name="doNotMoveFollowingChars">
        /// - If <c>true</c>, then room must be made by inserting a chunk before the current chunk.
        /// - If <c>false</c>, then room can be made by shifting characters ahead of <paramref name="index"/>
        ///   in this block forward by <paramref name="count"/> provided the characters will still fit in
        ///   the current chunk after being shifted.
        ///   - Providing <c>false</c> does not make a difference most of the time, but it can matter when someone
        ///     inserts lots of small strings at a position in the buffer.
        /// </param>
        /// <remarks>
        /// <para>
        /// Since chunks do not contain references to their successors, it is not always possible for us to make room
        /// by inserting space after <paramref name="index"/> in case this chunk runs out of space. Thus, we make room
        /// by inserting space before the specified index, and having logical indices refer to new locations by the end
        /// of this method.
        /// </para>
        /// <para>
        /// <see cref="ReplaceInPlaceAtChunk"/> can be used in conjunction with this method to fill in the newly created gap.
        /// </para>
        /// </remarks>
        private void MakeRoom(int index, int count, out StringBuilder chunk, out int indexInChunk, bool doNotMoveFollowingChars)
        {
            AssertInvariants();
            Debug.Assert(count > 0);
            Debug.Assert(index >= 0);

            if (count + Length > m_MaxCapacity || count + Length < count)
            {
                throw new ArgumentOutOfRangeException("requiredLength", SR.ArgumentOutOfRange_SmallCapacity);
            }

            chunk = this;
            while (chunk.m_ChunkOffset > index)
            {
                chunk.m_ChunkOffset += count;
                Debug.Assert(chunk.m_ChunkPrevious != null);
                chunk = chunk.m_ChunkPrevious;
            }
            indexInChunk = index - chunk.m_ChunkOffset;

            // Cool, we have some space in this block, and we don't have to copy much to get at it, so go ahead and use it.
            // This typically happens when someone repeatedly inserts small strings at a spot (usually the absolute front) of the buffer.
            if (!doNotMoveFollowingChars && chunk.m_ChunkLength <= DefaultCapacity * 2 && chunk.m_ChunkChars.Length - chunk.m_ChunkLength >= count)
            {
                for (int i = chunk.m_ChunkLength; i > indexInChunk;)
                {
                    --i;
                    chunk.m_ChunkChars[i + count] = chunk.m_ChunkChars[i];
                }
                chunk.m_ChunkLength += count;
                return;
            }

            // Allocate space for the new chunk, which will go before the current one.
            StringBuilder newChunk = new StringBuilder(Math.Max(count, DefaultCapacity), chunk.m_MaxCapacity, chunk.m_ChunkPrevious);
            newChunk.m_ChunkLength = count;

            // Copy the head of the current buffer to the new buffer.
            int copyCount1 = Math.Min(count, indexInChunk);
            if (copyCount1 > 0)
            {
                new ReadOnlySpan<char>(chunk.m_ChunkChars, 0, copyCount1).CopyTo(newChunk.m_ChunkChars);

                // Slide characters over in the current buffer to make room.
                int copyCount2 = indexInChunk - copyCount1;
                if (copyCount2 >= 0)
                {
                    new ReadOnlySpan<char>(chunk.m_ChunkChars, copyCount1, copyCount2).CopyTo(chunk.m_ChunkChars);
                    indexInChunk = copyCount2;
                }
            }

            // Wire in the new chunk.
            chunk.m_ChunkPrevious = newChunk;
            chunk.m_ChunkOffset += count;
            if (copyCount1 < count)
            {
                chunk = newChunk;
                indexInChunk = copyCount1;
            }

            AssertInvariants();
        }

        /// <summary>
        /// Used by <see cref="MakeRoom"/> to allocate another chunk.
        /// </summary>
        /// <param name="size">The size of the character buffer for this chunk.</param>
        /// <param name="maxCapacity">The maximum capacity, to be stored in this chunk.</param>
        /// <param name="previousBlock">The predecessor of this chunk.</param>
        private StringBuilder(int size, int maxCapacity, StringBuilder? previousBlock)
        {
            Debug.Assert(size > 0);
            Debug.Assert(maxCapacity > 0);

            m_ChunkChars = GC.AllocateUninitializedArray<char>(size);
            m_MaxCapacity = maxCapacity;
            m_ChunkPrevious = previousBlock;
            if (previousBlock != null)
            {
                m_ChunkOffset = previousBlock.m_ChunkOffset + previousBlock.m_ChunkLength;
            }

            AssertInvariants();
        }

        /// <summary>
        /// Removes a specified number of characters beginning at a logical index in this builder.
        /// </summary>
        /// <param name="startIndex">The logical index in this builder to start removing characters.</param>
        /// <param name="count">The number of characters to remove.</param>
        /// <param name="chunk">Receives the new chunk containing the logical index.</param>
        /// <param name="indexInChunk">
        /// Receives the new index in <paramref name="chunk"/> that is associated with the logical index.
        /// </param>
        private void Remove(int startIndex, int count, out StringBuilder chunk, out int indexInChunk)
        {
            AssertInvariants();
            Debug.Assert(startIndex >= 0 && startIndex < Length);

            int endIndex = startIndex + count;

            // Find the chunks for the start and end of the block to delete.
            chunk = this;
            StringBuilder? endChunk = null;
            int endIndexInChunk = 0;
            while (true)
            {
                if (endIndex - chunk.m_ChunkOffset >= 0)
                {
                    if (endChunk == null)
                    {
                        endChunk = chunk;
                        endIndexInChunk = endIndex - endChunk.m_ChunkOffset;
                    }
                    if (startIndex - chunk.m_ChunkOffset >= 0)
                    {
                        indexInChunk = startIndex - chunk.m_ChunkOffset;
                        break;
                    }
                }
                else
                {
                    chunk.m_ChunkOffset -= count;
                }

                Debug.Assert(chunk.m_ChunkPrevious != null);
                chunk = chunk.m_ChunkPrevious;
            }
            Debug.Assert(chunk != null, "We fell off the beginning of the string!");

            int copyTargetIndexInChunk = indexInChunk;
            int copyCount = endChunk.m_ChunkLength - endIndexInChunk;
            if (endChunk != chunk)
            {
                copyTargetIndexInChunk = 0;
                // Remove the characters after `startIndex` to the end of the chunk.
                chunk.m_ChunkLength = indexInChunk;

                // Remove the characters in chunks between the start and the end chunk.
                endChunk.m_ChunkPrevious = chunk;
                endChunk.m_ChunkOffset = chunk.m_ChunkOffset + chunk.m_ChunkLength;

                // If the start is 0, then we can throw away the whole start chunk.
                if (indexInChunk == 0)
                {
                    endChunk.m_ChunkPrevious = chunk.m_ChunkPrevious;
                    chunk = endChunk;
                }
            }
            endChunk.m_ChunkLength -= (endIndexInChunk - copyTargetIndexInChunk);

            // SafeCritical: We ensure that `endIndexInChunk + copyCount` is within range of `m_ChunkChars`, and
            // also ensure that `copyTargetIndexInChunk + copyCount` is within the chunk.

            // Remove any characters in the end chunk, by sliding the characters down.
            if (copyTargetIndexInChunk != endIndexInChunk) // Sometimes no move is necessary
            {
                new ReadOnlySpan<char>(endChunk.m_ChunkChars, endIndexInChunk, copyCount).CopyTo(endChunk.m_ChunkChars.AsSpan(copyTargetIndexInChunk));
            }

            Debug.Assert(chunk != null, "We fell off the beginning of the string!");
            AssertInvariants();
        }

        /// <summary>Provides a handler used by the language compiler to append interpolated strings into <see cref="StringBuilder"/> instances.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [InterpolatedStringHandler]
        public struct AppendInterpolatedStringHandler
        {
            // Implementation note:
            // As this type is only intended to be targeted by the compiler, public APIs eschew argument validation logic
            // in a variety of places, e.g. allowing a null input when one isn't expected to produce a NullReferenceException rather
            // than an ArgumentNullException.

            /// <summary>The associated StringBuilder to which to append.</summary>
            internal readonly StringBuilder _stringBuilder;
            /// <summary>Optional provider to pass to IFormattable.ToString or ISpanFormattable.TryFormat calls.</summary>
            private readonly IFormatProvider? _provider;
            /// <summary>Whether <see cref="_provider"/> provides an ICustomFormatter.</summary>
            /// <remarks>
            /// Custom formatters are very rare.  We want to support them, but it's ok if we make them more expensive
            /// in order to make them as pay-for-play as possible.  So, we avoid adding another reference type field
            /// to reduce the size of the handler and to reduce required zero'ing, by only storing whether the provider
            /// provides a formatter, rather than actually storing the formatter.  This in turn means, if there is a
            /// formatter, we pay for the extra interface call on each AppendFormatted that needs it.
            /// </remarks>
            private readonly bool _hasCustomFormatter;

            /// <summary>Creates a handler used to append an interpolated string into a <see cref="StringBuilder"/>.</summary>
            /// <param name="literalLength">The number of constant characters outside of interpolation expressions in the interpolated string.</param>
            /// <param name="formattedCount">The number of interpolation expressions in the interpolated string.</param>
            /// <param name="stringBuilder">The associated StringBuilder to which to append.</param>
            /// <remarks>This is intended to be called only by compiler-generated code. Arguments are not validated as they'd otherwise be for members intended to be used directly.</remarks>
            public AppendInterpolatedStringHandler(int literalLength, int formattedCount, StringBuilder stringBuilder)
            {
                _stringBuilder = stringBuilder;
                _provider = null;
                _hasCustomFormatter = false;
            }

            /// <summary>Creates a handler used to translate an interpolated string into a <see cref="string"/>.</summary>
            /// <param name="literalLength">The number of constant characters outside of interpolation expressions in the interpolated string.</param>
            /// <param name="formattedCount">The number of interpolation expressions in the interpolated string.</param>
            /// <param name="stringBuilder">The associated StringBuilder to which to append.</param>
            /// <param name="provider">An object that supplies culture-specific formatting information.</param>
            /// <remarks>This is intended to be called only by compiler-generated code. Arguments are not validated as they'd otherwise be for members intended to be used directly.</remarks>
            public AppendInterpolatedStringHandler(int literalLength, int formattedCount, StringBuilder stringBuilder, IFormatProvider? provider)
            {
                _stringBuilder = stringBuilder;
                _provider = provider;
                _hasCustomFormatter = provider is not null && DefaultInterpolatedStringHandler.HasCustomFormatter(provider);
            }

            /// <summary>Writes the specified string to the handler.</summary>
            /// <param name="value">The string to write.</param>
            public void AppendLiteral(string value) => _stringBuilder.Append(value);

            #region AppendFormatted
            // Design note:
            // This provides the same set of overloads and semantics as DefaultInterpolatedStringHandler.

            #region AppendFormatted T
            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <typeparam name="T">The type of the value to write.</typeparam>
            public void AppendFormatted<T>(T value)
            {
                // This method could delegate to AppendFormatted with a null format, but explicitly passing
                // default as the format to TryFormat helps to improve code quality in some cases when TryFormat is inlined,
                // e.g. for Int32 it enables the JIT to eliminate code in the inlined method based on a length check on the format.

                if (_hasCustomFormatter)
                {
                    // If there's a custom formatter, always use it.
                    AppendCustomFormatter(value, format: null);
                }
                else if (value is IFormattable)
                {
                    // Check first for IFormattable, even though we'll prefer to use ISpanFormattable, as the latter
                    // requires the former.  For value types, it won't matter as the type checks devolve into
                    // JIT-time constants.  For reference types, they're more likely to implement IFormattable
                    // than they are to implement ISpanFormattable: if they don't implement either, we save an
                    // interface check over first checking for ISpanFormattable and then for IFormattable, and
                    // if it only implements IFormattable, we come out even: only if it implements both do we
                    // end up paying for an extra interface check.

                    if (typeof(T).IsEnum)
                    {
                        if (Enum.TryFormatUnconstrained(value, _stringBuilder.RemainingCurrentChunk, out int charsWritten))
                        {
                            _stringBuilder.m_ChunkLength += charsWritten;
                        }
                        else
                        {
                            AppendFormattedWithTempSpace(value, 0, format: null);
                        }
                    }
                    else if (value is ISpanFormattable)
                    {
                        Span<char> destination = _stringBuilder.RemainingCurrentChunk;
                        if (((ISpanFormattable)value).TryFormat(destination, out int charsWritten, default, _provider)) // constrained call avoiding boxing for value types
                        {
                            if ((uint)charsWritten > (uint)destination.Length)
                            {
                                // Protect against faulty ISpanFormattable implementations returning invalid charsWritten values.
                                // Other code in _stringBuilder uses Unsafe manipulation, and we want to ensure m_ChunkLength remains safe.
                                ThrowHelper.ThrowFormatInvalidString();
                            }

                            _stringBuilder.m_ChunkLength += charsWritten;
                        }
                        else
                        {
                            // Not enough room in the current chunk.  Take the slow path that formats into temporary space
                            // and then copies the result into the StringBuilder.
                            AppendFormattedWithTempSpace(value, 0, format: null);
                        }
                    }
                    else
                    {
                        _stringBuilder.Append(((IFormattable)value).ToString(format: null, _provider)); // constrained call avoiding boxing for value types
                    }
                }
                else if (value is not null)
                {
                    _stringBuilder.Append(value.ToString());
                }
            }

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="format">The format string.</param>
            /// <typeparam name="T">The type of the value to write.</typeparam>
            public void AppendFormatted<T>(T value, string? format)
            {
                if (_hasCustomFormatter)
                {
                    // If there's a custom formatter, always use it.
                    AppendCustomFormatter(value, format);
                }
                else if (value is IFormattable)
                {
                    // Check first for IFormattable, even though we'll prefer to use ISpanFormattable, as the latter
                    // requires the former.  For value types, it won't matter as the type checks devolve into
                    // JIT-time constants.  For reference types, they're more likely to implement IFormattable
                    // than they are to implement ISpanFormattable: if they don't implement either, we save an
                    // interface check over first checking for ISpanFormattable and then for IFormattable, and
                    // if it only implements IFormattable, we come out even: only if it implements both do we
                    // end up paying for an extra interface check.

                    if (typeof(T).IsEnum)
                    {
                        if (Enum.TryFormatUnconstrained(value, _stringBuilder.RemainingCurrentChunk, out int charsWritten, format))
                        {
                            _stringBuilder.m_ChunkLength += charsWritten;
                        }
                        else
                        {
                            AppendFormattedWithTempSpace(value, 0, format);
                        }
                    }
                    else if (value is ISpanFormattable)
                    {
                        Span<char> destination = _stringBuilder.RemainingCurrentChunk;
                        if (((ISpanFormattable)value).TryFormat(destination, out int charsWritten, format, _provider)) // constrained call avoiding boxing for value types
                        {
                            if ((uint)charsWritten > (uint)destination.Length)
                            {
                                // Protect against faulty ISpanFormattable implementations returning invalid charsWritten values.
                                // Other code in _stringBuilder uses Unsafe manipulation, and we want to ensure m_ChunkLength remains safe.
                                ThrowHelper.ThrowFormatInvalidString();
                            }

                            _stringBuilder.m_ChunkLength += charsWritten;
                        }
                        else
                        {
                            // Not enough room in the current chunk.  Take the slow path that formats into temporary space
                            // and then copies the result into the StringBuilder.
                            AppendFormattedWithTempSpace(value, 0, format);
                        }
                    }
                    else
                    {
                        _stringBuilder.Append(((IFormattable)value).ToString(format, _provider)); // constrained call avoiding boxing for value types
                    }
                }
                else if (value is not null)
                {
                    _stringBuilder.Append(value.ToString());
                }
            }

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            /// <typeparam name="T">The type of the value to write.</typeparam>
            public void AppendFormatted<T>(T value, int alignment) =>
                AppendFormatted(value, alignment, format: null);

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="format">The format string.</param>
            /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            /// <typeparam name="T">The type of the value to write.</typeparam>
            public void AppendFormatted<T>(T value, int alignment, string? format)
            {
                if (alignment == 0)
                {
                    // This overload is used as a fallback from several disambiguation overloads, so special-case 0.
                    AppendFormatted(value, format);
                }
                else if (alignment < 0)
                {
                    // Left aligned: format into the handler, then append any additional padding required.
                    int start = _stringBuilder.Length;
                    AppendFormatted(value, format);
                    int paddingRequired = -alignment - (_stringBuilder.Length - start);
                    if (paddingRequired > 0)
                    {
                        _stringBuilder.Append(' ', paddingRequired);
                    }
                }
                else
                {
                    // Right aligned: format into temporary space and then copy that into the handler, appropriately aligned.
                    AppendFormattedWithTempSpace(value, alignment, format);
                }
            }

            /// <summary>Formats into temporary space and then appends the result into the StringBuilder.</summary>
            private void AppendFormattedWithTempSpace<T>(T value, int alignment, string? format)
            {
                // It's expected that either there's not enough space in the current chunk to store this formatted value,
                // or we have a non-0 alignment that could require padding inserted. So format into temporary space and
                // then append that written span into the StringBuilder: StringBuilder.Append(span) is able to split the
                // span across the current chunk and any additional chunks required.

                var handler = new DefaultInterpolatedStringHandler(0, 0, _provider, stackalloc char[string.StackallocCharBufferSizeLimit]);
                handler.AppendFormatted(value, format);
                AppendFormatted(handler.Text, alignment);
                handler.Clear();
            }
            #endregion

            #region AppendFormatted ReadOnlySpan<char>
            /// <summary>Writes the specified character span to the handler.</summary>
            /// <param name="value">The span to write.</param>
            public void AppendFormatted(ReadOnlySpan<char> value) => _stringBuilder.Append(value);

            /// <summary>Writes the specified string of chars to the handler.</summary>
            /// <param name="value">The span to write.</param>
            /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            /// <param name="format">The format string.</param>
            public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null)
            {
                if (alignment == 0)
                {
                    _stringBuilder.Append(value);
                }
                else
                {
                    bool leftAlign = false;
                    if (alignment < 0)
                    {
                        leftAlign = true;
                        alignment = -alignment;
                    }

                    int paddingRequired = alignment - value.Length;
                    if (paddingRequired <= 0)
                    {
                        _stringBuilder.Append(value);
                    }
                    else if (leftAlign)
                    {
                        _stringBuilder.Append(value);
                        _stringBuilder.Append(' ', paddingRequired);
                    }
                    else
                    {
                        _stringBuilder.Append(' ', paddingRequired);
                        _stringBuilder.Append(value);
                    }
                }
            }
            #endregion

            #region AppendFormatted string
            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            public void AppendFormatted(string? value)
            {
                if (!_hasCustomFormatter)
                {
                    _stringBuilder.Append(value);
                }
                else
                {
                    AppendFormatted<string?>(value);
                }
            }

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            /// <param name="format">The format string.</param>
            public void AppendFormatted(string? value, int alignment = 0, string? format = null) =>
                // Format is meaningless for strings and doesn't make sense for someone to specify.  We have the overload
                // simply to disambiguate between ROS<char> and object, just in case someone does specify a format, as
                // string is implicitly convertible to both. Just delegate to the T-based implementation.
                AppendFormatted<string?>(value, alignment, format);
            #endregion

            #region AppendFormatted object
            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            /// <param name="format">The format string.</param>
            public void AppendFormatted(object? value, int alignment = 0, string? format = null) =>
                // This overload is expected to be used rarely, only if either a) something strongly typed as object is
                // formatted with both an alignment and a format, or b) the compiler is unable to target type to T. It
                // exists purely to help make cases from (b) compile. Just delegate to the T-based implementation.
                AppendFormatted<object?>(value, alignment, format);
            #endregion
            #endregion

            /// <summary>Formats the value using the custom formatter from the provider.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="format">The format string.</param>
            /// <typeparam name="T">The type of the value to write.</typeparam>
            [MethodImpl(MethodImplOptions.NoInlining)]
            private void AppendCustomFormatter<T>(T value, string? format)
            {
                // This case is very rare, but we need to handle it prior to the other checks in case
                // a provider was used that supplied an ICustomFormatter which wanted to intercept the particular value.
                // We do the cast here rather than in the ctor, even though this could be executed multiple times per
                // formatting, to make the cast pay for play.
                Debug.Assert(_hasCustomFormatter);
                Debug.Assert(_provider != null);

                ICustomFormatter? formatter = (ICustomFormatter?)_provider.GetFormat(typeof(ICustomFormatter));
                Debug.Assert(formatter != null, "An incorrectly written provider said it implemented ICustomFormatter, and then didn't");

                if (formatter is not null)
                {
                    _stringBuilder.Append(formatter.Format(format, value, _provider));
                }
            }
        }
    }
}
