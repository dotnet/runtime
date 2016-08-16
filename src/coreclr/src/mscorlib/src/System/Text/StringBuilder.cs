// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: implementation of the StringBuilder
** class.
**
===========================================================*/
namespace System.Text {
    using System.Text;
    using System.Runtime;
    using System.Runtime.Serialization;
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;
    using System.Security;
    using System.Threading;
    using System.Globalization;
    using System.Diagnostics.Contracts;

    // This class represents a mutable string.  It is convenient for situations in
    // which it is desirable to modify a string, perhaps by removing, replacing, or 
    // inserting characters, without creating a new String subsequent to
    // each modification. 
    // 
    // The methods contained within this class do not return a new StringBuilder
    // object unless specified otherwise.  This class may be used in conjunction with the String
    // class to carry out modifications upon strings.
    // 
    // When passing null into a constructor in VJ and VC, the null
    // should be explicitly type cast.
    // For Example:
    // StringBuilder sb1 = new StringBuilder((StringBuilder)null);
    // StringBuilder sb2 = new StringBuilder((String)null);
    // Console.WriteLine(sb1);
    // Console.WriteLine(sb2);
    // 
    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public sealed class StringBuilder : ISerializable
    {
        // A StringBuilder is internally represented as a linked list of blocks each of which holds
        // a chunk of the string.  It turns out string as a whole can also be represented as just a chunk, 
        // so that is what we do.  

        //
        //
        //  CLASS VARIABLES
        //
        //
        internal char[] m_ChunkChars;                // The characters in this block
        internal StringBuilder m_ChunkPrevious;      // Link to the block logically before this block
        internal int m_ChunkLength;                  // The index in m_ChunkChars that represent the end of the block
        internal int m_ChunkOffset;                  // The logial offset (sum of all characters in previous blocks)
        internal int m_MaxCapacity = 0;

        //
        //
        // STATIC CONSTANTS
        //
        //
        internal const int DefaultCapacity = 16;
        private const String CapacityField = "Capacity";
        private const String MaxCapacityField = "m_MaxCapacity";
        private const String StringValueField = "m_StringValue";
        private const String ThreadIDField = "m_currentThread";
        // We want to keep chunk arrays out of large object heap (< 85K bytes ~ 40K chars) to be sure.
        // Making the maximum chunk size big means less allocation code called, but also more waste
        // in unused characters and slower inserts / replaces (since you do need to slide characters over
        // within a buffer).  
        internal const int MaxChunkSize = 8000;

        //
        //
        //CONSTRUCTORS
        //
        //

        // Creates a new empty string builder (i.e., it represents String.Empty)
        // with the default capacity (16 characters).
        public StringBuilder()
            : this(DefaultCapacity) {
        }

        // Create a new empty string builder (i.e., it represents String.Empty)
        // with the specified capacity.
        public StringBuilder(int capacity)
            : this(String.Empty, capacity) {
        }

        // Creates a new string builder from the specified string.  If value
        // is a null String (i.e., if it represents String.NullString)
        // then the new string builder will also be null (i.e., it will also represent
        //  String.NullString).
        // 
        public StringBuilder(String value)
            : this(value, DefaultCapacity) {
        }

        // Creates a new string builder from the specified string with the specified 
        // capacity.  If value is a null String (i.e., if it represents 
        // String.NullString) then the new string builder will also be null 
        // (i.e., it will also represent String.NullString).
        // The maximum number of characters this string may contain is set by capacity.
        // 
        public StringBuilder(String value, int capacity)
            : this(value, 0, ((value != null) ? value.Length : 0), capacity) {
        }

        // Creates a new string builder from the specifed substring with the specified
        // capacity.  The maximum number of characters is set by capacity.
        // 
        [System.Security.SecuritySafeCritical]  // auto-generated
        public StringBuilder(String value, int startIndex, int length, int capacity) {
            if (capacity<0) {
                throw new ArgumentOutOfRangeException("capacity",
                                                      Environment.GetResourceString("ArgumentOutOfRange_MustBePositive", "capacity"));
            }
            if (length<0) {
                throw new ArgumentOutOfRangeException("length",
                                                      Environment.GetResourceString("ArgumentOutOfRange_MustBeNonNegNum", "length"));
            }
            if (startIndex<0) {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            }
            Contract.EndContractBlock();

            if (value == null) {
                value = String.Empty;
            }
            if (startIndex > value.Length - length) {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_IndexLength"));
            }
            m_MaxCapacity = Int32.MaxValue;
            if (capacity == 0) {
                capacity = DefaultCapacity;
            }
            if (capacity < length)
                capacity = length;

            m_ChunkChars = new char[capacity];
            m_ChunkLength = length;

            unsafe {
                fixed (char* sourcePtr = value)
                    ThreadSafeCopy(sourcePtr + startIndex, m_ChunkChars, 0, length);
            }
        }

        // Creates an empty StringBuilder with a minimum capacity of capacity
        // and a maximum capacity of maxCapacity.
        public StringBuilder(int capacity, int maxCapacity) {
            if (capacity>maxCapacity) {
                throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_Capacity"));
            }
            if (maxCapacity<1) {
                throw new ArgumentOutOfRangeException("maxCapacity", Environment.GetResourceString("ArgumentOutOfRange_SmallMaxCapacity"));
            }
            if (capacity<0) {
                throw new ArgumentOutOfRangeException("capacity",
                                                      Environment.GetResourceString("ArgumentOutOfRange_MustBePositive", "capacity"));
            }
            Contract.EndContractBlock();

            if (capacity == 0) {
                capacity = Math.Min(DefaultCapacity, maxCapacity);
            }

            m_MaxCapacity = maxCapacity;
            m_ChunkChars = new char[capacity];
        }

        [System.Security.SecurityCritical]  // auto-generated
        private StringBuilder(SerializationInfo info, StreamingContext context) {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();

            int persistedCapacity = 0;
            string persistedString = null;
            int persistedMaxCapacity = Int32.MaxValue;
            bool capacityPresent = false;

            // Get the data
            SerializationInfoEnumerator enumerator = info.GetEnumerator();
            while (enumerator.MoveNext()) {
                switch (enumerator.Name) {
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
                        // Ignore other fields for forward compatibility.
                        break;
                }

            }

            // Check values and set defaults
            if (persistedString == null) {
                persistedString = String.Empty;
            }
            if (persistedMaxCapacity < 1 || persistedString.Length > persistedMaxCapacity) {
                throw new SerializationException(Environment.GetResourceString("Serialization_StringBuilderMaxCapacity"));
            }

            if (!capacityPresent) {
                // StringBuilder in V1.X did not persist the Capacity, so this is a valid legacy code path.
                persistedCapacity = DefaultCapacity;
                if (persistedCapacity < persistedString.Length) {
                    persistedCapacity = persistedString.Length;
                }
                if (persistedCapacity > persistedMaxCapacity) {
                    persistedCapacity = persistedMaxCapacity;
                }
            }
            if (persistedCapacity < 0 || persistedCapacity < persistedString.Length || persistedCapacity > persistedMaxCapacity) {
                throw new SerializationException(Environment.GetResourceString("Serialization_StringBuilderCapacity"));
            }

            // Assign
            m_MaxCapacity = persistedMaxCapacity;
            m_ChunkChars = new char[persistedCapacity];
            persistedString.CopyTo(0, m_ChunkChars, 0, persistedString.Length);
            m_ChunkLength = persistedString.Length;
            m_ChunkPrevious = null;
            VerifyClassInvariant();
        }

        [System.Security.SecurityCritical]  // auto-generated
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info==null) {
                throw new ArgumentNullException("info");
            }
            Contract.EndContractBlock();

