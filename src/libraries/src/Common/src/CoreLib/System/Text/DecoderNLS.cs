// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Text;
using System;
using System.Runtime.InteropServices;

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

    internal class DecoderNLS : Decoder
    {
        // Remember our encoding
        private Encoding _encoding;
        private bool _mustFlush;
        internal bool _throwOnOverflow;
        internal int _bytesUsed;

        internal DecoderNLS(Encoding encoding)
        {
            _encoding = encoding;
            _fallback = this._encoding.DecoderFallback;
            this.Reset();
        }

        // This is used by our child deserializers
        internal DecoderNLS()
        {
            _encoding = null;
            this.Reset();
        }

        public override void Reset()
        {
            _fallbackBuffer?.Reset();
        }

        public override unsafe int GetCharCount(byte[] bytes, int index, int count)
        {
            return GetCharCount(bytes, index, count, false);
        }

        public override unsafe int GetCharCount(byte[] bytes, int index, int count, bool flush)
        {
            // Validate Parameters
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes),
                    SR.ArgumentNull_Array);

            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? nameof(index) : nameof(count)),
                    SR.ArgumentOutOfRange_NeedNonNegNum);

            if (bytes.Length - index < count)
                throw new ArgumentOutOfRangeException(nameof(bytes),
                    SR.ArgumentOutOfRange_IndexCountBuffer);

            // Just call pointer version
            fixed (byte* pBytes = &MemoryMarshal.GetReference((Span<byte>)bytes))
                return GetCharCount(pBytes + index, count, flush);
        }

        public unsafe override int GetCharCount(byte* bytes, int count, bool flush)
        {
            // Validate parameters
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes),
                      SR.ArgumentNull_Array);

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count),
                      SR.ArgumentOutOfRange_NeedNonNegNum);

            // Remember the flush
            _mustFlush = flush;
            _throwOnOverflow = true;

            // By default just call the encoding version, no flush by default
            return _encoding.GetCharCount(bytes, count, this);
        }

        public override unsafe int GetChars(byte[] bytes, int byteIndex, int byteCount,
                                             char[] chars, int charIndex)
        {
            return GetChars(bytes, byteIndex, byteCount, chars, charIndex, false);
        }

        public override unsafe int GetChars(byte[] bytes, int byteIndex, int byteCount,
                                             char[] chars, int charIndex, bool flush)
        {
            // Validate Parameters
            if (bytes == null || chars == null)
                throw new ArgumentNullException(bytes == null ? nameof(bytes) : nameof(chars),
                    SR.ArgumentNull_Array);

            if (byteIndex < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((byteIndex < 0 ? nameof(byteIndex) : nameof(byteCount)),
                    SR.ArgumentOutOfRange_NeedNonNegNum);

            if (bytes.Length - byteIndex < byteCount)
                throw new ArgumentOutOfRangeException(nameof(bytes),
                    SR.ArgumentOutOfRange_IndexCountBuffer);

            if (charIndex < 0 || charIndex > chars.Length)
                throw new ArgumentOutOfRangeException(nameof(charIndex),
                    SR.ArgumentOutOfRange_Index);

            int charCount = chars.Length - charIndex;

            // Just call pointer version
            fixed (byte* pBytes = &MemoryMarshal.GetReference((Span<byte>)bytes))
            fixed (char* pChars = &MemoryMarshal.GetReference((Span<char>)chars))
                // Remember that charCount is # to decode, not size of array
                return GetChars(pBytes + byteIndex, byteCount,
                                pChars + charIndex, charCount, flush);
        }

        public unsafe override int GetChars(byte* bytes, int byteCount,
                                              char* chars, int charCount, bool flush)
        {
            // Validate parameters
            if (chars == null || bytes == null)
                throw new ArgumentNullException((chars == null ? nameof(chars) : nameof(bytes)),
                      SR.ArgumentNull_Array);

            if (byteCount < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((byteCount < 0 ? nameof(byteCount) : nameof(charCount)),
                      SR.ArgumentOutOfRange_NeedNonNegNum);

            // Remember our flush
            _mustFlush = flush;
            _throwOnOverflow = true;

            // By default just call the encodings version
            return _encoding.GetChars(bytes, byteCount, chars, charCount, this);
        }

        // This method is used when the output buffer might not be big enough.
        // Just call the pointer version.  (This gets chars)
        public override unsafe void Convert(byte[] bytes, int byteIndex, int byteCount,
                                              char[] chars, int charIndex, int charCount, bool flush,
                                              out int bytesUsed, out int charsUsed, out bool completed)
        {
            // Validate parameters
            if (bytes == null || chars == null)
                throw new ArgumentNullException((bytes == null ? nameof(bytes) : nameof(chars)),
                      SR.ArgumentNull_Array);

            if (byteIndex < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((byteIndex < 0 ? nameof(byteIndex) : nameof(byteCount)),
                      SR.ArgumentOutOfRange_NeedNonNegNum);

            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((charIndex < 0 ? nameof(charIndex) : nameof(charCount)),
                      SR.ArgumentOutOfRange_NeedNonNegNum);

            if (bytes.Length - byteIndex < byteCount)
                throw new ArgumentOutOfRangeException(nameof(bytes),
                      SR.ArgumentOutOfRange_IndexCountBuffer);

            if (chars.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException(nameof(chars),
                      SR.ArgumentOutOfRange_IndexCountBuffer);

            // Just call the pointer version (public overrides can't do this)
            fixed (byte* pBytes = &MemoryMarshal.GetReference((Span<byte>)bytes))
            {
                fixed (char* pChars = &MemoryMarshal.GetReference((Span<char>)chars))
                {
                    Convert(pBytes + byteIndex, byteCount, pChars + charIndex, charCount, flush,
                        out bytesUsed, out charsUsed, out completed);
                }
            }
        }

        // This is the version that used pointers.  We call the base encoding worker function
        // after setting our appropriate internal variables.  This is getting chars
        public unsafe override void Convert(byte* bytes, int byteCount,
                                              char* chars, int charCount, bool flush,
                                              out int bytesUsed, out int charsUsed, out bool completed)
        {
            // Validate input parameters
            if (chars == null || bytes == null)
                throw new ArgumentNullException(chars == null ? nameof(chars) : nameof(bytes),
                    SR.ArgumentNull_Array);

            if (byteCount < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((byteCount < 0 ? nameof(byteCount) : nameof(charCount)),
                    SR.ArgumentOutOfRange_NeedNonNegNum);

            // We don't want to throw
            _mustFlush = flush;
            _throwOnOverflow = false;
            _bytesUsed = 0;

            // Do conversion
            charsUsed = _encoding.GetChars(bytes, byteCount, chars, charCount, this);
            bytesUsed = _bytesUsed;

            // Its completed if they've used what they wanted AND if they didn't want flush or if we are flushed
            completed = (bytesUsed == byteCount) && (!flush || !this.HasState) &&
                               (_fallbackBuffer == null || _fallbackBuffer.Remaining == 0);

            // Our data thingy are now full, we can return
        }

        public bool MustFlush
        {
            get
            {
                return _mustFlush;
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
            _mustFlush = false;
        }
    }
}
