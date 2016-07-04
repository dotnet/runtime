// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text
{
    
    using System;
    using System.Diagnostics.Contracts;
    using System.Collections;
    using System.Runtime.Remoting;
    using System.Globalization;
    using System.Threading;
    using Win32Native = Microsoft.Win32.Win32Native;
    
    // This class overrides Encoding with the things we need for our NLS Encodings
    //
    // All of the GetBytes/Chars GetByte/CharCount methods are just wrappers for the pointer
    // plus decoder/encoder method that is our real workhorse.  Note that this is an internal
    // class, so our public classes cannot derive from this class.  Because of this, all of the
    // GetBytes/Chars GetByte/CharCount wrapper methods are duplicated in all of our public
    // encodings, which currently include:
    //
    //      EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, & UnicodeEncoding
    //
    // So if you change the wrappers in this class, you must change the wrappers in the other classes
    // as well because they should have the same behavior.
    //
    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    internal abstract class EncodingNLS : Encoding
    {    
        protected EncodingNLS(int codePage) : base(codePage)
        {
        }

        // Returns the number of bytes required to encode a range of characters in
        // a character array.
        // 
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe
        [System.Security.SecuritySafeCritical] // overrides public transparent member
        public override int GetByteCount(char[] chars, int index, int count)
        {
            return EncodingForwarder.GetByteCount(this, chars, index, count);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe
        [System.Security.SecuritySafeCritical] // overrides public transparent member
        public override int GetByteCount(String s)
        {
            return EncodingForwarder.GetByteCount(this, s);
        }       

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        [System.Security.SecurityCritical]  // auto-generated
        public override unsafe int GetByteCount(char* chars, int count)
        {
            return EncodingForwarder.GetByteCount(this, chars, count);
        }

        // Parent method is safe.
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        [System.Security.SecuritySafeCritical] // overrides public transparent member
        public override int GetBytes(String s, int charIndex, int charCount,
                                              byte[] bytes, int byteIndex)
        {
            return EncodingForwarder.GetBytes(this, s, charIndex, charCount, bytes, byteIndex);
        }
    
        // Encodes a range of characters in a character array into a range of bytes
        // in a byte array. An exception occurs if the byte array is not large
        // enough to hold the complete encoding of the characters. The
        // GetByteCount method can be used to determine the exact number of
        // bytes that will be produced for a given range of characters.
        // Alternatively, the GetMaxByteCount method can be used to
        // determine the maximum number of bytes that will be produced for a given
        // number of characters, regardless of the actual character values.
        // 
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe
        [System.Security.SecuritySafeCritical] // overrides public transparent member
        public override int GetBytes(char[] chars, int charIndex, int charCount,
                                               byte[] bytes, int byteIndex)
        {
            return EncodingForwarder.GetBytes(this, chars, charIndex, charCount, bytes, byteIndex);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        [System.Security.SecurityCritical]  // auto-generated
        public override unsafe int GetBytes(char* chars, int charCount, byte* bytes, int byteCount)
        {
            return EncodingForwarder.GetBytes(this, chars, charCount, bytes, byteCount);
        }                                              

        // Returns the number of characters produced by decoding a range of bytes
        // in a byte array.
        // 
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe
        [System.Security.SecuritySafeCritical] // overrides public transparent member
        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return EncodingForwarder.GetCharCount(this, bytes, index, count);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        [System.Security.SecurityCritical]  // auto-generated
        public override unsafe int GetCharCount(byte* bytes, int count)
        {
            return EncodingForwarder.GetCharCount(this, bytes, count);
        }        

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe
        [System.Security.SecuritySafeCritical] // overrides public transparent member
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount,
                                              char[] chars, int charIndex)
        {
            return EncodingForwarder.GetChars(this, bytes, byteIndex, byteCount, chars, charIndex);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        [System.Security.SecurityCritical]  // auto-generated
        public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount)
        {
            return EncodingForwarder.GetChars(this, bytes, byteCount, chars, charCount);
        }
    
        // Returns a string containing the decoded representation of a range of
        // bytes in a byte array.
        // 
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe
        [System.Security.SecuritySafeCritical] // overrides public transparent member
        public override String GetString(byte[] bytes, int index, int count)
        {
            return EncodingForwarder.GetString(this, bytes, index, count);
        }

        public override Decoder GetDecoder()
        {
            return new DecoderNLS(this);
        }

        public override Encoder GetEncoder()
        {
            return new EncoderNLS(this);
        }
    }
}