            VerifyClassInvariant();
            info.AddValue(MaxCapacityField, m_MaxCapacity);
            info.AddValue(CapacityField, Capacity);
            info.AddValue(StringValueField, ToString());
            // Note: persist "m_currentThread" to be compatible with old versions
            info.AddValue(ThreadIDField, 0);
        }

        [System.Diagnostics.Conditional("_DEBUG")]
        private void VerifyClassInvariant() {
            BCLDebug.Correctness((uint)(m_ChunkOffset + m_ChunkChars.Length) >= m_ChunkOffset, "Integer Overflow");
            StringBuilder currentBlock = this;
            int maxCapacity = this.m_MaxCapacity;
            for (; ; )
            {
                // All blocks have copy of the maxCapacity.
                Contract.Assert(currentBlock.m_MaxCapacity == maxCapacity, "Bad maxCapacity");
                Contract.Assert(currentBlock.m_ChunkChars != null, "Empty Buffer");

                Contract.Assert(currentBlock.m_ChunkLength <= currentBlock.m_ChunkChars.Length, "Out of range length");
                Contract.Assert(currentBlock.m_ChunkLength >= 0, "Negative length");
                Contract.Assert(currentBlock.m_ChunkOffset >= 0, "Negative offset");

                StringBuilder prevBlock = currentBlock.m_ChunkPrevious;
                if (prevBlock == null)
                {
                    Contract.Assert(currentBlock.m_ChunkOffset == 0, "First chunk's offset is not 0");
                    break;
                }
                // There are no gaps in the blocks. 
                Contract.Assert(currentBlock.m_ChunkOffset == prevBlock.m_ChunkOffset + prevBlock.m_ChunkLength, "There is a gap between chunks!");
                currentBlock = prevBlock;
            }
        }

        public int Capacity {
            get { return m_ChunkChars.Length + m_ChunkOffset; }
            set {
                if (value < 0) {
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NegativeCapacity"));
                }
                if (value > MaxCapacity) {
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_Capacity"));
                }
                if (value < Length) {
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
                }
                Contract.EndContractBlock();

                if (Capacity != value) {
                    int newLen = value - m_ChunkOffset;
                    char[] newArray = new char[newLen];
                    Array.Copy(m_ChunkChars, newArray, m_ChunkLength);
                    m_ChunkChars = newArray;
                }
            }
        }

        public int MaxCapacity {
            get { return m_MaxCapacity; }
        }

        // Read-Only Property 
        // Ensures that the capacity of this string builder is at least the specified value.  
        // If capacity is greater than the capacity of this string builder, then the capacity
        // is set to capacity; otherwise the capacity is unchanged.
        // 
        public int EnsureCapacity(int capacity) {
            if (capacity < 0) {
                throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_NegativeCapacity"));
            }
            Contract.EndContractBlock();

            if (Capacity < capacity)
                Capacity = capacity;
            return Capacity;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override String ToString() {
            Contract.Ensures(Contract.Result<String>() != null);

            VerifyClassInvariant();
            
            if (Length == 0)
                return String.Empty;

            string ret = string.FastAllocateString(Length);
            StringBuilder chunk = this;
            unsafe {
                fixed (char* destinationPtr = ret)
                {
                    do
                    {
                        if (chunk.m_ChunkLength > 0)
                        {
                            // Copy these into local variables so that they are stable even in the presence of race conditions
                            char[] sourceArray = chunk.m_ChunkChars;
                            int chunkOffset = chunk.m_ChunkOffset;
                            int chunkLength = chunk.m_ChunkLength;
    
                            // Check that we will not overrun our boundaries. 
                            if ((uint)(chunkLength + chunkOffset) <= ret.Length && (uint)chunkLength <= (uint)sourceArray.Length)
                            {
                                fixed (char* sourcePtr = sourceArray)
                                    string.wstrcpy(destinationPtr + chunkOffset, sourcePtr, chunkLength);
                            }
                            else
                            {
                                throw new ArgumentOutOfRangeException("chunkLength", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                            }
                        }
                        chunk = chunk.m_ChunkPrevious;
                    } while (chunk != null);
                }
            }
            return ret;
        }


        // Converts a substring of this string builder to a String.
        [System.Security.SecuritySafeCritical]  // auto-generated
        public String ToString(int startIndex, int length) {
            Contract.Ensures(Contract.Result<String>() != null);

            int currentLength = this.Length;
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            }
            if (startIndex > currentLength)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndexLargerThanLength"));
            }
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NegativeLength"));
            }
            if (startIndex > (currentLength - length))
            {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_IndexLength"));
            }

            VerifyClassInvariant();

            StringBuilder chunk = this;
            int sourceEndIndex = startIndex + length;

            string ret = string.FastAllocateString(length);
            int curDestIndex = length;
            unsafe {
                fixed (char* destinationPtr = ret)
                {
                    while (curDestIndex > 0)
                    {
                        int chunkEndIndex = sourceEndIndex - chunk.m_ChunkOffset;
                        if (chunkEndIndex >= 0)
                        {
                            if (chunkEndIndex > chunk.m_ChunkLength)
                                chunkEndIndex = chunk.m_ChunkLength;
    
                            int countLeft = curDestIndex;
                            int chunkCount = countLeft;
                            int chunkStartIndex = chunkEndIndex - countLeft;
                            if (chunkStartIndex < 0)
                            {
                                chunkCount += chunkStartIndex;
                                chunkStartIndex = 0;
                            }
                            curDestIndex -= chunkCount;
    
                            if (chunkCount > 0)
                            {
                                // work off of local variables so that they are stable even in the presence of race conditions
                                char[] sourceArray = chunk.m_ChunkChars;
    
                                // Check that we will not overrun our boundaries. 
                                if ((uint)(chunkCount + curDestIndex) <= length && (uint)(chunkCount + chunkStartIndex) <= (uint)sourceArray.Length)
                                {
                                    fixed (char* sourcePtr = &sourceArray[chunkStartIndex])
                                        string.wstrcpy(destinationPtr + curDestIndex, sourcePtr, chunkCount);
                                }
                                else
                                {
                                    throw new ArgumentOutOfRangeException("chunkCount", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                                }
                            }
                        }
                        chunk = chunk.m_ChunkPrevious;
                    }
                }
            }
            return ret;
        }

        // Convenience method for sb.Length=0;
        public StringBuilder Clear() {
            this.Length = 0;
            return this;
        }

        // Sets the length of the String in this buffer.  If length is less than the current
        // instance, the StringBuilder is truncated.  If length is greater than the current 
        // instance, nulls are appended.  The capacity is adjusted to be the same as the length.

        public int Length {
            get {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return m_ChunkOffset + m_ChunkLength;
            }
            set {
                //If the new length is less than 0 or greater than our Maximum capacity, bail.
                if (value<0) {
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NegativeLength"));
                }

                if (value>MaxCapacity) {
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
                }
                Contract.EndContractBlock();

                int originalCapacity = Capacity;

                if (value == 0 && m_ChunkPrevious == null)
                {
                    m_ChunkLength = 0;
                    m_ChunkOffset = 0;
                    Contract.Assert(Capacity >= originalCapacity, "setting the Length should never decrease the Capacity");
                    return;
                }

                int delta = value - Length;
                // if the specified length is greater than the current length
                if (delta > 0)
                {
                    // the end of the string value of the current StringBuilder object is padded with the Unicode NULL character
                    Append('\0', delta);        // We could improve on this, but who does this anyway?
                }
                // if the specified length is less than or equal to the current length
                else
                {
                    StringBuilder chunk = FindChunkForIndex(value);
                    if (chunk != this)
                    {
                        // we crossed a chunk boundary when reducing the Length, we must replace this middle-chunk with a new
                        // larger chunk to ensure the original capacity is preserved
                        int newLen = originalCapacity - chunk.m_ChunkOffset;
                        char[] newArray = new char[newLen];

                        Contract.Assert(newLen > chunk.m_ChunkChars.Length, "the new chunk should be larger than the one it is replacing");
                        Array.Copy(chunk.m_ChunkChars, newArray, chunk.m_ChunkLength);
                        
                        m_ChunkChars = newArray;
                        m_ChunkPrevious = chunk.m_ChunkPrevious;                        
                        m_ChunkOffset = chunk.m_ChunkOffset;
                    }
                    m_ChunkLength = value - chunk.m_ChunkOffset;
                    VerifyClassInvariant();
                }
                Contract.Assert(Capacity >= originalCapacity, "setting the Length should never decrease the Capacity");
            }
        }

        [System.Runtime.CompilerServices.IndexerName("Chars")]
        public char this[int index] {
            get {
                StringBuilder chunk = this;
                for (; ; )
                {
                    int indexInBlock = index - chunk.m_ChunkOffset;
                    if (indexInBlock >= 0)
                    {
                        if (indexInBlock >= chunk.m_ChunkLength)
                            throw new IndexOutOfRangeException();
                        return chunk.m_ChunkChars[indexInBlock];
                    }
                    chunk = chunk.m_ChunkPrevious;
                    if (chunk == null)
                        throw new IndexOutOfRangeException();
                }
            }
            set {
                StringBuilder chunk = this;
                for (; ; )
                {
                    int indexInBlock = index - chunk.m_ChunkOffset;
                    if (indexInBlock >= 0)
                    {
                        if (indexInBlock >= chunk.m_ChunkLength)
                            throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                        chunk.m_ChunkChars[indexInBlock] = value;
                        return;
                    }
                    chunk = chunk.m_ChunkPrevious;
                    if (chunk == null)
                        throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                }
            }
        }

        // Appends a character at the end of this string builder. The capacity is adjusted as needed.
        public StringBuilder Append(char value, int repeatCount) {
            if (repeatCount<0) {
                throw new ArgumentOutOfRangeException("repeatCount", Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));
            }
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            Contract.EndContractBlock();

            if (repeatCount==0) {
                return this;
            }
            int idx = m_ChunkLength;
            while (repeatCount > 0)
            {
                if (idx < m_ChunkChars.Length)
                {
                    m_ChunkChars[idx++] = value;
                    --repeatCount;
                }
                else
                {
                    m_ChunkLength = idx;
                    ExpandByABlock(repeatCount);
                    Contract.Assert(m_ChunkLength == 0, "Expand should create a new block");
                    idx = 0;
                }
            }
            m_ChunkLength = idx;
            VerifyClassInvariant();
            return this;
        }

        // Appends an array of characters at the end of this string builder. The capacity is adjusted as needed. 
        [System.Security.SecuritySafeCritical]  // auto-generated
        public StringBuilder Append(char[] value, int startIndex, int charCount) {
            if (startIndex < 0) {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
            }
            if (charCount<0) {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
            }
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            Contract.EndContractBlock();

            if (value == null) {
                if (startIndex == 0 && charCount == 0) {
                    return this;
                }
                throw new ArgumentNullException("value");
            }
            if (charCount > value.Length - startIndex) {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (charCount==0) {
                return this;
            }
            unsafe {
                fixed (char* valueChars = &value[startIndex])
                    Append(valueChars, charCount);
            }
            return this;
        }


        // Appends a copy of this string at the end of this string builder.
        [System.Security.SecuritySafeCritical]  // auto-generated
        public StringBuilder Append(String value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);

            if (value != null) {
                // This is a hand specialization of the 'AppendHelper' code below. 
                // We could have just called AppendHelper.  
                char[] chunkChars = m_ChunkChars;
                int chunkLength = m_ChunkLength;
                int valueLen = value.Length;
                int newCurrentIndex = chunkLength + valueLen;
                if (newCurrentIndex < chunkChars.Length)    // Use strictly < to avoid issue if count == 0, newIndex == length
                {
                    if (valueLen <= 2)
                    {
                        if (valueLen > 0)
                            chunkChars[chunkLength] = value[0];
                        if (valueLen > 1)
                            chunkChars[chunkLength + 1] = value[1];
                    }
                    else
                    {
                        unsafe {
                            fixed (char* valuePtr = value)
                            fixed (char* destPtr = &chunkChars[chunkLength])
                                string.wstrcpy(destPtr, valuePtr, valueLen);
                        }
                    }
                    m_ChunkLength = newCurrentIndex;
                }
                else
                    AppendHelper(value);
            }
            return this;
        }


        // We put this fixed in its own helper to avoid the cost zero initing valueChars in the
        // case we don't actually use it.  
        [System.Security.SecuritySafeCritical]  // auto-generated
        private void AppendHelper(string value) {
            unsafe {
                fixed (char* valueChars = value)
                    Append(valueChars, value.Length);
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityCritical]
        internal unsafe extern void ReplaceBufferInternal(char* newBuffer, int newLength);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityCritical]
        internal unsafe extern void ReplaceBufferAnsiInternal(sbyte* newBuffer, int newLength);

        // Appends a copy of the characters in value from startIndex to startIndex +
        // count at the end of this string builder.
        [System.Security.SecuritySafeCritical]  // auto-generated
        public StringBuilder Append(String value, int startIndex, int count) {
            if (startIndex < 0) {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (count < 0) {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
            }
            Contract.Ensures(Contract.Result<StringBuilder>() != null);

            //If the value being added is null, eat the null
            //and return.
            if (value == null) {
                if (startIndex == 0 && count == 0) {
                    return this;
                }
                throw new ArgumentNullException("value");
            }

            if (count == 0) {
                return this;
            }

            if (startIndex > value.Length - count) {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            unsafe {
                fixed (char* valueChars = value)
                    Append(valueChars + startIndex, count);
            }
            return this;
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public StringBuilder AppendLine() {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Append(Environment.NewLine);
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public StringBuilder AppendLine(string value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            Append(value);
            return Append(Environment.NewLine);
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        [SecuritySafeCritical]
        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) {
            if (destination == null) {
                throw new ArgumentNullException("destination");
            }

            if (count < 0) {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("Arg_NegativeArgCount"));
            }

            if (destinationIndex < 0) {
                throw new ArgumentOutOfRangeException("destinationIndex",
                    Environment.GetResourceString("ArgumentOutOfRange_MustBeNonNegNum", "destinationIndex"));
            }

            if (destinationIndex > destination.Length - count) {
                throw new ArgumentException(Environment.GetResourceString("ArgumentOutOfRange_OffsetOut"));
            }

            if ((uint)sourceIndex > (uint)Length) {
                throw new ArgumentOutOfRangeException("sourceIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (sourceIndex > Length - count) {
                throw new ArgumentException(Environment.GetResourceString("Arg_LongerThanSrcString"));
            }
            Contract.EndContractBlock();

            VerifyClassInvariant();

            StringBuilder chunk = this;
            int sourceEndIndex = sourceIndex + count;
            int curDestIndex = destinationIndex + count;
            while (count > 0)
            {
                int chunkEndIndex = sourceEndIndex - chunk.m_ChunkOffset;
                if (chunkEndIndex >= 0)
                {
                    if (chunkEndIndex > chunk.m_ChunkLength)
                        chunkEndIndex = chunk.m_ChunkLength;

                    int chunkCount = count;
                    int chunkStartIndex = chunkEndIndex - count;
                    if (chunkStartIndex < 0)
                    {
                        chunkCount += chunkStartIndex;
                        chunkStartIndex = 0;
                    }
                    curDestIndex -= chunkCount;
                    count -= chunkCount;

                    // SafeCritical: we ensure that chunkStartIndex + chunkCount are within range of m_chunkChars
                    // as well as ensuring that curDestIndex + chunkCount are within range of destination
                    ThreadSafeCopy(chunk.m_ChunkChars, chunkStartIndex, destination, curDestIndex, chunkCount);
                }
                chunk = chunk.m_ChunkPrevious;
            }
        }

        // Inserts multiple copies of a string into this string builder at the specified position.
        // Existing characters are shifted to make room for the new text.
        // The capacity is adjusted as needed. If value equals String.Empty, this
        // string builder is not changed. 
        // 
        [System.Security.SecuritySafeCritical]  // auto-generated
        public StringBuilder Insert(int index, String value, int count) {
            if (count < 0) {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            Contract.EndContractBlock();

            //Range check the index.
            int currentLength = Length;
            if ((uint)index > (uint)currentLength) {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            //If value is null, empty or count is 0, do nothing. This is ECMA standard.
            if (value == null || value.Length == 0 || count == 0) {
                return this;
            }

            //Ensure we don't insert more chars than we can hold, and we don't 
            //have any integer overflow in our inserted characters.
            long insertingChars = (long) value.Length * count;
            if (insertingChars > MaxCapacity - this.Length) {
                throw new OutOfMemoryException();
            }
            Contract.Assert(insertingChars + this.Length < Int32.MaxValue);

            StringBuilder chunk;
            int indexInChunk;
            MakeRoom(index, (int) insertingChars, out chunk, out indexInChunk, false);
            unsafe {
                fixed (char* valuePtr = value) {
                    while (count > 0)
                    {
                        ReplaceInPlaceAtChunk(ref chunk, ref indexInChunk, valuePtr, value.Length);
                        --count;
                    }
                }
            }
            return this;
        }

        // Removes the specified characters from this string builder.
        // The length of this string builder is reduced by 
        // length, but the capacity is unaffected.
        // 
        public StringBuilder Remove(int startIndex, int length) {
            if (length<0) {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NegativeLength"));
            }

            if (startIndex<0) {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            }

            if (length > Length - startIndex) {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            Contract.EndContractBlock();

            if (Length == length && startIndex == 0) {
                // Optimization.  If we are deleting everything  
                Length = 0;
                return this;
            }

            if (length > 0)
            {
                StringBuilder chunk;
                int indexInChunk;
                Remove(startIndex, length, out chunk, out indexInChunk);
            }
            return this;
        }

        //
        // PUBLIC INSTANCE FUNCTIONS
        //
        //

        /*====================================Append====================================
        **
        ==============================================================================*/
        // Appends a boolean to the end of this string builder.
        // The capacity is adjusted as needed. 
        public StringBuilder Append(bool value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Append(value.ToString());
        }

        // Appends an sbyte to this string builder.
        // The capacity is adjusted as needed. 
        [CLSCompliant(false)]
        public StringBuilder Append(sbyte value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Append(value.ToString(CultureInfo.CurrentCulture));
        }

        // Appends a ubyte to this string builder.
        // The capacity is adjusted as needed. 
        public StringBuilder Append(byte value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Append(value.ToString(CultureInfo.CurrentCulture));
        }

        // Appends a character at the end of this string builder. The capacity is adjusted as needed.
        public StringBuilder Append(char value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);

            if (m_ChunkLength < m_ChunkChars.Length)
                m_ChunkChars[m_ChunkLength++] = value;
            else
                Append(value, 1);
            return this;
        }

        // Appends a short to this string builder.
        // The capacity is adjusted as needed. 
        public StringBuilder Append(short value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Append(value.ToString(CultureInfo.CurrentCulture));
        }

        // Appends an int to this string builder.
        // The capacity is adjusted as needed. 
        public StringBuilder Append(int value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Append(value.ToString(CultureInfo.CurrentCulture));
        }

        // Appends a long to this string builder. 
        // The capacity is adjusted as needed. 
        public StringBuilder Append(long value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Append(value.ToString(CultureInfo.CurrentCulture));
        }

        // Appends a float to this string builder. 
        // The capacity is adjusted as needed. 
        public StringBuilder Append(float value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Append(value.ToString(CultureInfo.CurrentCulture));
        }

        // Appends a double to this string builder. 
        // The capacity is adjusted as needed. 
        public StringBuilder Append(double value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Append(value.ToString(CultureInfo.CurrentCulture));
        }

        public StringBuilder Append(decimal value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Append(value.ToString(CultureInfo.CurrentCulture));
        }

        // Appends an ushort to this string builder. 
        // The capacity is adjusted as needed. 
        [CLSCompliant(false)]
        public StringBuilder Append(ushort value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Append(value.ToString(CultureInfo.CurrentCulture));
        }

        // Appends an uint to this string builder. 
        // The capacity is adjusted as needed. 
        [CLSCompliant(false)]
        public StringBuilder Append(uint value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Append(value.ToString(CultureInfo.CurrentCulture));
        }

        // Appends an unsigned long to this string builder. 
        // The capacity is adjusted as needed. 
        [CLSCompliant(false)]
        public StringBuilder Append(ulong value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Append(value.ToString(CultureInfo.CurrentCulture));
        }

        // Appends an Object to this string builder. 
        // The capacity is adjusted as needed. 
        public StringBuilder Append(Object value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);

            if (null==value) {
                //Appending null is now a no-op.
                return this;
            }
            return Append(value.ToString());
        }

        // Appends all of the characters in value to the current instance.
        [System.Security.SecuritySafeCritical]  // auto-generated
        public StringBuilder Append(char[] value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);

            if (null != value && value.Length > 0)
            {
                unsafe {
                    fixed (char* valueChars = &value[0])
                        Append(valueChars, value.Length);
                }
            }
            return this;
        }

        /*====================================Insert====================================
        **
        ==============================================================================*/

        // Returns a reference to the StringBuilder with ; value inserted into 
        // the buffer at index. Existing characters are shifted to make room for the new text.
        // The capacity is adjusted as needed. If value equals String.Empty, the
        // StringBuilder is not changed.
        // 
        [System.Security.SecuritySafeCritical]  // auto-generated
        public StringBuilder Insert(int index, String value) {
            if ((uint)index > (uint)Length) {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            Contract.EndContractBlock();

            if (value != null)
            {
                unsafe {
                    fixed (char* sourcePtr = value)
                        Insert(index, sourcePtr, value.Length);
                }
            }
            return this;
        }

        // Returns a reference to the StringBuilder with ; value inserted into 
        // the buffer at index. Existing characters are shifted to make room for the new text.
        // The capacity is adjusted as needed. If value equals String.Empty, the
        // StringBuilder is not changed.
        // 
        public StringBuilder Insert( int index, bool value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Insert(index, value.ToString(), 1);
        }

        // Returns a reference to the StringBuilder with ; value inserted into 
        // the buffer at index. Existing characters are shifted to make room for the new text.
        // The capacity is adjusted as needed. If value equals String.Empty, the
        // StringBuilder is not changed.
        // 
        [CLSCompliant(false)]
        public StringBuilder Insert(int index, sbyte value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
        }

        // Returns a reference to the StringBuilder with ; value inserted into 
        // the buffer at index. Existing characters are shifted to make room for the new text.
        // The capacity is adjusted as needed. If value equals String.Empty, the
        // StringBuilder is not changed.
        // 
        public StringBuilder Insert(int index, byte value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
        }

        // Returns a reference to the StringBuilder with ; value inserted into 
        // the buffer at index. Existing characters are shifted to make room for the new text.
        // The capacity is adjusted as needed. If value equals String.Empty, the
        // StringBuilder is not changed.
        // 
        public StringBuilder Insert(int index, short value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
        }

        // Returns a reference to the StringBuilder with ; value inserted into 
        // the buffer at index. Existing characters are shifted to make room for the new text.
        // The capacity is adjusted as needed. If value equals String.Empty, the
        // StringBuilder is not changed.
        [SecuritySafeCritical]
        public StringBuilder Insert(int index, char value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);

            unsafe {
                Insert(index, &value, 1);
            }
            return this;
        }

        // Returns a reference to the StringBuilder with ; value inserted into 
        // the buffer at index. Existing characters are shifted to make room for the new text.
        // The capacity is adjusted as needed. If value equals String.Empty, the
        // StringBuilder is not changed.
        // 
        public StringBuilder Insert(int index, char[] value) {
            if ((uint)index > (uint)Length) {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            Contract.EndContractBlock();

            if (value != null)
                Insert(index, value, 0, value.Length);
            return this;
        }

        // Returns a reference to the StringBuilder with charCount characters from 
        // value inserted into the buffer at index.  Existing characters are shifted
        // to make room for the new text and capacity is adjusted as required.  If value is null, the StringBuilder
        // is unchanged.  Characters are taken from value starting at position startIndex.
        [System.Security.SecuritySafeCritical]  // auto-generated
        public StringBuilder Insert(int index, char[] value, int startIndex, int charCount) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);

            int currentLength = Length;
            if ((uint)index > (uint)currentLength) {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            //If they passed in a null char array, just jump out quickly.
            if (value == null) {
                if (startIndex == 0 && charCount == 0)
                {
                    return this;
                }
                throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_String"));
            }

            //Range check the array.
            if (startIndex < 0) {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            }

            if (charCount < 0) {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
            }

            if (startIndex > value.Length - charCount) {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (charCount > 0)
            {
                unsafe {
                    fixed (char* sourcePtr = &value[startIndex])
                        Insert(index, sourcePtr, charCount);
                }
            }
            return this;
        }

        // Returns a reference to the StringBuilder with ; value inserted into 
        // the buffer at index. Existing characters are shifted to make room for the new text.
        // The capacity is adjusted as needed. If value equals String.Empty, the
        // StringBuilder is not changed.
        // 
        public StringBuilder Insert(int index, int value){
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
        }

        // Returns a reference to the StringBuilder with ; value inserted into 
        // the buffer at index. Existing characters are shifted to make room for the new text.
        // The capacity is adjusted as needed. If value equals String.Empty, the
        // StringBuilder is not changed.
        // 
        public StringBuilder Insert(int index, long value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
        }

        // Returns a reference to the StringBuilder with ; value inserted into 
        // the buffer at index. Existing characters are shifted to make room for the new text.
        // The capacity is adjusted as needed. If value equals String.Empty, the
        // StringBuilder is not changed.
        // 
        public StringBuilder Insert(int index, float value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
        }

        // Returns a reference to the StringBuilder with ; value inserted into 
        // the buffer at index. Existing characters are shifted to make room for the new text.
        // The capacity is adjusted as needed. If value equals String.Empty, the
        // StringBuilder is not changed. 
        // 
        public StringBuilder Insert(int index, double value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
        }

        public StringBuilder Insert(int index, decimal value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
        }

        // Returns a reference to the StringBuilder with value inserted into 
        // the buffer at index. Existing characters are shifted to make room for the new text.
        // The capacity is adjusted as needed. 
        // 
        [CLSCompliant(false)]
        public StringBuilder Insert(int index, ushort value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
        }

        // Returns a reference to the StringBuilder with value inserted into 
        // the buffer at index. Existing characters are shifted to make room for the new text.
        // The capacity is adjusted as needed. 
        // 
        [CLSCompliant(false)]
        public StringBuilder Insert(int index, uint value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
        }

        // Returns a reference to the StringBuilder with value inserted into 
        // the buffer at index. Existing characters are shifted to make room for the new text.
        // The capacity is adjusted as needed. 
        // 
        [CLSCompliant(false)]
        public StringBuilder Insert(int index, ulong value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
        }

        // Returns a reference to this string builder with value inserted into 
        // the buffer at index. Existing characters are shifted to make room for the
        // new text.  The capacity is adjusted as needed. If value equals String.Empty, the
        // StringBuilder is not changed. No changes are made if value is null.
        // 
        public StringBuilder Insert(int index, Object value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            if (null == value) {
                return this;
            }
            return Insert(index, value.ToString(), 1);
        }

        public StringBuilder AppendFormat(String format, Object arg0) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return AppendFormatHelper(null, format, new ParamsArray(arg0));
        }

        public StringBuilder AppendFormat(String format, Object arg0, Object arg1) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return AppendFormatHelper(null, format, new ParamsArray(arg0, arg1));
        }

        public StringBuilder AppendFormat(String format, Object arg0, Object arg1, Object arg2) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return AppendFormatHelper(null, format, new ParamsArray(arg0, arg1, arg2));
        }

        public StringBuilder AppendFormat(String format, params Object[] args) {
            if (args == null)
            {
                // To preserve the original exception behavior, throw an exception about format if both
                // args and format are null. The actual null check for format is in AppendFormatHelper.
                throw new ArgumentNullException((format == null) ? "format" : "args");
            }
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            
            return AppendFormatHelper(null, format, new ParamsArray(args));
        }

        public StringBuilder AppendFormat(IFormatProvider provider, String format, Object arg0) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return AppendFormatHelper(provider, format, new ParamsArray(arg0));
        }
        
        public StringBuilder AppendFormat(IFormatProvider provider, String format, Object arg0, Object arg1) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return AppendFormatHelper(provider, format, new ParamsArray(arg0, arg1));
        }
        
        public StringBuilder AppendFormat(IFormatProvider provider, String format, Object arg0, Object arg1, Object arg2) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return AppendFormatHelper(provider, format, new ParamsArray(arg0, arg1, arg2));
        }
        
        public StringBuilder AppendFormat(IFormatProvider provider, String format, params Object[] args) {
            if (args == null)
            {
                // To preserve the original exception behavior, throw an exception about format if both
                // args and format are null. The actual null check for format is in AppendFormatHelper.
                throw new ArgumentNullException((format == null) ? "format" : "args");
            }
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            
            return AppendFormatHelper(provider, format, new ParamsArray(args));
        }
        
        private static void FormatError() {
            throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
        }

        // undocumented exclusive limits on the range for Argument Hole Index and Argument Hole Alignment.
        private const int Index_Limit = 1000000; // Note:            0 <= ArgIndex < Index_Limit
        private const int Width_Limit = 1000000; // Note: -Width_Limit <  ArgAlign < Width_Limit

        internal StringBuilder AppendFormatHelper(IFormatProvider provider, String format, ParamsArray args) {
            if (format == null) {
                throw new ArgumentNullException("format");
            }
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            Contract.EndContractBlock();

            int pos = 0;
            int len = format.Length;
            char ch = '\x0';

            ICustomFormatter cf = null;
            if (provider != null) {
                cf = (ICustomFormatter)provider.GetFormat(typeof(ICustomFormatter));
            }

            while (true) {
                while (pos < len) {
                    ch = format[pos];

                    pos++;
                    // Is it a closing brace?
                    if (ch == '}')
                    {
                        // Check next character (if there is one) to see if it is escaped. eg }}
                        if (pos < len && format[pos] == '}')
                            pos++;
                        else
                            // Otherwise treat it as an error (Mismatched closing brace)
                            FormatError();
                    }
                    // Is it a opening brace?
                    if (ch == '{')
                    {
                        // Check next character (if there is one) to see if it is escaped. eg {{
                        if (pos < len && format[pos] == '{')
                            pos++;
                        else
                        {
                            // Otherwise treat it as the opening brace of an Argument Hole.
                            pos--;
                            break;
                        }
                    }
                    // If it neither then treat the character as just text.
                    Append(ch);
                }

                //
                // Start of parsing of Argument Hole.
                // Argument Hole ::= { Index (, WS* Alignment WS*)? (: Formatting)? }
                //
                if (pos == len) break;
                
                //
                //  Start of parsing required Index parameter.
                //  Index ::= ('0'-'9')+ WS*
                //
                pos++;
                // If reached end of text then error (Unexpected end of text)
                // or character is not a digit then error (Unexpected Character)
                if (pos == len || (ch = format[pos]) < '0' || ch > '9') FormatError();
                int index = 0;
                do {
                    index = index * 10 + ch - '0';
                    pos++;
                    // If reached end of text then error (Unexpected end of text)
                    if (pos == len) FormatError();
                    ch = format[pos];
                    // so long as character is digit and value of the index is less than 1000000 ( index limit )
                } while (ch >= '0' && ch <= '9' && index < Index_Limit);

                // If value of index is not within the range of the arguments passed in then error (Index out of range)
                if (index >= args.Length) throw new FormatException(Environment.GetResourceString("Format_IndexOutOfRange"));
                
                // Consume optional whitespace.
                while (pos < len && (ch = format[pos]) == ' ') pos++;
                // End of parsing index parameter.

                //
                //  Start of parsing of optional Alignment
                //  Alignment ::= comma WS* minus? ('0'-'9')+ WS*
                //
                bool leftJustify = false;
                int width = 0;
                // Is the character a comma, which indicates the start of alignment parameter.
                if (ch == ',') {
                    pos++;
 
                    // Consume Optional whitespace
                    while (pos < len && format[pos] == ' ') pos++;
                    
                    // If reached the end of the text then error (Unexpected end of text)
                    if (pos == len) FormatError();
                    
                    // Is there a minus sign?
                    ch = format[pos];
                    if (ch == '-') {
                        // Yes, then alignment is left justified.
                        leftJustify = true;
                        pos++;
                        // If reached end of text then error (Unexpected end of text)
                        if (pos == len) FormatError();
                        ch = format[pos];
                    }
 
                    // If current character is not a digit then error (Unexpected character)
                    if (ch < '0' || ch > '9') FormatError();
                    // Parse alignment digits.
                    do {
                        width = width * 10 + ch - '0';
                        pos++;
                        // If reached end of text then error. (Unexpected end of text)
                        if (pos == len) FormatError();
                        ch = format[pos];
                        // So long a current character is a digit and the value of width is less than 100000 ( width limit )
                    } while (ch >= '0' && ch <= '9' && width < Width_Limit);
                    // end of parsing Argument Alignment
                }

                // Consume optional whitespace
                while (pos < len && (ch = format[pos]) == ' ') pos++;

                //
                // Start of parsing of optional formatting parameter.
                //
                Object arg = args[index];
                StringBuilder fmt = null;
                // Is current character a colon? which indicates start of formatting parameter.
                if (ch == ':') {
                    pos++;
                    while (true) {
                        // If reached end of text then error. (Unexpected end of text)
                        if (pos == len) FormatError();
                        ch = format[pos];
                        pos++;
                        // Is character a opening brace?
                        if (ch == '{')
                        {
                            // Yes, is next character also a opening brace, then treat as escaped. eg {{
                            if (pos < len && format[pos] == '{')
                                pos++;
                            else
                                // Error Argument Holes can not be nested.
                                FormatError();
                        }
                        // Is charecter a closing brace?
                        else if (ch == '}')
                        {
                            // Yes, is next character also a closing brace, then treat as escaped. eg }}
                            if (pos < len && format[pos] == '}')
                                pos++;
                            else
                            {
                                // No, then treat it as the closing brace of an Arg Hole.
                                pos--;
                                break;
                            }
                        }
                        // build up the argument format string.
                        if (fmt == null) {
                            fmt = new StringBuilder();
                        }
                        fmt.Append(ch);
                    }
                }
                // If current character is not a closing brace then error. (Unexpected Character)
                if (ch != '}') FormatError();
                // Construct the output for this arg hole.
                pos++;
                String sFmt = null;
                String s = null;
                if (cf != null) {
                    if (fmt != null) {
                        sFmt = fmt.ToString();
                    }
                    s = cf.Format(sFmt, arg, provider);
                }

                if (s == null) {
                    IFormattable formattableArg = arg as IFormattable;

                    if (formattableArg != null) {
                        if (sFmt == null && fmt != null) {
                            sFmt = fmt.ToString();
                        }

                        s = formattableArg.ToString(sFmt, provider);
                    } else if (arg != null) {
                        s = arg.ToString();
                    }
                }
                // Append it to the final output of the Format String.
                if (s == null) s = String.Empty;
                int pad = width - s.Length;
                if (!leftJustify && pad > 0) Append(' ', pad);
                Append(s);
                if (leftJustify && pad > 0) Append(' ', pad);
                // Continue to parse other characters.
            }
            return this;
        }

        // Returns a reference to the current StringBuilder with all instances of oldString 
        // replaced with newString.  If startIndex and count are specified,
        // we only replace strings completely contained in the range of startIndex to startIndex + 
        // count.  The strings to be replaced are checked on an ordinal basis (e.g. not culture aware).  If 
        // newValue is null, instances of oldValue are removed (e.g. replaced with nothing.).
        //
        public StringBuilder Replace(String oldValue, String newValue) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Replace(oldValue, newValue, 0, Length);
        }

        public bool Equals(StringBuilder sb) 
        {
            if (sb == null)
                return false;
            if (Capacity != sb.Capacity || MaxCapacity != sb.MaxCapacity || Length != sb.Length)
                return false;
            if (sb == this)
                return true;

            StringBuilder thisChunk = this;
            int thisChunkIndex = thisChunk.m_ChunkLength;
            StringBuilder sbChunk = sb;
            int sbChunkIndex = sbChunk.m_ChunkLength;
            for (; ; )
            {
                // Decrement the pointer to the 'this' StringBuilder
                --thisChunkIndex;
                --sbChunkIndex;

                while (thisChunkIndex < 0)
                {
                    thisChunk = thisChunk.m_ChunkPrevious;
                    if (thisChunk == null)
                        break;
                    thisChunkIndex = thisChunk.m_ChunkLength + thisChunkIndex;
                }

                // Decrement the pointer to the 'this' StringBuilder
                while (sbChunkIndex < 0)
                {
                    sbChunk = sbChunk.m_ChunkPrevious;
                    if (sbChunk == null)
                        break;
                    sbChunkIndex = sbChunk.m_ChunkLength + sbChunkIndex;
                }

                if (thisChunkIndex < 0)
                    return sbChunkIndex < 0;
                if (sbChunkIndex < 0)
                    return false;
                if (thisChunk.m_ChunkChars[thisChunkIndex] != sbChunk.m_ChunkChars[sbChunkIndex])
                    return false;
            }
        }

        public StringBuilder Replace(String oldValue, String newValue, int startIndex, int count)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);

            int currentLength = Length;
            if ((uint)startIndex > (uint)currentLength)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }
            if (count < 0 || startIndex > currentLength - count)
            {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }
            if (oldValue == null)
            {
                throw new ArgumentNullException("oldValue");
            }
            if (oldValue.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "oldValue");
            }

            if (newValue == null)
                newValue = "";

            int deltaLength = newValue.Length - oldValue.Length;

            int[] replacements = null;          // A list of replacement positions in a chunk to apply
            int replacementsCount = 0;

            // Find the chunk, indexInChunk for the starting point
            StringBuilder chunk = FindChunkForIndex(startIndex);
            int indexInChunk = startIndex - chunk.m_ChunkOffset;
            while (count > 0)
            {
                // Look for a match in the chunk,indexInChunk pointer 
                if (StartsWith(chunk, indexInChunk, count, oldValue))
                {
                    // Push it on my replacements array (with growth), we will do all replacements in a
                    // given chunk in one operation below (see ReplaceAllInChunk) so we don't have to slide
                    // many times.  
                    if (replacements == null)
                        replacements = new int[5];
                    else if (replacementsCount >= replacements.Length)
                    {
                        int[] newArray = new int[replacements.Length * 3 / 2 + 4];     // grow by 1.5X but more in the begining
                        Array.Copy(replacements, newArray, replacements.Length);
                        replacements = newArray;
                    }
                    replacements[replacementsCount++] = indexInChunk;
                    indexInChunk += oldValue.Length;
                    count -= oldValue.Length;
                }
                else
                {
                    indexInChunk++;
                    --count;
                }

                if (indexInChunk >= chunk.m_ChunkLength || count == 0)       // Have we moved out of the current chunk
                {
                    // Replacing mutates the blocks, so we need to convert to logical index and back afterward. 
                    int index = indexInChunk + chunk.m_ChunkOffset;
                    int indexBeforeAdjustment = index;

                    // See if we accumulated any replacements, if so apply them 
                    ReplaceAllInChunk(replacements, replacementsCount, chunk, oldValue.Length, newValue);
                    // The replacement has affected the logical index.  Adjust it.  
                    index += ((newValue.Length - oldValue.Length) * replacementsCount);
                    replacementsCount = 0;

                    chunk = FindChunkForIndex(index);
                    indexInChunk = index - chunk.m_ChunkOffset;
                    Contract.Assert(chunk != null || count == 0, "Chunks ended prematurely");
                }
            }
            VerifyClassInvariant();
            return this;
        }

        // Returns a StringBuilder with all instances of oldChar replaced with 
        // newChar.  The size of the StringBuilder is unchanged because we're only
        // replacing characters.  If startIndex and count are specified, we 
        // only replace characters in the range from startIndex to startIndex+count
        //
        public StringBuilder Replace(char oldChar, char newChar) {
            return Replace(oldChar, newChar, 0, Length);
        }
        public StringBuilder Replace(char oldChar, char newChar, int startIndex, int count) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);

            int currentLength = Length;
            if ((uint)startIndex > (uint)currentLength) {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (count < 0 || startIndex > currentLength - count) {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            int endIndex = startIndex + count;
            StringBuilder chunk = this;
            for (; ; )
            {
                int endIndexInChunk = endIndex - chunk.m_ChunkOffset;
                int startIndexInChunk = startIndex - chunk.m_ChunkOffset;
                if (endIndexInChunk >= 0)
                {
                    int curInChunk = Math.Max(startIndexInChunk, 0);
                    int endInChunk = Math.Min(chunk.m_ChunkLength, endIndexInChunk);
                    while (curInChunk < endInChunk)
                    {
                        if (chunk.m_ChunkChars[curInChunk] == oldChar)
                            chunk.m_ChunkChars[curInChunk] = newChar;
                        curInChunk++;
                    }
                }
                if (startIndexInChunk >= 0)
                    break;
                chunk = chunk.m_ChunkPrevious;
            }
            return this;
        }

        /// <summary>
        /// Appends 'value' of length 'count' to the stringBuilder. 
        /// </summary>
        [SecurityCritical]
        [System.CLSCompliantAttribute(false)]
        public unsafe StringBuilder Append(char* value, int valueCount)
        {
            // We don't check null value as this case will throw null reference exception anyway
            if (valueCount < 0)
            {
                throw new ArgumentOutOfRangeException("valueCount", Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));
            }

            // This case is so common we want to optimize for it heavily. 
            int newIndex = valueCount + m_ChunkLength;
            if (newIndex <= m_ChunkChars.Length)
            {
                ThreadSafeCopy(value, m_ChunkChars, m_ChunkLength, valueCount);
                m_ChunkLength = newIndex;
            }
            else
            {
                // Copy the first chunk
                int firstLength = m_ChunkChars.Length - m_ChunkLength;
                if (firstLength > 0)
                {
                    ThreadSafeCopy(value, m_ChunkChars, m_ChunkLength, firstLength);
                    m_ChunkLength = m_ChunkChars.Length;
                }

                // Expand the builder to add another chunk. 
                int restLength = valueCount - firstLength;
                ExpandByABlock(restLength);
                Contract.Assert(m_ChunkLength == 0, "Expand did not make a new block");

                // Copy the second chunk
                ThreadSafeCopy(value + firstLength, m_ChunkChars, 0, restLength);
                m_ChunkLength = restLength;
            }
            VerifyClassInvariant();
            return this;
        }

        /// <summary>
        /// Inserts 'value' of length 'cou
        /// </summary>
        [SecurityCritical]
        unsafe private void Insert(int index, char* value, int valueCount)
        {
            if ((uint)index > (uint)Length)
            {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (valueCount > 0)
            {
                StringBuilder chunk;
                int indexInChunk;
                MakeRoom(index, valueCount, out chunk, out indexInChunk, false);
                ReplaceInPlaceAtChunk(ref chunk, ref indexInChunk, value, valueCount);
            }
        }

        /// <summary>
        /// 'replacements' is a list of index (relative to the begining of the 'chunk' to remove
        /// 'removeCount' characters and replace them with 'value'.   This routine does all those 
        /// replacements in bulk (and therefore very efficiently. 
        /// with the string 'value'.  
        /// </summary>
        [System.Security.SecuritySafeCritical]  // auto-generated
        private void ReplaceAllInChunk(int[] replacements, int replacementsCount, StringBuilder sourceChunk, int removeCount, string value)
        {
            if (replacementsCount <= 0)
                return;

            unsafe {
                fixed (char* valuePtr = value)
                {
                    // calculate the total amount of extra space or space needed for all the replacements.  
                    int delta = (value.Length - removeCount) * replacementsCount;
    
                    StringBuilder targetChunk = sourceChunk;        // the target as we copy chars down
                    int targetIndexInChunk = replacements[0];
    
                    // Make the room needed for all the new characters if needed. 
                    if (delta > 0)
                        MakeRoom(targetChunk.m_ChunkOffset + targetIndexInChunk, delta, out targetChunk, out targetIndexInChunk, true);
                    // We made certain that characters after the insertion point are not moved, 
                    int i = 0;
                    for (; ; )
                    {
                        // Copy in the new string for the ith replacement
                        ReplaceInPlaceAtChunk(ref targetChunk, ref targetIndexInChunk, valuePtr, value.Length);
                        int gapStart = replacements[i] + removeCount;
                        i++;
                        if (i >= replacementsCount)
                            break;
    
                        int gapEnd = replacements[i];
                        Contract.Assert(gapStart < sourceChunk.m_ChunkChars.Length, "gap starts at end of buffer.  Should not happen");
                        Contract.Assert(gapStart <= gapEnd, "negative gap size");
                        Contract.Assert(gapEnd <= sourceChunk.m_ChunkLength, "gap too big");
                        if (delta != 0)     // can skip the sliding of gaps if source an target string are the same size.  
                        {
                            // Copy the gap data between the current replacement and the the next replacement
                            fixed (char* sourcePtr = &sourceChunk.m_ChunkChars[gapStart])
                                ReplaceInPlaceAtChunk(ref targetChunk, ref targetIndexInChunk, sourcePtr, gapEnd - gapStart);
                        }
                        else
                        {
                            targetIndexInChunk += gapEnd - gapStart;
                            Contract.Assert(targetIndexInChunk <= targetChunk.m_ChunkLength, "gap not in chunk");
                        }
                    }
    
                    // Remove extra space if necessary. 
                    if (delta < 0)
                        Remove(targetChunk.m_ChunkOffset + targetIndexInChunk, -delta, out targetChunk, out targetIndexInChunk);
                }
            }
        }

        /// <summary>
        /// Returns true if the string that is starts at 'chunk' and 'indexInChunk, and has a logical
        /// length of 'count' starts with the string 'value'. 
        /// </summary>
        private bool StartsWith(StringBuilder chunk, int indexInChunk, int count, string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (count == 0)
                    return false;
                if (indexInChunk >= chunk.m_ChunkLength)
                {
                    chunk = Next(chunk);
                    if (chunk == null)
                        return false;
                    indexInChunk = 0;
                }

                // See if there no match, break out of the inner for loop
                if (value[i] != chunk.m_ChunkChars[indexInChunk])
                    return false;

                indexInChunk++;
                --count;
            }
            return true;
        }

        /// <summary>
        /// ReplaceInPlaceAtChunk is the logical equivalent of 'memcpy'.  Given a chunk and ann index in
        /// that chunk, it copies in 'count' characters from 'value' and updates 'chunk, and indexInChunk to 
        /// point at the end of the characters just copyied (thus you can splice in strings from multiple 
        /// places by calling this mulitple times.  
        /// </summary>
        [SecurityCritical]
        unsafe private void ReplaceInPlaceAtChunk(ref StringBuilder chunk, ref int indexInChunk, char* value, int count)
        {
            if (count != 0)
            {
                for (; ; )
                {
                    int lengthInChunk = chunk.m_ChunkLength - indexInChunk;
                    Contract.Assert(lengthInChunk >= 0, "index not in chunk");

                    int lengthToCopy = Math.Min(lengthInChunk, count);
                    ThreadSafeCopy(value, chunk.m_ChunkChars, indexInChunk, lengthToCopy);

                    // Advance the index. 
                    indexInChunk += lengthToCopy;
                    if (indexInChunk >= chunk.m_ChunkLength)
                    {
                        chunk = Next(chunk);
                        indexInChunk = 0;
                    }
                    count -= lengthToCopy;
                    if (count == 0)
                        break;
                    value += lengthToCopy;
                }
            }
        }

        /// <summary>
        /// We have to prevent modification off the end of an array.
        /// The only way to do this is to copy all interesting variables out of the heap and then do the
        /// bounds check.  This is what we do here.   
        /// </summary>
        [SecurityCritical]
        unsafe private static void ThreadSafeCopy(char* sourcePtr, char[] destination, int destinationIndex, int count)
        {
            if (count > 0)
            {
                if ((uint)destinationIndex <= (uint)destination.Length && (destinationIndex + count) <= destination.Length)
                {
                    fixed (char* destinationPtr = &destination[destinationIndex])
                        string.wstrcpy(destinationPtr, sourcePtr, count);
                }
                else
                {
                    throw new ArgumentOutOfRangeException("destinationIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                }
            }
        }
        [SecurityCritical]
        private static void ThreadSafeCopy(char[] source, int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            if (count > 0)
            {
                if ((uint)sourceIndex <= (uint)source.Length && (sourceIndex + count) <= source.Length)
                {
                    unsafe {
                        fixed (char* sourcePtr = &source[sourceIndex])
                            ThreadSafeCopy(sourcePtr, destination, destinationIndex, count);
                    }
                }
                else
                {
                    throw new ArgumentOutOfRangeException("sourceIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                }
            }
        }

         // Copies the source StringBuilder to the destination IntPtr memory allocated with len bytes.
        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe void InternalCopy(IntPtr dest, int len) {
            if(len ==0)
                return;

            bool isLastChunk = true;
            byte* dstPtr = (byte*) dest.ToPointer();
            StringBuilder currentSrc = FindChunkForByte(len);

            do {
                int chunkOffsetInBytes = currentSrc.m_ChunkOffset*sizeof(char);
                int chunkLengthInBytes = currentSrc.m_ChunkLength*sizeof(char);
                fixed(char* charPtr = &currentSrc.m_ChunkChars[0]) {
                    byte* srcPtr = (byte*) charPtr;
                    if(isLastChunk) {
                        isLastChunk= false;
                        Buffer.Memcpy(dstPtr + chunkOffsetInBytes, srcPtr, len - chunkOffsetInBytes);
                    } else {
                        Buffer.Memcpy(dstPtr + chunkOffsetInBytes, srcPtr, chunkLengthInBytes);
                    }
                }
                currentSrc = currentSrc.m_ChunkPrevious;
            } while(currentSrc != null);
        }

        /// <summary>
        /// Finds the chunk for the logical index (number of characters in the whole stringbuilder) 'index'
        /// YOu can then get the offset in this chunk by subtracting the m_BlockOffset field from 'index' 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private StringBuilder FindChunkForIndex(int index)
        {
            Contract.Assert(0 <= index && index <= Length, "index not in string");

            StringBuilder ret = this;
            while (ret.m_ChunkOffset > index)
                ret = ret.m_ChunkPrevious;

            Contract.Assert(ret != null, "index not in string");
            return ret;
        }

        /// <summary>
        /// Finds the chunk for the logical byte index 'byteIndex'
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private StringBuilder FindChunkForByte(int byteIndex)
        {
            Contract.Assert(0 <= byteIndex && byteIndex <= Length*sizeof(char), "Byte Index not in string");

            StringBuilder ret = this;
            while (ret.m_ChunkOffset*sizeof(char) > byteIndex)
                ret = ret.m_ChunkPrevious;

            Contract.Assert(ret != null, "Byte Index not in string");
            return ret;
        }

        /// <summary>
        /// Finds the chunk that logically follows the 'chunk' chunk.  Chunks only persist the pointer to 
        /// the chunk that is logically before it, so this routine has to start at the this pointer (which 
        /// is a assumed to point at the chunk representing the whole stringbuilder) and search
        /// until it finds the current chunk (thus is O(n)).  So it is more expensive than a field fetch!
        /// </summary>
        private StringBuilder Next(StringBuilder chunk)
        {
            if (chunk == this)
                return null;
            return FindChunkForIndex(chunk.m_ChunkOffset + chunk.m_ChunkLength);
        }

        /// <summary>
        /// Assumes that 'this' is the last chunk in the list and that it is full.  Upon return the 'this'
        /// block is updated so that it is a new block that has at least 'minBlockCharCount' characters.
        /// that can be used to copy characters into it.   
        /// </summary>
        private void ExpandByABlock(int minBlockCharCount)
        {
            Contract.Requires(Capacity == Length, "Expand expect to be called only when there is no space left");        // We are currently full
            Contract.Requires(minBlockCharCount > 0, "Expansion request must be positive");

            VerifyClassInvariant();

            if ((minBlockCharCount + Length) > m_MaxCapacity || minBlockCharCount + Length < minBlockCharCount)
                throw new ArgumentOutOfRangeException("requiredLength", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));

            // Compute the length of the new block we need 
            // We make the new chunk at least big enough for the current need (minBlockCharCount)
            // But also as big as the current length (thus doubling capacity), up to a maximum
            // (so we stay in the small object heap, and never allocate really big chunks even if
            // the string gets really big. 
            int newBlockLength = Math.Max(minBlockCharCount, Math.Min(Length, MaxChunkSize));

            // Copy the current block to the new block, and initialize this to point at the new buffer. 
            m_ChunkPrevious = new StringBuilder(this);
            m_ChunkOffset += m_ChunkLength;
            m_ChunkLength = 0;

            // Check for integer overflow (logical buffer size > int.MaxInt)
            if (m_ChunkOffset + newBlockLength < newBlockLength)
            {
                m_ChunkChars = null;
                throw new OutOfMemoryException();
            }
            m_ChunkChars = new char[newBlockLength];

            VerifyClassInvariant();
        }

        /// <summary>
        /// Used by ExpandByABlock to create a new chunk.  The new chunk is a copied from 'from'
        /// In particular the buffer is shared.  It is expected that 'from' chunk (which represents
        /// the whole list, is then updated to point to point to this new chunk. 
        /// </summary>
        private StringBuilder(StringBuilder from)
        {
            m_ChunkLength = from.m_ChunkLength;
            m_ChunkOffset = from.m_ChunkOffset;
            m_ChunkChars = from.m_ChunkChars;
            m_ChunkPrevious = from.m_ChunkPrevious;
            m_MaxCapacity = from.m_MaxCapacity;
            VerifyClassInvariant();
        }

        /// <summary>
        /// Creates a gap of size 'count' at the logical offset (count of characters in the whole string
        /// builder) 'index'.  It returns the 'chunk' and 'indexInChunk' which represents a pointer to
        /// this gap that was just created.  You can then use 'ReplaceInPlaceAtChunk' to fill in the
        /// chunk
        ///
        /// ReplaceAllChunks relies on the fact that indexes above 'index' are NOT moved outside 'chunk'
        /// by this process (because we make the space by creating the cap BEFORE the chunk).  If we
        /// change this ReplaceAllChunks needs to be updated. 
        ///
        /// If dontMoveFollowingChars is true, then the room must be made by inserting a chunk BEFORE the
        /// current chunk (this is what it does most of the time anyway)
        /// </summary>
        [System.Security.SecuritySafeCritical]  // auto-generated
        private void MakeRoom(int index, int count, out StringBuilder chunk, out int indexInChunk, bool doneMoveFollowingChars)
        {
            VerifyClassInvariant();
            Contract.Assert(count > 0, "Count must be strictly positive");
            Contract.Assert(index >= 0, "Index can't be negative");
            if (count + Length > m_MaxCapacity || count + Length < count)
                throw new ArgumentOutOfRangeException("requiredLength", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));

            chunk = this;
            while (chunk.m_ChunkOffset > index)
            {
                chunk.m_ChunkOffset += count;
                chunk = chunk.m_ChunkPrevious;
            }
            indexInChunk = index - chunk.m_ChunkOffset;

            // Cool, we have some space in this block, and you don't have to copy much to get it, go ahead
            // and use it.  This happens typically  when you repeatedly insert small strings at a spot
            // (typically the absolute front) of the buffer.    
            if (!doneMoveFollowingChars && chunk.m_ChunkLength <= DefaultCapacity * 2 && chunk.m_ChunkChars.Length - chunk.m_ChunkLength >= count)
            {
                for (int i = chunk.m_ChunkLength; i > indexInChunk; )
                {
                    --i;
                    chunk.m_ChunkChars[i + count] = chunk.m_ChunkChars[i];
                }
                chunk.m_ChunkLength += count;
                return;
            }

            // Allocate space for the new chunk (will go before this one)
            StringBuilder newChunk = new StringBuilder(Math.Max(count, DefaultCapacity), chunk.m_MaxCapacity, chunk.m_ChunkPrevious);
            newChunk.m_ChunkLength = count;

            // Copy the head of the buffer to the  new buffer. 
            int copyCount1 = Math.Min(count, indexInChunk);
            if (copyCount1 > 0)
            {
                unsafe {
                    fixed (char* chunkCharsPtr = chunk.m_ChunkChars) {
                        ThreadSafeCopy(chunkCharsPtr, newChunk.m_ChunkChars, 0, copyCount1);
    
                        // Slide characters in the current buffer over to make room. 
                        int copyCount2 = indexInChunk - copyCount1;
                        if (copyCount2 >= 0)
                        {
                            ThreadSafeCopy(chunkCharsPtr + copyCount1, chunk.m_ChunkChars, 0, copyCount2);
                            indexInChunk = copyCount2;
                        }
                    }
                }
            }

            chunk.m_ChunkPrevious = newChunk;           // Wire in the new chunk
            chunk.m_ChunkOffset += count;
            if (copyCount1 < count)
            {
                chunk = newChunk;
                indexInChunk = copyCount1;
            }

            VerifyClassInvariant();
        }

        /// <summary>
        ///  Used by MakeRoom to allocate another chunk.  
        /// </summary>
        private StringBuilder(int size, int maxCapacity, StringBuilder previousBlock)
        {
            Contract.Assert(size > 0, "size not positive");
            Contract.Assert(maxCapacity > 0, "maxCapacity not positive");
            m_ChunkChars = new char[size];
            m_MaxCapacity = maxCapacity;
            m_ChunkPrevious = previousBlock;
            if (previousBlock != null)
                m_ChunkOffset = previousBlock.m_ChunkOffset + previousBlock.m_ChunkLength;
            VerifyClassInvariant();
        }

        /// <summary>
        /// Removes 'count' characters from the logical index 'startIndex' and returns the chunk and 
        /// index in the chunk of that logical index in the out parameters.  
        /// </summary>
        [SecuritySafeCritical]
        private void Remove(int startIndex, int count, out StringBuilder chunk, out int indexInChunk)
        {
            VerifyClassInvariant();
            Contract.Assert(startIndex >= 0 && startIndex < Length, "startIndex not in string");

            int endIndex = startIndex + count;

            // Find the chunks for the start and end of the block to delete. 
            chunk = this;
            StringBuilder endChunk = null;
            int endIndexInChunk = 0;
            for (; ; )
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
                chunk = chunk.m_ChunkPrevious;
            }
            Contract.Assert(chunk != null, "fell off beginning of string!");

            int copyTargetIndexInChunk = indexInChunk;
            int copyCount = endChunk.m_ChunkLength - endIndexInChunk;
            if (endChunk != chunk)
            {
                copyTargetIndexInChunk = 0;
                // Remove the characters after startIndex to end of the chunk
                chunk.m_ChunkLength = indexInChunk;

                // Remove the characters in chunks between start and end chunk
                endChunk.m_ChunkPrevious = chunk;
                endChunk.m_ChunkOffset = chunk.m_ChunkOffset + chunk.m_ChunkLength;

                // If the start is 0 then we can throw away the whole start chunk
                if (indexInChunk == 0)
                {
                    endChunk.m_ChunkPrevious = chunk.m_ChunkPrevious;
                    chunk = endChunk;
                }
            }
            endChunk.m_ChunkLength -= (endIndexInChunk - copyTargetIndexInChunk);

            // SafeCritical: We ensure that endIndexInChunk + copyCount is within range of m_ChunkChars and
            // also ensure that copyTargetIndexInChunk + copyCount is within the chunk
            //
            // Remove any characters in the end chunk, by sliding the characters down. 
            if (copyTargetIndexInChunk != endIndexInChunk)  // Sometimes no move is necessary
                ThreadSafeCopy(endChunk.m_ChunkChars, endIndexInChunk, endChunk.m_ChunkChars, copyTargetIndexInChunk, copyCount);

            Contract.Assert(chunk != null, "fell off beginning of string!");
            VerifyClassInvariant();
        }
    }
}
