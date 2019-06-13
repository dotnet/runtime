// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Internal.Runtime.CompilerServices;

namespace System
{
    // The String class represents a static string of characters.  Many of
    // the string methods perform some type of transformation on the current
    // instance and return the result as a new string.  As with arrays, character
    // positions (indices) are zero-based.

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed partial class String : IComparable, IEnumerable, IConvertible, IEnumerable<char>, IComparable<string?>,
        // IEquatable<string> is invariant by design.  However, the lack of covariance means that String?
        // couldn't be used in places constrained to T : IEquatable<String>.  As a workaround, until the
        // language provides a mechanism for this, we make the generic type argument oblivious, in conjunction
        // with making all such constraints oblivious as well.
#nullable disable
        IEquatable<string>,
#nullable restore
        ICloneable
    {
        /*
         * CONSTRUCTORS
         *
         * Defining a new constructor for string-like types (like String) requires changes both
         * to the managed code below and to the native VM code. See the comment at the top of
         * src/vm/ecall.cpp for instructions on how to add new overloads.
         */

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern String(char[] value);

#if PROJECTN
        [DependencyReductionRoot]
#endif
#if !CORECLR
        static
#endif
        private string Ctor(char[]? value)
        {
            if (value == null || value.Length == 0)
                return Empty;

            string result = FastAllocateString(value.Length);
            unsafe
            {
                fixed (char* dest = &result._firstChar, source = value)
                    wstrcpy(dest, source, value.Length);
            }
            return result;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern String(char[] value, int startIndex, int length);

#if PROJECTN
        [DependencyReductionRoot]
#endif
#if !CORECLR
        static
#endif
        private string Ctor(char[] value, int startIndex, int length)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_StartIndex);

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_NegativeLength);

            if (startIndex > value.Length - length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_Index);

            if (length == 0)
                return Empty;

            string result = FastAllocateString(length);
            unsafe
            {
                fixed (char* dest = &result._firstChar, source = value)
                    wstrcpy(dest, source + startIndex, length);
            }
            return result;
        }

        [CLSCompliant(false)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern unsafe String(char* value);

#if PROJECTN
        [DependencyReductionRoot]
#endif
#if !CORECLR
        static
#endif
        private unsafe string Ctor(char* ptr)
        {
            if (ptr == null)
                return Empty;

            int count = wcslen(ptr);
            if (count == 0)
                return Empty;

            string result = FastAllocateString(count);
            fixed (char* dest = &result._firstChar)
                wstrcpy(dest, ptr, count);
            return result;
        }

        [CLSCompliant(false)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern unsafe String(char* value, int startIndex, int length);

#if PROJECTN
        [DependencyReductionRoot]
#endif
#if !CORECLR
        static
#endif
        private unsafe string Ctor(char* ptr, int startIndex, int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_NegativeLength);

            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_StartIndex);

            char* pStart = ptr + startIndex;

            // overflow check
            if (pStart < ptr)
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_PartialWCHAR);

            if (length == 0)
                return Empty;

            if (ptr == null)
                throw new ArgumentOutOfRangeException(nameof(ptr), SR.ArgumentOutOfRange_PartialWCHAR);

            string result = FastAllocateString(length);
            fixed (char* dest = &result._firstChar)
                wstrcpy(dest, pStart, length);
            return result;
        }

        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern unsafe String(sbyte* value);

#if PROJECTN
        [DependencyReductionRoot]
#endif
#if !CORECLR
        static
#endif
        private unsafe string Ctor(sbyte* value)
        {
            byte* pb = (byte*)value;
            if (pb == null)
                return Empty;

            int numBytes = strlen((byte*)value);

            return CreateStringForSByteConstructor(pb, numBytes);
        }

        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern unsafe String(sbyte* value, int startIndex, int length);

#if PROJECTN
        [DependencyReductionRoot]
#endif
#if !CORECLR
        static
