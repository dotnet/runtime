// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text
{
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Text;
    using System;
    using System.Diagnostics.Contracts;
    using Runtime.CompilerServices;

    // An Encoder is used to encode a sequence of blocks of characters into
    // a sequence of blocks of bytes. Following instantiation of an encoder,
    // sequential blocks of characters are converted into blocks of bytes through
    // calls to the GetBytes method. The encoder maintains state between the
    // conversions, allowing it to correctly encode character sequences that span
    // adjacent blocks.
    //
    // Instances of specific implementations of the Encoder abstract base
    // class are typically obtained through calls to the GetEncoder method
    // of Encoding objects.
    //

    [Serializable]
    internal class EncoderNLS : Encoder, ISerializable
    {
        // Need a place for the last left over character, most of our encodings use this
        internal char   charLeftOver;
        
        protected Encoding m_encoding;
        
        [NonSerialized] protected   bool     m_mustFlush;
        [NonSerialized] internal    bool     m_throwOnOverflow;
        [NonSerialized] internal    int      m_charsUsed;

#region Serialization

        // Constructor called by serialization. called during deserialization.
        internal EncoderNLS(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException(
                        String.Format(
                            System.Globalization.CultureInfo.CurrentCulture, 
                            Environment.GetResourceString("NotSupported_TypeCannotDeserialized"), this.GetType()));
        }

        // ISerializable implementation. called during serialization.
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            SerializeEncoder(info);
            info.AddValue("encoding", this.m_encoding);
            info.AddValue("charLeftOver", this.charLeftOver);
            info.SetType(typeof(Encoding.DefaultEncoder));
        }

#endregion Serialization 

        internal EncoderNLS(Encoding encoding)
        {
            this.m_encoding = encoding;
            this.m_fallback = this.m_encoding.EncoderFallback;
            this.Reset();
        }

        // This one is used when deserializing (like UTF7Encoding.Encoder)
        internal EncoderNLS()
        {
            this.m_encoding = null;
            this.Reset();
        }

        public override void Reset()
        {
            this.charLeftOver = (char)0;
            if (m_fallbackBuffer != null)
                m_fallbackBuffer.Reset();
        }

        public override unsafe int GetByteCount(char[] chars, int index, int count, bool flush)
        {
            // Validate input parameters
            if ((chars == null) ||
                (index < 0) ||
                (count < 0) ||
                (chars.Length - index < count))
            {
                EncodingForwarder.ThrowValidationFailedException(chars, index, count);
            }
            Contract.EndContractBlock();

            // Avoid empty input problem
            if (chars.Length == 0)
                chars = new char[1];

            // Just call the pointer version
            int result = -1;
            fixed (char* pChars = &chars[0])
            {
                result = GetByteCountValidated(pChars + index, count, flush);
            }
            return result;
        }

        public unsafe override int GetByteCount(char* chars, int count, bool flush)
        {
            // Validate input parameters
            if (chars == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.chars, ExceptionResource.ArgumentNull_Array);
            if (count < 0)
                ThrowHelper.ThrowCountArgumentOutOfRange_NeedNonNegNumException();
            Contract.EndContractBlock();

            return GetByteCountValidated(chars, count, flush);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe int GetByteCountValidated(char* chars, int count, bool flush)
        {
            this.m_mustFlush = flush;
            this.m_throwOnOverflow = true;
            return m_encoding.GetByteCount(chars, count, this);
        }

        public override unsafe int GetBytes(char[] chars, int charIndex, int charCount,
                                              byte[] bytes, int byteIndex, bool flush)
        {
            // Validate parameters
            if ((chars == null) ||
                (bytes == null) ||
                (charIndex < 0) ||
                (charCount < 0) ||
                (chars.Length - charIndex < charCount) ||
                (byteIndex < 0 || byteIndex > bytes.Length))
            {
                EncodingForwarder.ThrowValidationFailedException(chars, charIndex, charCount, bytes);
            }
            Contract.EndContractBlock();

            if (chars.Length == 0)
                chars = new char[1];

            int byteCount = bytes.Length - byteIndex;
            if (bytes.Length == 0)
                bytes = new byte[1];


            // Just call pointer version
            fixed (char* pChars = &chars[0])
            fixed (byte* pBytes = &bytes[0])
            {
                return GetBytesValidated(pChars + charIndex, charCount, pBytes + byteIndex, byteCount, flush);
            }

        }

        public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount, bool flush)
        {
            // Validate parameters
            if ((bytes == null) ||
                (chars == null) ||
                (charCount < 0) ||
                (byteCount < 0))
            {
                EncodingForwarder.ThrowValidationFailedException(chars, charCount, bytes);
            }
            Contract.EndContractBlock();

            return GetBytesValidated(chars, charCount, bytes, byteCount, flush);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe int GetBytesValidated(char* chars, int charCount, byte* bytes, int byteCount, bool flush)
        {
            this.m_mustFlush = flush;
            this.m_throwOnOverflow = true;
            return m_encoding.GetBytes(chars, charCount, bytes, byteCount, this);
        }

        // This method is used when your output buffer might not be large enough for the entire result.
        // Just call the pointer version.  (This gets bytes)
        public override unsafe void Convert(char[] chars, int charIndex, int charCount,
                                              byte[] bytes, int byteIndex, int byteCount, bool flush,
                                              out int charsUsed, out int bytesUsed, out bool completed)
        {
            // Validate parameters
            if (chars == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.chars, ExceptionResource.ArgumentNull_Array);
            if (bytes == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bytes, ExceptionResource.ArgumentNull_Array);
            if (charIndex < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.charIndex, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            if (charCount < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.charCount, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            if (byteIndex < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.byteIndex, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            if (byteCount < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.byteCount, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            if (chars.Length - charIndex < charCount)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.chars, ExceptionResource.ArgumentOutOfRange_IndexCountBuffer);
            if (bytes.Length - byteIndex < byteCount)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.bytes, ExceptionResource.ArgumentOutOfRange_IndexCountBuffer);
            Contract.EndContractBlock();

            StartConversion(flush);

            // Avoid empty input problem
            if (chars.Length == 0)
                chars = new char[1];
            if (bytes.Length == 0)
                bytes = new byte[1];

            // Just call the pointer version (can't do this for non-msft encoders)
            fixed (char* pChars = &chars[0])
            fixed (byte* pBytes = &bytes[0])
            {
                bytesUsed = this.m_encoding.GetBytes(pChars + charIndex, charCount, pBytes + byteIndex, byteCount, this);
            }

            FinishConversion(charCount, flush, out charsUsed, out completed);
        }

        // This is the version that uses pointers.  We call the base encoding worker function
        // after setting our appropriate internal variables.  This is getting bytes
        public override unsafe void Convert(char* chars, int charCount,
                                              byte* bytes, int byteCount, bool flush,
                                              out int charsUsed, out int bytesUsed, out bool completed)
        {
            // Validate input parameters
            if ((bytes == null) ||
                (chars == null) ||
                (charCount < 0) ||
                (byteCount < 0))
            {
                EncodingForwarder.ThrowValidationFailedException(chars, charCount, bytes);
            }
            Contract.EndContractBlock();

            StartConversion(flush);

            // Do conversion
            bytesUsed = this.m_encoding.GetBytes(chars, charCount, bytes, byteCount, this);

            FinishConversion(charCount, flush, out charsUsed, out completed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StartConversion(bool flush)
        {
            // We don't want to throw
            this.m_mustFlush = flush;
            this.m_throwOnOverflow = false;
            this.m_charsUsed = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FinishConversion(int charCount, bool flush, out int charsUsed, out bool completed)
        {
            charsUsed = this.m_charsUsed;
            // Its completed if they've used what they wanted AND if they didn't want flush or if we are flushed
            completed = (charsUsed == charCount) && (!flush || !this.HasState) &&
                        (m_fallbackBuffer == null || m_fallbackBuffer.Remaining == 0);
        }

        public Encoding Encoding
        {
            get
            {
                return m_encoding;
            }
        }

        public bool MustFlush
        {
            get
            {
                return m_mustFlush;
            }
        }


        // Anything left in our encoder?
        internal virtual bool HasState
        {
            get
            {
                return (this.charLeftOver != (char)0);
            }
        }

        // Allow encoding to clear our must flush instead of throwing (in ThrowBytesOverflow)
        internal void ClearMustFlush()
        {
            m_mustFlush = false;
        }

    }
}
