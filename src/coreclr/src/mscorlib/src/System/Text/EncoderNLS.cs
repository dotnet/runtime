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
        [System.Security.SecurityCritical]  // auto-generated_required
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

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override unsafe int GetByteCount(char[] chars, int index, int count, bool flush)
        {
            // Validate input parameters
            if (chars == null)
                throw new ArgumentNullException( "chars",
                      Environment.GetResourceString("ArgumentNull_Array"));

            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index<0 ? "index" : "count"),
                      Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));

            if (chars.Length - index < count)
                throw new ArgumentOutOfRangeException("chars",
                      Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            Contract.EndContractBlock();

            // Avoid empty input problem
            if (chars.Length == 0)
                chars = new char[1];

            // Just call the pointer version
            int result = -1;
            fixed (char* pChars = chars)
            {
                result = GetByteCount(pChars + index, count, flush);
            }
            return result;
        }

        [System.Security.SecurityCritical]  // auto-generated
        public unsafe override int GetByteCount(char* chars, int count, bool flush)
        {
            // Validate input parameters
            if (chars == null)
                throw new ArgumentNullException( "chars",
                      Environment.GetResourceString("ArgumentNull_Array"));

            if (count < 0)
                throw new ArgumentOutOfRangeException("count",
                      Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();

            this.m_mustFlush = flush;
            this.m_throwOnOverflow = true;
            return m_encoding.GetByteCount(chars, count, this);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override unsafe int GetBytes(char[] chars, int charIndex, int charCount,
                                              byte[] bytes, int byteIndex, bool flush)
        {
            // Validate parameters
            if (chars == null || bytes == null)
                throw new ArgumentNullException((chars == null ? "chars" : "bytes"),
                      Environment.GetResourceString("ArgumentNull_Array"));

            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((charIndex<0 ? "charIndex" : "charCount"),
                      Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));

            if (chars.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException("chars",
                      Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));

            if (byteIndex < 0 || byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException("byteIndex",
                     Environment.GetResourceString("ArgumentOutOfRange_Index"));
            Contract.EndContractBlock();

            if (chars.Length == 0)
                chars = new char[1];

            int byteCount = bytes.Length - byteIndex;
            if (bytes.Length == 0)
                bytes = new byte[1];

            // Just call pointer version
            fixed (char* pChars = chars)
                fixed (byte* pBytes = bytes)

                    // Remember that charCount is # to decode, not size of array.
                    return GetBytes(pChars + charIndex, charCount,
                                    pBytes + byteIndex, byteCount, flush);
        }

        [System.Security.SecurityCritical]  // auto-generated
        public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount, bool flush)
        {
            // Validate parameters
            if (chars == null || bytes == null)
                throw new ArgumentNullException((chars == null ? "chars" : "bytes"),
                      Environment.GetResourceString("ArgumentNull_Array"));

            if (byteCount < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((byteCount<0 ? "byteCount" : "charCount"),
                      Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();

            this.m_mustFlush = flush;
            this.m_throwOnOverflow = true;
            return m_encoding.GetBytes(chars, charCount, bytes, byteCount, this);
        }

        // This method is used when your output buffer might not be large enough for the entire result.
        // Just call the pointer version.  (This gets bytes)
        [System.Security.SecuritySafeCritical]  // auto-generated
        public override unsafe void Convert(char[] chars, int charIndex, int charCount,
                                              byte[] bytes, int byteIndex, int byteCount, bool flush,
                                              out int charsUsed, out int bytesUsed, out bool completed)
        {
            // Validate parameters
            if (chars == null || bytes == null)
                throw new ArgumentNullException((chars == null ? "chars" : "bytes"),
                      Environment.GetResourceString("ArgumentNull_Array"));

            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((charIndex<0 ? "charIndex" : "charCount"),
                      Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));

            if (byteIndex < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((byteIndex<0 ? "byteIndex" : "byteCount"),
                      Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));

            if (chars.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException("chars",
                      Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));

            if (bytes.Length - byteIndex < byteCount)
                throw new ArgumentOutOfRangeException("bytes",
                      Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));

            Contract.EndContractBlock();

            // Avoid empty input problem
            if (chars.Length == 0)
                chars = new char[1];
            if (bytes.Length == 0)
                bytes = new byte[1];

            // Just call the pointer version (can't do this for non-msft encoders)
            fixed (char* pChars = chars)
            {
                fixed (byte* pBytes = bytes)
                {
                    Convert(pChars + charIndex, charCount, pBytes + byteIndex, byteCount, flush,
                        out charsUsed, out bytesUsed, out completed);
                }
            }
        }

        // This is the version that uses pointers.  We call the base encoding worker function
        // after setting our appropriate internal variables.  This is getting bytes
        [System.Security.SecurityCritical]  // auto-generated
        public override unsafe void Convert(char* chars, int charCount,
                                              byte* bytes, int byteCount, bool flush,
                                              out int charsUsed, out int bytesUsed, out bool completed)
        {
            // Validate input parameters
            if (bytes == null || chars == null)
                throw new ArgumentNullException(bytes == null ? "bytes" : "chars",
                    Environment.GetResourceString("ArgumentNull_Array"));
            if (charCount < 0 || byteCount < 0)
            throw new ArgumentOutOfRangeException((charCount<0 ? "charCount" : "byteCount"),
                Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();

            // We don't want to throw
            this.m_mustFlush = flush;
            this.m_throwOnOverflow = false;
            this.m_charsUsed = 0;

            // Do conversion
            bytesUsed = this.m_encoding.GetBytes(chars, charCount, bytes, byteCount, this);
            charsUsed = this.m_charsUsed;

            // Its completed if they've used what they wanted AND if they didn't want flush or if we are flushed
            completed = (charsUsed == charCount) && (!flush || !this.HasState) &&
                (m_fallbackBuffer == null || m_fallbackBuffer.Remaining == 0);

            // Our data thingys are now full, we can return
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
