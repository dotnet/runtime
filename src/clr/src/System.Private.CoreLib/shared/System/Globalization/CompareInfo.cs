// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text.Unicode;
using Internal.Runtime.CompilerServices;

namespace System.Globalization
{
    /// <summary>
    /// This class implements a set of methods for comparing strings.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public partial class CompareInfo : IDeserializationCallback
    {
        // Mask used to check if IndexOf()/LastIndexOf()/IsPrefix()/IsPostfix() has the right flags.
        private const CompareOptions ValidIndexMaskOffFlags =
            ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace |
              CompareOptions.IgnoreWidth | CompareOptions.IgnoreKanaType);

        // Mask used to check if Compare() has the right flags.
        private const CompareOptions ValidCompareMaskOffFlags =
            ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace |
              CompareOptions.IgnoreWidth | CompareOptions.IgnoreKanaType | CompareOptions.StringSort);

        // Mask used to check if GetHashCodeOfString() has the right flags.
        private const CompareOptions ValidHashCodeOfStringMaskOffFlags =
            ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace |
              CompareOptions.IgnoreWidth | CompareOptions.IgnoreKanaType);

        // Mask used to check if we have the right flags.
        private const CompareOptions ValidSortkeyCtorMaskOffFlags =
            ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace |
              CompareOptions.IgnoreWidth | CompareOptions.IgnoreKanaType | CompareOptions.StringSort);

        // Cache the invariant CompareInfo
        internal static readonly CompareInfo Invariant = CultureInfo.InvariantCulture.CompareInfo;

        // CompareInfos have an interesting identity.  They are attached to the locale that created them,
        // ie: en-US would have an en-US sort.  For haw-US (custom), then we serialize it as haw-US.
        // The interesting part is that since haw-US doesn't have its own sort, it has to point at another
        // locale, which is what SCOMPAREINFO does.
        [OptionalField(VersionAdded = 2)]
        private string m_name;  // The name used to construct this CompareInfo. Do not rename (binary serialization)

        [NonSerialized]
        private string _sortName = null!; // The name that defines our behavior

        [OptionalField(VersionAdded = 3)]
        private SortVersion? m_SortVersion; // Do not rename (binary serialization)

        private int culture; // Do not rename (binary serialization). The fields sole purpose is to support Desktop serialization.

        internal CompareInfo(CultureInfo culture)
        {
            m_name = culture._name;
            InitSort(culture);
        }

        /// <summary>
        /// Get the CompareInfo constructed from the data table in the specified
        /// assembly for the specified culture.
        /// Warning: The assembly versioning mechanism is dead!
        /// </summary>
        public static CompareInfo GetCompareInfo(int culture, Assembly assembly)
        {
            // Parameter checking.
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }
            if (assembly != typeof(object).Module.Assembly)
            {
                throw new ArgumentException(SR.Argument_OnlyMscorlib, nameof(assembly));
            }

            return GetCompareInfo(culture);
        }

        /// <summary>
        /// Get the CompareInfo constructed from the data table in the specified
        /// assembly for the specified culture.
        /// The purpose of this method is to provide version for CompareInfo tables.
        /// </summary>
        public static CompareInfo GetCompareInfo(string name, Assembly assembly)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }
            if (assembly != typeof(object).Module.Assembly)
            {
                throw new ArgumentException(SR.Argument_OnlyMscorlib, nameof(assembly));
            }

            return GetCompareInfo(name);
        }

        /// <summary>
        /// Get the CompareInfo for the specified culture.
        /// This method is provided for ease of integration with NLS-based software.
        /// </summary>
        public static CompareInfo GetCompareInfo(int culture)
        {
            if (CultureData.IsCustomCultureId(culture))
            {
                throw new ArgumentException(SR.Argument_CustomCultureCannotBePassedByNumber, nameof(culture));
            }

            return CultureInfo.GetCultureInfo(culture).CompareInfo;
        }

        /// <summary>
        /// Get the CompareInfo for the specified culture.
        /// </summary>
        public static CompareInfo GetCompareInfo(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return CultureInfo.GetCultureInfo(name).CompareInfo;
        }

        public static unsafe bool IsSortable(char ch)
        {
            if (GlobalizationMode.Invariant)
            {
                return true;
            }

            char *pChar = &ch;
            return IsSortable(pChar, 1);
        }

        public static unsafe bool IsSortable(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (text.Length == 0)
            {
                return false;
            }

            if (GlobalizationMode.Invariant)
            {
                return true;
            }

            fixed (char *pChar = text)
            {
                return IsSortable(pChar, text.Length);
            }
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx)
        {
            // TODO-NULLABLE: this becomes null for a brief moment before deserialization
            //                after serialization is finished it is never null
            m_name = null!;
        }

        void IDeserializationCallback.OnDeserialization(object sender)
        {
            OnDeserialized();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            OnDeserialized();
        }

        private void OnDeserialized()
        {
            // If we didn't have a name, use the LCID
            if (m_name == null)
            {
                // From whidbey, didn't have a name
                m_name = CultureInfo.GetCultureInfo(culture)._name;
            }
            else
            {
                InitSort(CultureInfo.GetCultureInfo(m_name));
            }
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext ctx)
        {
            // This is merely for serialization compatibility with Whidbey/Orcas, it can go away when we don't want that compat any more.
            culture = CultureInfo.GetCultureInfo(Name).LCID; // This is the lcid of the constructing culture (still have to dereference to get target sort)
            Debug.Assert(m_name != null, "CompareInfo.OnSerializing - expected m_name to be set already");
        }

        /// <summary>
        ///  Returns the name of the culture (well actually, of the sort).
        ///  Very important for providing a non-LCID way of identifying
        ///  what the sort is.
        ///
        ///  Note that this name isn't dereferenced in case the CompareInfo is a different locale
        ///  which is consistent with the behaviors of earlier versions.  (so if you ask for a sort
        ///  and the locale's changed behavior, then you'll get changed behavior, which is like
        ///  what happens for a version update)
        /// </summary>
        public virtual string Name
        {
            get
            {
                Debug.Assert(m_name != null, "CompareInfo.Name Expected _name to be set");
                if (m_name == "zh-CHT" || m_name == "zh-CHS")
                {
                    return m_name;
                }

                return _sortName;
            }
        }

        /// <summary>
        /// Compares the two strings with the given options.  Returns 0 if the
        /// two strings are equal, a number less than 0 if string1 is less
        /// than string2, and a number greater than 0 if string1 is greater
        /// than string2.
        /// </summary>
        public virtual int Compare(string? string1, string? string2)
        {
            return Compare(string1, string2, CompareOptions.None);
        }

        public virtual int Compare(string? string1, string? string2, CompareOptions options)
        {
            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return string.Compare(string1, string2, StringComparison.OrdinalIgnoreCase);
            }

            // Verify the options before we do any real comparison.
            if ((options & CompareOptions.Ordinal) != 0)
            {
                if (options != CompareOptions.Ordinal)
                {
                    throw new ArgumentException(SR.Argument_CompareOptionOrdinal, nameof(options));
                }

                return string.CompareOrdinal(string1, string2);
            }

            if ((options & ValidCompareMaskOffFlags) != 0)
            {
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));
            }

            // Our paradigm is that null sorts less than any other string and
            // that two nulls sort as equal.
            if (string1 == null)
            {
                if (string2 == null)
                {
                    return 0;
                }
                return -1; // null < non-null
            }
            if (string2 == null)
            {
                return 1; // non-null > null
            }

            if (GlobalizationMode.Invariant)
            {
                if ((options & CompareOptions.IgnoreCase) != 0)
                {
                    return CompareOrdinalIgnoreCase(string1, string2);
                }

                return string.CompareOrdinal(string1, string2);
            }

            return CompareString(string1.AsSpan(), string2.AsSpan(), options);
        }

        // TODO https://github.com/dotnet/coreclr/issues/13827:
        // This method shouldn't be necessary, as we should be able to just use the overload
        // that takes two spans.  But due to this issue, that's adding significant overhead.
        internal int Compare(ReadOnlySpan<char> string1, string? string2, CompareOptions options)
        {
            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return CompareOrdinalIgnoreCase(string1, string2.AsSpan());
            }

            // Verify the options before we do any real comparison.
            if ((options & CompareOptions.Ordinal) != 0)
            {
                if (options != CompareOptions.Ordinal)
                {
                    throw new ArgumentException(SR.Argument_CompareOptionOrdinal, nameof(options));
                }

                return string.CompareOrdinal(string1, string2.AsSpan());
            }

            if ((options & ValidCompareMaskOffFlags) != 0)
            {
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));
            }

            // null sorts less than any other string.
            if (string2 == null)
            {
                return 1;
            }

            if (GlobalizationMode.Invariant)
            {
                return (options & CompareOptions.IgnoreCase) != 0 ?
                    CompareOrdinalIgnoreCase(string1, string2.AsSpan()) :
                    string.CompareOrdinal(string1, string2.AsSpan());
            }

            return CompareString(string1, string2, options);
        }

        internal int CompareOptionNone(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2)
        {
            // Check for empty span or span from a null string
            if (string1.Length == 0 || string2.Length == 0)
            {
                return string1.Length - string2.Length;
            }

            return GlobalizationMode.Invariant ?
                string.CompareOrdinal(string1, string2) :
                CompareString(string1, string2, CompareOptions.None);
        }

        internal int CompareOptionIgnoreCase(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2)
        {
            // Check for empty span or span from a null string
            if (string1.Length == 0 || string2.Length == 0)
            {
                return string1.Length - string2.Length;
            }

            return GlobalizationMode.Invariant ?
                CompareOrdinalIgnoreCase(string1, string2) :
                CompareString(string1, string2, CompareOptions.IgnoreCase);
        }

        /// <summary>
        /// Compares the specified regions of the two strings with the given
        /// options.
        /// Returns 0 if the two strings are equal, a number less than 0 if
        /// string1 is less than string2, and a number greater than 0 if
        /// string1 is greater than string2.
        /// </summary>
        public virtual int Compare(string? string1, int offset1, int length1, string? string2, int offset2, int length2)
        {
            return Compare(string1, offset1, length1, string2, offset2, length2, 0);
        }

        public virtual int Compare(string? string1, int offset1, string? string2, int offset2, CompareOptions options)
        {
            return Compare(string1, offset1, string1 == null ? 0 : string1.Length - offset1,
                           string2, offset2, string2 == null ? 0 : string2.Length - offset2, options);
        }

        public virtual int Compare(string? string1, int offset1, string? string2, int offset2)
        {
            return Compare(string1, offset1, string2, offset2, 0);
        }

        public virtual int Compare(string? string1, int offset1, int length1, string? string2, int offset2, int length2, CompareOptions options)
        {
            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                int result = string.Compare(string1, offset1, string2, offset2, length1 < length2 ? length1 : length2, StringComparison.OrdinalIgnoreCase);
                if ((length1 != length2) && result == 0)
                {
                    return length1 > length2 ? 1 : -1;
                }

                return result;
            }

            if (length1 < 0 || length2 < 0)
            {
                throw new ArgumentOutOfRangeException((length1 < 0) ? nameof(length1) : nameof(length2), SR.ArgumentOutOfRange_NeedPosNum);
            }
            if (offset1 < 0 || offset2 < 0)
            {
                throw new ArgumentOutOfRangeException((offset1 < 0) ? nameof(offset1) : nameof(offset2), SR.ArgumentOutOfRange_NeedPosNum);
            }
            if (offset1 > (string1 == null ? 0 : string1.Length) - length1)
            {
                throw new ArgumentOutOfRangeException(nameof(string1), SR.ArgumentOutOfRange_OffsetLength);
            }
            if (offset2 > (string2 == null ? 0 : string2.Length) - length2)
            {
                throw new ArgumentOutOfRangeException(nameof(string2), SR.ArgumentOutOfRange_OffsetLength);
            }
            if ((options & CompareOptions.Ordinal) != 0)
            {
                if (options != CompareOptions.Ordinal)
                {
                    throw new ArgumentException(SR.Argument_CompareOptionOrdinal,
                                                nameof(options));
                }
            }
            else if ((options & ValidCompareMaskOffFlags) != 0)
            {
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));
            }

            if (string1 == null)
            {
                if (string2 == null)
                {
                    return 0;
                }
                return -1;
            }
            if (string2 == null)
            {
                return 1;
            }

            ReadOnlySpan<char> span1 = string1.AsSpan(offset1, length1);
            ReadOnlySpan<char> span2 = string2.AsSpan(offset2, length2);

            if (options == CompareOptions.Ordinal)
            {
                return string.CompareOrdinal(span1, span2);
            }

            if (GlobalizationMode.Invariant)
            {
                if ((options & CompareOptions.IgnoreCase) != 0)
                {
                    return CompareOrdinalIgnoreCase(span1, span2);
                }

                return string.CompareOrdinal(span1, span2);
            }

            return CompareString(span1, span2, options);
        }

        /// <summary>
        /// CompareOrdinalIgnoreCase compare two string ordinally with ignoring the case.
        /// it assumes the strings are Ascii string till we hit non Ascii character in strA or strB and then we continue the comparison by
        /// calling the OS.
        /// </summary>
        internal static int CompareOrdinalIgnoreCase(string strA, int indexA, int lengthA, string strB, int indexB, int lengthB)
        {
            Debug.Assert(indexA + lengthA <= strA.Length);
            Debug.Assert(indexB + lengthB <= strB.Length);
            return CompareOrdinalIgnoreCase(
                ref Unsafe.Add(ref strA.GetRawStringData(), indexA),
                lengthA,
                ref Unsafe.Add(ref strB.GetRawStringData(), indexB),
                lengthB);
        }

        internal static int CompareOrdinalIgnoreCase(ReadOnlySpan<char> strA, ReadOnlySpan<char> strB)
        {
            return CompareOrdinalIgnoreCase(ref MemoryMarshal.GetReference(strA), strA.Length, ref MemoryMarshal.GetReference(strB), strB.Length);
        }

        internal static int CompareOrdinalIgnoreCase(string strA, string strB)
        {
            return CompareOrdinalIgnoreCase(ref strA.GetRawStringData(), strA.Length, ref strB.GetRawStringData(), strB.Length);
        }

        internal static int CompareOrdinalIgnoreCase(ref char strA, int lengthA, ref char strB, int lengthB)
        {
            int length = Math.Min(lengthA, lengthB);
            int range = length;

            ref char charA = ref strA;
            ref char charB = ref strB;

            // in InvariantMode we support all range and not only the ascii characters.
            char maxChar = (GlobalizationMode.Invariant ? (char)0xFFFF : (char)0x7F);

            while (length != 0 && charA <= maxChar && charB <= maxChar)
            {
                // Ordinal equals or lowercase equals if the result ends up in the a-z range
                if (charA == charB ||
                    ((charA | 0x20) == (charB | 0x20) &&
                        (uint)((charA | 0x20) - 'a') <= (uint)('z' - 'a')))
                {
                    length--;
                    charA = ref Unsafe.Add(ref charA, 1);
                    charB = ref Unsafe.Add(ref charB, 1);
                }
                else
                {
                    int currentA = charA;
                    int currentB = charB;

                    // Uppercase both chars if needed
                    if ((uint)(charA - 'a') <= 'z' - 'a')
                    {
                        currentA -= 0x20;
                    }
                    if ((uint)(charB - 'a') <= 'z' - 'a')
                    {
                        currentB -= 0x20;
                    }

                    // Return the (case-insensitive) difference between them.
                    return currentA - currentB;
                }
            }

            if (length == 0)
            {
                return lengthA - lengthB;
            }

            Debug.Assert(!GlobalizationMode.Invariant);

            range -= length;

            return CompareStringOrdinalIgnoreCase(ref charA, lengthA - range, ref charB, lengthB - range);
        }


        internal static bool EqualsOrdinalIgnoreCase(ref char charA, ref char charB, int length)
        {
            IntPtr byteOffset = IntPtr.Zero;

#if BIT64
            // Read 4 chars (64 bits) at a time from each string
            while ((uint)length >= 4)
            {
                ulong valueA = Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<char, byte>(ref Unsafe.AddByteOffset(ref charA, byteOffset)));
                ulong valueB = Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<char, byte>(ref Unsafe.AddByteOffset(ref charB, byteOffset)));

                // A 32-bit test - even with the bit-twiddling here - is more efficient than a 64-bit test.
                ulong temp = valueA | valueB;
                if (!Utf16Utility.AllCharsInUInt32AreAscii((uint)temp | (uint)(temp >> 32)))
                {
                    goto NonAscii; // one of the inputs contains non-ASCII data
                }

                // Generally, the caller has likely performed a first-pass check that the input strings
                // are likely equal. Consider a dictionary which computes the hash code of its key before
                // performing a proper deep equality check of the string contents. We want to optimize for
                // the case where the equality check is likely to succeed, which means that we want to avoid
                // branching within this loop unless we're about to exit the loop, either due to failure or
                // due to us running out of input data.

                if (!Utf16Utility.UInt64OrdinalIgnoreCaseAscii(valueA, valueB))
                {
                    return false;
                }

                byteOffset += 8;
                length -= 4;
            }
