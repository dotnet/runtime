// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Contracts;

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

        public unsafe static int GetByteCount(Encoding encoding, string s)
        {
            Contract.Assert(encoding != null);
            if (s == null)
            {
                throw new ArgumentNullException("s");
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

        public unsafe static int GetBytes(Encoding encoding, string s, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            Contract.Assert(encoding != null);
            if (s == null || bytes == null)
            {
                throw new ArgumentNullException(s == null ? "s" : "bytes", Environment.GetResourceString("ArgumentNull_Array"));
            }
            if (charIndex < 0 || charCount < 0)
            {
                throw new ArgumentOutOfRangeException(charIndex < 0 ? "charIndex" : "charCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if (s.Length - charIndex < charCount)
            {
                throw new ArgumentOutOfRangeException("s", Environment.GetResourceString("ArgumentOutOfRange_IndexCount"));
            }
            if (byteIndex < 0 || byteIndex > bytes.Length)
            {
                throw new ArgumentOutOfRangeException("byteIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }
            Contract.EndContractBlock();

            int byteCount = bytes.Length - byteIndex;

            // Fixed doesn't like empty arrays
            // TODO: Consider just throwing an
            // exception here instead of allocating
            // a new array, if (byteCount == 0)
            if (bytes.Length == 0)
                bytes = new byte[1];
            
            fixed (char* pChars = s) fixed (byte* pBytes = bytes)
            {
                return encoding.GetBytes(pChars + charIndex, charCount, pBytes + byteIndex, byteCount, encoder: null);
            }
        }

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
    }
}
