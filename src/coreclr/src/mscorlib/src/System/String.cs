// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: Your favorite String class.  Native methods 
** are implemented in StringNative.cpp
**
**
===========================================================*/
namespace System {
    using System.Text;
    using System;
    using System.Runtime;
    using System.Runtime.ConstrainedExecution;
    using System.Globalization;
    using System.Threading;
    using System.Collections;
    using System.Collections.Generic;    
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;    
    using System.Runtime.Versioning;
    using Microsoft.Win32;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Security;

    //
    // For Information on these methods, please see COMString.cpp
    //
    // The String class represents a static string of characters.  Many of
    // the String methods perform some type of transformation on the current
    // instance and return the result as a new String. All comparison methods are
    // implemented as a part of String.  As with arrays, character positions
    // (indices) are zero-based.
    
    [ComVisible(true)]
    [Serializable]
    public sealed partial class String : IComparable, ICloneable, IConvertible, IEnumerable
        , IComparable<String>, IEnumerable<char>, IEquatable<String>
    {
        
        //
        //NOTE NOTE NOTE NOTE
        //These fields map directly onto the fields in an EE StringObject.  See object.h for the layout.
        //
        [NonSerialized] private int m_stringLength;

        // For empty strings, this will be '\0' since
        // strings are both null-terminated and length prefixed
        [NonSerialized] private char m_firstChar;
    
        // The Empty constant holds the empty string value. It is initialized by the EE during startup.
        // It is treated as intrinsic by the JIT as so the static constructor would never run.
        // Leaving it uninitialized would confuse debuggers.
        //
        //We need to call the String constructor so that the compiler doesn't mark this as a literal.
        //Marking this as a literal would mean that it doesn't show up as a field which we can access 
        //from native.
        public static readonly String Empty;

        internal char FirstChar { get { return m_firstChar; } }
        //
        // This is a helper method for the security team.  They need to uppercase some strings (guaranteed to be less 
        // than 0x80) before security is fully initialized.  Without security initialized, we can't grab resources (the nlp's)
        // from the assembly.  This provides a workaround for that problem and should NOT be used anywhere else.
        //
        internal unsafe static string SmallCharToUpper(string strIn) {
            Contract.Requires(strIn != null);
            Contract.EndContractBlock();
            //
            // Get the length and pointers to each of the buffers.  Walk the length
            // of the string and copy the characters from the inBuffer to the outBuffer,
            // capitalizing it if necessary.  We assert that all of our characters are
            // less than 0x80.
            //
            int length = strIn.Length;
            String strOut = FastAllocateString(length);
            fixed (char * inBuff = &strIn.m_firstChar, outBuff = &strOut.m_firstChar) {

                for(int i = 0; i < length; i++) {
                    int c = inBuff[i];
                    Debug.Assert(c <= 0x7F, "string has to be ASCII");

                    // uppercase - notice that we need just one compare
                    if ((uint)(c - 'a') <= (uint)('z' - 'a')) c -= 0x20;

                    outBuff[i] = (char)c;
                }

                Debug.Assert(outBuff[length]=='\0', "outBuff[length]=='\0'");
            }
            return strOut;
        }
    
        // Gets the character at a specified position.
        //
        // Spec#: Apply the precondition here using a contract assembly.  Potential perf issue.
        [System.Runtime.CompilerServices.IndexerName("Chars")]
        public extern char this[int index] {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        // Converts a substring of this string to an array of characters.  Copies the
        // characters of this string beginning at position sourceIndex and ending at
        // sourceIndex + count - 1 to the character array buffer, beginning
        // at destinationIndex.
        //
        unsafe public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));
            if (sourceIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (count > Length - sourceIndex)
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), Environment.GetResourceString("ArgumentOutOfRange_IndexCount"));
            if (destinationIndex > destination.Length - count || destinationIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(destinationIndex), Environment.GetResourceString("ArgumentOutOfRange_IndexCount"));
            Contract.EndContractBlock();

            // Note: fixed does not like empty arrays
            if (count > 0)
            {
                fixed (char* src = &this.m_firstChar)
                    fixed (char* dest = destination)
                        wstrcpy(dest + destinationIndex, src + sourceIndex, count);
            }
        }
        
        // Returns the entire string as an array of characters.
        unsafe public char[] ToCharArray() {
            int length = Length;
            if (length > 0)
            {
                char[] chars = new char[length];
                fixed (char* src = &this.m_firstChar) fixed (char* dest = chars)
                {
                    wstrcpy(dest, src, length);
                }
                return chars;
            }

            return Array.Empty<char>();
        }
    
        // Returns a substring of this string as an array of characters.
        //
        unsafe public char[] ToCharArray(int startIndex, int length)
        {
            // Range check everything.
            if (startIndex < 0 || startIndex > Length || startIndex > Length - length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), Environment.GetResourceString("ArgumentOutOfRange_Index"));
            Contract.EndContractBlock();

            if (length > 0)
            {
                char[] chars = new char[length];
                fixed (char* src = &this.m_firstChar) fixed (char* dest = chars)
                {
                    wstrcpy(dest, src + startIndex, length);
                }
                return chars;
            }

            return Array.Empty<char>();
        }

        [Pure]
        public static bool IsNullOrEmpty(String value) {
            return (value == null || value.Length == 0);
        }

        [Pure]
        public static bool IsNullOrWhiteSpace(String value) {
            if (value == null) return true;

            for(int i = 0; i < value.Length; i++) {
                if(!Char.IsWhiteSpace(value[i])) return false;
            }

            return true;
        }

        // Gets the length of this string
        //
        /// This is a EE implemented function so that the JIT can recognise is specially
        /// and eliminate checks on character fetchs in a loop like:
        ///        for(int i = 0; i < str.Length; i++) str[i]
        /// The actually code generated for this will be one instruction and will be inlined.
        //
        // Spec#: Add postcondition in a contract assembly.  Potential perf problem.
        public extern int Length {
            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            get;
        }
    
        // Creates a new string with the characters copied in from ptr. If
        // ptr is null, a 0-length string (like String.Empty) is returned.
        //
        [CLSCompliant(false), MethodImplAttribute(MethodImplOptions.InternalCall)]
        unsafe public extern String(char *value);
        [CLSCompliant(false), MethodImplAttribute(MethodImplOptions.InternalCall)]
        unsafe public extern String(char *value, int startIndex, int length);
    
        [CLSCompliant(false), MethodImplAttribute(MethodImplOptions.InternalCall)]
        unsafe public extern String(sbyte *value);
        [CLSCompliant(false), MethodImplAttribute(MethodImplOptions.InternalCall)]
        unsafe public extern String(sbyte *value, int startIndex, int length);

        [CLSCompliant(false), MethodImplAttribute(MethodImplOptions.InternalCall)]
        unsafe public extern String(sbyte *value, int startIndex, int length, Encoding enc);
        
        unsafe static private String CreateString(sbyte *value, int startIndex, int length, Encoding enc) {            
            if (enc == null)
                return new String(value, startIndex, length); // default to ANSI

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            if ((value + startIndex) < value) {
                // overflow check
                throw new ArgumentOutOfRangeException(nameof(startIndex), Environment.GetResourceString("ArgumentOutOfRange_PartialWCHAR"));
            }

            byte [] b = new byte[length];

            try {
                Buffer.Memcpy(b, 0, (byte*)value, startIndex, length);
            }
            catch(NullReferenceException) {
                // If we got a NullReferencException. It means the pointer or 
                // the index is out of range
                throw new ArgumentOutOfRangeException(nameof(value), 
                        Environment.GetResourceString("ArgumentOutOfRange_PartialWCHAR"));                
            }

            return enc.GetString(b);
        }
        
        // Helper for encodings so they can talk to our buffer directly
        // stringLength must be the exact size we'll expect
        unsafe static internal String CreateStringFromEncoding(
            byte* bytes, int byteLength, Encoding encoding)
        {
            Contract.Requires(bytes != null);
            Contract.Requires(byteLength >= 0);

            // Get our string length
            int stringLength = encoding.GetCharCount(bytes, byteLength, null);
            Debug.Assert(stringLength >= 0, "stringLength >= 0");
            
            // They gave us an empty string if they needed one
            // 0 bytelength might be possible if there's something in an encoder
            if (stringLength == 0)
                return String.Empty;
            
            String s = FastAllocateString(stringLength);
            fixed(char* pTempChars = &s.m_firstChar)
            {
                int doubleCheck = encoding.GetChars(bytes, byteLength, pTempChars, stringLength, null);
                Debug.Assert(stringLength == doubleCheck, 
                    "Expected encoding.GetChars to return same length as encoding.GetCharCount");
            }

            return s;
        }

        // This is only intended to be used by char.ToString.
        // It is necessary to put the code in this class instead of Char, since m_firstChar is a private member.
        // Making m_firstChar internal would be dangerous since it would make it much easier to break String's immutability.
        internal static string CreateFromChar(char c)
        {
            string result = FastAllocateString(1);
            result.m_firstChar = c;
            return result;
        }
                
        unsafe internal int GetBytesFromEncoding(byte* pbNativeBuffer, int cbNativeBuffer,Encoding encoding)
        {
            // encoding == Encoding.UTF8
            fixed (char* pwzChar = &this.m_firstChar)
            {
                return encoding.GetBytes(pwzChar, m_stringLength, pbNativeBuffer, cbNativeBuffer);
            }            
        }

        unsafe internal int ConvertToAnsi(byte *pbNativeBuffer, int cbNativeBuffer, bool fBestFit, bool fThrowOnUnmappableChar)
        {
            Debug.Assert(cbNativeBuffer >= (Length + 1) * Marshal.SystemMaxDBCSCharSize, "Insufficient buffer length passed to ConvertToAnsi");

            const uint CP_ACP = 0;
            int nb;

            const uint WC_NO_BEST_FIT_CHARS = 0x00000400;

            uint flgs = (fBestFit ? 0 : WC_NO_BEST_FIT_CHARS);
            uint DefaultCharUsed = 0;

            fixed (char* pwzChar = &this.m_firstChar)
            {
                nb = Win32Native.WideCharToMultiByte(
                    CP_ACP,
                    flgs,
                    pwzChar,
                    this.Length,
                    pbNativeBuffer,
                    cbNativeBuffer,
                    IntPtr.Zero,
                    (fThrowOnUnmappableChar ? new IntPtr(&DefaultCharUsed) : IntPtr.Zero));
            }

            if (0 != DefaultCharUsed)
            {
                throw new ArgumentException(Environment.GetResourceString("Interop_Marshal_Unmappable_Char"));
            }

            pbNativeBuffer[nb] = 0;
            return nb;
        }

        // Normalization Methods
        // These just wrap calls to Normalization class
        public bool IsNormalized()
        {
#if !FEATURE_NORM_IDNA_ONLY
            // Default to Form C
            return IsNormalized(NormalizationForm.FormC);
#else
            // Default to Form IDNA
            return IsNormalized((NormalizationForm)ExtendedNormalizationForms.FormIdna);
#endif
        }

        public bool IsNormalized(NormalizationForm normalizationForm)
        {
#if !FEATURE_NORM_IDNA_ONLY
            if (this.IsFastSort())
            {
                // If its FastSort && one of the 4 main forms, then its already normalized
                if( normalizationForm == NormalizationForm.FormC ||
                    normalizationForm == NormalizationForm.FormKC ||
                    normalizationForm == NormalizationForm.FormD ||
                    normalizationForm == NormalizationForm.FormKD )
                    return true;
            }            
#endif // !FEATURE_NORM_IDNA_ONLY            
            return Normalization.IsNormalized(this, normalizationForm);
        }

        public String Normalize()
        {
#if !FEATURE_NORM_IDNA_ONLY
            // Default to Form C
            return Normalize(NormalizationForm.FormC);
#else
            // Default to Form IDNA
            return Normalize((NormalizationForm)ExtendedNormalizationForms.FormIdna);
#endif
        }

        public String Normalize(NormalizationForm normalizationForm)
        {
#if !FEATURE_NORM_IDNA_ONLY        
            if (this.IsAscii())
            {
                // If its FastSort && one of the 4 main forms, then its already normalized
                if( normalizationForm == NormalizationForm.FormC ||
                    normalizationForm == NormalizationForm.FormKC ||
                    normalizationForm == NormalizationForm.FormD ||
                    normalizationForm == NormalizationForm.FormKD )
                    return this;
            }
#endif // !FEATURE_NORM_IDNA_ONLY            
            return Normalization.Normalize(this, normalizationForm);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static String FastAllocateString(int length);

        // Creates a new string from the characters in a subarray.  The new string will
        // be created from the characters in value between startIndex and
        // startIndex + length - 1.
        //
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern String(char [] value, int startIndex, int length);
    
        // Creates a new string from the characters in a subarray.  The new string will be
        // created from the characters in value.
        //
        
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern String(char [] value);

        internal static unsafe void wstrcpy(char *dmem, char *smem, int charCount)
        {
            Buffer.Memcpy((byte*)dmem, (byte*)smem, charCount * 2); // 2 used everywhere instead of sizeof(char)
        }

        private String CtorCharArray(char [] value)
        {
            if (value != null && value.Length != 0) {
                String result = FastAllocateString(value.Length);

                unsafe {
                    fixed (char* dest = &result.m_firstChar, source = value) {
                        wstrcpy(dest, source, value.Length);
                    }
                }
                return result;
            }
            else
                return String.Empty;
        }

        private String CtorCharArrayStartLength(char [] value, int startIndex, int length)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), Environment.GetResourceString("ArgumentOutOfRange_NegativeLength"));

            if (startIndex > value.Length - length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), Environment.GetResourceString("ArgumentOutOfRange_Index"));
            Contract.EndContractBlock();

            if (length > 0) {
                String result = FastAllocateString(length);

                unsafe {
                    fixed (char* dest = &result.m_firstChar, source = value) {
                        wstrcpy(dest, source + startIndex, length);
                    }
                }
                return result;
            }
            else
                return String.Empty;
        }

        private String CtorCharCount(char c, int count)
        {
            if (count > 0) {
                String result = FastAllocateString(count);
                if (c != 0)
                {
                    unsafe {
                        fixed (char* dest = &result.m_firstChar) {
                            char *dmem = dest;
                            while (((uint)dmem & 3) != 0 && count > 0) {
                                *dmem++ = c;
                                count--;
                            }
                            uint cc = (uint)((c << 16) | c);
                            if (count >= 4) {
                                count -= 4;
                                do{
                                    ((uint *)dmem)[0] = cc;
                                    ((uint *)dmem)[1] = cc;
                                    dmem += 4;
                                    count -= 4;
                                } while (count >= 0);
                            }
                            if ((count & 2) != 0) {
                                ((uint *)dmem)[0] = cc;
                                dmem += 2;
                            }
                            if ((count & 1) != 0)
                                dmem[0] = c;
                        }
                    }
                }
                return result;
            }
            else if (count == 0)
                return String.Empty;
            else
                throw new ArgumentOutOfRangeException(nameof(count), Environment.GetResourceString("ArgumentOutOfRange_MustBeNonNegNum", nameof(count)));
        }

        private static unsafe int wcslen(char *ptr)
        {
            char *end = ptr;
            
            // First make sure our pointer is aligned on a word boundary
            int alignment = IntPtr.Size - 1;

            // If ptr is at an odd address (e.g. 0x5), this loop will simply iterate all the way
            while (((uint)end & (uint)alignment) != 0)
            {
                if (*end == 0) goto FoundZero;
                end++;
            }

#if !BIT64
            // The following code is (somewhat surprisingly!) significantly faster than a naive loop,
            // at least on x86 and the current jit.

            // The loop condition below works because if "end[0] & end[1]" is non-zero, that means
            // neither operand can have been zero. If is zero, we have to look at the operands individually,
            // but we hope this going to fairly rare.

            // In general, it would be incorrect to access end[1] if we haven't made sure
            // end[0] is non-zero. However, we know the ptr has been aligned by the loop above
            // so end[0] and end[1] must be in the same word (and therefore page), so they're either both accessible, or both not.

            while ((end[0] & end[1]) != 0 || (end[0] != 0 && end[1] != 0)) {
                end += 2;
            }

            Debug.Assert(end[0] == 0 || end[1] == 0);
            if (end[0] != 0) end++;
#else // !BIT64
            // Based on https://graphics.stanford.edu/~seander/bithacks.html#ZeroInWord

            // 64-bit implementation: process 1 ulong (word) at a time

            // What we do here is add 0x7fff from each of the
            // 4 individual chars within the ulong, using MagicMask.
            // If the char > 0 and < 0x8001, it will have its high bit set.
            // We then OR with MagicMask, to set all the other bits.
            // This will result in all bits set (ulong.MaxValue) for any
            // char that fits the above criteria, and something else otherwise.

            // Note that for any char > 0x8000, this will be a false
            // positive and we will fallback to the slow path and
            // check each char individually. This is OK though, since
            // we optimize for the common case (ASCII chars, which are < 0x80).

            // NOTE: We can access a ulong a time since the ptr is aligned,
            // and therefore we're only accessing the same word/page. (See notes
            // for the 32-bit version above.)
            
            const ulong MagicMask = 0x7fff7fff7fff7fff;

            while (true)
            {
                ulong word = *(ulong*)end;
                word += MagicMask; // cause high bit to be set if not zero, and <= 0x8000
                word |= MagicMask; // set everything besides the high bits

                if (word == ulong.MaxValue) // 0xffff...
                {
                    // all of the chars have their bits set (and therefore none can be 0)
                    end += 4;
                    continue;
                }

                // at least one of them didn't have their high bit set!
                // go through each char and check for 0.

                if (end[0] == 0) goto EndAt0;
                if (end[1] == 0) goto EndAt1;
                if (end[2] == 0) goto EndAt2;
                if (end[3] == 0) goto EndAt3;

                // if we reached here, it was a false positive-- just continue
                end += 4;
            }

            EndAt3: end++;
            EndAt2: end++;
            EndAt1: end++;
            EndAt0:
#endif // !BIT64

            FoundZero:
            Debug.Assert(*end == 0);

            int count = (int)(end - ptr);

            return count;
        }

        private unsafe String CtorCharPtr(char *ptr)
        {
            if (ptr == null)
                return String.Empty;

#if !FEATURE_PAL
            if (ptr < (char*)64000)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeStringPtrNotAtom"));
#endif // FEATURE_PAL

            Debug.Assert(this == null, "this == null");        // this is the string constructor, we allocate it

            try {
                int count = wcslen(ptr);
                if (count == 0)
                    return String.Empty;

                String result = FastAllocateString(count);
                fixed (char* dest = &result.m_firstChar)
                    wstrcpy(dest, ptr, count);
                return result;
            }
            catch (NullReferenceException) {
                throw new ArgumentOutOfRangeException(nameof(ptr), Environment.GetResourceString("ArgumentOutOfRange_PartialWCHAR"));
            }
        }

        private unsafe String CtorCharPtrStartLength(char *ptr, int startIndex, int length)
        {
            if (length < 0) {
                throw new ArgumentOutOfRangeException(nameof(length), Environment.GetResourceString("ArgumentOutOfRange_NegativeLength"));
            }

            if (startIndex < 0) {
                throw new ArgumentOutOfRangeException(nameof(startIndex), Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            }
            Contract.EndContractBlock();
            Debug.Assert(this == null, "this == null");        // this is the string constructor, we allocate it

            char *pFrom = ptr + startIndex;
            if (pFrom < ptr) {
                // This means that the pointer operation has had an overflow
                throw new ArgumentOutOfRangeException(nameof(startIndex), Environment.GetResourceString("ArgumentOutOfRange_PartialWCHAR"));
            }

            if (length == 0)
                return String.Empty;

            String result = FastAllocateString(length);

            try {
                fixed (char* dest = &result.m_firstChar)
                    wstrcpy(dest, pFrom, length);
                return result;
            }
            catch (NullReferenceException) {
                throw new ArgumentOutOfRangeException(nameof(ptr), Environment.GetResourceString("ArgumentOutOfRange_PartialWCHAR"));
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern String(char c, int count);

   
        // Returns this string.
        public override String ToString() {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return this;
        }

        public String ToString(IFormatProvider provider) {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return this;
        }
    
        // Method required for the ICloneable interface.
        // There's no point in cloning a string since they're immutable, so we simply return this.
        public Object Clone() {
            Contract.Ensures(Contract.Result<Object>() != null);
            Contract.EndContractBlock();
            return this;
        }
    
        unsafe public static String Copy (String str) {
            if (str==null) {
                throw new ArgumentNullException(nameof(str));
            }
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            int length = str.Length;

            String result = FastAllocateString(length);

            fixed(char* dest = &result.m_firstChar)
                fixed(char* src = &str.m_firstChar) {
                     wstrcpy(dest, src, length);
                }
             return result;
        }
        
        public static String Intern(String str) {
            if (str==null) {
                throw new ArgumentNullException(nameof(str));
            }
            Contract.Ensures(Contract.Result<String>().Length == str.Length);
            Contract.Ensures(str.Equals(Contract.Result<String>()));
            Contract.EndContractBlock();

            return Thread.GetDomain().GetOrInternString(str);
        }

        [Pure]
        public static String IsInterned(String str) {
            if (str==null) {
                throw new ArgumentNullException(nameof(str));
            }
            Contract.Ensures(Contract.Result<String>() == null || Contract.Result<String>().Length == str.Length);
            Contract.EndContractBlock();

            return Thread.GetDomain().IsStringInterned(str);
        }


        //
        // IConvertible implementation
        // 
      
        public TypeCode GetTypeCode() {
            return TypeCode.String;
        }

        /// <internalonly/>
        bool IConvertible.ToBoolean(IFormatProvider provider) {
            return Convert.ToBoolean(this, provider);
        }

        /// <internalonly/>
        char IConvertible.ToChar(IFormatProvider provider) {
            return Convert.ToChar(this, provider);
        }

        /// <internalonly/>
        sbyte IConvertible.ToSByte(IFormatProvider provider) {
            return Convert.ToSByte(this, provider);
        }

        /// <internalonly/>
        byte IConvertible.ToByte(IFormatProvider provider) {
            return Convert.ToByte(this, provider);
        }

        /// <internalonly/>
        short IConvertible.ToInt16(IFormatProvider provider) {
            return Convert.ToInt16(this, provider);
        }

        /// <internalonly/>
        ushort IConvertible.ToUInt16(IFormatProvider provider) {
            return Convert.ToUInt16(this, provider);
        }

        /// <internalonly/>
        int IConvertible.ToInt32(IFormatProvider provider) {
            return Convert.ToInt32(this, provider);
        }

        /// <internalonly/>
        uint IConvertible.ToUInt32(IFormatProvider provider) {
            return Convert.ToUInt32(this, provider);
        }

        /// <internalonly/>
        long IConvertible.ToInt64(IFormatProvider provider) {
            return Convert.ToInt64(this, provider);
        }

        /// <internalonly/>
        ulong IConvertible.ToUInt64(IFormatProvider provider) {
            return Convert.ToUInt64(this, provider);
        }

        /// <internalonly/>
        float IConvertible.ToSingle(IFormatProvider provider) {
            return Convert.ToSingle(this, provider);
        }

        /// <internalonly/>
        double IConvertible.ToDouble(IFormatProvider provider) {
            return Convert.ToDouble(this, provider);
        }

        /// <internalonly/>
        Decimal IConvertible.ToDecimal(IFormatProvider provider) {
            return Convert.ToDecimal(this, provider);
        }

        /// <internalonly/>
        DateTime IConvertible.ToDateTime(IFormatProvider provider) {
            return Convert.ToDateTime(this, provider);
        }

        /// <internalonly/>
        Object IConvertible.ToType(Type type, IFormatProvider provider) {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }

        // Is this a string that can be compared quickly (that is it has only characters > 0x80 
        // and not a - or '
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern bool IsFastSort();
        // Is this a string that only contains characters < 0x80.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern bool IsAscii();

        // Set extra byte for odd-sized strings that came from interop as BSTR.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern void SetTrailByte(byte data);
        // Try to retrieve the extra byte - returns false if not present.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern bool TryGetTrailByte(out byte data);

        public CharEnumerator GetEnumerator() {
            Contract.Ensures(Contract.Result<CharEnumerator>() != null);
            Contract.EndContractBlock();
            BCLDebug.Perf(false, "Avoid using String's CharEnumerator until C# special cases foreach on String - use the indexed property on String instead.");
            return new CharEnumerator(this);
        }

        IEnumerator<char> IEnumerable<char>.GetEnumerator() {
            Contract.Ensures(Contract.Result<IEnumerator<char>>() != null);
            Contract.EndContractBlock();
            BCLDebug.Perf(false, "Avoid using String's CharEnumerator until C# special cases foreach on String - use the indexed property on String instead.");
            return new CharEnumerator(this);
        }

        /// <internalonly/>
        IEnumerator IEnumerable.GetEnumerator() {
            Contract.Ensures(Contract.Result<IEnumerator>() != null);
            Contract.EndContractBlock();
            BCLDebug.Perf(false, "Avoid using String's CharEnumerator until C# special cases foreach on String - use the indexed property on String instead.");
            return new CharEnumerator(this);
        }

         // Copies the source String (byte buffer) to the destination IntPtr memory allocated with len bytes.
        internal unsafe static void InternalCopy(String src, IntPtr dest,int len)
        {
            if (len == 0)
                return;
            fixed(char* charPtr = &src.m_firstChar) {
                byte* srcPtr = (byte*) charPtr;
                byte* dstPtr = (byte*) dest;
                Buffer.Memcpy(dstPtr, srcPtr, len);
            }
        }

        internal ref char GetFirstCharRef() {
            return ref m_firstChar;
        }
    }
}
