// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Contracts;
using System.Security;

namespace System.Text
{
    // Shared implementations for commonly overriden Encoding methods

    internal static class EncodingForwarder
    {
        // We normally have to duplicate a lot of code between UTF8Encoding,
        // UTF7Encoding, EncodingNLS, etc. because we want to override many
        // of the methods in all of those classes to just forward to the unsafe
        // version. (e.g. GetBytes(char[]))
        // Ideally, everything would just derive from EncodingNLS, but that's
        // not exposed in the public API, and C# prohibits a public class from
        // inheriting from an internal one. So, we have to override each of the
        // methods in question and repeat the argument validation/logic.

        // These set of methods exist so instead of duplicating code, we can
        // simply have those overriden methods call here to do the actual work.

        // NOTE: This class should ONLY be called from Encodings that override
        // the internal methods which accept an Encoder/DecoderNLS. The reason
        // for this is that by default, those methods just call the same overload
        // except without the encoder/decoder parameter. If an overriden method
        // without that parameter calls this class, which calls the overload with
        // the parameter, it will call the same method again, which will eventually
        // lead to a StackOverflowException.

        [SecuritySafeCritical]
        public unsafe static int GetByteCount(Encoding encoding, char[] chars, int index, int count)
        {
            // Validate parameters

            Contract.Assert(encoding != null); // this parameter should only be affected internally, so just do a debug check here
            if (chars == null)
            {
                throw new ArgumentNullException("chars", Environment.GetResourceString("ArgumentNull_Array"));
            }
            if (index < 0 || count < 0)
            {
                throw new ArgumentOutOfRangeException(index < 0 ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if (chars.Length - index < count)
            {
                throw new ArgumentOutOfRangeException("chars", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            }
            Contract.EndContractBlock();

            // If no input, return 0, avoid fixed empty array problem
            if (count == 0)
                return 0;

            // Just call the (internal) pointer version
            fixed (char* pChars = chars)
                return encoding.GetByteCount(pChars + index, count, encoder: null);
        }

        [SecuritySafeCritical]
        public unsafe static int GetByteCount(Encoding encoding, string s)
        {
            Contract.Assert(encoding != null);
            if (s == null)
            {
                string paramName = encoding is ASCIIEncoding ? "chars" : "s"; // ASCIIEncoding calls the string chars
                // UTF8Encoding does this as well, but it originally threw an ArgumentNull for "s" so don't check for that
                throw new ArgumentNullException(paramName);
            }
            Contract.EndContractBlock();

            // NOTE: The behavior of fixed *is* defined by
            // the spec for empty strings, although not for
            // null strings/empty char arrays. See
            // http://stackoverflow.com/q/37757751/4077294
            // Regardless, we may still want to check
            // for if (s.Length == 0) in the future
            // and short-circuit as an optimization (TODO).

            fixed (char* pChars = s)
                return encoding.GetByteCount(pChars, s.Length, encoder: null);
        }

        [SecurityCritical]
        public unsafe static int GetByteCount(Encoding encoding, char* chars, int count)
        {
            Contract.Assert(encoding != null);
            if (chars == null)
            {
                throw new ArgumentNullException("chars", Environment.GetResourceString("ArgumentNull_Array"));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            Contract.EndContractBlock();

            // Call the internal version, with an empty encoder
            return encoding.GetByteCount(chars, count, encoder: null);
        }

        [SecuritySafeCritical]
        public unsafe static int GetBytes(Encoding encoding, string s, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            Contract.Assert(encoding != null);
            if (s == null || bytes == null)
            {
                string stringName = encoding is ASCIIEncoding ? "chars" : "s"; // ASCIIEncoding calls the first parameter chars
                throw new ArgumentNullException(s == null ? stringName : "bytes", Environment.GetResourceString("ArgumentNull_Array"));
            }
            if (charIndex < 0 || charCount < 0)
            {
                throw new ArgumentOutOfRangeException(charIndex < 0 ? "charIndex" : "charCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if (s.Length - charIndex < charCount)
            {
                string stringName = encoding is ASCIIEncoding ? "chars" : "s"; // ASCIIEncoding calls the first parameter chars
                // Duplicate the above check since we don't want the overhead of a type check on the general path
                throw new ArgumentOutOfRangeException(stringName, Environment.GetResourceString("ArgumentOutOfRange_IndexCount"));
            }
            if (byteIndex < 0 || byteIndex > bytes.Length)
            {
                throw new ArgumentOutOfRangeException("byteIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }
            Contract.EndContractBlock();

            int byteCount = bytes.Length - byteIndex;

            // Fixed doesn't like empty arrays
            if (bytes.Length == 0)
                bytes = new byte[1];
            
            fixed (char* pChars = s) fixed (byte* pBytes = bytes)
            {
                return encoding.GetBytes(pChars + charIndex, charCount, pBytes + byteIndex, byteCount, encoder: null);
            }
        }

        [SecuritySafeCritical]
        public unsafe static int GetBytes(Encoding encoding, char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            Contract.Assert(encoding != null);
            if (chars == null || bytes == null)
            {
                throw new ArgumentNullException(chars == null ? "chars" : "bytes", Environment.GetResourceString("ArgumentNull_Array"));
            }
            if (charIndex < 0 || charCount < 0)
            {
                throw new ArgumentOutOfRangeException(charIndex < 0 ? "charIndex" : "charCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if (chars.Length - charIndex < charCount)
            {
                throw new ArgumentOutOfRangeException("chars", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            }
            if (byteIndex < 0 || byteIndex > bytes.Length)
            {
                throw new ArgumentOutOfRangeException("byteIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }
            Contract.EndContractBlock();

            // If nothing to encode return 0, avoid fixed problem
            if (charCount == 0)
                return 0;

            // Note that this is the # of bytes to decode,
            // not the size of the array
            int byteCount = bytes.Length - byteIndex;

            // Fixed doesn't like 0 length arrays.
            if (bytes.Length == 0)
                bytes = new byte[1];
            
            // Just call the (internal) pointer version
            fixed (char* pChars = chars) fixed (byte* pBytes = bytes)
            {
                return encoding.GetBytes(pChars + charIndex, charCount, pBytes + byteIndex, byteCount, encoder: null);
            }
        }

        [SecurityCritical]
        public unsafe static int GetBytes(Encoding encoding, char* chars, int charCount, byte* bytes, int byteCount)
        {
            Contract.Assert(encoding != null);
            if (bytes == null || chars == null)
            {
                throw new ArgumentNullException(bytes == null ? "bytes" : "chars", Environment.GetResourceString("ArgumentNull_Array"));
            }
            if (charCount < 0 || byteCount < 0)
            {
                throw new ArgumentOutOfRangeException(charCount < 0 ? "charCount" : "byteCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            Contract.EndContractBlock();

            return encoding.GetBytes(chars, charCount, bytes, byteCount, encoder: null);
        }

        [SecuritySafeCritical]
        public unsafe static int GetCharCount(Encoding encoding, byte[] bytes, int index, int count)
        {
            Contract.Assert(encoding != null);
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes", Environment.GetResourceString("ArgumentNull_Array"));
            }
            if (index < 0 || count < 0)
            {
                throw new ArgumentOutOfRangeException(index < 0 ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if (bytes.Length - index < count)
            {
                throw new ArgumentOutOfRangeException("bytes", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            }
            Contract.EndContractBlock();

            // If no input just return 0, fixed doesn't like 0 length arrays.
            if (count == 0)
                return 0;

            // Just call pointer version
            fixed (byte* pBytes = bytes)
                return encoding.GetCharCount(pBytes + index, count, decoder: null);
        }

        [SecurityCritical]
        public unsafe static int GetCharCount(Encoding encoding, byte* bytes, int count)
        {
            Contract.Assert(encoding != null);
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes", Environment.GetResourceString("ArgumentNull_Array"));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            Contract.EndContractBlock();

            return encoding.GetCharCount(bytes, count, decoder: null);
        }

        [SecuritySafeCritical]
        public unsafe static int GetChars(Encoding encoding, byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            Contract.Assert(encoding != null);
            if (bytes == null || chars == null)
            {
                throw new ArgumentNullException(bytes == null ? "bytes" : "chars", Environment.GetResourceString("ArgumentNull_Array"));
            }
            if (byteIndex < 0 || byteCount < 0)
            {
                throw new ArgumentOutOfRangeException(byteIndex < 0 ? "byteIndex" : "byteCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if (bytes.Length - byteIndex < byteCount)
            {
                throw new ArgumentOutOfRangeException("bytes", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            }
            if (charIndex < 0 || charIndex > chars.Length)
            {
                throw new ArgumentOutOfRangeException("charIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }
            Contract.EndContractBlock();

            if (byteCount == 0)
                return 0;

            // NOTE: This is the # of chars we can decode,
            // not the size of the array
            int charCount = chars.Length - charIndex;

            // Fixed doesn't like 0 length arrays.
            if (chars.Length == 0)
                chars = new char[1];

            fixed (byte* pBytes = bytes) fixed (char* pChars = chars)
            {
                return encoding.GetChars(pBytes + byteIndex, byteCount, pChars + charIndex, charCount, decoder: null);
            }
        }

        [SecurityCritical]
        public unsafe static int GetChars(Encoding encoding, byte* bytes, int byteCount, char* chars, int charCount)
        {
            Contract.Assert(encoding != null);
            if (bytes == null || chars == null)
            {
                throw new ArgumentNullException(bytes == null ? "bytes" : "chars", Environment.GetResourceString("ArgumentNull_Array"));
            }
            if (charCount < 0 || byteCount < 0)
            {
                throw new ArgumentOutOfRangeException(charCount < 0 ? "charCount" : "byteCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            Contract.EndContractBlock();

            return encoding.GetChars(bytes, byteCount, chars, charCount, decoder: null);
        }

        [SecuritySafeCritical]
        public unsafe static string GetString(Encoding encoding, byte[] bytes, int index, int count)
        {
            Contract.Assert(encoding != null);
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes", Environment.GetResourceString("ArgumentNull_Array"));
            }
            if (index < 0 || count < 0)
            {
                // ASCIIEncoding has different names for its parameters here (byteIndex, byteCount)
                bool ascii = encoding is ASCIIEncoding;
                string indexName = ascii ? "byteIndex" : "index";
                string countName = ascii ? "byteCount" : "count";
                throw new ArgumentOutOfRangeException(index < 0 ? indexName : countName, Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if (bytes.Length - index < count)
            {
                throw new ArgumentOutOfRangeException("bytes", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            }
            Contract.EndContractBlock();
            
            // Avoid problems with empty input buffer
            if (count == 0)
                return string.Empty;

            // Call string.CreateStringFromEncoding here, which
            // allocates a string and lets the Encoding modify
            // it in place. This way, we don't have to allocate
            // an intermediary char[] to decode into and then
            // call the string constructor; instead we decode
            // directly into the string.

            fixed (byte* pBytes = bytes)
            {
                return string.CreateStringFromEncoding(pBytes + index, count, encoding);
            }
        }
    }
}