#endif

            // Read 2 chars (32 bits) at a time from each string
#if BIT64
            if ((uint)length >= 2)
#else
            while ((uint)length >= 2)
#endif
            {
                uint valueA = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<char, byte>(ref Unsafe.AddByteOffset(ref charA, byteOffset)));
                uint valueB = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<char, byte>(ref Unsafe.AddByteOffset(ref charB, byteOffset)));

                if (!Utf16Utility.AllCharsInUInt32AreAscii(valueA | valueB))
                {
                    goto NonAscii; // one of the inputs contains non-ASCII data
                }

                // Generally, the caller has likely performed a first-pass check that the input strings
                // are likely equal. Consider a dictionary which computes the hash code of its key before
                // performing a proper deep equality check of the string contents. We want to optimize for
                // the case where the equality check is likely to succeed, which means that we want to avoid
                // branching within this loop unless we're about to exit the loop, either due to failure or
                // due to us running out of input data.

                if (!Utf16Utility.UInt32OrdinalIgnoreCaseAscii(valueA, valueB))
                {
                    return false;
                }

                byteOffset += 4;
                length -= 2;
            }

            if (length != 0)
            {
                Debug.Assert(length == 1);

                uint valueA = Unsafe.AddByteOffset(ref charA, byteOffset);
                uint valueB = Unsafe.AddByteOffset(ref charB, byteOffset);

                if ((valueA | valueB) > 0x7Fu)
                {
                    goto NonAscii; // one of the inputs contains non-ASCII data
                }

                if (valueA == valueB)
                {
                    return true; // exact match
                }

                valueA |= 0x20u;
                if ((uint)(valueA - 'a') > (uint)('z' - 'a'))
                {
                    return false; // not exact match, and first input isn't in [A-Za-z]
                }

                // The ternary operator below seems redundant but helps RyuJIT generate more optimal code.
                // See https://github.com/dotnet/coreclr/issues/914.
                return (valueA == (valueB | 0x20u)) ? true : false;
            }

            Debug.Assert(length == 0);
            return true;

        NonAscii:
            // The non-ASCII case is factored out into its own helper method so that the JIT
            // doesn't need to emit a complex prolog for its caller (this method).
            return EqualsOrdinalIgnoreCaseNonAscii(ref Unsafe.AddByteOffset(ref charA, byteOffset), ref Unsafe.AddByteOffset(ref charB, byteOffset), length);
        }

        private static bool EqualsOrdinalIgnoreCaseNonAscii(ref char charA, ref char charB, int length)
        {
            if (!GlobalizationMode.Invariant)
            {
                return CompareStringOrdinalIgnoreCase(ref charA, length, ref charB, length) == 0;
            }
            else
            {
                // If we don't have localization tables to consult, we'll still perform a case-insensitive
                // check for ASCII characters, but if we see anything outside the ASCII range we'll immediately
                // fail if it doesn't have true bitwise equality.

                IntPtr byteOffset = IntPtr.Zero;
                while (length != 0)
                {
                    // Ordinal equals or lowercase equals if the result ends up in the a-z range
                    uint valueA = Unsafe.AddByteOffset(ref charA, byteOffset);
                    uint valueB = Unsafe.AddByteOffset(ref charB, byteOffset);

                    if (valueA == valueB ||
                        ((valueA | 0x20) == (valueB | 0x20) &&
                            (uint)((valueA | 0x20) - 'a') <= (uint)('z' - 'a')))
                    {
                        byteOffset += 2;
                        length--;
                    }
                    else
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Determines whether prefix is a prefix of string.  If prefix equals
        /// string.Empty, true is returned.
        /// </summary>
        public virtual bool IsPrefix(string source, string prefix, CompareOptions options)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            if (prefix.Length == 0)
            {
                return true;
            }
            if (source.Length == 0)
            {
                return false;
            }

            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return source.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }

            if (options == CompareOptions.Ordinal)
            {
                return source.StartsWith(prefix, StringComparison.Ordinal);
            }

            if ((options & ValidIndexMaskOffFlags) != 0)
            {
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));
            }

            if (GlobalizationMode.Invariant)
            {
                return source.StartsWith(prefix, (options & CompareOptions.IgnoreCase) != 0 ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
            }

            return StartsWith(source, prefix, options);
        }

        internal bool IsPrefix(ReadOnlySpan<char> source, ReadOnlySpan<char> prefix, CompareOptions options)
        {
            Debug.Assert(prefix.Length != 0);
            Debug.Assert(source.Length != 0);
            Debug.Assert((options & ValidIndexMaskOffFlags) == 0);
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            return StartsWith(source, prefix, options);
        }

        public virtual bool IsPrefix(string source, string prefix)
        {
            return IsPrefix(source, prefix, 0);
        }

        /// <summary>
        /// Determines whether suffix is a suffix of string.  If suffix equals
        /// string.Empty, true is returned.
        /// </summary>
        public virtual bool IsSuffix(string source, string suffix, CompareOptions options)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (suffix == null)
            {
                throw new ArgumentNullException(nameof(suffix));
            }

            if (suffix.Length == 0)
            {
                return true;
            }
            if (source.Length == 0)
            {
                return false;
            }

            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return source.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
            }

            if (options == CompareOptions.Ordinal)
            {
                return source.EndsWith(suffix, StringComparison.Ordinal);
            }

            if ((options & ValidIndexMaskOffFlags) != 0)
            {
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));
            }

            if (GlobalizationMode.Invariant)
            {
                return source.EndsWith(suffix, (options & CompareOptions.IgnoreCase) != 0 ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
            }

            return EndsWith(source, suffix, options);
        }

        internal bool IsSuffix(ReadOnlySpan<char> source, ReadOnlySpan<char> suffix, CompareOptions options)
        {
            Debug.Assert(suffix.Length != 0);
            Debug.Assert(source.Length != 0);
            Debug.Assert((options & ValidIndexMaskOffFlags) == 0);
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            return EndsWith(source, suffix, options);
        }

        public virtual bool IsSuffix(string source, string suffix)
        {
            return IsSuffix(source, suffix, 0);
        }

        /// <summary>
        /// Returns the first index where value is found in string.  The
        /// search starts from startIndex and ends at endIndex.  Returns -1 if
        /// the specified value is not found.  If value equals string.Empty,
        /// startIndex is returned.  Throws IndexOutOfRange if startIndex or
        /// endIndex is less than zero or greater than the length of string.
        /// Throws ArgumentException if value is null.
        /// </summary>
        public virtual int IndexOf(string source, char value)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return IndexOf(source, value, 0, source.Length, CompareOptions.None);
        }

        public virtual int IndexOf(string source, string value)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return IndexOf(source, value, 0, source.Length, CompareOptions.None);
        }

        public virtual int IndexOf(string source, char value, CompareOptions options)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return IndexOf(source, value, 0, source.Length, options);
        }

        public virtual int IndexOf(string source, string value, CompareOptions options)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return IndexOf(source, value, 0, source.Length, options);
        }

        public virtual int IndexOf(string source, char value, int startIndex)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return IndexOf(source, value, startIndex, source.Length - startIndex, CompareOptions.None);
        }

        public virtual int IndexOf(string source, string value, int startIndex)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return IndexOf(source, value, startIndex, source.Length - startIndex, CompareOptions.None);
        }

        public virtual int IndexOf(string source, char value, int startIndex, CompareOptions options)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return IndexOf(source, value, startIndex, source.Length - startIndex, options);
        }

        public virtual int IndexOf(string source, string value, int startIndex, CompareOptions options)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return IndexOf(source, value, startIndex, source.Length - startIndex, options);
        }

        public virtual int IndexOf(string source, char value, int startIndex, int count)
        {
            return IndexOf(source, value, startIndex, count, CompareOptions.None);
        }


        public virtual int IndexOf(string source, string value, int startIndex, int count)
        {
            return IndexOf(source, value, startIndex, count, CompareOptions.None);
        }

        public unsafe virtual int IndexOf(string source, char value, int startIndex, int count, CompareOptions options)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (startIndex < 0 || startIndex > source.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_Index);
            }
            if (count < 0 || startIndex > source.Length - count)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_Count);
            }

            if (source.Length == 0)
            {
                return -1;
            }

            // Validate CompareOptions
            // Ordinal can't be selected with other flags
            if ((options & ValidIndexMaskOffFlags) != 0 && (options != CompareOptions.Ordinal && options != CompareOptions.OrdinalIgnoreCase))
            {
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));
            }

            return IndexOf(source, char.ToString(value), startIndex, count, options, null);
        }

        public unsafe virtual int IndexOf(string source, string value, int startIndex, int count, CompareOptions options)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (startIndex > source.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_Index);
            }

            // In Everett we used to return -1 for empty string even if startIndex is negative number so we keeping same behavior here.
            // We return 0 if both source and value are empty strings for Everett compatibility too.
            if (source.Length == 0)
            {
                if (value.Length == 0)
                {
                    return 0;
                }
                return -1;
            }

            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_Index);
            }

            if (count < 0 || startIndex > source.Length - count)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_Count);
            }

            // Validate CompareOptions
            // Ordinal can't be selected with other flags
            if ((options & ValidIndexMaskOffFlags) != 0 && (options != CompareOptions.Ordinal && options != CompareOptions.OrdinalIgnoreCase))
            {
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));
            }

            return IndexOf(source, value, startIndex, count, options, null);
        }

        internal int IndexOfOrdinalIgnoreCase(ReadOnlySpan<char> source, ReadOnlySpan<char> value)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!source.IsEmpty);
            Debug.Assert(!value.IsEmpty);

            return IndexOfOrdinalCore(source, value, ignoreCase: true, fromBeginning: true);
        }

        internal int LastIndexOfOrdinal(ReadOnlySpan<char> source, ReadOnlySpan<char> value, bool ignoreCase)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!source.IsEmpty);
            Debug.Assert(!value.IsEmpty);
            return IndexOfOrdinalCore(source, value, ignoreCase, fromBeginning: false);
        }

        internal unsafe int IndexOf(ReadOnlySpan<char> source, ReadOnlySpan<char> value, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!source.IsEmpty);
            Debug.Assert(!value.IsEmpty);
            return IndexOfCore(source, value, options, null, fromBeginning: true);
        }

        internal unsafe int LastIndexOf(ReadOnlySpan<char> source, ReadOnlySpan<char> value, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!source.IsEmpty);
            Debug.Assert(!value.IsEmpty);
            return IndexOfCore(source, value, options, null, fromBeginning: false);
        }

        /// <summary>
        /// The following IndexOf overload is mainly used by String.Replace. This overload assumes the parameters are already validated
        /// and the caller is passing a valid matchLengthPtr pointer.
        /// </summary>
        internal unsafe int IndexOf(string source, string value, int startIndex, int count, CompareOptions options, int* matchLengthPtr)
        {
            Debug.Assert(source != null);
            Debug.Assert(value != null);
            Debug.Assert(startIndex >= 0);

            if (matchLengthPtr != null)
            {
                *matchLengthPtr = 0;
            }

            if (value.Length == 0)
            {
                return startIndex;
            }

            if (startIndex >= source.Length)
            {
                return -1;
            }

            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                int res = IndexOfOrdinal(source, value, startIndex, count, ignoreCase: true);
                if (res >= 0 && matchLengthPtr != null)
                {
                    *matchLengthPtr = value.Length;
                }
                return res;
            }

            if (GlobalizationMode.Invariant)
            {
                int res = IndexOfOrdinal(source, value, startIndex, count, ignoreCase: (options & (CompareOptions.IgnoreCase | CompareOptions.OrdinalIgnoreCase)) != 0);
                if (res >= 0 && matchLengthPtr != null)
                {
                    *matchLengthPtr = value.Length;
                }
                return res;
            }

            if (options == CompareOptions.Ordinal)
            {
                int retValue = SpanHelpers.IndexOf(
                    ref Unsafe.Add(ref source.GetRawStringData(), startIndex),
                    count,
                    ref value.GetRawStringData(),
                    value.Length);

                if (retValue >= 0)
                {
                    retValue += startIndex;
                    if (matchLengthPtr != null)
                    {
                        *matchLengthPtr = value.Length;
                    }
                }

                return retValue;
            }
            else
            {
                return IndexOfCore(source, value, startIndex, count, options, matchLengthPtr);
            }
        }

        internal int IndexOfOrdinal(string source, string value, int startIndex, int count, bool ignoreCase)
        {
            if (!ignoreCase)
            {
                int result = SpanHelpers.IndexOf(
                    ref Unsafe.Add(ref source.GetRawStringData(), startIndex),
                    count,
                    ref value.GetRawStringData(),
                    value.Length);

                return (result >= 0 ? startIndex : 0) + result;
            }

            if (GlobalizationMode.Invariant)
            {
                return InvariantIndexOf(source, value, startIndex, count, ignoreCase);
            }

            return IndexOfOrdinalCore(source, value, startIndex, count, ignoreCase);
        }

        /// <summary>
        /// Returns the last index where value is found in string.  The
        /// search starts from startIndex and ends at endIndex.  Returns -1 if
        /// the specified value is not found.  If value equals string.Empty,
        /// endIndex is returned.  Throws IndexOutOfRange if startIndex or
        /// endIndex is less than zero or greater than the length of string.
        /// Throws ArgumentException if value is null.
        /// </summary>
        public virtual int LastIndexOf(string source, char value)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            // Can't start at negative index, so make sure we check for the length == 0 case.
            return LastIndexOf(source, value, source.Length - 1, source.Length, CompareOptions.None);
        }


        public virtual int LastIndexOf(string source, string value)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            // Can't start at negative index, so make sure we check for the length == 0 case.
            return LastIndexOf(source, value, source.Length - 1,
                source.Length, CompareOptions.None);
        }


        public virtual int LastIndexOf(string source, char value, CompareOptions options)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            // Can't start at negative index, so make sure we check for the length == 0 case.
            return LastIndexOf(source, value, source.Length - 1, source.Length, options);
        }

        public virtual int LastIndexOf(string source, string value, CompareOptions options)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            // Can't start at negative index, so make sure we check for the length == 0 case.
            return LastIndexOf(source, value, source.Length - 1, source.Length, options);
        }

        public virtual int LastIndexOf(string source, char value, int startIndex)
        {
            return LastIndexOf(source, value, startIndex, startIndex + 1, CompareOptions.None);
        }

        public virtual int LastIndexOf(string source, string value, int startIndex)
        {
            return LastIndexOf(source, value, startIndex, startIndex + 1, CompareOptions.None);
        }

        public virtual int LastIndexOf(string source, char value, int startIndex, CompareOptions options)
        {
            return LastIndexOf(source, value, startIndex, startIndex + 1, options);
        }

        public virtual int LastIndexOf(string source, string value, int startIndex, CompareOptions options)
        {
            return LastIndexOf(source, value, startIndex, startIndex + 1, options);
        }

        public virtual int LastIndexOf(string source, char value, int startIndex, int count)
        {
            return LastIndexOf(source, value, startIndex, count, CompareOptions.None);
        }

        public virtual int LastIndexOf(string source, string value, int startIndex, int count)
        {
            return LastIndexOf(source, value, startIndex, count, CompareOptions.None);
        }

        public virtual int LastIndexOf(string source, char value, int startIndex, int count, CompareOptions options)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            // Validate CompareOptions
            // Ordinal can't be selected with other flags
            if ((options & ValidIndexMaskOffFlags) != 0 &&
                (options != CompareOptions.Ordinal) &&
                (options != CompareOptions.OrdinalIgnoreCase))
            {
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));
            }

            // Special case for 0 length input strings
            if (source.Length == 0 && (startIndex == -1 || startIndex == 0))
            {
                return -1;
            }

            // Make sure we're not out of range
            if (startIndex < 0 || startIndex > source.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_Index);
            }

            // Make sure that we allow startIndex == source.Length
            if (startIndex == source.Length)
            {
                startIndex--;
                if (count > 0)
                {
                    count--;
                }
            }

            // 2nd have of this also catches when startIndex == MAXINT, so MAXINT - 0 + 1 == -1, which is < 0.
            if (count < 0 || startIndex - count + 1 < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_Count);
            }

            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return source.LastIndexOf(value.ToString(), startIndex, count, StringComparison.OrdinalIgnoreCase);
            }

            if (GlobalizationMode.Invariant)
            {
                return InvariantLastIndexOf(source, char.ToString(value), startIndex, count, (options & (CompareOptions.IgnoreCase | CompareOptions.OrdinalIgnoreCase)) != 0);
            }

            return LastIndexOfCore(source, value.ToString(), startIndex, count, options);
        }

        public virtual int LastIndexOf(string source, string value, int startIndex, int count, CompareOptions options)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            // Validate CompareOptions
            // Ordinal can't be selected with other flags
            if ((options & ValidIndexMaskOffFlags) != 0 &&
                (options != CompareOptions.Ordinal) &&
                (options != CompareOptions.OrdinalIgnoreCase))
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));

            // Special case for 0 length input strings
            if (source.Length == 0 && (startIndex == -1 || startIndex == 0))
            {
                return (value.Length == 0) ? 0 : -1;
            }

            // Make sure we're not out of range
            if (startIndex < 0 || startIndex > source.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_Index);
            }

            // Make sure that we allow startIndex == source.Length
            if (startIndex == source.Length)
            {
                startIndex--;
                if (count > 0)
                {
                    count--;
                }

                // If we are looking for nothing, just return 0
                if (value.Length == 0 && count >= 0 && startIndex - count + 1 >= 0)
                {
                    return startIndex;
                }
            }

            // 2nd half of this also catches when startIndex == MAXINT, so MAXINT - 0 + 1 == -1, which is < 0.
            if (count < 0 || startIndex - count + 1 < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_Count);
            }

            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return LastIndexOfOrdinal(source, value, startIndex, count, ignoreCase: true);
            }

            if (GlobalizationMode.Invariant)
                return InvariantLastIndexOf(source, value, startIndex, count, (options & (CompareOptions.IgnoreCase | CompareOptions.OrdinalIgnoreCase)) != 0);

            return LastIndexOfCore(source, value, startIndex, count, options);
        }

        internal int LastIndexOfOrdinal(string source, string value, int startIndex, int count, bool ignoreCase)
        {
            if (GlobalizationMode.Invariant)
            {
                return InvariantLastIndexOf(source, value, startIndex, count, ignoreCase);
            }

            return LastIndexOfOrdinalCore(source, value, startIndex, count, ignoreCase);
        }

        /// <summary>
        /// Gets the SortKey for the given string with the given options.
        /// </summary>
        public virtual SortKey GetSortKey(string source, CompareOptions options)
        {
            if (GlobalizationMode.Invariant)
            {
                return InvariantCreateSortKey(source, options);
            }

            return CreateSortKey(source, options);
        }


        public virtual SortKey GetSortKey(string source)
        {
            if (GlobalizationMode.Invariant)
            {
                return InvariantCreateSortKey(source, CompareOptions.None);
            }

            return CreateSortKey(source, CompareOptions.None);
        }

        public override bool Equals(object? value)
        {
            return value is CompareInfo otherCompareInfo
                && Name == otherCompareInfo.Name;
        }

        public override int GetHashCode() => Name.GetHashCode();

        /// <summary>
        /// This internal method allows a method that allows the equivalent of creating a Sortkey for a
        /// string from CompareInfo, and generate a hashcode value from it.  It is not very convenient
        /// to use this method as is and it creates an unnecessary Sortkey object that will be GC'ed.
        ///
        /// The hash code is guaranteed to be the same for string A and B where A.Equals(B) is true and both
        /// the CompareInfo and the CompareOptions are the same. If two different CompareInfo objects
        /// treat the string the same way, this implementation will treat them differently (the same way that
        /// Sortkey does at the moment).
        ///
        /// This method will never be made public itself, but public consumers of it could be created, e.g.:
        ///
        ///     string.GetHashCode(CultureInfo)
        ///     string.GetHashCode(CompareInfo)
        ///     string.GetHashCode(CultureInfo, CompareOptions)
        ///     string.GetHashCode(CompareInfo, CompareOptions)
        ///     etc.
        ///
        /// (the methods above that take a CultureInfo would use CultureInfo.CompareInfo)
        /// </summary>
        internal int GetHashCodeOfString(string source, CompareOptions options)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if ((options & ValidHashCodeOfStringMaskOffFlags) == 0)
            {
                // No unsupported flags are set - continue on with the regular logic
                if (GlobalizationMode.Invariant)
                {
                    return ((options & CompareOptions.IgnoreCase) != 0) ? source.GetHashCodeOrdinalIgnoreCase() : source.GetHashCode();
                }

                return GetHashCodeOfStringCore(source, options);
            }
            else if (options == CompareOptions.Ordinal)
            {
                // We allow Ordinal in isolation
                return source.GetHashCode();
            }
            else if (options == CompareOptions.OrdinalIgnoreCase)
            {
                // We allow OrdinalIgnoreCase in isolation
                return source.GetHashCodeOrdinalIgnoreCase();
            }
            else
            {
                // Unsupported combination of flags specified
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));
            }
        }

        public virtual int GetHashCode(string source, CompareOptions options)
        {
            // virtual method delegates to non-virtual method
            return GetHashCodeOfString(source, options);
        }

        public int GetHashCode(ReadOnlySpan<char> source, CompareOptions options)
        {
            if ((options & ValidHashCodeOfStringMaskOffFlags) == 0)
            {
                // No unsupported flags are set - continue on with the regular logic
                if (GlobalizationMode.Invariant)
                {
                    return ((options & CompareOptions.IgnoreCase) != 0) ? string.GetHashCodeOrdinalIgnoreCase(source) : string.GetHashCode(source);
                }

                return GetHashCodeOfStringCore(source, options);
            }
            else if (options == CompareOptions.Ordinal)
            {
                // We allow Ordinal in isolation
                return string.GetHashCode(source);
            }
            else if (options == CompareOptions.OrdinalIgnoreCase)
            {
                // We allow OrdinalIgnoreCase in isolation
                return string.GetHashCodeOrdinalIgnoreCase(source);
            }
            else
            {
                // Unsupported combination of flags specified
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));
            }
        }

        public override string ToString() => "CompareInfo - " + Name;

        public SortVersion Version
        {
            get
            {
                if (m_SortVersion == null)
                {
                    if (GlobalizationMode.Invariant)
                    {
                        m_SortVersion = new SortVersion(0, CultureInfo.LOCALE_INVARIANT, new Guid(0, 0, 0, 0, 0, 0, 0,
                                                                        (byte) (CultureInfo.LOCALE_INVARIANT >> 24),
                                                                        (byte) ((CultureInfo.LOCALE_INVARIANT  & 0x00FF0000) >> 16),
                                                                        (byte) ((CultureInfo.LOCALE_INVARIANT  & 0x0000FF00) >> 8),
                                                                        (byte) (CultureInfo.LOCALE_INVARIANT  & 0xFF)));
                    }
                    else
                    {
                        m_SortVersion = GetSortVersion();
                    }
                }

                return m_SortVersion;
            }
        }

        public int LCID => CultureInfo.GetCultureInfo(Name).LCID;
    }
}
