// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
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

        public unsafe static int GetByteCount(Encoding encoding, char[] chars, int index, int count)
        {
            // Validate parameters
            Debug.Assert(encoding != null); // this parameter should only be affected internally, so just do a debug check here
            if ((chars == null) ||
                (index < 0) ||
                (count < 0) ||
                (chars.Length - index < count))
            {
                ThrowValidationFailedException(chars, index, count);
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
            Debug.Assert(encoding != null);
            if (s == null)
            {
                string paramName = encoding is ASCIIEncoding ? "chars" : nameof(s); // ASCIIEncoding calls the string chars
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

        public unsafe static int GetByteCount(Encoding encoding, char* chars, int count)
        {
            Debug.Assert(encoding != null);
            if (chars == null)
            {
                throw new ArgumentNullException(nameof(chars), Environment.GetResourceString("ArgumentNull_Array"));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            Contract.EndContractBlock();

            // Call the internal version, with an empty encoder
            return encoding.GetByteCount(chars, count, encoder: null);
        }

        public unsafe static int GetBytes(Encoding encoding, string s, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            Debug.Assert(encoding != null);
            if (s == null || bytes == null)
            {
                string stringName = encoding is ASCIIEncoding ? "chars" : nameof(s); // ASCIIEncoding calls the first parameter chars
                throw new ArgumentNullException(s == null ? stringName : nameof(bytes), Environment.GetResourceString("ArgumentNull_Array"));
            }
            if (charIndex < 0 || charCount < 0)
            {
                throw new ArgumentOutOfRangeException(charIndex < 0 ? nameof(charIndex) : nameof(charCount), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if (s.Length - charIndex < charCount)
            {
                string stringName = encoding is ASCIIEncoding ? "chars" : nameof(s); // ASCIIEncoding calls the first parameter chars
                // Duplicate the above check since we don't want the overhead of a type check on the general path
                throw new ArgumentOutOfRangeException(stringName, Environment.GetResourceString("ArgumentOutOfRange_IndexCount"));
            }
            if (byteIndex < 0 || byteIndex > bytes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(byteIndex), Environment.GetResourceString("ArgumentOutOfRange_Index"));
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

        public unsafe static int GetBytes(Encoding encoding, char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            Debug.Assert(encoding != null);
            // Validate parameters
            if ((chars == null) ||
                (bytes == null) ||
                (charIndex < 0) ||
                (charCount < 0) ||
                (chars.Length - charIndex < charCount) ||
                (byteIndex < 0 || byteIndex > bytes.Length))
            {
                ThrowValidationFailedException(chars, charIndex, charCount, bytes);
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

        public unsafe static int GetBytes(Encoding encoding, char* chars, int charCount, byte* bytes, int byteCount)
        {
            Debug.Assert(encoding != null);
            // Validate parameters
            if ((bytes == null) ||
                (chars == null) ||
                (charCount < 0) ||
                (byteCount < 0))
            {
                ThrowValidationFailedException(chars, charCount, bytes);
            }
            Contract.EndContractBlock();

            return encoding.GetBytes(chars, charCount, bytes, byteCount, encoder: null);
        }

#if !BIGENDIAN
        // Ascii fast-paths
        public unsafe static byte[] GetBytesAsciiFastPath(Encoding encoding, String s)
        {
            // Fast path for pure ASCII data for ASCII and UTF8 encoding
            Debug.Assert(encoding != null);
            if (s == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s, ExceptionResource.ArgumentNull_String);
            Contract.EndContractBlock();

            int charCount = s.Length;

            byte[] bytes;
            if (charCount > 0) 
            {
                fixed (char* input = s)
                    bytes = GetBytesAsciiFastPath(encoding, input, charCount);
            } 
            else 
            {
                bytes = Array.Empty<byte>();
            }

            return bytes;
        }

        internal unsafe static byte[] GetBytesAsciiFastPath(Encoding encoding, char[] chars, int index, int count)
        {
            // Fast path for pure ASCII data for ASCII and UTF8 encoding
            Debug.Assert(encoding != null);
            if ((chars == null) ||
                (index < 0) ||
                (count < 0) ||
                (chars.Length - index < count))
            {
                ThrowValidationFailedException(chars, index, count);
            }
            Contract.EndContractBlock();

            byte[] bytes;
            if (count > 0) 
            {
                fixed (char* input = chars)
                    bytes = GetBytesAsciiFastPath(encoding, input + index, count);
            } 
            else 
            {
                bytes = Array.Empty<byte>();
            }

            return bytes;

        }

        public unsafe static int GetBytesAsciiFastPath(Encoding encoding, char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            // Fast path for pure ASCII data for ASCII and UTF8 encoding
            Debug.Assert(encoding != null);
            if ((chars == null) ||
                (bytes == null) ||
                (charIndex < 0) ||
                (charCount < 0) ||
                (chars.Length - charIndex < charCount) ||
                (byteIndex < 0 || byteIndex > bytes.Length))
            {
                ThrowValidationFailedException(chars, charIndex, charCount, bytes);
            }
            Contract.EndContractBlock();

            // Note that byteCount is the # of bytes to decode, not the size of the array
            int byteCount = bytes.Length - byteIndex;
            if (charCount > 0 && byteCount == 0)
                ThrowBytesOverflow(encoding);

            int lengthEncoded;
            if (charCount > 0 && byteCount > 0) 
            {
                fixed (char* pInput = chars)
                fixed (byte* pOutput = &bytes[0]) 
                {
                    char* input = pInput + charIndex;
                    byte* output = pOutput + byteIndex;
                    var lengthToEncode = Math.Min(charCount, byteCount);
                    lengthEncoded = GetBytesAsciiFastPath(input, output, lengthToEncode);
                    if (lengthEncoded < lengthToEncode) 
                    {
                        // Not all ASCII, use encoding's GetBytes for remaining conversion
                        lengthEncoded += encoding.GetBytesFallback(input + lengthEncoded, charCount - lengthEncoded, output + lengthEncoded, byteCount - lengthEncoded, null);
                    }
                }
            } 
            else 
            {
                // Nothing to encode
                lengthEncoded = 0;
            }

            return lengthEncoded;
        }

        public unsafe static int GetBytesAsciiFastPath(Encoding encoding, char* chars, int charCount, byte* bytes, int byteCount)
        {
            // Fast path for pure ASCII data for ASCII and UTF8 encoding
            Debug.Assert(encoding != null);
            if ((bytes == null) || 
                (chars == null) ||
                (charCount < 0) ||
                (byteCount < 0))
            {
                ThrowValidationFailedException(chars, charCount, bytes);
            }
            Contract.EndContractBlock();

            if (charCount > 0 && byteCount == 0)
                ThrowBytesOverflow(encoding);

            int lengthEncoded;
            if (charCount > 0 && byteCount > 0)
            {
                var lengthToEncode = Math.Min(charCount, byteCount);
                lengthEncoded = GetBytesAsciiFastPath(chars, bytes, lengthToEncode);
                if (lengthEncoded < lengthToEncode)
                {
                    // Not all ASCII, use encoding's GetBytes for remaining conversion
                    lengthEncoded += encoding.GetBytesFallback(chars + lengthEncoded, charCount - lengthEncoded, bytes + lengthEncoded, byteCount - lengthEncoded, null);
                }
            }
            else
            {
                // Nothing to encode
                lengthEncoded = 0;
            }

            return lengthEncoded;
        }

        public unsafe static int GetBytesAsciiFastPath(Encoding encoding, char* chars, int charCount, byte* bytes, int byteCount, EncoderNLS encoder)
        {
            // Fast path for pure ASCII data for ASCII and UTF8 encoding
            // Just need to Assert, this is called by internal EncoderNLS and parameters should already be checked
            Debug.Assert(encoding != null);
            Debug.Assert(bytes != null);
            Debug.Assert(chars != null);
            Debug.Assert(charCount >= 0);
            Debug.Assert(byteCount >= 0);

            int lengthEncoded;
            if ((encoder?.InternalHasFallbackBuffer ?? false) && 
                (encoder.FallbackBuffer.Remaining > 0))
            {
                // Non-ASCII data already in Fallback buffer, so straight to encoder's version
                lengthEncoded = encoding.GetBytesFallback(chars, charCount, bytes, byteCount, encoder);
            } 
            else if (charCount > 0 && byteCount > 0)
            {
                var lengthToEncode = Math.Min(charCount, byteCount);
                lengthEncoded = GetBytesAsciiFastPath(chars, bytes, lengthToEncode);
                if (lengthEncoded < lengthToEncode)
                {
                    // Not all ASCII, use encoding's GetBytes for remaining conversion
                    lengthEncoded += encoding.GetBytesFallback(chars + lengthEncoded, charCount - lengthEncoded, bytes + lengthEncoded, byteCount - lengthEncoded, encoder);
                }
            }
            else
            {
                // Nothing to encode
                lengthEncoded = 0;
            }

            return lengthEncoded;
        }

        public unsafe static int GetBytesAsciiFastPath(Encoding encoding, String s, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            // Fast path for pure ASCII data for ASCII and UTF8 encoding
            Debug.Assert(encoding != null);
            if ((s == null) ||
               (bytes == null) ||
               (charIndex < 0) ||
               (charCount < 0) ||
               (s.Length - charIndex < charCount) ||
               (byteIndex < 0 || byteIndex > bytes.Length))
            {
                ThrowValidationFailed(s, charIndex, charCount, bytes);
            }
            Contract.EndContractBlock();

            // Note that byteCount is the # of bytes to decode, not the size of the array
            int byteCount = bytes.Length - byteIndex;
            if (charCount > 0 && byteCount == 0)
                ThrowBytesOverflow(encoding);

            int lengthEncoded;
            if (charCount > 0 && byteCount > 0) 
            {
                fixed (char* pInput = s)
                fixed (byte* pOutput = &bytes[0]) 
                {
                    char* input = pInput + charIndex;
                    byte* output = pOutput + byteIndex;
                    var lengthToEncode = Math.Min(charCount, byteCount);
                    lengthEncoded = GetBytesAsciiFastPath(input, output, lengthToEncode);
                    if (lengthEncoded < lengthToEncode) 
                    {
                        // Not all ASCII, use encoding's GetBytes for remaining conversion
                        lengthEncoded += encoding.GetBytesFallback(input + lengthEncoded, charCount - lengthEncoded, output + lengthEncoded, byteCount - lengthEncoded, null);
                    }
                }
            } 
            else 
            {
                // Nothing to encode
                lengthEncoded = 0;
            }

            return lengthEncoded;
        }

        private unsafe static byte[] GetBytesAsciiFastPath(Encoding encoding, char* input, int charCount)
        {
            // Fast path for pure ASCII data for ASCII and UTF8 encoding
            int asciiLength;
            int remaining = 0;
            // Assume string is all ASCII and size array for that
            byte[] bytes = new byte[charCount];

            fixed (byte* output = &bytes[0]) 
            {
                asciiLength = GetBytesAsciiFastPath(input, output, charCount);
                if (asciiLength < charCount) 
                {
                    // Not all ASCII, get the byte count for the remaining encoded conversion
                    remaining = encoding.GetByteCount(input + asciiLength, charCount - asciiLength, null);
                }
            }

            if (remaining > 0) 
            {
                // Not all ASCII, fallback to slower path for remaining encoding
                var encoded = ResizeGetRemainingBytes(encoding, input, charCount, ref bytes, asciiLength, remaining);
                Debug.Assert(encoded == remaining);
            }

            return bytes;
        }

        internal unsafe static int ResizeGetRemainingBytes(Encoding encoding, char* chars, int charCount, ref byte[] bytes, int alreadyEncoded, int remaining)
        {
            // Resize the array to the correct size
            Array.Resize(ref bytes, alreadyEncoded + remaining);

            int encoded;
            fixed (byte* output = &bytes[0]) 
            {
                // Use encoding's GetBytes for remaining conversion
                encoded = encoding.GetBytesFallback(chars + alreadyEncoded, charCount - alreadyEncoded, output + alreadyEncoded, remaining, null);
            }

            return encoded;
        }

        internal unsafe static int GetBytesAsciiFastPath(char* input, byte* output, int byteCount)
        {
            const int Shift16Shift24 = (1 << 16) | (1 << 24);
            const int Shift8Identity = (1 <<  8) | (1);

            // Encode as bytes upto the first non-ASCII byte and return count encoded
            int i = 0;
#if BIT64
            if (byteCount < 4) goto trailing;

            int unaligned = (int)(((ulong)input) & 0x7) >> 1;
            // Unaligned chars
            for (; i < unaligned; i++) 
            {
                char ch = *(input + i);
                if (ch > 0x7F) 
                {
                    goto exit; // Found non-ASCII, bail
                }
                else 
                {
                    *(output + i) = (byte)ch; // Cast convert
                }
            }

            // Aligned
            int ulongDoubleCount = (byteCount - i) & ~0x7;
            for (; i < ulongDoubleCount; i += 8) 
            {
                ulong inputUlong0 = *(ulong*)(input + i);
                ulong inputUlong1 = *(ulong*)(input + i + 4);
                if (((inputUlong0 | inputUlong1) & 0xFF80FF80FF80FF80) != 0) 
                {
                    goto exit; // Found non-ASCII, bail
                }
                // Pack 16 ASCII chars into 16 bytes
                *(uint*)(output + i) =
                    ((uint)((inputUlong0 * Shift16Shift24) >> 24) & 0xffff) |
                    ((uint)((inputUlong0 * Shift8Identity) >> 24) & 0xffff0000);
                *(uint*)(output + i + 4) =
                    ((uint)((inputUlong1 * Shift16Shift24) >> 24) & 0xffff) |
                    ((uint)((inputUlong1 * Shift8Identity) >> 24) & 0xffff0000);
            }
            if (byteCount - 4 > i) 
            {
                ulong inputUlong = *(ulong*)(input + i);
                if ((inputUlong & 0xFF80FF80FF80FF80) != 0) 
                {
                    goto exit; // Found non-ASCII, bail
                }
                // Pack 8 ASCII chars into 8 bytes
                *(uint*)(output + i) =
                    ((uint)((inputUlong * Shift16Shift24) >> 24) & 0xffff) |
                    ((uint)((inputUlong * Shift8Identity) >> 24) & 0xffff0000);
                i += 4;
            }

         trailing:
            for (; i < byteCount; i++) 
            {
                char ch = *(input + i);
                if (ch > 0x7F) 
                {
                    goto exit; // Found non-ASCII, bail
                }
                else
                {
                    *(output + i) = (byte)ch; // Cast convert
                }
            }
#else
            // Unaligned chars
            if ((unchecked((int)input) & 0x2) != 0) 
            {
                char ch = *input;
                if (ch > 0x7F) 
                {
                    goto exit; // Found non-ASCII, bail
                } 
                else 
                {
                    i = 1;
                    *(output) = (byte)ch; // Cast convert
                }
            }

            // Aligned
            int uintCount = (byteCount - i) & ~0x3;
            for (; i < uintCount; i += 4) 
            {
                uint inputUint0 = *(uint*)(input + i);
                uint inputUint1 = *(uint*)(input + i + 2);
                if (((inputUint0 | inputUint1) & 0xFF80FF80) != 0) 
                {
                    goto exit; // Found non-ASCII, bail
                }
                // Pack 4 ASCII chars into 4 bytes
                *(ushort*)(output + i) = (ushort)(inputUint0 | (inputUint0 >> 8));
                *(ushort*)(output + i + 2) = (ushort)(inputUint1 | (inputUint1 >> 8));
            }
            if (byteCount - 1 > i) 
            {
                uint inputUint = *(uint*)(input + i);
                if ((inputUint & 0xFF80FF80) != 0) 
                {
                    goto exit; // Found non-ASCII, bail
                }
                // Pack 2 ASCII chars into 2 bytes
                *(ushort*)(output + i) = (ushort)(inputUint | (inputUint >> 8));
                i += 2;
            }

            if (i < byteCount) 
            {
                char ch = *(input + i);
                if (ch > 0x7F) 
                {
                    goto exit; // Found non-ASCII, bail
                }
                else 
                {
                    *(output + i) = (byte)ch; // Cast convert
                    i = byteCount;
                }
            }
#endif // BIT64
        exit:
            return i;
        }
#endif // !BIGENDIAN

        public unsafe static int GetCharCount(Encoding encoding, byte[] bytes, int index, int count)
        {
            Debug.Assert(encoding != null);
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes), Environment.GetResourceString("ArgumentNull_Array"));
            }
            if (index < 0 || count < 0)
            {
                throw new ArgumentOutOfRangeException(index < 0 ? nameof(index) : nameof(count), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if (bytes.Length - index < count)
            {
                throw new ArgumentOutOfRangeException(nameof(bytes), Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            }
            Contract.EndContractBlock();

            // If no input just return 0, fixed doesn't like 0 length arrays.
            if (count == 0)
                return 0;

            // Just call pointer version
            fixed (byte* pBytes = bytes)
                return encoding.GetCharCount(pBytes + index, count, decoder: null);
        }

        public unsafe static int GetCharCount(Encoding encoding, byte* bytes, int count)
        {
            Debug.Assert(encoding != null);
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes), Environment.GetResourceString("ArgumentNull_Array"));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            Contract.EndContractBlock();

            return encoding.GetCharCount(bytes, count, decoder: null);
        }

        public unsafe static int GetChars(Encoding encoding, byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            Debug.Assert(encoding != null);
            if (bytes == null || chars == null)
            {
                throw new ArgumentNullException(bytes == null ? nameof(bytes) : nameof(chars), Environment.GetResourceString("ArgumentNull_Array"));
            }
            if (byteIndex < 0 || byteCount < 0)
            {
                throw new ArgumentOutOfRangeException(byteIndex < 0 ? nameof(byteIndex) : nameof(byteCount), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if (bytes.Length - byteIndex < byteCount)
            {
                throw new ArgumentOutOfRangeException(nameof(bytes), Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            }
            if (charIndex < 0 || charIndex > chars.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(charIndex), Environment.GetResourceString("ArgumentOutOfRange_Index"));
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

        public unsafe static int GetChars(Encoding encoding, byte* bytes, int byteCount, char* chars, int charCount)
        {
            Debug.Assert(encoding != null);
            if (bytes == null || chars == null)
            {
                throw new ArgumentNullException(bytes == null ? nameof(bytes) : nameof(chars), Environment.GetResourceString("ArgumentNull_Array"));
            }
            if (charCount < 0 || byteCount < 0)
            {
                throw new ArgumentOutOfRangeException(charCount < 0 ? nameof(charCount) : nameof(byteCount), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            Contract.EndContractBlock();

            return encoding.GetChars(bytes, byteCount, chars, charCount, decoder: null);
        }

        public unsafe static string GetString(Encoding encoding, byte[] bytes, int index, int count)
        {
            Debug.Assert(encoding != null);
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes), Environment.GetResourceString("ArgumentNull_Array"));
            }
            if (index < 0 || count < 0)
            {
                // ASCIIEncoding has different names for its parameters here (byteIndex, byteCount)
                bool ascii = encoding is ASCIIEncoding;
                string indexName = ascii ? "byteIndex" : nameof(index);
                string countName = ascii ? "byteCount" : nameof(count);
                throw new ArgumentOutOfRangeException(index < 0 ? indexName : countName, Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if (bytes.Length - index < count)
            {
                throw new ArgumentOutOfRangeException(nameof(bytes), Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
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

        private static void ThrowBytesOverflow(Encoding encoding)
        {
            throw GetArgumentException_ThrowBytesOverflow(encoding);
        }

        internal static void ThrowValidationFailedException(char[] chars, int index, int count)
        {
            throw GetValidationFailedException(chars, index, count);
        }

        internal static void ThrowValidationFailedException(char[] chars, int charIndex, int charCount, byte[] bytes)
        {
            throw GetValidationFailedException(chars, charIndex, charCount, bytes);
        }

        internal static void ThrowValidationFailed(string s, int index, int count)
        {
            throw GetValidationFailedException(s, index, count);
        }
        
        private static void ThrowValidationFailed(string s, int charIndex, int charCount, byte[] bytes)
        {
            throw GetValidationFailedException(s, charIndex, charCount, bytes);
        }

        internal static unsafe void ThrowValidationFailedException(char* chars, int charCount, byte* bytes)
        {
            throw GetValidationFailedException(chars, charCount, bytes);
        }

        private static ArgumentException GetArgumentException_ThrowBytesOverflow(Encoding encoding)
        {
            throw new ArgumentException(
                Environment.GetResourceString("Argument_EncodingConversionOverflowBytes",
                encoding.EncodingName, encoding.EncoderFallback.GetType()), "bytes");
        }

        private static Exception GetValidationFailedException(char[] chars, int index, int count)
        {
            if (chars == null)
                return ThrowHelper.GetArgumentNullException(ExceptionArgument.chars, ExceptionResource.ArgumentNull_Array);
            if (index < 0)
                return ThrowHelper.GetArgumentOutOfRangeException(ExceptionArgument.charIndex, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                return ThrowHelper.GetArgumentOutOfRangeException(ExceptionArgument.charCount, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            Debug.Assert(chars.Length - index < count);
            return ThrowHelper.GetArgumentOutOfRangeException(ExceptionArgument.chars, ExceptionResource.ArgumentOutOfRange_IndexCountBuffer);
        }

        private static Exception GetValidationFailedException(char[] chars, int charIndex, int charCount, byte[] bytes)
        {
            if (chars == null)
                return ThrowHelper.GetArgumentNullException(ExceptionArgument.chars, ExceptionResource.ArgumentNull_Array);
            if (bytes == null)
                return ThrowHelper.GetArgumentNullException(ExceptionArgument.bytes, ExceptionResource.ArgumentNull_Array);
            if (charIndex < 0)
                return ThrowHelper.GetArgumentOutOfRangeException(ExceptionArgument.charIndex, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            if (charCount < 0)
                return ThrowHelper.GetArgumentOutOfRangeException(ExceptionArgument.charCount, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            if (chars.Length - charIndex < charCount)
                return ThrowHelper.GetArgumentOutOfRangeException(ExceptionArgument.chars, ExceptionResource.ArgumentOutOfRange_IndexCountBuffer);
            //if (byteIndex < 0 || byteIndex > bytes.Length)
            return ThrowHelper.GetArgumentOutOfRangeException(ExceptionArgument.byteIndex, ExceptionResource.ArgumentOutOfRange_Index);
        }

        private static Exception GetValidationFailedException(string s, int index, int count)
        {
            if (s == null)
                return ThrowHelper.GetArgumentNullException(ExceptionArgument.s, ExceptionResource.ArgumentNull_String);
            if (index < 0)
                return ThrowHelper.GetArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_Index);
            if (count < 0)
                return ThrowHelper.GetArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            Debug.Assert(index > s.Length - count);
            return ThrowHelper.GetArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_IndexCountBuffer);
        }
        
        private static unsafe Exception GetValidationFailedException(char* chars, int charCount, byte* bytes)
        {
            if (bytes == null)
                return ThrowHelper.GetArgumentNullException(ExceptionArgument.bytes, ExceptionResource.ArgumentNull_Array);
            if (chars == null)
                return ThrowHelper.GetArgumentNullException(ExceptionArgument.chars, ExceptionResource.ArgumentNull_Array);
            if (charCount < 0)
                return ThrowHelper.GetArgumentOutOfRangeException(ExceptionArgument.charCount, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            // (byteCount < 0)
            return ThrowHelper.GetArgumentOutOfRangeException(ExceptionArgument.byteCount, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
        }

        private static Exception GetValidationFailedException(string s, int charIndex, int charCount, byte[] bytes)
        {
            if (s == null)
                return ThrowHelper.GetArgumentNullException(ExceptionArgument.s, ExceptionResource.ArgumentNull_String);
            if (bytes == null)
                return ThrowHelper.GetArgumentNullException(ExceptionArgument.bytes, ExceptionResource.ArgumentNull_Array);
            if (charIndex < 0)
                return ThrowHelper.GetArgumentOutOfRangeException(ExceptionArgument.charIndex, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            if (charCount < 0)
                return ThrowHelper.GetArgumentOutOfRangeException(ExceptionArgument.charCount, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            if (s.Length - charIndex < charCount)
                return ThrowHelper.GetArgumentOutOfRangeException(ExceptionArgument.chars, ExceptionResource.ArgumentOutOfRange_IndexCountBuffer);
            // (byteIndex < 0 || byteIndex > bytes.Length)
            return ThrowHelper.GetArgumentOutOfRangeException(ExceptionArgument.byteIndex, ExceptionResource.ArgumentOutOfRange_Index);
        }
    }
}