#endif
        private unsafe string Ctor(sbyte* value, int startIndex, int length)
        {
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_StartIndex);

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_NegativeLength);

            if (value == null)
            {
                if (length == 0)
                    return Empty;

                throw new ArgumentNullException(nameof(value));
            }

            byte* pStart = (byte*)(value + startIndex);

            // overflow check
            if (pStart < value)
                throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_PartialWCHAR);

            return CreateStringForSByteConstructor(pStart, length);
        }

        // Encoder for String..ctor(sbyte*) and String..ctor(sbyte*, int, int)
        private static unsafe string CreateStringForSByteConstructor(byte *pb, int numBytes)
        {
            Debug.Assert(numBytes >= 0);
            Debug.Assert(pb <= (pb + numBytes));

            if (numBytes == 0)
                return Empty;

#if PLATFORM_WINDOWS
            int numCharsRequired = Interop.Kernel32.MultiByteToWideChar(Interop.Kernel32.CP_ACP, Interop.Kernel32.MB_PRECOMPOSED, pb, numBytes, (char*)null, 0);
            if (numCharsRequired == 0)
                throw new ArgumentException(SR.Arg_InvalidANSIString);

            string newString = FastAllocateString(numCharsRequired);
            fixed (char *pFirstChar = &newString._firstChar)
            {
                numCharsRequired = Interop.Kernel32.MultiByteToWideChar(Interop.Kernel32.CP_ACP, Interop.Kernel32.MB_PRECOMPOSED, pb, numBytes, pFirstChar, numCharsRequired);
            }
            if (numCharsRequired == 0)
                throw new ArgumentException(SR.Arg_InvalidANSIString);
            return newString;
#else
            return Encoding.UTF8.GetString(pb, numBytes);
#endif
        }

        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern unsafe String(sbyte* value, int startIndex, int length, Encoding enc);

#if PROJECTN
        [DependencyReductionRoot]
#endif
#if !CORECLR
        static
#endif
        private unsafe string Ctor(sbyte* value, int startIndex, int length, Encoding? enc)
        {
            if (enc == null)
                return new string(value, startIndex, length);

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_StartIndex);

            if (value == null)
            {
                if (length == 0)
                    return Empty;

                throw new ArgumentNullException(nameof(value));
            }

            byte* pStart = (byte*)(value + startIndex);

            // overflow check
            if (pStart < value)
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_PartialWCHAR);

            return enc.GetString(new ReadOnlySpan<byte>(pStart, length));
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern String(char c, int count);

#if PROJECTN
        [DependencyReductionRoot]
#endif
#if !CORECLR
        static
#endif
        private string Ctor(char c, int count)
        {
            if (count <= 0)
            {
                if (count == 0)
                    return Empty;
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NegativeCount);
            }

            string result = FastAllocateString(count);

            if (c != '\0') // Fast path null char string
            {
                unsafe
                {
                    fixed (char* dest = &result._firstChar)
                    {
                        uint cc = (uint)((c << 16) | c);
                        uint* dmem = (uint*)dest;
                        if (count >= 4)
                        {
                            count -= 4;
                            do
                            {
                                dmem[0] = cc;
                                dmem[1] = cc;
                                dmem += 2;
                                count -= 4;
                            } while (count >= 0);
                        }
                        if ((count & 2) != 0)
                        {
                            *dmem = cc;
                            dmem++;
                        }
                        if ((count & 1) != 0)
                            ((char*)dmem)[0] = c;
                    }
                }
            }
            return result;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern String(ReadOnlySpan<char> value);

#if PROJECTN
        [DependencyReductionRoot]
#endif
#if !CORECLR
        static
