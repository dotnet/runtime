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
    public sealed class String : IComparable, ICloneable, IConvertible, IEnumerable
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

        private const int TrimHead = 0;
        private const int TrimTail = 1;
        private const int TrimBoth = 2;
    
        // The Empty constant holds the empty string value. It is initialized by the EE during startup.
        // It is treated as intrinsic by the JIT as so the static constructor would never run.
        // Leaving it uninitialized would confuse debuggers.
        //
        //We need to call the String constructor so that the compiler doesn't mark this as a literal.
        //Marking this as a literal would mean that it doesn't show up as a field which we can access 
        //from native.
        public static readonly String Empty;

        //
        //Native Static Methods
        //
    
        // Joins an array of strings together as one string with a separator between each original string.
        //
        public static String Join(String separator, params String[] value) {
            if (value==null)
                throw new ArgumentNullException("value");
            Contract.EndContractBlock();
            return Join(separator, value, 0, value.Length);
        }

        [ComVisible(false)]
        public static string Join(string separator, params object[] values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            Contract.EndContractBlock();

            if (values.Length == 0 || values[0] == null)
                return string.Empty;

            string firstString = values[0].ToString();

            if (values.Length == 1)
            {
                return firstString ?? string.Empty;
            }

            StringBuilder result = StringBuilderCache.Acquire();
            result.Append(firstString);

            for (int i = 1; i < values.Length; i++)
            {
                result.Append(separator);
                object value = values[i];
                if (value != null)
                {
                    result.Append(value.ToString());
                }
            }

            return StringBuilderCache.GetStringAndRelease(result);
        }

        [ComVisible(false)]
        public static String Join<T>(String separator, IEnumerable<T> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            using (IEnumerator<T> en = values.GetEnumerator())
            {
                if (!en.MoveNext())
                    return string.Empty;
                
                // We called MoveNext once, so this will be the first item
                T currentValue = en.Current;

                // Call ToString before calling MoveNext again, since
                // we want to stay consistent with the below loop
                // Everything should be called in the order
                // MoveNext-Current-ToString, unless further optimizations
                // can be made, to avoid breaking changes
                string firstString = currentValue?.ToString();

                // If there's only 1 item, simply call ToString on that
                if (!en.MoveNext())
                {
                    // We have to handle the case of either currentValue
                    // or its ToString being null
                    return firstString ?? string.Empty;
                }

                StringBuilder result = StringBuilderCache.Acquire();
                
                result.Append(firstString);

                do
                {
                    currentValue = en.Current;

                    result.Append(separator);
                    if (currentValue != null)
                    {
                        result.Append(currentValue.ToString());
                    }
                }
                while (en.MoveNext());

                return StringBuilderCache.GetStringAndRelease(result);
            }
        }

        [ComVisible(false)]
        public static String Join(String separator, IEnumerable<String> values) {
            if (values == null)
                throw new ArgumentNullException("values");
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            using(IEnumerator<String> en = values.GetEnumerator()) {
                if (!en.MoveNext())
                    return String.Empty;

                String firstValue = en.Current;

                if (!en.MoveNext()) {
                    // Only one value available
                    return firstValue ?? String.Empty;
                }

                // Null separator and values are handled by the StringBuilder
                StringBuilder result = StringBuilderCache.Acquire();
                result.Append(firstValue);

                do {
                    result.Append(separator);
                    result.Append(en.Current);
                } while (en.MoveNext());
                return StringBuilderCache.GetStringAndRelease(result);
            }
        }

        internal char FirstChar { get { return m_firstChar; } }

        // Joins an array of strings together as one string with a separator between each original string.
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe static String Join(String separator, String[] value, int startIndex, int count) {
            //Range check the array
            if (value == null)
                throw new ArgumentNullException("value");

            if (startIndex < 0)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));

            if (startIndex > value.Length - count)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            Contract.EndContractBlock();

            //Treat null as empty string.
            if (separator == null) {
                separator = String.Empty;
            }

            //If count is 0, that skews a whole bunch of the calculations below, so just special case that.
            if (count == 0) {
                return String.Empty;
            }

            if (count == 1) {
                return value[startIndex] ?? String.Empty;
            }

            int jointLength = 0;
            //Figure out the total length of the strings in value
            int endIndex = startIndex + count - 1;
            for (int stringToJoinIndex = startIndex; stringToJoinIndex <= endIndex; stringToJoinIndex++) {
                string currentValue = value[stringToJoinIndex];

                if (currentValue != null) {
                    jointLength += currentValue.Length;
                }
            }
            
            //Add enough room for the separator.
            jointLength += (count - 1) * separator.Length;

            // Note that we may not catch all overflows with this check (since we could have wrapped around the 4gb range any number of times
            // and landed back in the positive range.) The input array might be modifed from other threads, 
            // so we have to do an overflow check before each append below anyway. Those overflows will get caught down there.
            if ((jointLength < 0) || ((jointLength + 1) < 0) ) {
                throw new OutOfMemoryException();
            }

            //If this is an empty string, just return.
            if (jointLength == 0) {
                return String.Empty;
            }

            string jointString = FastAllocateString( jointLength );
            fixed (char * pointerToJointString = &jointString.m_firstChar) {
                UnSafeCharBuffer charBuffer = new UnSafeCharBuffer( pointerToJointString, jointLength);                
                
                // Append the first string first and then append each following string prefixed by the separator.
                charBuffer.AppendString( value[startIndex] );
                for (int stringToJoinIndex = startIndex + 1; stringToJoinIndex <= endIndex; stringToJoinIndex++) {
                    charBuffer.AppendString( separator );
                    charBuffer.AppendString( value[stringToJoinIndex] );
                }
                Contract.Assert(*(pointerToJointString + charBuffer.Length) == '\0', "String must be null-terminated!");
            }

            return jointString;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        private unsafe static int CompareOrdinalIgnoreCaseHelper(String strA, String strB)
        {
            Contract.Requires(strA != null);
            Contract.Requires(strB != null);
            Contract.EndContractBlock();
            int length = Math.Min(strA.Length, strB.Length);
    
            fixed (char* ap = &strA.m_firstChar) fixed (char* bp = &strB.m_firstChar)
            {
                char* a = ap;
                char* b = bp;

                while (length != 0) 
                {
                    int charA = *a;
                    int charB = *b;

                    Contract.Assert((charA | charB) <= 0x7F, "strings have to be ASCII");

                    // uppercase both chars - notice that we need just one compare per char
                    if ((uint)(charA - 'a') <= (uint)('z' - 'a')) charA -= 0x20;
                    if ((uint)(charB - 'a') <= (uint)('z' - 'a')) charB -= 0x20;

                    //Return the (case-insensitive) difference between them.
                    if (charA != charB)
                        return charA - charB;

                    // Next char
                    a++; b++;
                    length--;
                }

                return strA.Length - strB.Length;
            }
        }

        // native call to COMString::CompareOrdinalEx
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int CompareOrdinalHelper(String strA, int indexA, int countA, String strB, int indexB, int countB);

        //This will not work in case-insensitive mode for any character greater than 0x80.  
        //We'll throw an ArgumentException.
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        unsafe internal static extern int nativeCompareOrdinalIgnoreCaseWC(String strA, sbyte *strBBytes);
        //
        // This is a helper method for the security team.  They need to uppercase some strings (guaranteed to be less 
        // than 0x80) before security is fully initialized.  Without security initialized, we can't grab resources (the nlp's)
        // from the assembly.  This provides a workaround for that problem and should NOT be used anywhere else.
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
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
                    Contract.Assert(c <= 0x7F, "string has to be ASCII");

                    // uppercase - notice that we need just one compare
                    if ((uint)(c - 'a') <= (uint)('z' - 'a')) c -= 0x20;

                    outBuff[i] = (char)c;
                }

                Contract.Assert(outBuff[length]=='\0', "outBuff[length]=='\0'");
            }
            return strOut;
        }

        //
        //
        // NATIVE INSTANCE METHODS
        //
        //
    
        //
        // Search/Query methods
        //

        [System.Security.SecuritySafeCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private unsafe static bool EqualsHelper(String strA, String strB)
        {
            Contract.Requires(strA != null);
            Contract.Requires(strB != null);
            Contract.Requires(strA.Length == strB.Length);

            int length = strA.Length;

            fixed (char* ap = &strA.m_firstChar) fixed (char* bp = &strB.m_firstChar)
            {
                char* a = ap;
                char* b = bp;

#if BIT64
                // Single int read aligns pointers for the following long reads
                // PERF: No length check needed as there is always an int32 worth of string allocated
                //       This read can also include the null terminator which both strings will have
                if (*(int*)a != *(int*)b) return false;
                length -= 2; a += 2; b += 2;

                // for AMD64 bit platform we unroll by 12 and
                // check 3 qword at a time. This is less code
                // than the 32 bit case and is a shorter path length.

                while (length >= 12)
                {
                    if (*(long*)a != *(long*)b) goto ReturnFalse;
                    if (*(long*)(a + 4) != *(long*)(b + 4)) goto ReturnFalse;
                    if (*(long*)(a + 8) != *(long*)(b + 8)) goto ReturnFalse;
                    length -= 12; a += 12; b += 12;
                }
#else
                while (length >= 10)
                {
                    if (*(int*)a != *(int*)b) goto ReturnFalse;
                    if (*(int*)(a + 2) != *(int*)(b + 2)) goto ReturnFalse;
                    if (*(int*)(a + 4) != *(int*)(b + 4)) goto ReturnFalse;
                    if (*(int*)(a + 6) != *(int*)(b + 6)) goto ReturnFalse;
                    if (*(int*)(a + 8) != *(int*)(b + 8)) goto ReturnFalse;
                    length -= 10; a += 10; b += 10;
                }
#endif

                // This depends on the fact that the String objects are
                // always zero terminated and that the terminating zero is not included
                // in the length. For odd string sizes, the last compare will include
                // the zero terminator.
                while (length > 0) 
                {
                    if (*(int*)a != *(int*)b) goto ReturnFalse;
                    length -= 2; a += 2; b += 2;
                }

                return true;

                ReturnFalse:
                return false;
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private unsafe static bool StartsWithOrdinalHelper(String str, String startsWith)
        {
            Contract.Requires(str != null);
            Contract.Requires(startsWith != null);
            Contract.Requires(str.Length >= startsWith.Length);

            int length = startsWith.Length;

            fixed (char* ap = &str.m_firstChar) fixed (char* bp = &startsWith.m_firstChar)
            {
                char* a = ap;
                char* b = bp;

#if BIT64
                // Single int read aligns pointers for the following long reads
                // No length check needed as this method is called when length >= 2
                Contract.Assert(length >= 2);
                if (*(int*)a != *(int*)b) goto ReturnFalse;
                length -= 2; a += 2; b += 2;

                while (length >= 12)
                {
                    if (*(long*)a != *(long*)b) goto ReturnFalse;
                    if (*(long*)(a + 4) != *(long*)(b + 4)) goto ReturnFalse;
                    if (*(long*)(a + 8) != *(long*)(b + 8)) goto ReturnFalse;
                    length -= 12; a += 12; b += 12;
                }
#else
                while (length >= 10)
                {
                    if (*(int*)a != *(int*)b) goto ReturnFalse;
                    if (*(int*)(a+2) != *(int*)(b+2)) goto ReturnFalse;
                    if (*(int*)(a+4) != *(int*)(b+4)) goto ReturnFalse;
                    if (*(int*)(a+6) != *(int*)(b+6)) goto ReturnFalse;
                    if (*(int*)(a+8) != *(int*)(b+8)) goto ReturnFalse;
                    length -= 10; a += 10; b += 10;
                }
#endif

                while (length >= 2)
                {
                    if (*(int*)a != *(int*)b) goto ReturnFalse;
                    length -= 2; a += 2; b += 2;
                }

                // PERF: This depends on the fact that the String objects are always zero terminated 
                // and that the terminating zero is not included in the length. For even string sizes
                // this compare can include the zero terminator. Bitwise OR avoids a branch.
                return length == 0 | *a == *b;

                ReturnFalse:
                return false;
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        private unsafe static int CompareOrdinalHelper(String strA, String strB)
        {
            Contract.Requires(strA != null);
            Contract.Requires(strB != null);

            // NOTE: This may be subject to change if eliminating the check
            // in the callers makes them small enough to be inlined by the JIT
            Contract.Assert(strA.m_firstChar == strB.m_firstChar,
                "For performance reasons, callers of this method should " +
                "check/short-circuit beforehand if the first char is the same.");

            int length = Math.Min(strA.Length, strB.Length);

            fixed (char* ap = &strA.m_firstChar) fixed (char* bp = &strB.m_firstChar)
            {
                char* a = ap;
                char* b = bp;

                // Check if the second chars are different here
                // The reason we check if m_firstChar is different is because
                // it's the most common case and allows us to avoid a method call
                // to here.
                // The reason we check if the second char is different is because
                // if the first two chars the same we can increment by 4 bytes,
                // leaving us word-aligned on both 32-bit (12 bytes into the string)
                // and 64-bit (16 bytes) platforms.
        
                // For empty strings, the second char will be null due to padding.
                // The start of the string (not including sync block pointer)
                // is the method table pointer + string length, which takes up
                // 8 bytes on 32-bit, 12 on x64. For empty strings the null
                // terminator immediately follows, leaving us with an object
                // 10/14 bytes in size. Since everything needs to be a multiple
                // of 4/8, this will get padded and zeroed out.
                
                // For one-char strings the second char will be the null terminator.

                // NOTE: If in the future there is a way to read the second char
                // without pinning the string (e.g. System.Runtime.CompilerServices.Unsafe
                // is exposed to mscorlib, or a future version of C# allows inline IL),
                // then do that and short-circuit before the fixed.

                if (*(a + 1) != *(b + 1)) goto DiffOffset1;
                
                // Since we know that the first two chars are the same,
                // we can increment by 2 here and skip 4 bytes.
                // This leaves us 8-byte aligned, which results
                // on better perf for 64-bit platforms.
                length -= 2; a += 2; b += 2;

                // unroll the loop
#if BIT64
                while (length >= 12)
                {
                    if (*(long*)a != *(long*)b) goto DiffOffset0;
                    if (*(long*)(a + 4) != *(long*)(b + 4)) goto DiffOffset4;
                    if (*(long*)(a + 8) != *(long*)(b + 8)) goto DiffOffset8;
                    length -= 12; a += 12; b += 12;
                }
#else // BIT64
                while (length >= 10)
                {
                    if (*(int*)a != *(int*)b) goto DiffOffset0;
                    if (*(int*)(a + 2) != *(int*)(b + 2)) goto DiffOffset2;
                    if (*(int*)(a + 4) != *(int*)(b + 4)) goto DiffOffset4;
                    if (*(int*)(a + 6) != *(int*)(b + 6)) goto DiffOffset6;
                    if (*(int*)(a + 8) != *(int*)(b + 8)) goto DiffOffset8;
                    length -= 10; a += 10; b += 10; 
                }
#endif // BIT64

                // Fallback loop:
                // go back to slower code path and do comparison on 4 bytes at a time.
                // This depends on the fact that the String objects are
                // always zero terminated and that the terminating zero is not included
                // in the length. For odd string sizes, the last compare will include
                // the zero terminator.
                while (length > 0)
                {
                    if (*(int*)a != *(int*)b) goto DiffNextInt;
                    length -= 2;
                    a += 2; 
                    b += 2; 
                }

                // At this point, we have compared all the characters in at least one string.
                // The longer string will be larger.
                return strA.Length - strB.Length;
                
#if BIT64
                DiffOffset8: a += 4; b += 4;
                DiffOffset4: a += 4; b += 4;
#else // BIT64
                // Use jumps instead of falling through, since
                // otherwise going to DiffOffset8 will involve
                // 8 add instructions before getting to DiffNextInt
                DiffOffset8: a += 8; b += 8; goto DiffOffset0;
                DiffOffset6: a += 6; b += 6; goto DiffOffset0;
                DiffOffset4: a += 2; b += 2;
                DiffOffset2: a += 2; b += 2;
#endif // BIT64
                
                DiffOffset0:
                // If we reached here, we already see a difference in the unrolled loop above
#if BIT64
                if (*(int*)a == *(int*)b)
                {
                    a += 2; b += 2;
                }
#endif // BIT64

                DiffNextInt:
                if (*a != *b) return *a - *b;

                DiffOffset1:
                Contract.Assert(*(a + 1) != *(b + 1), "This char must be different if we reach here!");
                return *(a + 1) - *(b + 1);
            }
        }

        // Determines whether two strings match.
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public override bool Equals(Object obj)
        {
            if (this == null)                        // this is necessary to guard against reverse-pinvokes and
                throw new NullReferenceException();  // other callers who do not use the callvirt instruction

            if (object.ReferenceEquals(this, obj))
                return true;

            string str = obj as string;
            if (str == null)
                return false;

            if (this.Length != str.Length)
                return false;

            return EqualsHelper(this, str);
        }

        // Determines whether two strings match.
        [Pure]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public bool Equals(String value)
        {
            if (this == null)                        // this is necessary to guard against reverse-pinvokes and
                throw new NullReferenceException();  // other callers who do not use the callvirt instruction

            if (object.ReferenceEquals(this, value))
                return true;

            // NOTE: No need to worry about casting to object here.
            // If either side of an == comparison between strings
            // is null, Roslyn generates a simple ceq instruction
            // instead of calling string.op_Equality.
            if (value == null)
                return false;
            
            if (this.Length != value.Length)
                return false;

            return EqualsHelper(this, value);
        }

        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        public bool Equals(String value, StringComparison comparisonType) {
            if (comparisonType < StringComparison.CurrentCulture || comparisonType > StringComparison.OrdinalIgnoreCase)
                throw new ArgumentException(Environment.GetResourceString("NotSupported_StringComparison"), "comparisonType");
            Contract.EndContractBlock();

            if ((Object)this == (Object)value) {
                return true;
            }

            if ((Object)value == null) {
                return false;
            }

            switch (comparisonType) {
                case StringComparison.CurrentCulture:
                    return (CultureInfo.CurrentCulture.CompareInfo.Compare(this, value, CompareOptions.None) == 0);

                case StringComparison.CurrentCultureIgnoreCase:
                    return (CultureInfo.CurrentCulture.CompareInfo.Compare(this, value, CompareOptions.IgnoreCase) == 0);

                case StringComparison.InvariantCulture:
                    return (CultureInfo.InvariantCulture.CompareInfo.Compare(this, value, CompareOptions.None) == 0);

                case StringComparison.InvariantCultureIgnoreCase:
                    return (CultureInfo.InvariantCulture.CompareInfo.Compare(this, value, CompareOptions.IgnoreCase) == 0);

                case StringComparison.Ordinal:
                    if (this.Length != value.Length)
                        return false;
                    return EqualsHelper(this, value);

                case StringComparison.OrdinalIgnoreCase:
                    if (this.Length != value.Length)
                        return false;

                    // If both strings are ASCII strings, we can take the fast path.
                    if (this.IsAscii() && value.IsAscii()) {
                        return (CompareOrdinalIgnoreCaseHelper(this, value) == 0);
                    }

#if FEATURE_COREFX_GLOBALIZATION
                    return (CompareInfo.CompareOrdinalIgnoreCase(this, 0, this.Length, value, 0, value.Length) == 0);
#else
                    // Take the slow path.
                    return (TextInfo.CompareOrdinalIgnoreCase(this, value) == 0);
#endif

                default:
                    throw new ArgumentException(Environment.GetResourceString("NotSupported_StringComparison"), "comparisonType");
            }
        }


        // Determines whether two Strings match.
        [Pure]
        public static bool Equals(String a, String b) {
            if ((Object)a==(Object)b) {
                return true;
            }

            if ((Object)a == null || (Object)b == null || a.Length != b.Length) {
                return false;
            }

            return EqualsHelper(a, b);
        }

        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        public static bool Equals(String a, String b, StringComparison comparisonType) {
            if (comparisonType < StringComparison.CurrentCulture || comparisonType > StringComparison.OrdinalIgnoreCase)
                throw new ArgumentException(Environment.GetResourceString("NotSupported_StringComparison"), "comparisonType");
            Contract.EndContractBlock();

            if ((Object)a==(Object)b) {
                return true;
            }
    
            if ((Object)a==null || (Object)b==null) {
                return false;
            }

            switch (comparisonType) {
                case StringComparison.CurrentCulture:
                    return (CultureInfo.CurrentCulture.CompareInfo.Compare(a, b, CompareOptions.None) == 0);

                case StringComparison.CurrentCultureIgnoreCase:
                    return (CultureInfo.CurrentCulture.CompareInfo.Compare(a, b, CompareOptions.IgnoreCase) == 0);

                case StringComparison.InvariantCulture:
                    return (CultureInfo.InvariantCulture.CompareInfo.Compare(a, b, CompareOptions.None) == 0);

                case StringComparison.InvariantCultureIgnoreCase:
                    return (CultureInfo.InvariantCulture.CompareInfo.Compare(a, b, CompareOptions.IgnoreCase) == 0);

                case StringComparison.Ordinal:
                    if (a.Length != b.Length)
                        return false;

                    return EqualsHelper(a, b);

                case StringComparison.OrdinalIgnoreCase:
                    if (a.Length != b.Length)
                        return false;
                    else {
                        // If both strings are ASCII strings, we can take the fast path.
                        if (a.IsAscii() && b.IsAscii()) {
                            return (CompareOrdinalIgnoreCaseHelper(a, b) == 0);
                        }
                        // Take the slow path.

#if FEATURE_COREFX_GLOBALIZATION
                        return (CompareInfo.CompareOrdinalIgnoreCase(a, 0, a.Length, b, 0, b.Length) == 0);
#else
                        return (TextInfo.CompareOrdinalIgnoreCase(a, b) == 0);
#endif
                    }

                default:
                    throw new ArgumentException(Environment.GetResourceString("NotSupported_StringComparison"), "comparisonType");
            }
        }

        public static bool operator == (String a, String b) {
           return String.Equals(a, b);
        }

        public static bool operator != (String a, String b) {
           return !String.Equals(a, b);
        }
    
        // Gets the character at a specified position.
        //
        // Spec#: Apply the precondition here using a contract assembly.  Potential perf issue.
        [System.Runtime.CompilerServices.IndexerName("Chars")]
        public extern char this[int index] {
            [MethodImpl(MethodImplOptions.InternalCall)]
            [System.Security.SecuritySafeCritical] // public member
            get;
        }

        // Converts a substring of this string to an array of characters.  Copies the
        // characters of this string beginning at position sourceIndex and ending at
        // sourceIndex + count - 1 to the character array buffer, beginning
        // at destinationIndex.
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
        unsafe public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            if (destination == null)
                throw new ArgumentNullException("destination");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));
            if (sourceIndex < 0)
                throw new ArgumentOutOfRangeException("sourceIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (count > Length - sourceIndex)
                throw new ArgumentOutOfRangeException("sourceIndex", Environment.GetResourceString("ArgumentOutOfRange_IndexCount"));
            if (destinationIndex > destination.Length - count || destinationIndex < 0)
                throw new ArgumentOutOfRangeException("destinationIndex", Environment.GetResourceString("ArgumentOutOfRange_IndexCount"));
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
        [System.Security.SecuritySafeCritical]  // auto-generated
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
            
#if FEATURE_CORECLR
            return Array.Empty<char>();
#else
            return new char[0];
#endif
        }
    
        // Returns a substring of this string as an array of characters.
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
        unsafe public char[] ToCharArray(int startIndex, int length)
        {
            // Range check everything.
            if (startIndex < 0 || startIndex > Length || startIndex > Length - length)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (length < 0)
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_Index"));
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
            
#if FEATURE_CORECLR
            return Array.Empty<char>();
#else
            return new char[0];
#endif
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

#if FEATURE_RANDOMIZED_STRING_HASHING
        // Do not remove!
        // This method is called by reflection in System.Xml
        [System.Security.SecurityCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int InternalMarvin32HashString(string s, int strLen, long additionalEntropy);

        [System.Security.SecuritySafeCritical]
        internal static bool UseRandomizedHashing() {
            return InternalUseRandomizedHashing();
        }

        [System.Security.SecurityCritical]
        [System.Security.SuppressUnmanagedCodeSecurity]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern bool InternalUseRandomizedHashing();
#endif

        // Gets a hash code for this string.  If strings A and B are such that A.Equals(B), then
        // they will return the same hash code.
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public override int GetHashCode()
        {
#if FEATURE_RANDOMIZED_STRING_HASHING
            if (HashHelpers.s_UseRandomizedStringHashing)
            {
                return InternalMarvin32HashString(this, this.Length, 0);
            }
#endif // FEATURE_RANDOMIZED_STRING_HASHING

            return GetLegacyNonRandomizedHashCode();
        }

        // Use this if and only if you need the hashcode to not change across app domains (e.g. you have an app domain agile
        // hash table).
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        internal int GetLegacyNonRandomizedHashCode() {
            unsafe {
                fixed (char* src = &m_firstChar) {
                    Contract.Assert(src[this.Length] == '\0', "src[this.Length] == '\\0'");
                    Contract.Assert( ((int)src)%4 == 0, "Managed string should start at 4 bytes boundary");
#if BIT64
                    int hash1 = 5381;
#else // !BIT64 (32)
                    int hash1 = (5381<<16) + 5381;
#endif
                    int hash2 = hash1;

#if BIT64
                    int     c;
                    char *s = src;
                    while ((c = s[0]) != 0) {
                        hash1 = ((hash1 << 5) + hash1) ^ c;
                        c = s[1];
                        if (c == 0)
                            break;
                        hash2 = ((hash2 << 5) + hash2) ^ c;
                        s += 2;
                    }
#else // !BIT64 (32)
                    // 32 bit machines.
                    int* pint = (int *)src;
                    int len = this.Length;
                    while (len > 2)
                    {
                        hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ pint[0];
                        hash2 = ((hash2 << 5) + hash2 + (hash2 >> 27)) ^ pint[1];
                        pint += 2;
                        len  -= 4;
                    }

                    if (len > 0)
                    {
                        hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ pint[0];
                    }
#endif
#if DEBUG
                    // We want to ensure we can change our hash function daily.
                    // This is perfectly fine as long as you don't persist the
                    // value from GetHashCode to disk or count on String A 
                    // hashing before string B.  Those are bugs in your code.
                    hash1 ^= ThisAssembly.DailyBuildNumber;
#endif
                    return hash1 + (hash2 * 1566083941);
                }
            }
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
            [System.Security.SecuritySafeCritical]  // auto-generated
            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            get;
        }

        [ComVisible(false)]
        public String[] Split(char separator) {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return SplitInternal(separator, Int32.MaxValue, StringSplitOptions.None);
        }

        [ComVisible(false)]
        public String[] Split(char separator, StringSplitOptions options) {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return SplitInternal(separator, Int32.MaxValue, options);
        }

        [ComVisible(false)]
        public String[] Split(char separator, int count, StringSplitOptions options) {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return SplitInternal(separator, count, options);
        }

        // Creates an array of strings by splitting this string at each
        // occurrence of a separator.  The separator is searched for, and if found,
        // the substring preceding the occurrence is stored as the first element in
        // the array of strings.  We then continue in this manner by searching
        // the substring that follows the occurrence.  On the other hand, if the separator
        // is not found, the array of strings will contain this instance as its only element.
        // If the separator is null
        // whitespace (i.e., Character.IsWhitespace) is used as the separator.
        //
        public String [] Split(params char [] separator) {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return SplitInternal(separator, Int32.MaxValue, StringSplitOptions.None);
        }

        // Creates an array of strings by splitting this string at each
        // occurrence of a separator.  The separator is searched for, and if found,
        // the substring preceding the occurrence is stored as the first element in
        // the array of strings.  We then continue in this manner by searching
        // the substring that follows the occurrence.  On the other hand, if the separator
        // is not found, the array of strings will contain this instance as its only element.
        // If the separator is the empty string (i.e., String.Empty), then
        // whitespace (i.e., Character.IsWhitespace) is used as the separator.
        // If there are more than count different strings, the last n-(count-1)
        // elements are concatenated and added as the last String.
        //
        public string[] Split(char[] separator, int count) {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return SplitInternal(separator, count, StringSplitOptions.None);
        }

        [ComVisible(false)]
        public String[] Split(char[] separator, StringSplitOptions options) {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return SplitInternal(separator, Int32.MaxValue, options);
        }

        [ComVisible(false)]
        public String[] Split(char[] separator, int count, StringSplitOptions options)
        {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return SplitInternal(separator, count, options);
        }

        [System.Security.SecuritySafeCritical]
        private unsafe String[] SplitInternal(char separator, int count, StringSplitOptions options)
        {
            char* pSeparators = stackalloc char[1];
            pSeparators[0] = separator;
            return SplitInternal(pSeparators, /*separatorsLength*/ 1, count, options);
        }

        [ComVisible(false)]
        [System.Security.SecuritySafeCritical]
        internal String[] SplitInternal(char[] separator, int count, StringSplitOptions options)
        {
            unsafe
            {
                fixed (char* pSeparators = separator)
                {
                    int separatorsLength = separator == null ? 0 : separator.Length;
                    return SplitInternal(pSeparators, separatorsLength, count, options);
                }
            }
        }

        [System.Security.SecurityCritical]
        private unsafe String[] SplitInternal(char* separators, int separatorsLength, int count, StringSplitOptions options)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException("count",
                    Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));

            if (options < StringSplitOptions.None || options > StringSplitOptions.RemoveEmptyEntries)
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", options));
            Contract.Ensures(Contract.Result<String[]>() != null);
            Contract.EndContractBlock();

            bool omitEmptyEntries = (options == StringSplitOptions.RemoveEmptyEntries);

            if ((count == 0) || (omitEmptyEntries && this.Length == 0)) 
            {           
#if FEATURE_CORECLR
                return EmptyArray<String>.Value;
#else
                // Keep the old behavior of returning a new empty array
                // to mitigate any potential compat risk.
                return new String[0];
#endif
            }

            if (count == 1)
            {
                return new String[] { this };
            }
            
            int[] sepList = new int[Length];            
            int numReplaces = MakeSeparatorList(separators, separatorsLength, sepList);
            
            // Handle the special case of no replaces.
            if (0 == numReplaces) {
                return new String[] { this };
            }            

            if(omitEmptyEntries) 
            {
                return InternalSplitOmitEmptyEntries(sepList, null, 1, numReplaces, count);
            }
            else 
            {
                return InternalSplitKeepEmptyEntries(sepList, null, 1, numReplaces, count);
            }            
        }

        [ComVisible(false)]
        public String[] Split(String separator) {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return SplitInternal(separator ?? String.Empty, null, Int32.MaxValue, StringSplitOptions.None);
        }

        [ComVisible(false)]
        public String[] Split(String separator, StringSplitOptions options) {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return SplitInternal(separator ?? String.Empty, null, Int32.MaxValue, options);
        }

        [ComVisible(false)]
        public String[] Split(String separator, Int32 count, StringSplitOptions options) {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return SplitInternal(separator ?? String.Empty, null, count, options);
        }

        [ComVisible(false)]
        public String [] Split(String[] separator, StringSplitOptions options) {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return SplitInternal(null, separator, Int32.MaxValue, options);
        }

        [ComVisible(false)]
        public String[] Split(String[] separator, Int32 count, StringSplitOptions options) {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return SplitInternal(null, separator, count, options);
        }

        private String[] SplitInternal(String separator, String[] separators, Int32 count, StringSplitOptions options)
        {
            if (count < 0) {
                throw new ArgumentOutOfRangeException("count",
                    Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));
            }

            if (options < StringSplitOptions.None || options > StringSplitOptions.RemoveEmptyEntries) {
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)options));
            }
            Contract.EndContractBlock();

            bool omitEmptyEntries = (options == StringSplitOptions.RemoveEmptyEntries);

            bool singleSeparator = separator != null;

            if (!singleSeparator && (separators == null || separators.Length == 0)) {
                return SplitInternal((char[]) null, count, options);
            }
            
            if ((count == 0) || (omitEmptyEntries && this.Length ==0)) {
#if FEATURE_CORECLR
                return EmptyArray<String>.Value;
#else
                // Keep the old behavior of returning a new empty array
                // to mitigate any potential compat risk.
                return new String[0];
#endif
            }

            if (count == 1) {
                return new String[] { this };
            }

            if (singleSeparator && separator.Length == 0) {
                return new[] { this };
            }

            int[] sepList = new int[Length];
            int[] lengthList;
            int defaultLength;
            int numReplaces;

            if (singleSeparator) {
                lengthList = null;
                defaultLength = separator.Length;
                numReplaces = MakeSeparatorList(separator, sepList);
            }
            else {
                lengthList = new int[Length];
                defaultLength = 0;
                numReplaces = MakeSeparatorList(separators, sepList, lengthList);
            }

            // Handle the special case of no replaces.
            if (0 == numReplaces) {
                return new String[] { this };
            }
            
            if (omitEmptyEntries) {
                return InternalSplitOmitEmptyEntries(sepList, lengthList, defaultLength, numReplaces, count);
            }
            else {
                return InternalSplitKeepEmptyEntries(sepList, lengthList, defaultLength, numReplaces, count);
            }
        }                        
        
        // Note a special case in this function:
        //     If there is no separator in the string, a string array which only contains 
        //     the original string will be returned regardless of the count. 
        //

        private String[] InternalSplitKeepEmptyEntries(Int32[] sepList, Int32[] lengthList, Int32 defaultLength, Int32 numReplaces, int count) {
            Contract.Requires(numReplaces >= 0);
            Contract.Requires(count >= 2);
            Contract.Ensures(Contract.Result<String[]>() != null);
        
            int currIndex = 0;
            int arrIndex = 0;

            count--;
            int numActualReplaces = (numReplaces < count) ? numReplaces : count;

            //Allocate space for the new array.
            //+1 for the string from the end of the last replace to the end of the String.
            String[] splitStrings = new String[numActualReplaces+1];

            for (int i = 0; i < numActualReplaces && currIndex < Length; i++) {
                splitStrings[arrIndex++] = Substring(currIndex, sepList[i]-currIndex );                            
                currIndex=sepList[i] + ((lengthList == null) ? defaultLength : lengthList[i]);
            }

            //Handle the last string at the end of the array if there is one.
            if (currIndex < Length && numActualReplaces >= 0) {
                splitStrings[arrIndex] = Substring(currIndex);
            } 
            else if (arrIndex==numActualReplaces) {
                //We had a separator character at the end of a string.  Rather than just allowing
                //a null character, we'll replace the last element in the array with an empty string.
                splitStrings[arrIndex] = String.Empty;

            }

            return splitStrings;
        }

        
        // This function will not keep the Empty String 
        private String[] InternalSplitOmitEmptyEntries(Int32[] sepList, Int32[] lengthList, Int32 defaultLength, Int32 numReplaces, int count) {
            Contract.Requires(numReplaces >= 0);
            Contract.Requires(count >= 2);
            Contract.Ensures(Contract.Result<String[]>() != null);

            // Allocate array to hold items. This array may not be 
            // filled completely in this function, we will create a 
            // new array and copy string references to that new array.

            int maxItems = (numReplaces < count) ? (numReplaces+1): count ;
            String[] splitStrings = new String[maxItems];

            int currIndex = 0;
            int arrIndex = 0;

            for(int i=0; i< numReplaces && currIndex < Length; i++) {
                if( sepList[i]-currIndex > 0) { 
                    splitStrings[arrIndex++] = Substring(currIndex, sepList[i]-currIndex );                            
                }
                currIndex=sepList[i] + ((lengthList == null) ? defaultLength : lengthList[i]);
                if( arrIndex == count -1 )  {
                    // If all the remaining entries at the end are empty, skip them
                    while( i < numReplaces - 1 && currIndex == sepList[++i]) { 
                        currIndex += ((lengthList == null) ? defaultLength : lengthList[i]);
                    }
                    break;
                }
            }

            // we must have at least one slot left to fill in the last string.
            Contract.Assert(arrIndex < maxItems);

            //Handle the last string at the end of the array if there is one.
            if (currIndex< Length) {                
                splitStrings[arrIndex++] = Substring(currIndex);
            }

            String[] stringArray = splitStrings;
            if( arrIndex!= maxItems) { 
                stringArray = new String[arrIndex];
                for( int j = 0; j < arrIndex; j++) {
                    stringArray[j] = splitStrings[j];
                }   
            }
            return stringArray;
        }       

        //--------------------------------------------------------------------    
        // This function returns the number of the places within this instance where 
        // characters in Separator occur.
        // Args: separator  -- A string containing all of the split characters.
        //       sepList    -- an array of ints for split char indicies.
        //--------------------------------------------------------------------    
        [System.Security.SecurityCritical]
        private unsafe int MakeSeparatorList(char* separators, int separatorsLength, int[] sepList) {
            Contract.Assert(separatorsLength >= 0, "separatorsLength >= 0");
            int foundCount=0;

            if (separators == null || separatorsLength == 0) {
                fixed (char* pwzChars = &this.m_firstChar) {
                    //If they passed null or an empty string, look for whitespace.
                    for (int i=0; i < Length && foundCount < sepList.Length; i++) {
                        if (Char.IsWhiteSpace(pwzChars[i])) {
                            sepList[foundCount++]=i;
                        }
                    }
                }
            } 
            else {
                int sepListCount = sepList.Length;
                //If they passed in a string of chars, actually look for those chars.
                fixed (char* pwzChars = &this.m_firstChar) {
                    for (int i=0; i< Length && foundCount < sepListCount; i++) {                        
                        char* pSep = separators;
                        for (int j = 0; j < separatorsLength; j++, pSep++) {
                           if ( pwzChars[i] == *pSep) {
                               sepList[foundCount++]=i;
                               break;
                           }
                        }
                    }
                }
            }
            return foundCount;
        }        
        
        //--------------------------------------------------------------------
        // This function returns number of the places within baseString where
        // instances of the separator string occurs.
        // Args: separator  -- the separator
        //       sepList    -- an array of ints for split string indicies.
        //--------------------------------------------------------------------
        [System.Security.SecuritySafeCritical]  // auto-generated
        private unsafe int MakeSeparatorList(string separator, int[] sepList) {
            Contract.Assert(!string.IsNullOrEmpty(separator), "!string.IsNullOrEmpty(separator)");

            int foundCount = 0;
            int sepListCount = sepList.Length;
            int currentSepLength = separator.Length;

            fixed (char* pwzChars = &this.m_firstChar) {
                for (int i = 0; i < Length && foundCount < sepListCount; i++) {
                    if (pwzChars[i] == separator[0] && currentSepLength <= Length - i) {
                        if (currentSepLength == 1
                            || String.CompareOrdinal(this, i, separator, 0, currentSepLength) == 0) {
                            sepList[foundCount] = i;
                            foundCount++;
                            i += currentSepLength - 1;
                        }
                    }
                }
            }
            return foundCount;
        }

        //--------------------------------------------------------------------    
        // This function returns the number of the places within this instance where 
        // instances of separator strings occur.
        // Args: separators -- An array containing all of the split strings.
        //       sepList    -- an array of ints for split string indicies.
        //       lengthList -- an array of ints for split string lengths.
        //--------------------------------------------------------------------    
        [System.Security.SecuritySafeCritical]  // auto-generated
        private unsafe int MakeSeparatorList(String[] separators, int[] sepList, int[] lengthList) {
            Contract.Assert(separators != null && separators.Length > 0, "separators != null && separators.Length > 0");
            
            int foundCount = 0;
            int sepListCount = sepList.Length;
            int sepCount = separators.Length;

            fixed (char* pwzChars = &this.m_firstChar) {
                for (int i=0; i< Length && foundCount < sepListCount; i++) {                        
                    for( int j =0; j < separators.Length; j++) {
                        String separator = separators[j];
                        if (String.IsNullOrEmpty(separator)) {
                            continue;
                        }
                        Int32 currentSepLength = separator.Length;
                        if ( pwzChars[i] == separator[0] && currentSepLength <= Length - i) {
                            if (currentSepLength == 1 
                                || String.CompareOrdinal(this, i, separator, 0, currentSepLength) == 0) {
                                sepList[foundCount] = i;
                                lengthList[foundCount] = currentSepLength;
                                foundCount++;
                                i += currentSepLength - 1;
                                break;
                            }
                        }
                    }
                }
            }
            return foundCount;
        }
       
        // Returns a substring of this string.
        //
        public String Substring (int startIndex) {
            return this.Substring (startIndex, Length-startIndex);
        }
    
        // Returns a substring of this string.
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
        public String Substring(int startIndex, int length) {
                    
            //Bounds Checking.
            if (startIndex < 0) {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            }

            if (startIndex > Length) {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndexLargerThanLength"));
            }

            if (length < 0) {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NegativeLength"));
            }

            if (startIndex > Length - length) {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_IndexLength"));
            }
            Contract.EndContractBlock();

            if( length == 0) {
                return String.Empty;
            }

            if( startIndex == 0 && length == this.Length) {
                return this;
            }

            return InternalSubString(startIndex, length);
        }

        [System.Security.SecurityCritical]  // auto-generated
        unsafe string InternalSubString(int startIndex, int length) {
            Contract.Assert( startIndex >= 0 && startIndex <= this.Length, "StartIndex is out of range!");
            Contract.Assert( length >= 0 && startIndex <= this.Length - length, "length is out of range!");            
            
            String result = FastAllocateString(length);

            fixed(char* dest = &result.m_firstChar)
                fixed(char* src = &this.m_firstChar) {
                    wstrcpy(dest, src + startIndex, length);
                }

            return result;
        }
    
    
        // Removes a set of characters from the end of this string.
        [Pure]
        public String Trim(params char[] trimChars) {
            if (null==trimChars || trimChars.Length == 0) {
                return TrimHelper(TrimBoth);
            }
            return TrimHelper(trimChars,TrimBoth);
        }
    
        // Removes a set of characters from the beginning of this string.
        public String TrimStart(params char[] trimChars) {
            if (null==trimChars || trimChars.Length == 0) {
                return TrimHelper(TrimHead);
            }
            return TrimHelper(trimChars,TrimHead);
        }
    
    
        // Removes a set of characters from the end of this string.
        public String TrimEnd(params char[] trimChars) {
            if (null==trimChars || trimChars.Length == 0) {
                return TrimHelper(TrimTail);
            }
            return TrimHelper(trimChars,TrimTail);
        }
    
        // Creates a new string with the characters copied in from ptr. If
        // ptr is null, a 0-length string (like String.Empty) is returned.
        //
        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false), MethodImplAttribute(MethodImplOptions.InternalCall)]
        unsafe public extern String(char *value);
        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false), MethodImplAttribute(MethodImplOptions.InternalCall)]
        unsafe public extern String(char *value, int startIndex, int length);
    
        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false), MethodImplAttribute(MethodImplOptions.InternalCall)]
        unsafe public extern String(sbyte *value);
        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false), MethodImplAttribute(MethodImplOptions.InternalCall)]
        unsafe public extern String(sbyte *value, int startIndex, int length);

        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false), MethodImplAttribute(MethodImplOptions.InternalCall)]
        unsafe public extern String(sbyte *value, int startIndex, int length, Encoding enc);
        
        [System.Security.SecurityCritical]  // auto-generated
        unsafe static private String CreateString(sbyte *value, int startIndex, int length, Encoding enc) {            
            if (enc == null)
                return new String(value, startIndex, length); // default to ANSI

            if (length < 0)
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            if ((value + startIndex) < value) {
                // overflow check
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_PartialWCHAR"));
            }

            byte [] b = new byte[length];

            try {
                Buffer.Memcpy(b, 0, (byte*)value, startIndex, length);
            }
            catch(NullReferenceException) {
                // If we got a NullReferencException. It means the pointer or 
                // the index is out of range
                throw new ArgumentOutOfRangeException("value", 
                        Environment.GetResourceString("ArgumentOutOfRange_PartialWCHAR"));                
            }

            return enc.GetString(b);
        }
        
        // Helper for encodings so they can talk to our buffer directly
        // stringLength must be the exact size we'll expect
        [System.Security.SecurityCritical]  // auto-generated
        unsafe static internal String CreateStringFromEncoding(
            byte* bytes, int byteLength, Encoding encoding)
        {
            Contract.Requires(bytes != null);
            Contract.Requires(byteLength >= 0);

            // Get our string length
            int stringLength = encoding.GetCharCount(bytes, byteLength, null);
            Contract.Assert(stringLength >= 0, "stringLength >= 0");
            
            // They gave us an empty string if they needed one
            // 0 bytelength might be possible if there's something in an encoder
            if (stringLength == 0)
                return String.Empty;
            
            String s = FastAllocateString(stringLength);
            fixed(char* pTempChars = &s.m_firstChar)
            {
                int doubleCheck = encoding.GetChars(bytes, byteLength, pTempChars, stringLength, null);
                Contract.Assert(stringLength == doubleCheck, 
                    "Expected encoding.GetChars to return same length as encoding.GetCharCount");
            }

            return s;
        }

        // This is only intended to be used by char.ToString.
        // It is necessary to put the code in this class instead of Char, since m_firstChar is a private member.
        // Making m_firstChar internal would be dangerous since it would make it much easier to break String's immutability.
        [SecuritySafeCritical]
        internal static string CreateFromChar(char c)
        {
            string result = FastAllocateString(1);
            result.m_firstChar = c;
            return result;
        }
                
        [System.Security.SecuritySafeCritical]  // auto-generated
        unsafe internal int GetBytesFromEncoding(byte* pbNativeBuffer, int cbNativeBuffer,Encoding encoding)
        {
            // encoding == Encoding.UTF8
            fixed (char* pwzChar = &this.m_firstChar)
            {
                return encoding.GetBytes(pwzChar, m_stringLength, pbNativeBuffer, cbNativeBuffer);
            }            
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        unsafe internal int ConvertToAnsi(byte *pbNativeBuffer, int cbNativeBuffer, bool fBestFit, bool fThrowOnUnmappableChar)
        {
            Contract.Assert(cbNativeBuffer >= (Length + 1) * Marshal.SystemMaxDBCSCharSize, "Insufficient buffer length passed to ConvertToAnsi");

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

        [System.Security.SecuritySafeCritical]  // auto-generated
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

        [System.Security.SecuritySafeCritical]  // auto-generated
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

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static String FastAllocateString(int length);

        [System.Security.SecuritySafeCritical]  // auto-generated
        unsafe private static void FillStringChecked(String dest, int destPos, String src)
        {
            Contract.Requires(dest != null);
            Contract.Requires(src != null);
            if (src.Length > dest.Length - destPos) {
                throw new IndexOutOfRangeException();
            }
            Contract.EndContractBlock();

            fixed(char *pDest = &dest.m_firstChar)
                fixed (char *pSrc = &src.m_firstChar) {
                    wstrcpy(pDest + destPos, pSrc, src.Length);
                }
        }

        // Creates a new string from the characters in a subarray.  The new string will
        // be created from the characters in value between startIndex and
        // startIndex + length - 1.
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern String(char [] value, int startIndex, int length);
    
        // Creates a new string from the characters in a subarray.  The new string will be
        // created from the characters in value.
        //
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern String(char [] value);

        [System.Security.SecurityCritical]  // auto-generated
        internal static unsafe void wstrcpy(char *dmem, char *smem, int charCount)
        {
            Buffer.Memcpy((byte*)dmem, (byte*)smem, charCount * 2); // 2 used everywhere instead of sizeof(char)
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
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

        [System.Security.SecuritySafeCritical]  // auto-generated
        private String CtorCharArrayStartLength(char [] value, int startIndex, int length)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            if (startIndex < 0)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));

            if (length < 0)
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NegativeLength"));

            if (startIndex > value.Length - length)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
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

        [System.Security.SecuritySafeCritical]  // auto-generated
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
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_MustBeNonNegNum", "count"));
        }

        [System.Security.SecurityCritical]  // auto-generated
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

            Contract.Assert(end[0] == 0 || end[1] == 0);
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
            Contract.Assert(*end == 0);

            int count = (int)(end - ptr);

            return count;
        }

        [System.Security.SecurityCritical]  // auto-generated
        private unsafe String CtorCharPtr(char *ptr)
        {
            if (ptr == null)
                return String.Empty;

#if !FEATURE_PAL
            if (ptr < (char*)64000)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeStringPtrNotAtom"));
#endif // FEATURE_PAL

            Contract.Assert(this == null, "this == null");        // this is the string constructor, we allocate it

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
                throw new ArgumentOutOfRangeException("ptr", Environment.GetResourceString("ArgumentOutOfRange_PartialWCHAR"));
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private unsafe String CtorCharPtrStartLength(char *ptr, int startIndex, int length)
        {
            if (length < 0) {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NegativeLength"));
            }

            if (startIndex < 0) {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            }
            Contract.EndContractBlock();
            Contract.Assert(this == null, "this == null");        // this is the string constructor, we allocate it

            char *pFrom = ptr + startIndex;
            if (pFrom < ptr) {
                // This means that the pointer operation has had an overflow
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_PartialWCHAR"));
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
                throw new ArgumentOutOfRangeException("ptr", Environment.GetResourceString("ArgumentOutOfRange_PartialWCHAR"));
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern String(char c, int count);
    
        // Provides a culture-correct string comparison. StrA is compared to StrB
        // to determine whether it is lexicographically less, equal, or greater, and then returns
        // either a negative integer, 0, or a positive integer; respectively.
        //
        [Pure]
        public static int Compare(String strA, String strB)
        {
            return Compare(strA, strB, StringComparison.CurrentCulture);
        }
    

        // Provides a culture-correct string comparison. strA is compared to strB
        // to determine whether it is lexicographically less, equal, or greater, and then a
        // negative integer, 0, or a positive integer is returned; respectively.
        // The case-sensitive option is set by ignoreCase
        //
        [Pure]
        public static int Compare(String strA, String strB, bool ignoreCase)
        {
            var comparisonType = ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture;
            return Compare(strA, strB, comparisonType);
        }

  
        // Provides a more flexible function for string comparision. See StringComparison 
        // for meaning of different comparisonType.
        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        public static int Compare(String strA, String strB, StringComparison comparisonType) 
        {
            // Single comparison to check if comparisonType is within [CurrentCulture .. OrdinalIgnoreCase]
            if ((uint)(comparisonType - StringComparison.CurrentCulture) > (uint)(StringComparison.OrdinalIgnoreCase - StringComparison.CurrentCulture))
            {
                throw new ArgumentException(Environment.GetResourceString("NotSupported_StringComparison"), "comparisonType");
            }
            Contract.EndContractBlock();

            if (object.ReferenceEquals(strA, strB))
            {
                return 0;
            }

            // They can't both be null at this point.
            if (strA == null)
            {
                return -1;
            }
            if (strB == null)
            {
                return 1;
            }

            switch (comparisonType) {
                case StringComparison.CurrentCulture:
                    return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, strB, CompareOptions.None);

                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, strB, CompareOptions.IgnoreCase);

                case StringComparison.InvariantCulture:
                    return CultureInfo.InvariantCulture.CompareInfo.Compare(strA, strB, CompareOptions.None);

                case StringComparison.InvariantCultureIgnoreCase:
                    return CultureInfo.InvariantCulture.CompareInfo.Compare(strA, strB, CompareOptions.IgnoreCase);

                case StringComparison.Ordinal:
                    // Most common case: first character is different.
                    // Returns false for empty strings.
                    if (strA.m_firstChar != strB.m_firstChar)
                    {
                        return strA.m_firstChar - strB.m_firstChar;
                    }

                    return CompareOrdinalHelper(strA, strB);

                case StringComparison.OrdinalIgnoreCase:
                    // If both strings are ASCII strings, we can take the fast path.
                    if (strA.IsAscii() && strB.IsAscii()) {
                        return (CompareOrdinalIgnoreCaseHelper(strA, strB));
                    }

#if FEATURE_COREFX_GLOBALIZATION
                    return CompareInfo.CompareOrdinalIgnoreCase(strA, 0, strA.Length, strB, 0, strB.Length);
#else
                    // Take the slow path.
                    return TextInfo.CompareOrdinalIgnoreCase(strA, strB);
#endif

                default:
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_StringComparison"));
            }
        }


        // Provides a culture-correct string comparison. strA is compared to strB
        // to determine whether it is lexicographically less, equal, or greater, and then a
        // negative integer, 0, or a positive integer is returned; respectively.
        //
        [Pure]
        public static int Compare(String strA, String strB, CultureInfo culture, CompareOptions options) {
            if (culture == null)
            {
                throw new ArgumentNullException("culture");
            }
            Contract.EndContractBlock();

            return culture.CompareInfo.Compare(strA, strB, options);
        }



        // Provides a culture-correct string comparison. strA is compared to strB
        // to determine whether it is lexicographically less, equal, or greater, and then a
        // negative integer, 0, or a positive integer is returned; respectively.
        // The case-sensitive option is set by ignoreCase, and the culture is set
        // by culture
        //
        [Pure]
        public static int Compare(String strA, String strB, bool ignoreCase, CultureInfo culture)
        {
            var options = ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None;
            return Compare(strA, strB, culture, options);
        }

        // Determines whether two string regions match.  The substring of strA beginning
        // at indexA of length count is compared with the substring of strB
        // beginning at indexB of the same length.
        //
        [Pure]
        public static int Compare(String strA, int indexA, String strB, int indexB, int length)
        {
            // NOTE: It's important we call the boolean overload, and not the StringComparison
            // one. The two have some subtly different behavior (see notes in the former).
            return Compare(strA, indexA, strB, indexB, length, ignoreCase: false);
        }

        // Determines whether two string regions match.  The substring of strA beginning
        // at indexA of length count is compared with the substring of strB
        // beginning at indexB of the same length.  Case sensitivity is determined by the ignoreCase boolean.
        //
        [Pure]
        public static int Compare(String strA, int indexA, String strB, int indexB, int length, bool ignoreCase)
        {
            // Ideally we would just forward to the string.Compare overload that takes
            // a StringComparison parameter, and just pass in CurrentCulture/CurrentCultureIgnoreCase.
            // That function will return early if an optimization can be applied, e.g. if
            // (object)strA == strB && indexA == indexB then it will return 0 straightaway.
            // There are a couple of subtle behavior differences that prevent us from doing so
            // however:
            // - string.Compare(null, -1, null, -1, -1, StringComparison.CurrentCulture) works
            //   since that method also returns early for nulls before validation. It shouldn't
            //   for this overload.
            // - Since we originally forwarded to CompareInfo.Compare for all of the argument
            //   validation logic, the ArgumentOutOfRangeExceptions thrown will contain different
            //   parameter names.
            // Therefore, we have to duplicate some of the logic here.

            int lengthA = length;
            int lengthB = length;
            
            if (strA != null)
            {
                lengthA = Math.Min(lengthA, strA.Length - indexA);
            }

            if (strB != null)
            {
                lengthB = Math.Min(lengthB, strB.Length - indexB);
            }

            var options = ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None;
            return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, indexA, lengthA, strB, indexB, lengthB, options);
        }

        // Determines whether two string regions match.  The substring of strA beginning
        // at indexA of length length is compared with the substring of strB
        // beginning at indexB of the same length.  Case sensitivity is determined by the ignoreCase boolean,
        // and the culture is set by culture.
        //
        [Pure]
        public static int Compare(String strA, int indexA, String strB, int indexB, int length, bool ignoreCase, CultureInfo culture)
        {
            var options = ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None;
            return Compare(strA, indexA, strB, indexB, length, culture, options);
        }


        // Determines whether two string regions match.  The substring of strA beginning
        // at indexA of length length is compared with the substring of strB
        // beginning at indexB of the same length.
        //
        [Pure]
        public static int Compare(String strA, int indexA, String strB, int indexB, int length, CultureInfo culture, CompareOptions options)
        {
            if (culture == null)
            {
                throw new ArgumentNullException("culture");
            }
            Contract.EndContractBlock();

            int lengthA = length;
            int lengthB = length;

            if (strA != null)
            {
                lengthA = Math.Min(lengthA, strA.Length - indexA);
            }

            if (strB != null)
            {
                lengthB = Math.Min(lengthB, strB.Length - indexB);
            }
    
            return culture.CompareInfo.Compare(strA, indexA, lengthA, strB, indexB, lengthB, options);
        }

        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        public static int Compare(String strA, int indexA, String strB, int indexB, int length, StringComparison comparisonType) {
            if (comparisonType < StringComparison.CurrentCulture || comparisonType > StringComparison.OrdinalIgnoreCase) {
                throw new ArgumentException(Environment.GetResourceString("NotSupported_StringComparison"), "comparisonType");
            }
            Contract.EndContractBlock();
            
            if (strA == null || strB == null)
            {
                if (object.ReferenceEquals(strA, strB))
                {
                    // They're both null
                    return 0;
                }

                return strA == null ? -1 : 1;
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NegativeLength"));
            }

            if (indexA < 0 || indexB < 0)
            {
                string paramName = indexA < 0 ? "indexA" : "indexB";
                throw new ArgumentOutOfRangeException(paramName, Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (strA.Length - indexA < 0 || strB.Length - indexB < 0)
            {
                string paramName = strA.Length - indexA < 0 ? "indexA" : "indexB";
                throw new ArgumentOutOfRangeException(paramName, Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (length == 0 || (object.ReferenceEquals(strA, strB) && indexA == indexB))
            {
                return 0;
            }

            int lengthA = Math.Min(length, strA.Length - indexA);
            int lengthB = Math.Min(length, strB.Length - indexB);

            switch (comparisonType) {
                case StringComparison.CurrentCulture:
                    return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, indexA, lengthA, strB, indexB, lengthB, CompareOptions.None);

                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, indexA, lengthA, strB, indexB, lengthB, CompareOptions.IgnoreCase);

                case StringComparison.InvariantCulture:
                    return CultureInfo.InvariantCulture.CompareInfo.Compare(strA, indexA, lengthA, strB, indexB, lengthB, CompareOptions.None);

                case StringComparison.InvariantCultureIgnoreCase:
                    return CultureInfo.InvariantCulture.CompareInfo.Compare(strA, indexA, lengthA, strB, indexB, lengthB, CompareOptions.IgnoreCase);

                case StringComparison.Ordinal:
                    return CompareOrdinalHelper(strA, indexA, lengthA, strB, indexB, lengthB);

                case StringComparison.OrdinalIgnoreCase:
#if FEATURE_COREFX_GLOBALIZATION
                    return (CompareInfo.CompareOrdinalIgnoreCase(strA, indexA, lengthA, strB, indexB, lengthB));
#else
                    return (TextInfo.CompareOrdinalIgnoreCaseEx(strA, indexA, strB, indexB, lengthA, lengthB));
#endif

                default:
                    throw new ArgumentException(Environment.GetResourceString("NotSupported_StringComparison"));
            }

        }

        // Compares this String to another String (cast as object), returning an integer that
        // indicates the relationship. This method returns a value less than 0 if this is less than value, 0
        // if this is equal to value, or a value greater than 0 if this is greater than value.
        //
        [Pure]
        public int CompareTo(Object value)
        {
            if (value == null)
            {
                return 1;
            }

            string other = value as string;

            if (other == null)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeString"));
            }

            return CompareTo(other); // will call the string-based overload
        }
    
        // Determines the sorting relation of StrB to the current instance.
        //
        [Pure]
        public int CompareTo(String strB)
        {
            return string.Compare(this, strB, StringComparison.CurrentCulture);
        }

        // Compares strA and strB using an ordinal (code-point) comparison.
        //
        [Pure]
        public static int CompareOrdinal(String strA, String strB)
        {
            if (object.ReferenceEquals(strA, strB))
            {
                return 0;
            }

            // They can't both be null at this point.
            if (strA == null)
            {
                return -1;
            }
            if (strB == null)
            {
                return 1;
            }

            // Most common case, first character is different.
            // This will return false for empty strings.
            if (strA.m_firstChar != strB.m_firstChar)
            {
                return strA.m_firstChar - strB.m_firstChar;
            }

            return CompareOrdinalHelper(strA, strB);
        }
        

        // Compares strA and strB using an ordinal (code-point) comparison.
        //
        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        public static int CompareOrdinal(String strA, int indexA, String strB, int indexB, int length)
        {
            if (strA == null || strB == null)
            {
                if (object.ReferenceEquals(strA, strB))
                {
                    // They're both null
                    return 0;
                }

                return strA == null ? -1 : 1;
            }

            // COMPAT: Checking for nulls should become before the arguments are validated,
            // but other optimizations which allow us to return early should come after.

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));
            }

            if (indexA < 0 || indexB < 0)
            {
                string paramName = indexA < 0 ? "indexA" : "indexB";
                throw new ArgumentOutOfRangeException(paramName, Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }
            
            int lengthA = Math.Min(length, strA.Length - indexA);
            int lengthB = Math.Min(length, strB.Length - indexB);

            if (lengthA < 0 || lengthB < 0)
            {
                string paramName = lengthA < 0 ? "indexA" : "indexB";
                throw new ArgumentOutOfRangeException(paramName, Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (length == 0 || (object.ReferenceEquals(strA, strB) && indexA == indexB))
            {
                return 0;
            }

            return CompareOrdinalHelper(strA, indexA, lengthA, strB, indexB, lengthB);
        }


        [Pure]
        public bool Contains( string value ) {
            return ( IndexOf(value, StringComparison.Ordinal) >=0 );
        }

        // Determines whether a specified string is a suffix of the the current instance.
        //
        // The case-sensitive and culture-sensitive option is set by options,
        // and the default culture is used.
        //        
        [Pure]
        public Boolean EndsWith(String value) {
            return EndsWith(value, StringComparison.CurrentCulture);
        }

        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ComVisible(false)]
        public Boolean EndsWith(String value, StringComparison comparisonType) {
            if( (Object)value == null) {
                throw new ArgumentNullException("value");                                
            }

            if( comparisonType < StringComparison.CurrentCulture || comparisonType > StringComparison.OrdinalIgnoreCase) {
                throw new ArgumentException(Environment.GetResourceString("NotSupported_StringComparison"), "comparisonType");
            }
            Contract.EndContractBlock();

            if( (Object)this == (Object)value) {
                return true;
            }

            if( value.Length == 0) {
                return true;
            }
            
            switch (comparisonType) {
                case StringComparison.CurrentCulture:
                    return CultureInfo.CurrentCulture.CompareInfo.IsSuffix(this, value, CompareOptions.None);

                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.IsSuffix(this, value, CompareOptions.IgnoreCase);

                case StringComparison.InvariantCulture:
                    return CultureInfo.InvariantCulture.CompareInfo.IsSuffix(this, value, CompareOptions.None);

                case StringComparison.InvariantCultureIgnoreCase:
                    return CultureInfo.InvariantCulture.CompareInfo.IsSuffix(this, value, CompareOptions.IgnoreCase);                    

                case StringComparison.Ordinal:
                    return this.Length < value.Length ? false : (CompareOrdinalHelper(this, this.Length - value.Length, value.Length, value, 0, value.Length) == 0);

                case StringComparison.OrdinalIgnoreCase:
#if FEATURE_COREFX_GLOBALIZATION
                    return this.Length < value.Length ? false : (CompareInfo.CompareOrdinalIgnoreCase(this, this.Length - value.Length, value.Length, value, 0, value.Length) == 0);
#else                    
                    return this.Length < value.Length ? false : (TextInfo.CompareOrdinalIgnoreCaseEx(this, this.Length - value.Length, value, 0, value.Length, value.Length) == 0);
#endif
                default:
                    throw new ArgumentException(Environment.GetResourceString("NotSupported_StringComparison"), "comparisonType");
            }                        
        }

        [Pure]
        public Boolean EndsWith(String value, Boolean ignoreCase, CultureInfo culture) {
            if (null==value) {
                throw new ArgumentNullException("value");
            }
            Contract.EndContractBlock();
            
            if((object)this == (object)value) {
                return true;
            }

            CultureInfo referenceCulture;
            if (culture == null)
                referenceCulture = CultureInfo.CurrentCulture;
            else
                referenceCulture = culture;

            return referenceCulture.CompareInfo.IsSuffix(this, value, ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None);
        }

        [Pure]
        internal bool EndsWith(char value) {
            int thisLen = this.Length;
            if (thisLen != 0) {
                if (this[thisLen - 1] == value)
                    return true;
            }
            return false;
        }
    
    
        // Returns the index of the first occurrence of a specified character in the current instance.
        // The search starts at startIndex and runs thorough the next count characters.
        //
        [Pure]
        public int IndexOf(char value) {
            return IndexOf(value, 0, this.Length);
        }

        [Pure]
        public int IndexOf(char value, int startIndex) {
            return IndexOf(value, startIndex, this.Length - startIndex);
        }

        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe int IndexOf(char value, int startIndex, int count) {
            if (startIndex < 0 || startIndex > Length)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));

            if (count < 0 || count > Length - startIndex)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));

            fixed (char* pChars = &m_firstChar)
            {
                char* pCh = pChars + startIndex;

                while (count >= 4)
                {
                    if (*pCh == value) goto ReturnIndex;
                    if (*(pCh + 1) == value) goto ReturnIndex1;
                    if (*(pCh + 2) == value) goto ReturnIndex2;
                    if (*(pCh + 3) == value) goto ReturnIndex3;

                    count -= 4;
                    pCh += 4;
                }

                while (count > 0)
                {
                    if (*pCh == value)
                        goto ReturnIndex;

                    count--;
                    pCh++;
                }

                return -1;

                ReturnIndex3: pCh++;
                ReturnIndex2: pCh++;
                ReturnIndex1: pCh++;
                ReturnIndex:
                return (int)(pCh - pChars);
            }
        }
    
        // Returns the index of the first occurrence of any specified character in the current instance.
        // The search starts at startIndex and runs to startIndex + count -1.
        //
        [Pure]        
        public int IndexOfAny(char [] anyOf) {
            return IndexOfAny(anyOf,0, this.Length);
        }
    
        [Pure]
        public int IndexOfAny(char [] anyOf, int startIndex) {
            return IndexOfAny(anyOf, startIndex, this.Length - startIndex);
        }
    
        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern int IndexOfAny(char [] anyOf, int startIndex, int count);
    
        
        // Determines the position within this string of the first occurrence of the specified
        // string, according to the specified search criteria.  The search begins at
        // the first character of this string, it is case-sensitive and the current culture
        // comparison is used.
        //
        [Pure]
        public int IndexOf(String value) {
            return IndexOf(value, StringComparison.CurrentCulture);
        }

        // Determines the position within this string of the first occurrence of the specified
        // string, according to the specified search criteria.  The search begins at
        // startIndex, it is case-sensitive and the current culture comparison is used.
        //
        [Pure]
        public int IndexOf(String value, int startIndex) {
            return IndexOf(value, startIndex, StringComparison.CurrentCulture);
        }

        // Determines the position within this string of the first occurrence of the specified
        // string, according to the specified search criteria.  The search begins at
        // startIndex, ends at endIndex and the current culture comparison is used.
        //
        [Pure]
        public int IndexOf(String value, int startIndex, int count) {
            if (startIndex < 0 || startIndex > this.Length) {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (count < 0 || count > this.Length - startIndex) {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
            }
            Contract.EndContractBlock();
            
            return IndexOf(value, startIndex, count, StringComparison.CurrentCulture);
        }

        [Pure]
        public int IndexOf(String value, StringComparison comparisonType) {
            return IndexOf(value, 0, this.Length, comparisonType);
        }

        [Pure]
        public int IndexOf(String value, int startIndex, StringComparison comparisonType) {
            return IndexOf(value, startIndex, this.Length - startIndex, comparisonType);
        }

        [Pure]
        [System.Security.SecuritySafeCritical]
        public int IndexOf(String value, int startIndex, int count, StringComparison comparisonType) {
            // Validate inputs
            if (value == null)
                throw new ArgumentNullException("value");

            if (startIndex < 0 || startIndex > this.Length)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));

            if (count < 0 || startIndex > this.Length - count)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
            Contract.EndContractBlock();

            switch (comparisonType) {
                case StringComparison.CurrentCulture:
                    return CultureInfo.CurrentCulture.CompareInfo.IndexOf(this, value, startIndex, count, CompareOptions.None);

                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.IndexOf(this, value, startIndex, count, CompareOptions.IgnoreCase);

                case StringComparison.InvariantCulture:
                    return CultureInfo.InvariantCulture.CompareInfo.IndexOf(this, value, startIndex, count, CompareOptions.None);

                case StringComparison.InvariantCultureIgnoreCase:
                    return CultureInfo.InvariantCulture.CompareInfo.IndexOf(this, value, startIndex, count, CompareOptions.IgnoreCase);

                case StringComparison.Ordinal:
                    return CultureInfo.InvariantCulture.CompareInfo.IndexOf(this, value, startIndex, count, CompareOptions.Ordinal);

                case StringComparison.OrdinalIgnoreCase:
                    if (value.IsAscii() && this.IsAscii())
                        return CultureInfo.InvariantCulture.CompareInfo.IndexOf(this, value, startIndex, count, CompareOptions.IgnoreCase);
                    else
                        return TextInfo.IndexOfStringOrdinalIgnoreCase(this, value, startIndex, count);

                default:
                    throw new ArgumentException(Environment.GetResourceString("NotSupported_StringComparison"), "comparisonType");
            }  
        }

        // Returns the index of the last occurrence of a specified character in the current instance.
        // The search starts at startIndex and runs backwards to startIndex - count + 1.
        // The character at position startIndex is included in the search.  startIndex is the larger
        // index within the string.
        //
        [Pure]
        public int LastIndexOf(char value) {
            return LastIndexOf(value, this.Length-1, this.Length);
        }

        [Pure]
        public int LastIndexOf(char value, int startIndex){
            return LastIndexOf(value,startIndex,startIndex + 1);
        }

        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe int LastIndexOf(char value, int startIndex, int count) {
            if (Length == 0)
                return -1;

            if (startIndex < 0 || startIndex >= Length)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));

            if (count < 0 || count - 1 > startIndex)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));

            fixed (char* pChars = &m_firstChar)
            {
                char* pCh = pChars + startIndex;

                //We search [startIndex..EndIndex]
                while (count >= 4)
                {
                    if (*pCh == value) goto ReturnIndex;
                    if (*(pCh - 1) == value) goto ReturnIndex1;
                    if (*(pCh - 2) == value) goto ReturnIndex2;
                    if (*(pCh - 3) == value) goto ReturnIndex3;

                    count -= 4;
                    pCh -= 4;
                }

                while (count > 0)
                {
                    if (*pCh == value)
                        goto ReturnIndex;

                    count--;
                    pCh--;
                }

                return -1;

                ReturnIndex3: pCh--;
                ReturnIndex2: pCh--;
                ReturnIndex1: pCh--;
                ReturnIndex:
                return (int)(pCh - pChars);
            }
        }
    
        // Returns the index of the last occurrence of any specified character in the current instance.
        // The search starts at startIndex and runs backwards to startIndex - count + 1.
        // The character at position startIndex is included in the search.  startIndex is the larger
        // index within the string.
        //
        
        //ForceInline ... Jit can't recognize String.get_Length to determine that this is "fluff"
        [Pure]
        public int LastIndexOfAny(char [] anyOf) {
            return LastIndexOfAny(anyOf,this.Length-1,this.Length);
        }
    
        [Pure]
        public int LastIndexOfAny(char [] anyOf, int startIndex) {
            return LastIndexOfAny(anyOf,startIndex,startIndex + 1);
        }

        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern int LastIndexOfAny(char [] anyOf, int startIndex, int count);
    
    
        // Returns the index of the last occurrence of any character in value in the current instance.
        // The search starts at startIndex and runs backwards to startIndex - count + 1.
        // The character at position startIndex is included in the search.  startIndex is the larger
        // index within the string.
        //
        [Pure]
        public int LastIndexOf(String value) {
            return LastIndexOf(value, this.Length-1,this.Length, StringComparison.CurrentCulture);
        }

        [Pure]
        public int LastIndexOf(String value, int startIndex) {
            return LastIndexOf(value, startIndex, startIndex + 1, StringComparison.CurrentCulture);
        }

        [Pure]
        public int LastIndexOf(String value, int startIndex, int count) {
            if (count<0) {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
            }
            Contract.EndContractBlock();

            return LastIndexOf(value, startIndex, count, StringComparison.CurrentCulture);
        }

        [Pure]
        public int LastIndexOf(String value, StringComparison comparisonType) {
            return LastIndexOf(value, this.Length-1, this.Length, comparisonType);
        }

        [Pure]
        public int LastIndexOf(String value, int startIndex, StringComparison comparisonType) {
            return LastIndexOf(value, startIndex, startIndex + 1, comparisonType);
        }

        [Pure]
        [System.Security.SecuritySafeCritical]
        public int LastIndexOf(String value, int startIndex, int count, StringComparison comparisonType) {
            if (value == null)
                throw new ArgumentNullException("value");
            Contract.EndContractBlock();

            // Special case for 0 length input strings
            if (this.Length == 0 && (startIndex == -1 || startIndex == 0))
                return (value.Length == 0) ? 0 : -1;

            // Now after handling empty strings, make sure we're not out of range
            if (startIndex < 0 || startIndex > this.Length)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            
            // Make sure that we allow startIndex == this.Length
            if (startIndex == this.Length)
            {
                startIndex--;
                if (count > 0)
                    count--;

                // If we are looking for nothing, just return 0
                if (value.Length == 0 && count >= 0 && startIndex - count + 1 >= 0)
                    return startIndex;
            }

            // 2nd half of this also catches when startIndex == MAXINT, so MAXINT - 0 + 1 == -1, which is < 0.
            if (count < 0 || startIndex - count + 1 < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));


            switch (comparisonType) {
                case StringComparison.CurrentCulture:
                    return CultureInfo.CurrentCulture.CompareInfo.LastIndexOf(this, value, startIndex, count, CompareOptions.None);

                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.LastIndexOf(this, value, startIndex, count, CompareOptions.IgnoreCase);

                case StringComparison.InvariantCulture:
                    return CultureInfo.InvariantCulture.CompareInfo.LastIndexOf(this, value, startIndex, count, CompareOptions.None);

                case StringComparison.InvariantCultureIgnoreCase:
                    return CultureInfo.InvariantCulture.CompareInfo.LastIndexOf(this, value, startIndex, count, CompareOptions.IgnoreCase);
                case StringComparison.Ordinal:
                    return CultureInfo.InvariantCulture.CompareInfo.LastIndexOf(this, value, startIndex, count, CompareOptions.Ordinal);
                     
                case StringComparison.OrdinalIgnoreCase:
                    if (value.IsAscii() && this.IsAscii())
                        return CultureInfo.InvariantCulture.CompareInfo.LastIndexOf(this, value, startIndex, count, CompareOptions.IgnoreCase);
                    else
                        return TextInfo.LastIndexOfStringOrdinalIgnoreCase(this, value, startIndex, count);
                default:
                    throw new ArgumentException(Environment.GetResourceString("NotSupported_StringComparison"), "comparisonType");
            }  
        }
        
        //
        //
        [Pure]
        public String PadLeft(int totalWidth) {
            return PadLeft(totalWidth, ' ');
        }

        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        public String PadLeft(int totalWidth, char paddingChar) {
            if (totalWidth < 0)
                throw new ArgumentOutOfRangeException("totalWidth", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            int oldLength = Length;
            int count = totalWidth - oldLength;
            if (count <= 0)
                return this;
            String result = FastAllocateString(totalWidth);
            unsafe
            {
                fixed (char* dst = &result.m_firstChar)
                {
                    for (int i = 0; i < count; i++)
                        dst[i] = paddingChar;
                    fixed (char* src = &m_firstChar)
                    {
                        wstrcpy(dst + count, src, oldLength);
                    }
                }
            }
            return result;
        }

        [Pure]
        public String PadRight(int totalWidth) {
            return PadRight(totalWidth, ' ');
        }

        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        public String PadRight(int totalWidth, char paddingChar) {
            if (totalWidth < 0)
                throw new ArgumentOutOfRangeException("totalWidth", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            int oldLength = Length;
            int count = totalWidth - oldLength;
            if (count <= 0)
                return this;
            String result = FastAllocateString(totalWidth);
            unsafe
            {
                fixed (char* dst = &result.m_firstChar)
                {
                    fixed (char* src = &m_firstChar)
                    {
                        wstrcpy(dst, src, oldLength);
                    }
                    for (int i = 0; i < count; i++)
                        dst[oldLength + i] = paddingChar;
                }
            }
            return result;
        }
    
        // Determines whether a specified string is a prefix of the current instance
        //
        [Pure]
        public Boolean StartsWith(String value) {
            if ((Object)value == null) {
                throw new ArgumentNullException("value");
            }
            Contract.EndContractBlock();
            return StartsWith(value, StringComparison.CurrentCulture);
        }

        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ComVisible(false)]
        public Boolean StartsWith(String value, StringComparison comparisonType) {
            if( (Object)value == null) {
                throw new ArgumentNullException("value");                                
            }

            if( comparisonType < StringComparison.CurrentCulture || comparisonType > StringComparison.OrdinalIgnoreCase) {
                throw new ArgumentException(Environment.GetResourceString("NotSupported_StringComparison"), "comparisonType");
            }
            Contract.EndContractBlock();

            if( (Object)this == (Object)value) {
                return true;
            }

            if( value.Length == 0) {
                return true;
            }

            switch (comparisonType) {
                case StringComparison.CurrentCulture:
                    return CultureInfo.CurrentCulture.CompareInfo.IsPrefix(this, value, CompareOptions.None);

                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.IsPrefix(this, value, CompareOptions.IgnoreCase);

                case StringComparison.InvariantCulture:
                    return CultureInfo.InvariantCulture.CompareInfo.IsPrefix(this, value, CompareOptions.None);

                case StringComparison.InvariantCultureIgnoreCase:
                    return CultureInfo.InvariantCulture.CompareInfo.IsPrefix(this, value, CompareOptions.IgnoreCase);                    

                case StringComparison.Ordinal:
                    if( this.Length < value.Length || m_firstChar != value.m_firstChar) {
                        return false;
                    }
                    return (value.Length == 1) ?
                            true :                 // First char is the same and thats all there is to compare
                            StartsWithOrdinalHelper(this, value);

                case StringComparison.OrdinalIgnoreCase:
                    if( this.Length < value.Length) {
                        return false;
                    }
                    
#if FEATURE_COREFX_GLOBALIZATION
                    return (CompareInfo.CompareOrdinalIgnoreCase(this, 0, value.Length, value, 0, value.Length) == 0);
#else
                    return (TextInfo.CompareOrdinalIgnoreCaseEx(this, 0, value, 0, value.Length, value.Length) == 0);
#endif

                default:
                    throw new ArgumentException(Environment.GetResourceString("NotSupported_StringComparison"), "comparisonType");
            }                        
        }

        [Pure]
        public Boolean StartsWith(String value, Boolean ignoreCase, CultureInfo culture) {
            if (null==value) {
                throw new ArgumentNullException("value");
            }
            Contract.EndContractBlock();

            if((object)this == (object)value) {
                return true;
            }

            CultureInfo referenceCulture;
            if (culture == null)
                referenceCulture = CultureInfo.CurrentCulture;
            else
                referenceCulture = culture;

            return referenceCulture.CompareInfo.IsPrefix(this, value, ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None);
        }
  
        // Creates a copy of this string in lower case.
        [Pure]
        public String ToLower() {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return this.ToLower(CultureInfo.CurrentCulture);
        }
    
        // Creates a copy of this string in lower case.  The culture is set by culture.
        [Pure]
        public String ToLower(CultureInfo culture) {
            if (culture == null)
            {
                throw new ArgumentNullException("culture");
            }
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return culture.TextInfo.ToLower(this);
        }

        // Creates a copy of this string in lower case based on invariant culture.
        [Pure]
        public String ToLowerInvariant() {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return this.ToLower(CultureInfo.InvariantCulture);
        }
    
        // Creates a copy of this string in upper case.
        [Pure]
        public String ToUpper() {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return this.ToUpper(CultureInfo.CurrentCulture);
        }
   

        // Creates a copy of this string in upper case.  The culture is set by culture.
        [Pure]
        public String ToUpper(CultureInfo culture) {
            if (culture == null)
            {
                throw new ArgumentNullException("culture");
            }
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return culture.TextInfo.ToUpper(this);
        }


        //Creates a copy of this string in upper case based on invariant culture.
        [Pure]
        public String ToUpperInvariant() {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return this.ToUpper(CultureInfo.InvariantCulture);
        }

   
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

        // Trims the whitespace from both ends of the string.  Whitespace is defined by
        // Char.IsWhiteSpace.
        //
        [Pure]
        public String Trim() {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            return TrimHelper(TrimBoth);        
        }

       
        [System.Security.SecuritySafeCritical]  // auto-generated
        private String TrimHelper(int trimType) {
            //end will point to the first non-trimmed character on the right
            //start will point to the first non-trimmed character on the Left
            int end = this.Length-1;
            int start=0;

            //Trim specified characters.
            if (trimType !=TrimTail)  {
                for (start=0; start < this.Length; start++) {
                    if (!Char.IsWhiteSpace(this[start])) break;
                }
            }
            
            if (trimType !=TrimHead) {
                for (end= Length -1; end >= start;  end--) {
                    if (!Char.IsWhiteSpace(this[end])) break;
                }
            }

            return CreateTrimmedString(start, end);
        }
    
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        private String TrimHelper(char[] trimChars, int trimType) {
            //end will point to the first non-trimmed character on the right
            //start will point to the first non-trimmed character on the Left
            int end = this.Length-1;
            int start=0;

            //Trim specified characters.
            if (trimType !=TrimTail)  {
                for (start=0; start < this.Length; start++) {
                    int i = 0;
                    char ch = this[start];
                    for( i = 0; i < trimChars.Length; i++) {
                        if( trimChars[i] == ch) break;
                    }
                    if( i == trimChars.Length) { // the character is not white space
                        break;  
                    }
                }
            }
            
            if (trimType !=TrimHead) {
                for (end= Length -1; end >= start;  end--) {
                    int i = 0;    
                    char ch = this[end];                    
                    for(i = 0; i < trimChars.Length; i++) {
                        if( trimChars[i] == ch) break;
                    }
                    if( i == trimChars.Length) { // the character is not white space
                        break;  
                    }                    
                }
            }

            return CreateTrimmedString(start, end);
        }


        [System.Security.SecurityCritical]  // auto-generated
        private String CreateTrimmedString(int start, int end) {
            int len = end -start + 1;
            if (len == this.Length) {
                // Don't allocate a new string as the trimmed string has not changed.
                return this;
            }

            if( len == 0) {
                return String.Empty;
            }
            return InternalSubString(start, len);
        }
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        public String Insert(int startIndex, String value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (startIndex < 0 || startIndex > this.Length)
                throw new ArgumentOutOfRangeException("startIndex");
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.Ensures(Contract.Result<String>().Length == this.Length + value.Length);
            Contract.EndContractBlock();
            
            int oldLength = Length;
            int insertLength = value.Length;
            
            if (oldLength == 0)
                return value;
            if (insertLength == 0)
                return this;
            
            // In case this computation overflows, newLength will be negative and FastAllocateString throws OutOfMemoryException
            int newLength = oldLength + insertLength;
            String result = FastAllocateString(newLength);
            unsafe
            {
                fixed (char* srcThis = &m_firstChar)
                {
                    fixed (char* srcInsert = &value.m_firstChar)
                    {
                        fixed (char* dst = &result.m_firstChar)
                        {
                            wstrcpy(dst, srcThis, startIndex);
                            wstrcpy(dst + startIndex, srcInsert, insertLength);
                            wstrcpy(dst + startIndex + insertLength, srcThis + startIndex, oldLength - startIndex);
                        }
                    }
                }
            }
            return result;
        }

        // Replaces all instances of oldChar with newChar.
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
        public String Replace(char oldChar, char newChar)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.Ensures(Contract.Result<String>().Length == this.Length);
            Contract.EndContractBlock();

            if (oldChar == newChar)
                return this;

            unsafe
            {
                int remainingLength = Length;

                fixed (char* pChars = &m_firstChar)
                {
                    char* pSrc = pChars;

                    while (remainingLength > 0)
                    {
                        if (*pSrc == oldChar)
                        {
                            break;
                        }

                        remainingLength--;
                        pSrc++;
                    }
                }

                if (remainingLength == 0)
                    return this;

                String result = FastAllocateString(Length);

                fixed (char* pChars = &m_firstChar)
                {
                    fixed (char* pResult = &result.m_firstChar)
                    {
                        int copyLength = Length - remainingLength;

                        //Copy the characters already proven not to match.
                        if (copyLength > 0)
                        {
                            wstrcpy(pResult, pChars, copyLength);
                        }

                        //Copy the remaining characters, doing the replacement as we go.
                        char* pSrc = pChars + copyLength;
                        char* pDst = pResult + copyLength;

                        do
                        {
                            char currentChar = *pSrc;
                            if (currentChar == oldChar)
                                currentChar = newChar;
                            *pDst = currentChar;

                            remainingLength--;
                            pSrc++;
                            pDst++;
                        } while (remainingLength > 0);
                    }
                }

                return result;
            }
        }

        // This method contains the same functionality as StringBuilder Replace. The only difference is that
        // a new String has to be allocated since Strings are immutable
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern String ReplaceInternal(String oldValue, String newValue);

        public String Replace(String oldValue, String newValue)
        {
            if (oldValue == null)
                throw new ArgumentNullException("oldValue");
            // Note that if newValue is null, we treat it like String.Empty.
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            return ReplaceInternal(oldValue, newValue);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public String Remove(int startIndex, int count)
        {
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException("startIndex", 
                    Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", 
                    Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));
            if (count > Length - startIndex)
                throw new ArgumentOutOfRangeException("count", 
                    Environment.GetResourceString("ArgumentOutOfRange_IndexCount"));
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.Ensures(Contract.Result<String>().Length == this.Length - count);
            Contract.EndContractBlock();
            
            if (count == 0)
                return this;
            int newLength = Length - count;
            if (newLength == 0)
                return String.Empty;
            
            String result = FastAllocateString(newLength);
            unsafe
            {
                fixed (char* src = &m_firstChar)
                {
                    fixed (char* dst = &result.m_firstChar)
                    {
                        wstrcpy(dst, src, startIndex);
                        wstrcpy(dst + startIndex, src + startIndex + count, newLength - startIndex);
                    }
                }
            }
            return result;
        }

        // a remove that just takes a startindex. 
        public string Remove( int startIndex ) {
            if (startIndex < 0) {
                throw new ArgumentOutOfRangeException("startIndex", 
                        Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            }
            
            if (startIndex >= Length) {
                throw new ArgumentOutOfRangeException("startIndex", 
                        Environment.GetResourceString("ArgumentOutOfRange_StartIndexLessThanLength"));                
            }
            
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            return Substring(0, startIndex);
        }   
    
        public static String Format(String format, Object arg0) {
            Contract.Ensures(Contract.Result<String>() != null);
            return FormatHelper(null, format, new ParamsArray(arg0));
        }
    
        public static String Format(String format, Object arg0, Object arg1) {
            Contract.Ensures(Contract.Result<String>() != null);
            return FormatHelper(null, format, new ParamsArray(arg0, arg1));
        }
    
        public static String Format(String format, Object arg0, Object arg1, Object arg2) {
            Contract.Ensures(Contract.Result<String>() != null);
            return FormatHelper(null, format, new ParamsArray(arg0, arg1, arg2));
        }

        public static String Format(String format, params Object[] args) {
            if (args == null)
            {
                // To preserve the original exception behavior, throw an exception about format if both
                // args and format are null. The actual null check for format is in FormatHelper.
                throw new ArgumentNullException((format == null) ? "format" : "args");
            }
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            
            return FormatHelper(null, format, new ParamsArray(args));
        }
        
        public static String Format(IFormatProvider provider, String format, Object arg0) {
            Contract.Ensures(Contract.Result<String>() != null);
            return FormatHelper(provider, format, new ParamsArray(arg0));
        }
    
        public static String Format(IFormatProvider provider, String format, Object arg0, Object arg1) {
            Contract.Ensures(Contract.Result<String>() != null);
            return FormatHelper(provider, format, new ParamsArray(arg0, arg1));
        }
    
        public static String Format(IFormatProvider provider, String format, Object arg0, Object arg1, Object arg2) {
            Contract.Ensures(Contract.Result<String>() != null);
            return FormatHelper(provider, format, new ParamsArray(arg0, arg1, arg2));
        }

        public static String Format(IFormatProvider provider, String format, params Object[] args) {
            if (args == null)
            {
                // To preserve the original exception behavior, throw an exception about format if both
                // args and format are null. The actual null check for format is in FormatHelper.
                throw new ArgumentNullException((format == null) ? "format" : "args");
            }
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            
            return FormatHelper(provider, format, new ParamsArray(args));
        }
        
        private static String FormatHelper(IFormatProvider provider, String format, ParamsArray args) {
            if (format == null)
                throw new ArgumentNullException("format");
            
            return StringBuilderCache.GetStringAndRelease(
                StringBuilderCache
                    .Acquire(format.Length + args.Length * 8)
                    .AppendFormatHelper(provider, format, args));
        }
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        unsafe public static String Copy (String str) {
            if (str==null) {
                throw new ArgumentNullException("str");
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

        public static String Concat(Object arg0) {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            if (arg0 == null)
            {
                return String.Empty;
            }
            return arg0.ToString();
        }
    
        public static String Concat(Object arg0, Object arg1) {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            if (arg0 == null)
            {
                arg0 = String.Empty;
            }
    
            if (arg1==null) {
                arg1 = String.Empty;
            }
            return Concat(arg0.ToString(), arg1.ToString());
        }
    
        public static String Concat(Object arg0, Object arg1, Object arg2) {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            if (arg0 == null)
            {
                arg0 = String.Empty;
            }
    
            if (arg1==null) {
                arg1 = String.Empty;
            }
    
            if (arg2==null) {
                arg2 = String.Empty;
            }
    
            return Concat(arg0.ToString(), arg1.ToString(), arg2.ToString());
        }

        [CLSCompliant(false)] 
        public static String Concat(Object arg0, Object arg1, Object arg2, Object arg3, __arglist) 
        {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            Object[]   objArgs;
            int        argCount;
            
            ArgIterator args = new ArgIterator(__arglist);

            //+4 to account for the 4 hard-coded arguments at the beginning of the list.
            argCount = args.GetRemainingCount() + 4;
    
            objArgs = new Object[argCount];
            
            //Handle the hard-coded arguments
            objArgs[0] = arg0;
            objArgs[1] = arg1;
            objArgs[2] = arg2;
            objArgs[3] = arg3;
            
            //Walk all of the args in the variable part of the argument list.
            for (int i=4; i<argCount; i++) {
                objArgs[i] = TypedReference.ToObject(args.GetNextArg());
            }

            return Concat(objArgs);
        }

        [System.Security.SecuritySafeCritical]
        public static string Concat(params object[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            if (args.Length <= 1)
            {
                return args.Length == 0 ?
                    string.Empty :
                    args[0]?.ToString() ?? string.Empty;
            }

            // We need to get an intermediary string array
            // to fill with each of the args' ToString(),
            // and then just concat that in one operation.

            // This way we avoid any intermediary string representations,
            // or buffer resizing if we use StringBuilder (although the
            // latter case is partially alleviated due to StringBuilder's
            // linked-list style implementation)

            var strings = new string[args.Length];
            
            int totalLength = 0;

            for (int i = 0; i < args.Length; i++)
            {
                object value = args[i];

                string toString = value?.ToString() ?? string.Empty; // We need to handle both the cases when value or value.ToString() is null
                strings[i] = toString;

                totalLength += toString.Length;

                if (totalLength < 0) // Check for a positive overflow
                {
                    throw new OutOfMemoryException();
                }
            }

            // If all of the ToStrings are null/empty, just return string.Empty
            if (totalLength == 0)
            {
                return string.Empty;
            }

            string result = FastAllocateString(totalLength);
            int position = 0; // How many characters we've copied so far

            for (int i = 0; i < strings.Length; i++)
            {
                string s = strings[i];

                Contract.Assert(s != null);
                Contract.Assert(position <= totalLength - s.Length, "We didn't allocate enough space for the result string!");

                FillStringChecked(result, position, s);
                position += s.Length;
            }

            return result;
        }

        [ComVisible(false)]
        public static string Concat<T>(IEnumerable<T> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            using (IEnumerator<T> en = values.GetEnumerator())
            {
                if (!en.MoveNext())
                    return string.Empty;
                
                // We called MoveNext once, so this will be the first item
                T currentValue = en.Current;

                // Call ToString before calling MoveNext again, since
                // we want to stay consistent with the below loop
                // Everything should be called in the order
                // MoveNext-Current-ToString, unless further optimizations
                // can be made, to avoid breaking changes
                string firstString = currentValue?.ToString();

                // If there's only 1 item, simply call ToString on that
                if (!en.MoveNext())
                {
                    // We have to handle the case of either currentValue
                    // or its ToString being null
                    return firstString ?? string.Empty;
                }

                StringBuilder result = StringBuilderCache.Acquire();
                
                result.Append(firstString);

                do
                {
                    currentValue = en.Current;

                    if (currentValue != null)
                    {
                        result.Append(currentValue.ToString());
                    }
                }
                while (en.MoveNext());

                return StringBuilderCache.GetStringAndRelease(result);
            }
        }


        [ComVisible(false)]
        public static string Concat(IEnumerable<string> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            using (IEnumerator<string> en = values.GetEnumerator())
            {
                if (!en.MoveNext())
                    return string.Empty;
                
                string firstValue = en.Current;

                if (!en.MoveNext())
                {
                    return firstValue ?? string.Empty;
                }

                StringBuilder result = StringBuilderCache.Acquire();
                result.Append(firstValue);

                do
                {
                    result.Append(en.Current);
                }
                while (en.MoveNext());

                return StringBuilderCache.GetStringAndRelease(result);
            }
        }


        [System.Security.SecuritySafeCritical]  // auto-generated
        public static String Concat(String str0, String str1) {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.Ensures(Contract.Result<String>().Length ==
                (str0 == null ? 0 : str0.Length) +
                (str1 == null ? 0 : str1.Length));
            Contract.EndContractBlock();

            if (IsNullOrEmpty(str0)) {
                if (IsNullOrEmpty(str1)) {
                    return String.Empty;
                }
                return str1;
            }

            if (IsNullOrEmpty(str1)) {
                return str0;
            }

            int str0Length = str0.Length;
            
            String result = FastAllocateString(str0Length + str1.Length);
            
            FillStringChecked(result, 0,        str0);
            FillStringChecked(result, str0Length, str1);
            
            return result;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static String Concat(String str0, String str1, String str2) {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.Ensures(Contract.Result<String>().Length ==
                (str0 == null ? 0 : str0.Length) +
                (str1 == null ? 0 : str1.Length) +
                (str2 == null ? 0 : str2.Length));
            Contract.EndContractBlock();

            if (IsNullOrEmpty(str0))
            {
                return Concat(str1, str2);
            }

            if (IsNullOrEmpty(str1))
            {
                return Concat(str0, str2);
            }

            if (IsNullOrEmpty(str2))
            {
                return Concat(str0, str1);
            }

            int totalLength = str0.Length + str1.Length + str2.Length;

            String result = FastAllocateString(totalLength);
            FillStringChecked(result, 0, str0);
            FillStringChecked(result, str0.Length, str1);
            FillStringChecked(result, str0.Length + str1.Length, str2);

            return result;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static String Concat(String str0, String str1, String str2, String str3) {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.Ensures(Contract.Result<String>().Length == 
                (str0 == null ? 0 : str0.Length) +
                (str1 == null ? 0 : str1.Length) +
                (str2 == null ? 0 : str2.Length) +
                (str3 == null ? 0 : str3.Length));
            Contract.EndContractBlock();

            if (IsNullOrEmpty(str0))
            {
                return Concat(str1, str2, str3);
            }

            if (IsNullOrEmpty(str1))
            {
                return Concat(str0, str2, str3);
            }

            if (IsNullOrEmpty(str2))
            {
                return Concat(str0, str1, str3);
            }

            if (IsNullOrEmpty(str3))
            {
                return Concat(str0, str1, str2);
            }

            int totalLength = str0.Length + str1.Length + str2.Length + str3.Length;

            String result = FastAllocateString(totalLength);
            FillStringChecked(result, 0, str0);
            FillStringChecked(result, str0.Length, str1);
            FillStringChecked(result, str0.Length + str1.Length, str2);
            FillStringChecked(result, str0.Length + str1.Length + str2.Length, str3);

            return result;
        }

        [System.Security.SecuritySafeCritical]
        public static String Concat(params String[] values) {
            if (values == null)
                throw new ArgumentNullException("values");
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            if (values.Length <= 1)
            {
                return values.Length == 0 ?
                    string.Empty :
                    values[0] ?? string.Empty;
            }

            // It's possible that the input values array could be changed concurrently on another
            // thread, such that we can't trust that each read of values[i] will be equivalent.
            // Worst case, we can make a defensive copy of the array and use that, but we first
            // optimistically try the allocation and copies assuming that the array isn't changing,
            // which represents the 99.999% case, in particular since string.Concat is used for
            // string concatenation by the languages, with the input array being a params array.

            // Sum the lengths of all input strings
            long totalLengthLong = 0;
            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i];
                if (value != null)
                {
                    totalLengthLong += value.Length;
                }
            }

            // If it's too long, fail, or if it's empty, return an empty string.
            if (totalLengthLong > int.MaxValue)
            {
                throw new OutOfMemoryException();
            }
            int totalLength = (int)totalLengthLong;
            if (totalLength == 0)
            {
                return string.Empty;
            }

            // Allocate a new string and copy each input string into it
            string result = FastAllocateString(totalLength);
            int copiedLength = 0;
            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i];
                if (!string.IsNullOrEmpty(value))
                {
                    int valueLen = value.Length;
                    if (valueLen > totalLength - copiedLength)
                    {
                        copiedLength = -1;
                        break;
                    }

                    FillStringChecked(result, copiedLength, value);
                    copiedLength += valueLen;
                }
            }

            // If we copied exactly the right amount, return the new string.  Otherwise,
            // something changed concurrently to mutate the input array: fall back to
            // doing the concatenation again, but this time with a defensive copy. This
            // fall back should be extremely rare.
            return copiedLength == totalLength ? result : Concat((string[])values.Clone());
        }
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public static String Intern(String str) {
            if (str==null) {
                throw new ArgumentNullException("str");
            }
            Contract.Ensures(Contract.Result<String>().Length == str.Length);
            Contract.Ensures(str.Equals(Contract.Result<String>()));
            Contract.EndContractBlock();

            return Thread.GetDomain().GetOrInternString(str);
        }

        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        public static String IsInterned(String str) {
            if (str==null) {
                throw new ArgumentNullException("str");
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
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern bool IsFastSort();
        // Is this a string that only contains characters < 0x80.
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern bool IsAscii();

        // Set extra byte for odd-sized strings that came from interop as BSTR.
        [System.Security.SecurityCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern void SetTrailByte(byte data);
        // Try to retrieve the extra byte - returns false if not present.
        [System.Security.SecurityCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern bool TryGetTrailByte(out byte data);

#if !FEATURE_CORECLR
        public CharEnumerator GetEnumerator() {
            Contract.Ensures(Contract.Result<CharEnumerator>() != null);
            Contract.EndContractBlock();
            BCLDebug.Perf(false, "Avoid using String's CharEnumerator until C# special cases foreach on String - use the indexed property on String instead.");
            return new CharEnumerator(this);
        }
#endif // !FEATURE_CORECLR

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
        [System.Security.SecurityCritical]  // auto-generated
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
    }

    [ComVisible(false)]
    [Flags]
    public enum StringSplitOptions {
        None = 0,
        RemoveEmptyEntries = 1
    }
}
