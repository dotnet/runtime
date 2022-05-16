// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace System.Text
{
    // A Decoder is used to decode a sequence of blocks of bytes into a
    // sequence of blocks of characters. Following instantiation of a decoder,
    // sequential blocks of bytes are converted into blocks of characters through
    // calls to the GetChars method. The decoder maintains state between the
    // conversions, allowing it to correctly decode byte sequences that span
    // adjacent blocks.
    //
    // Instances of specific implementations of the Decoder abstract base
    // class are typically obtained through calls to the GetDecoder method
    // of Encoding objects.
    //
    internal class DecoderNLS : Decoder, ISerializable
    {
        // Remember our encoding
        protected EncodingNLS m_encoding;
        protected bool m_mustFlush;
        internal bool m_throwOnOverflow;
        internal int m_bytesUsed;
        internal DecoderFallback m_fallback;
        internal DecoderFallbackBuffer? m_fallbackBuffer;

        internal DecoderNLS(EncodingNLS encoding)
        {
            m_encoding = encoding;
            m_fallback = m_encoding.DecoderFallback;
            Reset();
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        internal new DecoderFallback Fallback
        {
            get { return m_fallback; }
        }

        internal bool InternalHasFallbackBuffer
        {
            get
            {
                return m_fallbackBuffer is not null;
            }
        }

        public new DecoderFallbackBuffer FallbackBuffer
        {
            get
            {
                m_fallbackBuffer ??= m_fallback is not null ?
                    m_fallback.CreateFallbackBuffer() :
                    DecoderFallback.ReplacementFallback.CreateFallbackBuffer();

                return m_fallbackBuffer;
            }
        }

        public override void Reset()
        {
            m_fallbackBuffer?.Reset();
        }

        public override unsafe int GetCharCount(byte[] bytes, int index, int count)
        {
            return GetCharCount(bytes, index, count, false);
        }

        public override unsafe int GetCharCount(byte[] bytes, int index, int count, bool flush)
        {
            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));

            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? nameof(index) : nameof(count)), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (bytes.Length - index < count)
                throw new ArgumentOutOfRangeException(nameof(bytes), SR.ArgumentOutOfRange_IndexCountBuffer);

            // Avoid null fixed problem
            if (bytes.Length == 0)
                bytes = new byte[1];

            // Just call pointer version
            fixed (byte* pBytes = &bytes[0])
                return GetCharCount(pBytes + index, count, flush);
        }

        public override unsafe int GetCharCount(byte* bytes, int count, bool flush)
        {
            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);

            // Remember the flush
            m_mustFlush = flush;
            m_throwOnOverflow = true;

            // By default just call the encoding version, no flush by default
            return m_encoding.GetCharCount(bytes, count, this);
        }

        public override unsafe int GetChars(byte[] bytes, int byteIndex, int byteCount,
                                             char[] chars, int charIndex)
        {
            return GetChars(bytes, byteIndex, byteCount, chars, charIndex, false);
        }

        public override unsafe int GetChars(byte[] bytes, int byteIndex, int byteCount,
                                            char[] chars, int charIndex, bool flush)
        {
            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));

            if (chars is null)
                throw new ArgumentNullException(nameof(chars));

            if (byteIndex < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((byteIndex < 0 ? nameof(byteIndex) : nameof(byteCount)), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (bytes.Length - byteIndex < byteCount)
                throw new ArgumentOutOfRangeException(nameof(bytes), SR.ArgumentOutOfRange_IndexCountBuffer);

            if (charIndex < 0 || charIndex > chars.Length)
                throw new ArgumentOutOfRangeException(nameof(charIndex), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);

            // Avoid empty input fixed problem
            if (bytes.Length == 0)
                bytes = new byte[1];

            int charCount = chars.Length - charIndex;
            if (chars.Length == 0)
                chars = new char[1];

            // Just call pointer version
            fixed (byte* pBytes = &bytes[0])
            fixed (char* pChars = &chars[0])
                // Remember that charCount is # to decode, not size of array
                return GetChars(pBytes + byteIndex, byteCount, pChars + charIndex, charCount, flush);
        }

        public override unsafe int GetChars(byte* bytes, int byteCount,
                                            char* chars, int charCount, bool flush)
        {
            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));

            if (chars is null)
                throw new ArgumentNullException(nameof(chars));

            if (byteCount < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((byteCount < 0 ? nameof(byteCount) : nameof(charCount)), SR.ArgumentOutOfRange_NeedNonNegNum);

            // Remember our flush
            m_mustFlush = flush;
            m_throwOnOverflow = true;

            // By default just call the encoding's version
            return m_encoding.GetChars(bytes, byteCount, chars, charCount, this);
        }

        // This method is used when the output buffer might not be big enough.
        // Just call the pointer version.  (This gets chars)
        public override unsafe void Convert(byte[] bytes, int byteIndex, int byteCount,
                                            char[] chars, int charIndex, int charCount, bool flush,
                                            out int bytesUsed, out int charsUsed, out bool completed)
        {
            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));

            if (chars is null)
                throw new ArgumentNullException(nameof(chars));

            if (byteIndex < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((byteIndex < 0 ? nameof(byteIndex) : nameof(byteCount)), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((charIndex < 0 ? nameof(charIndex) : nameof(charCount)), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (bytes.Length - byteIndex < byteCount)
                throw new ArgumentOutOfRangeException(nameof(bytes), SR.ArgumentOutOfRange_IndexCountBuffer);

            if (chars.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException(nameof(chars), SR.ArgumentOutOfRange_IndexCountBuffer);

            // Avoid empty input problem
            if (bytes.Length == 0)
                bytes = new byte[1];
            if (chars.Length == 0)
                chars = new char[1];

            // Just call the pointer version (public overrides can't do this)
            fixed (byte* pBytes = &bytes[0])
            {
                fixed (char* pChars = &chars[0])
                {
                    Convert(pBytes + byteIndex, byteCount, pChars + charIndex, charCount, flush,
                        out bytesUsed, out charsUsed, out completed);
                }
            }
        }

        // This is the version that used pointers.  We call the base encoding worker function
        // after setting our appropriate internal variables.  This is getting chars
        public override unsafe void Convert(byte* bytes, int byteCount,
                                            char* chars, int charCount, bool flush,
                                            out int bytesUsed, out int charsUsed, out bool completed)
        {
            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));

            if (chars is null)
                throw new ArgumentNullException(nameof(chars));

            if (byteCount < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((byteCount < 0 ? nameof(byteCount) : nameof(charCount)), SR.ArgumentOutOfRange_NeedNonNegNum);

            // We don't want to throw
            m_mustFlush = flush;
            m_throwOnOverflow = false;
            m_bytesUsed = 0;

            // Do conversion
            charsUsed = m_encoding.GetChars(bytes, byteCount, chars, charCount, this);
            bytesUsed = m_bytesUsed;

            // It's completed if they've used what they wanted AND if they didn't want flush or if we are flushed
            completed = (bytesUsed == byteCount) && (!flush || !HasState) &&
                               (m_fallbackBuffer is null || m_fallbackBuffer.Remaining == 0);
            // Our data thingies are now full, we can return
        }

        public bool MustFlush
        {
            get
            {
                return m_mustFlush;
            }
        }

        // Anything left in our decoder?
        internal virtual bool HasState
        {
            get
            {
                return false;
            }
        }

        // Allow encoding to clear our must flush instead of throwing (in ThrowCharsOverflow)
        internal void ClearMustFlush()
        {
            m_mustFlush = false;
        }
    }
}