#endif
        private unsafe string Ctor(ReadOnlySpan<char> value)
        {
            if (value.Length == 0)
                return Empty;

            string result = FastAllocateString(value.Length);
            Buffer.Memmove(ref result._firstChar, ref MemoryMarshal.GetReference(value), (uint)value.Length);
            return result;
        }

        public static string Create<TState>(int length, TState state, SpanAction<char, TState> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (length <= 0)
            {
                if (length == 0)
                    return Empty;
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            string result = FastAllocateString(length);
            action(new Span<char>(ref result.GetRawStringData(), length), state);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<char>(string? value) =>
            value != null ? new ReadOnlySpan<char>(ref value.GetRawStringData(), value.Length) : default;

        public object Clone()
        {
            return this;
        }

        public static unsafe string Copy(string str)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            string result = FastAllocateString(str.Length);
            fixed (char* dest = &result._firstChar, src = &str._firstChar)
                wstrcpy(dest, src, str.Length);
            return result;
        }

        // Converts a substring of this string to an array of characters.  Copies the
        // characters of this string beginning at position sourceIndex and ending at
        // sourceIndex + count - 1 to the character array buffer, beginning
        // at destinationIndex.
        //
        public unsafe void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NegativeCount);
            if (sourceIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), SR.ArgumentOutOfRange_Index);
            if (count > Length - sourceIndex)
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), SR.ArgumentOutOfRange_IndexCount);
            if (destinationIndex > destination.Length - count || destinationIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(destinationIndex), SR.ArgumentOutOfRange_IndexCount);

            fixed (char* src = &_firstChar, dest = destination)
                wstrcpy(dest + destinationIndex, src + sourceIndex, count);
        }

        // Returns the entire string as an array of characters.
        public unsafe char[] ToCharArray()
        {
            if (Length == 0)
                return Array.Empty<char>();

            char[] chars = new char[Length];
            fixed (char* src = &_firstChar, dest = &chars[0])
                wstrcpy(dest, src, Length);
            return chars;
        }

        // Returns a substring of this string as an array of characters.
        //
        public unsafe char[] ToCharArray(int startIndex, int length)
        {
            // Range check everything.
            if (startIndex < 0 || startIndex > Length || startIndex > Length - length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_Index);

            if (length <= 0)
            {
                if (length == 0)
                    return Array.Empty<char>();
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_Index);
            }

            char[] chars = new char[length];
            fixed (char* src = &_firstChar, dest = &chars[0])
                wstrcpy(dest, src + startIndex, length);
            return chars;
        }

        [NonVersionable]
        public static bool IsNullOrEmpty([NotNullWhen(false)] string? value)
        {
            // Using 0u >= (uint)value.Length rather than
            // value.Length == 0 as it will elide the bounds check to
            // the first char: value[0] if that is performed following the test
            // for the same test cost.
            // Ternary operator returning true/false prevents redundant asm generation:
            // https://github.com/dotnet/coreclr/issues/914
            return (value == null || 0u >= (uint)value.Length) ? true : false;
        }

        public static bool IsNullOrWhiteSpace([NotNullWhen(false)] string? value)
        {
            if (value == null) return true;

            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsWhiteSpace(value[i])) return false;
            }

            return true;
        }

        /// <summary>
        /// Returns a reference to the first element of the String. If the string is null, an access will throw a NullReferenceException.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [NonVersionable]
        public ref readonly char GetPinnableReference() => ref _firstChar;

        internal ref char GetRawStringData() => ref _firstChar;

        // Helper for encodings so they can talk to our buffer directly
        // stringLength must be the exact size we'll expect
        internal static unsafe string CreateStringFromEncoding(
            byte* bytes, int byteLength, Encoding encoding)
        {
            Debug.Assert(bytes != null);
            Debug.Assert(byteLength >= 0);

            // Get our string length
            int stringLength = encoding.GetCharCount(bytes, byteLength);
            Debug.Assert(stringLength >= 0, "stringLength >= 0");

            // They gave us an empty string if they needed one
            // 0 bytelength might be possible if there's something in an encoder
            if (stringLength == 0)
                return Empty;

            string s = FastAllocateString(stringLength);
            fixed (char* pTempChars = &s._firstChar)
            {
                int doubleCheck = encoding.GetChars(bytes, byteLength, pTempChars, stringLength);
                Debug.Assert(stringLength == doubleCheck,
                    "Expected encoding.GetChars to return same length as encoding.GetCharCount");
            }

            return s;
        }

        // This is only intended to be used by char.ToString.
        // It is necessary to put the code in this class instead of Char, since _firstChar is a private member.
        // Making _firstChar internal would be dangerous since it would make it much easier to break String's immutability.
        internal static string CreateFromChar(char c)
        {
            string result = FastAllocateString(1);
            result._firstChar = c;
            return result;
        }

        internal static string CreateFromChar(char c1, char c2)
        {
            string result = FastAllocateString(2);
            result._firstChar = c1;
            Unsafe.Add(ref result._firstChar, 1) = c2;
            return result;
        }

        internal static unsafe void wstrcpy(char* dmem, char* smem, int charCount)
        {
            Buffer.Memmove((byte*)dmem, (byte*)smem, ((uint)charCount) * 2);
        }


        // Returns this string.
        public override string ToString()
        {
            return this;
        }

        // Returns this string.
        public string ToString(IFormatProvider? provider)
        {
            return this;
        }

        public CharEnumerator GetEnumerator()
        {
            return new CharEnumerator(this);
        }

        IEnumerator<char> IEnumerable<char>.GetEnumerator()
        {
            return new CharEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new CharEnumerator(this);
        }

        /// <summary>
        /// Returns an enumeration of <see cref="Rune"/> from this string.
        /// </summary>
        /// <remarks>
        /// Invalid sequences will be represented in the enumeration by <see cref="Rune.ReplacementChar"/>.
        /// </remarks>
        public StringRuneEnumerator EnumerateRunes()
        {
            return new StringRuneEnumerator(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe int wcslen(char* ptr)
        {
            // IndexOf processes memory in aligned chunks, and thus it won't crash even if it accesses memory beyond the null terminator.
            int length = SpanHelpers.IndexOf(ref *ptr, '\0', int.MaxValue);
            if (length < 0)
            {
                ThrowMustBeNullTerminatedString();
            }

            return length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe int strlen(byte* ptr)
        {
            // IndexOf processes memory in aligned chunks, and thus it won't crash even if it accesses memory beyond the null terminator.
            int length = SpanHelpers.IndexOf(ref *ptr, (byte)'\0', int.MaxValue);
            if (length < 0)
            {
                ThrowMustBeNullTerminatedString();
            }

            return length;
        }

        [DoesNotReturn]
        private static void ThrowMustBeNullTerminatedString()
        {
            throw new ArgumentException(SR.Arg_MustBeNullTerminatedString);
        }

        //
        // IConvertible implementation
        //

        public TypeCode GetTypeCode()
        {
            return TypeCode.String;
        }

        bool IConvertible.ToBoolean(IFormatProvider? provider)
        {
            return Convert.ToBoolean(this, provider);
        }

        char IConvertible.ToChar(IFormatProvider? provider)
        {
            return Convert.ToChar(this, provider);
        }

        sbyte IConvertible.ToSByte(IFormatProvider? provider)
        {
            return Convert.ToSByte(this, provider);
        }

        byte IConvertible.ToByte(IFormatProvider? provider)
        {
            return Convert.ToByte(this, provider);
        }

        short IConvertible.ToInt16(IFormatProvider? provider)
        {
            return Convert.ToInt16(this, provider);
        }

        ushort IConvertible.ToUInt16(IFormatProvider? provider)
        {
            return Convert.ToUInt16(this, provider);
        }

        int IConvertible.ToInt32(IFormatProvider? provider)
        {
            return Convert.ToInt32(this, provider);
        }

        uint IConvertible.ToUInt32(IFormatProvider? provider)
        {
            return Convert.ToUInt32(this, provider);
        }

        long IConvertible.ToInt64(IFormatProvider? provider)
        {
            return Convert.ToInt64(this, provider);
        }

        ulong IConvertible.ToUInt64(IFormatProvider? provider)
        {
            return Convert.ToUInt64(this, provider);
        }

        float IConvertible.ToSingle(IFormatProvider? provider)
        {
            return Convert.ToSingle(this, provider);
        }

        double IConvertible.ToDouble(IFormatProvider? provider)
        {
            return Convert.ToDouble(this, provider);
        }

        decimal IConvertible.ToDecimal(IFormatProvider? provider)
        {
            return Convert.ToDecimal(this, provider);
        }

        DateTime IConvertible.ToDateTime(IFormatProvider? provider)
        {
            return Convert.ToDateTime(this, provider);
        }

        object IConvertible.ToType(Type type, IFormatProvider? provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }

        // Normalization Methods
        // These just wrap calls to Normalization class
        public bool IsNormalized()
        {
            return IsNormalized(NormalizationForm.FormC);
        }

        public bool IsNormalized(NormalizationForm normalizationForm)
        {
            if (this.IsAscii())
            {
                // If its ASCII && one of the 4 main forms, then its already normalized
                if (normalizationForm == NormalizationForm.FormC ||
                    normalizationForm == NormalizationForm.FormKC ||
                    normalizationForm == NormalizationForm.FormD ||
                    normalizationForm == NormalizationForm.FormKD)
                    return true;
            }
            return Normalization.IsNormalized(this, normalizationForm);
        }

        public string Normalize()
        {
            return Normalize(NormalizationForm.FormC);
        }

        public string Normalize(NormalizationForm normalizationForm)
        {
            if (this.IsAscii())
            {
                // If its ASCII && one of the 4 main forms, then its already normalized
                if (normalizationForm == NormalizationForm.FormC ||
                    normalizationForm == NormalizationForm.FormKC ||
                    normalizationForm == NormalizationForm.FormD ||
                    normalizationForm == NormalizationForm.FormKD)
                    return this;
            }
            return Normalization.Normalize(this, normalizationForm);
        }

        private unsafe bool IsAscii()
        {
            fixed (char* str = &_firstChar)
            {
                return ASCIIUtility.GetIndexOfFirstNonAsciiChar(str, (uint)Length) == (uint)Length;
            }
        }
    }
}
