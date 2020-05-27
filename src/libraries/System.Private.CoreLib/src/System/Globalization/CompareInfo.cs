// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Unicode;
using Internal.Runtime.CompilerServices;

namespace System.Globalization
{
    /// <summary>
    /// This class implements a set of methods for comparing strings.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed partial class CompareInfo : IDeserializationCallback
    {
        // Mask used to check if IndexOf()/LastIndexOf()/IsPrefix()/IsPostfix() has the right flags.
        private const CompareOptions ValidIndexMaskOffFlags =
            ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace |
              CompareOptions.IgnoreWidth | CompareOptions.IgnoreKanaType);

        // Mask used to check if Compare() / GetHashCode(string) / GetSortKey has the right flags.
        private const CompareOptions ValidCompareMaskOffFlags =
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
        private IntPtr _sortHandle;

        [NonSerialized]
        private string _sortName; // The name that defines our behavior

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

        public static bool IsSortable(char ch)
        {
            return IsSortable(MemoryMarshal.CreateReadOnlySpan(ref ch, 1));
        }

        public static bool IsSortable(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return IsSortable(text.AsSpan());
        }

        /// <summary>
        /// Indicates whether a specified Unicode string is sortable.
        /// </summary>
        /// <param name="text">A string of zero or more Unicode characters.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="text"/> is non-empty and contains
        /// only sortable Unicode characters; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsSortable(ReadOnlySpan<char> text)
        {
            if (text.Length == 0)
            {
                return false;
            }

            if (GlobalizationMode.Invariant)
            {
                return true; // all chars are sortable in invariant mode
            }

            return (GlobalizationMode.UseNls) ? NlsIsSortable(text) : IcuIsSortable(text);
        }

        /// <summary>
        /// Indicates whether a specified <see cref="Rune"/> is sortable.
        /// </summary>
        /// <param name="value">A Unicode scalar value.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="value"/> is a sortable Unicode scalar
        /// value; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsSortable(Rune value)
        {
            Span<char> valueAsUtf16 = stackalloc char[Rune.MaxUtf16CharsPerRune];
            int charCount = value.EncodeToUtf16(valueAsUtf16);
            return IsSortable(valueAsUtf16.Slice(0, charCount));
        }

        [MemberNotNull(nameof(_sortName))]
        private void InitSort(CultureInfo culture)
        {
            _sortName = culture.SortName;

            if (GlobalizationMode.UseNls)
            {
                NlsInitSortHandle();
            }
            else
            {
                IcuInitSortHandle();
            }
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx)
        {
            // this becomes null for a brief moment before deserialization
            // after serialization is finished it is never null.
            m_name = null!;
        }

        void IDeserializationCallback.OnDeserialization(object? sender)
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
        public string Name
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
        public int Compare(string? string1, string? string2)
        {
            return Compare(string1, string2, CompareOptions.None);
        }

        public int Compare(string? string1, string? string2, CompareOptions options)
        {
            int retVal;

            // Our paradigm is that null sorts less than any other string and
            // that two nulls sort as equal.

            if (string1 == null)
            {
                retVal = (string2 == null) ? 0 : -1;
                goto CheckOptionsAndReturn;
            }
            if (string2 == null)
            {
                retVal = 1;
                goto CheckOptionsAndReturn;
            }

            return Compare(string1.AsSpan(), string2.AsSpan(), options);

        CheckOptionsAndReturn:

            // If we're short-circuiting the globalization logic, we still need to check that
            // the provided options were valid.

            CheckCompareOptionsForCompare(options);
            return retVal;
        }

        internal int CompareOptionIgnoreCase(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2)
        {
            return GlobalizationMode.Invariant ?
                CompareOrdinalIgnoreCase(string1, string2) :
                CompareStringCore(string1, string2, CompareOptions.IgnoreCase);
        }

        /// <summary>
        /// Compares the specified regions of the two strings with the given
        /// options.
        /// Returns 0 if the two strings are equal, a number less than 0 if
        /// string1 is less than string2, and a number greater than 0 if
        /// string1 is greater than string2.
        /// </summary>
        public int Compare(string? string1, int offset1, int length1, string? string2, int offset2, int length2)
        {
            return Compare(string1, offset1, length1, string2, offset2, length2, CompareOptions.None);
        }

        public int Compare(string? string1, int offset1, string? string2, int offset2, CompareOptions options)
        {
            return Compare(string1, offset1, string1 == null ? 0 : string1.Length - offset1,
                           string2, offset2, string2 == null ? 0 : string2.Length - offset2, options);
        }

        public int Compare(string? string1, int offset1, string? string2, int offset2)
        {
            return Compare(string1, offset1, string2, offset2, CompareOptions.None);
        }

        public int Compare(string? string1, int offset1, int length1, string? string2, int offset2, int length2, CompareOptions options)
        {
            ReadOnlySpan<char> span1 = default;
            ReadOnlySpan<char> span2 = default;

            if (string1 == null)
            {
                if (offset1 != 0 || length1 != 0)
                {
                    goto BoundsCheckError;
                }
            }
            else if (!string1.TryGetSpan(offset1, length1, out span1))
            {
                goto BoundsCheckError;
            }

            if (string2 == null)
            {
                if (offset2 != 0 || length2 != 0)
                {
                    goto BoundsCheckError;
                }
            }
            else if (!string2.TryGetSpan(offset2, length2, out span2))
            {
                goto BoundsCheckError;
            }

            // At this point both string1 and string2 have been bounds-checked.

            int retVal;

            // Our paradigm is that null sorts less than any other string and
            // that two nulls sort as equal.

            if (string1 == null)
            {
                retVal = (string2 == null) ? 0 : -1;
                goto CheckOptionsAndReturn;
            }
            if (string2 == null)
            {
                retVal = 1;
                goto CheckOptionsAndReturn;
            }

            // At this point we know both string1 and string2 weren't null,
            // though they may have been empty.

            Debug.Assert(!Unsafe.IsNullRef(ref MemoryMarshal.GetReference(span1)));
            Debug.Assert(!Unsafe.IsNullRef(ref MemoryMarshal.GetReference(span2)));

            return Compare(span1, span2, options);

        CheckOptionsAndReturn:

            // If we're short-circuiting the globalization logic, we still need to check that
            // the provided options were valid.

            CheckCompareOptionsForCompare(options);
            return retVal;

        BoundsCheckError:

            // We know a bounds check error occurred. Now we just need to figure
            // out the correct error message to surface.

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

            Debug.Assert(offset2 > (string2 == null ? 0 : string2.Length) - length2);
            throw new ArgumentOutOfRangeException(nameof(string2), SR.ArgumentOutOfRange_OffsetLength);
        }

        /// <summary>
        /// Compares two strings.
        /// </summary>
        /// <param name="string1">The first string to compare.</param>
        /// <param name="string2">The second string to compare.</param>
        /// <param name="options">The <see cref="CompareOptions"/> to use during the comparison.</param>
        /// <returns>
        /// Zero if <paramref name="string1"/> and <paramref name="string2"/> are equal;
        /// or a negative value if <paramref name="string1"/> sorts before <paramref name="string2"/>;
        /// or a positive value if <paramref name="string1"/> sorts after <paramref name="string2"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="options"/> contains an unsupported combination of flags.
        /// </exception>
        public int Compare(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, CompareOptions options = CompareOptions.None)
        {
            if (string1 == string2) // referential equality + length
            {
                CheckCompareOptionsForCompare(options);
                return 0;
            }

            if ((options & ValidCompareMaskOffFlags) == 0)
            {
                // Common case: caller is attempting to perform linguistic comparison.
                // Pass the flags down to NLS or ICU unless we're running in invariant
                // mode, at which point we normalize the flags to Orginal[IgnoreCase].

                if (!GlobalizationMode.Invariant)
                {
                    return CompareStringCore(string1, string2, options);
                }
                else if ((options & CompareOptions.IgnoreCase) == 0)
                {
                    goto ReturnOrdinal;
                }
                else
                {
                    goto ReturnOrdinalIgnoreCase;
                }
            }
            else
            {
                // Less common case: caller is attempting to perform non-linguistic comparison,
                // or an invalid combination of flags was supplied.

                if (options == CompareOptions.Ordinal)
                {
                    goto ReturnOrdinal;
                }
                else if (options == CompareOptions.OrdinalIgnoreCase)
                {
                    goto ReturnOrdinalIgnoreCase;
                }
                else
                {
                    ThrowCompareOptionsCheckFailed(options);
                }
            }

        ReturnOrdinal:
            return string1.SequenceCompareTo(string2);

        ReturnOrdinalIgnoreCase:
            return CompareOrdinalIgnoreCase(string1, string2);
        }

        // Checks that 'CompareOptions' is valid for a call to Compare, throwing the appropriate
        // exception if the check fails.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        private static void CheckCompareOptionsForCompare(CompareOptions options)
        {
            // Any combination of defined CompareOptions flags is valid, except for
            // Ordinal and OrdinalIgnoreCase, which may only be used in isolation.

            if ((options & ValidCompareMaskOffFlags) != 0)
            {
                if (options != CompareOptions.Ordinal && options != CompareOptions.OrdinalIgnoreCase)
                {
                    ThrowCompareOptionsCheckFailed(options);
                }
            }
        }

        [DoesNotReturn]
        [StackTraceHidden]
        private static void ThrowCompareOptionsCheckFailed(CompareOptions options)
        {
            throw new ArgumentException(
                paramName: nameof(options),
                message: ((options & CompareOptions.Ordinal) != 0) ? SR.Argument_CompareOptionOrdinal : SR.Argument_InvalidFlag);
        }

        private unsafe int CompareStringCore(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, CompareOptions options) =>
            GlobalizationMode.UseNls ?
                NlsCompareString(string1, string2, options) :
                IcuCompareString(string1, string2, options);

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

            return CompareStringOrdinalIgnoreCaseCore(ref charA, lengthA - range, ref charB, lengthB - range);
        }

        internal static bool EqualsOrdinalIgnoreCase(ref char charA, ref char charB, int length)
        {
            IntPtr byteOffset = IntPtr.Zero;

#if TARGET_64BIT
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
#if TARGET_64BIT
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
                // See https://github.com/dotnet/runtime/issues/4207.
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
                return CompareStringOrdinalIgnoreCaseCore(ref charA, length, ref charB, length) == 0;
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

        private static unsafe int CompareStringOrdinalIgnoreCaseCore(ref char string1, int count1, ref char string2, int count2) =>
            GlobalizationMode.UseNls ?
                NlsCompareStringOrdinalIgnoreCase(ref string1, count1, ref string2, count2) :
                IcuCompareStringOrdinalIgnoreCase(ref string1, count1, ref string2, count2);

        /// <summary>
        /// Determines whether prefix is a prefix of string.  If prefix equals
        /// string.Empty, true is returned.
        /// </summary>
        public bool IsPrefix(string source, string prefix, CompareOptions options)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }
            if (prefix == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.prefix);
            }

            return IsPrefix(source.AsSpan(), prefix.AsSpan(), options);
        }

        /// <summary>
        /// Determines whether a string starts with a specific prefix.
        /// </summary>
        /// <param name="source">The string to search within.</param>
        /// <param name="prefix">The prefix to attempt to match at the start of <paramref name="source"/>.</param>
        /// <param name="options">The <see cref="CompareOptions"/> to use during the match.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="prefix"/> occurs at the start of <paramref name="source"/>;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="options"/> contains an unsupported combination of flags.
        /// </exception>
        public bool IsPrefix(ReadOnlySpan<char> source, ReadOnlySpan<char> prefix, CompareOptions options = CompareOptions.None)
        {
            // The empty string is trivially a prefix of every other string. For compat with
            // earlier versions of the Framework we'll early-exit here before validating the
            // 'options' argument.

            if (prefix.IsEmpty)
            {
                return true;
            }

            if ((options & ValidIndexMaskOffFlags) == 0)
            {
                // Common case: caller is attempting to perform a linguistic search.
                // Pass the flags down to NLS or ICU unless we're running in invariant
                // mode, at which point we normalize the flags to Orginal[IgnoreCase].

                if (!GlobalizationMode.Invariant)
                {
                    return StartsWithCore(source, prefix, options);
                }
                else if ((options & CompareOptions.IgnoreCase) == 0)
                {
                    goto ReturnOrdinal;
                }
                else
                {
                    goto ReturnOrdinalIgnoreCase;
                }
            }
            else
            {
                // Less common case: caller is attempting to perform non-linguistic comparison,
                // or an invalid combination of flags was supplied.

                if (options == CompareOptions.Ordinal)
                {
                    goto ReturnOrdinal;
                }
                else if (options == CompareOptions.OrdinalIgnoreCase)
                {
                    goto ReturnOrdinalIgnoreCase;
                }
                else
                {
                    ThrowCompareOptionsCheckFailed(options);
                }
            }

        ReturnOrdinal:
            return source.StartsWith(prefix);

        ReturnOrdinalIgnoreCase:
            return source.StartsWithOrdinalIgnoreCase(prefix);
        }

        private unsafe bool StartsWithCore(ReadOnlySpan<char> source, ReadOnlySpan<char> prefix, CompareOptions options) =>
            GlobalizationMode.UseNls ?
                NlsStartsWith(source, prefix, options) :
                IcuStartsWith(source, prefix, options);

        public bool IsPrefix(string source, string prefix)
        {
            return IsPrefix(source, prefix, CompareOptions.None);
        }

        /// <summary>
        /// Determines whether suffix is a suffix of string.  If suffix equals
        /// string.Empty, true is returned.
        /// </summary>
        public bool IsSuffix(string source, string suffix, CompareOptions options)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }
            if (suffix == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.suffix);
            }

            return IsSuffix(source.AsSpan(), suffix.AsSpan(), options);
        }

        /// <summary>
        /// Determines whether a string ends with a specific suffix.
        /// </summary>
        /// <param name="source">The string to search within.</param>
        /// <param name="suffix">The suffix to attempt to match at the end of <paramref name="source"/>.</param>
        /// <param name="options">The <see cref="CompareOptions"/> to use during the match.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="suffix"/> occurs at the end of <paramref name="source"/>;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="options"/> contains an unsupported combination of flags.
        /// </exception>
        public bool IsSuffix(ReadOnlySpan<char> source, ReadOnlySpan<char> suffix, CompareOptions options = CompareOptions.None)
        {
            // The empty string is trivially a suffix of every other string. For compat with
            // earlier versions of the Framework we'll early-exit here before validating the
            // 'options' argument.

            if (suffix.IsEmpty)
            {
                return true;
            }

            if ((options & ValidIndexMaskOffFlags) == 0)
            {
                // Common case: caller is attempting to perform a linguistic search.
                // Pass the flags down to NLS or ICU unless we're running in invariant
                // mode, at which point we normalize the flags to Orginal[IgnoreCase].

                if (!GlobalizationMode.Invariant)
                {
                    return EndsWithCore(source, suffix, options);
                }
                else if ((options & CompareOptions.IgnoreCase) == 0)
                {
                    goto ReturnOrdinal;
                }
                else
                {
                    goto ReturnOrdinalIgnoreCase;
                }
            }
            else
            {
                // Less common case: caller is attempting to perform non-linguistic comparison,
                // or an invalid combination of flags was supplied.

                if (options == CompareOptions.Ordinal)
                {
                    goto ReturnOrdinal;
                }
                else if (options == CompareOptions.OrdinalIgnoreCase)
                {
                    goto ReturnOrdinalIgnoreCase;
                }
                else
                {
                    ThrowCompareOptionsCheckFailed(options);
                }
            }

        ReturnOrdinal:
            return source.EndsWith(suffix);

        ReturnOrdinalIgnoreCase:
            return source.EndsWithOrdinalIgnoreCase(suffix);
        }

        public bool IsSuffix(string source, string suffix)
        {
            return IsSuffix(source, suffix, CompareOptions.None);
        }

        private unsafe bool EndsWithCore(ReadOnlySpan<char> source, ReadOnlySpan<char> suffix, CompareOptions options) =>
            GlobalizationMode.UseNls ?
                NlsEndsWith(source, suffix, options) :
                IcuEndsWith(source, suffix, options);

        /// <summary>
        /// Returns the first index where value is found in string.  The
        /// search starts from startIndex and ends at endIndex.  Returns -1 if
        /// the specified value is not found.  If value equals string.Empty,
        /// startIndex is returned.  Throws IndexOutOfRange if startIndex or
        /// endIndex is less than zero or greater than the length of string.
        /// Throws ArgumentException if value (as a string) is null.
        /// </summary>
        public int IndexOf(string source, char value)
        {
            return IndexOf(source, value, CompareOptions.None);
        }

        public int IndexOf(string source, string value)
        {
            return IndexOf(source, value, CompareOptions.None);
        }

        public int IndexOf(string source, char value, CompareOptions options)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return IndexOf(source, MemoryMarshal.CreateReadOnlySpan(ref value, 1), options);
        }

        public int IndexOf(string source, string value, CompareOptions options)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }
            if (value == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            return IndexOf(source.AsSpan(), value.AsSpan(), options);
        }

        public int IndexOf(string source, char value, int startIndex)
        {
            return IndexOf(source, value, startIndex, CompareOptions.None);
        }

        public int IndexOf(string source, string value, int startIndex)
        {
            return IndexOf(source, value, startIndex, CompareOptions.None);
        }

        public int IndexOf(string source, char value, int startIndex, CompareOptions options)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return IndexOf(source, value, startIndex, source.Length - startIndex, options);

        }

        public int IndexOf(string source, string value, int startIndex, CompareOptions options)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return IndexOf(source, value, startIndex, source.Length - startIndex, options);
        }

        public int IndexOf(string source, char value, int startIndex, int count)
        {
            return IndexOf(source, value, startIndex, count, CompareOptions.None);
        }

        public int IndexOf(string source, string value, int startIndex, int count)
        {
            return IndexOf(source, value, startIndex, count, CompareOptions.None);
        }

        public unsafe int IndexOf(string source, char value, int startIndex, int count, CompareOptions options)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (!source.TryGetSpan(startIndex, count, out ReadOnlySpan<char> sourceSpan))
            {
                // Bounds check failed - figure out exactly what went wrong so that we can
                // surface the correct argument exception.

                if ((uint)startIndex > (uint)source.Length)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
                }
                else
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_Count);
                }
            }

            int result = IndexOf(sourceSpan, MemoryMarshal.CreateReadOnlySpan(ref value, 1), options);
            if (result >= 0)
            {
                result += startIndex;
            }
            return result;
        }

        public unsafe int IndexOf(string source, string value, int startIndex, int count, CompareOptions options)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }
            if (value == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            if (!source.TryGetSpan(startIndex, count, out ReadOnlySpan<char> sourceSpan))
            {
                // Bounds check failed - figure out exactly what went wrong so that we can
                // surface the correct argument exception.

                if ((uint)startIndex > (uint)source.Length)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
                }
                else
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_Count);
                }
            }

            int result = IndexOf(sourceSpan, value, options);
            if (result >= 0)
            {
                result += startIndex;
            }
            return result;
        }

        /// <summary>
        /// Searches for the first occurrence of a substring within a source string.
        /// </summary>
        /// <param name="source">The string to search within.</param>
        /// <param name="value">The substring to locate within <paramref name="source"/>.</param>
        /// <param name="options">The <see cref="CompareOptions"/> to use during the search.</param>
        /// <returns>
        /// The zero-based index into <paramref name="source"/> where the substring <paramref name="value"/>
        /// first appears; or -1 if <paramref name="value"/> cannot be found within <paramref name="source"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="options"/> contains an unsupported combination of flags.
        /// </exception>
        public unsafe int IndexOf(ReadOnlySpan<char> source, ReadOnlySpan<char> value, CompareOptions options = CompareOptions.None)
        {
            if ((options & ValidIndexMaskOffFlags) == 0)
            {
                // Common case: caller is attempting to perform a linguistic search.
                // Pass the flags down to NLS or ICU unless we're running in invariant
                // mode, at which point we normalize the flags to Orginal[IgnoreCase].

                if (!GlobalizationMode.Invariant)
                {
                    if (value.IsEmpty)
                    {
                        return 0; // Empty target string trivially occurs at index 0 of every search space.
                    }
                    else
                    {
                        return IndexOfCore(source, value, options, null /* matchLengthPtr */, fromBeginning: true);
                    }
                }
                else if ((options & CompareOptions.IgnoreCase) == 0)
                {
                    goto ReturnOrdinal;
                }
                else
                {
                    goto ReturnOrdinalIgnoreCase;
                }
            }
            else
            {
                // Less common case: caller is attempting to perform non-linguistic comparison,
                // or an invalid combination of flags was supplied.

                if (options == CompareOptions.Ordinal)
                {
                    goto ReturnOrdinal;
                }
                else if (options == CompareOptions.OrdinalIgnoreCase)
                {
                    goto ReturnOrdinalIgnoreCase;
                }
                else
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidFlag, ExceptionArgument.options);
                }
            }

        ReturnOrdinal:
            return source.IndexOf(value);

        ReturnOrdinalIgnoreCase:
            return IndexOfOrdinalIgnoreCase(source, value, fromBeginning: true);
        }

        /// <summary>
        /// Searches for the first occurrence of a <see cref="Rune"/> within a source string.
        /// </summary>
        /// <param name="source">The string to search within.</param>
        /// <param name="value">The <see cref="Rune"/> to locate within <paramref name="source"/>.</param>
        /// <param name="options">The <see cref="CompareOptions"/> to use during the search.</param>
        /// <returns>
        /// The zero-based index into <paramref name="source"/> where <paramref name="value"/>
        /// first appears; or -1 if <paramref name="value"/> cannot be found within <paramref name="source"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="options"/> contains an unsupported combination of flags.
        /// </exception>
        public int IndexOf(ReadOnlySpan<char> source, Rune value, CompareOptions options = CompareOptions.None)
        {
            Span<char> valueAsUtf16 = stackalloc char[Rune.MaxUtf16CharsPerRune];
            int charCount = value.EncodeToUtf16(valueAsUtf16);
            return IndexOf(source, valueAsUtf16.Slice(0, charCount), options);
        }

        private static int IndexOfOrdinalCore(ReadOnlySpan<char> source, ReadOnlySpan<char> value, bool ignoreCase, bool fromBeginning) =>
            GlobalizationMode.UseNls ?
                NlsIndexOfOrdinalCore(source, value, ignoreCase, fromBeginning) :
                IcuIndexOfOrdinalCore(source, value, ignoreCase, fromBeginning);

        internal static int IndexOfOrdinalIgnoreCase(ReadOnlySpan<char> source, ReadOnlySpan<char> value, bool fromBeginning)
        {
            if (value.IsEmpty)
            {
                // Empty target string trivially appears at all indexes of all search spaces.

                return (fromBeginning) ? 0 : source.Length;
            }

            if (value.Length > source.Length)
            {
                // A non-linguistic search compares chars directly against one another, so large
                // target strings can never be found inside small search spaces. This check also
                // handles empty 'source' spans.

                return -1;
            }

            if (GlobalizationMode.Invariant)
            {
                return InvariantIndexOf(source, value, ignoreCase: true, fromBeginning);
            }
            else
            {
                return IndexOfOrdinalCore(source, value, ignoreCase: true, fromBeginning);
            }
        }

        /// <summary>
        /// The following IndexOf overload is mainly used by String.Replace. This overload assumes the parameters are already validated
        /// and the caller is passing a valid matchLengthPtr pointer.
        /// </summary>
        internal unsafe int IndexOf(ReadOnlySpan<char> source, ReadOnlySpan<char> value, int* matchLengthPtr, CompareOptions options, bool fromBeginning)
        {
            Debug.Assert(matchLengthPtr != null);
            *matchLengthPtr = 0;

            if ((options & ValidIndexMaskOffFlags) == 0)
            {
                // Common case: caller is attempting to perform a linguistic search.
                // Pass the flags down to NLS or ICU unless we're running in invariant
                // mode, at which point we normalize the flags to Orginal[IgnoreCase].

                if (!GlobalizationMode.Invariant)
                {
                    if (value.IsEmpty)
                    {
                        // empty target substring trivially occurs at beginning / end of search space
                        return (fromBeginning) ? 0 : source.Length;
                    }
                    else
                    {
                        return IndexOfCore(source, value, options, matchLengthPtr, fromBeginning);
                    }
                }
                else if ((options & CompareOptions.IgnoreCase) == 0)
                {
                    goto ReturnOrdinal;
                }
                else
                {
                    goto ReturnOrdinalIgnoreCase;
                }
            }
            else
            {
                // Less common case: caller is attempting to perform non-linguistic comparison,
                // or an invalid combination of flags was supplied.

                if (options == CompareOptions.Ordinal)
                {
                    goto ReturnOrdinal;
                }
                else if (options == CompareOptions.OrdinalIgnoreCase)
                {
                    goto ReturnOrdinalIgnoreCase;
                }
                else
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidFlag, ExceptionArgument.options);
                }
            }

        ReturnOrdinal:
            int retVal = (fromBeginning) ? source.IndexOf(value) : source.LastIndexOf(value);
            goto OrdinalReturn;

        ReturnOrdinalIgnoreCase:
            retVal = IndexOfOrdinalIgnoreCase(source, value, fromBeginning);
            goto OrdinalReturn;

        OrdinalReturn:
            // Both Ordinal and OrdinalIgnoreCase match by individual code points in a non-linguistic manner.
            // Non-BMP code points will never match BMP code points, so given UTF-16 inputs the match length
            // will always be equivalent to the target string length.

            if (retVal >= 0)
            {
                *matchLengthPtr = value.Length;
            }
            return retVal;
        }

        private unsafe int IndexOfCore(ReadOnlySpan<char> source, ReadOnlySpan<char> target, CompareOptions options, int* matchLengthPtr, bool fromBeginning) =>
            GlobalizationMode.UseNls ?
                NlsIndexOfCore(source, target, options, matchLengthPtr, fromBeginning) :
                IcuIndexOfCore(source, target, options, matchLengthPtr, fromBeginning);

        internal static int IndexOfOrdinal(string source, string value, int startIndex, int count, bool ignoreCase)
        {
            Debug.Assert(source != null);
            Debug.Assert(value != null);
            Debug.Assert((uint)startIndex <= (uint)source.Length);
            Debug.Assert((uint)count <= (uint)(source.Length - startIndex));

            // For ordinal (non-linguistic) comparisons, an empty target string is always
            // found at the beginning of the search space, and a non-empty target string
            // can never be found within an empty search space. This assumption is not
            // valid for linguistic comparisons, including InvariantCulture comparisons.

            if (value.Length == 0)
            {
                return startIndex;
            }

            if (count == 0)
            {
                return -1;
            }

            int result;

            if (!ignoreCase)
            {
                result = SpanHelpers.IndexOf(
                    ref Unsafe.Add(ref source.GetRawStringData(), startIndex),
                    count,
                    ref value.GetRawStringData(),
                    value.Length);
            }
            else if (GlobalizationMode.Invariant)
            {
                result = InvariantIndexOf(source.AsSpan(startIndex, count), value, ignoreCase, fromBeginning: true);
            }
            else
            {
                result = IndexOfOrdinalCore(source.AsSpan(startIndex, count), value, ignoreCase, fromBeginning: true);
            }

            if (result >= 0)
            {
                result += startIndex;
            }
            return result;
        }

        /// <summary>
        /// Returns the last index where value is found in string.  The
        /// search starts from startIndex and ends at endIndex.  Returns -1 if
        /// the specified value is not found.  If value equals string.Empty,
        /// endIndex is returned.  Throws IndexOutOfRange if startIndex or
        /// endIndex is less than zero or greater than the length of string.
        /// Throws ArgumentException if value (as a string) is null.
        /// </summary>
        public int LastIndexOf(string source, char value)
        {
            return LastIndexOf(source, value, CompareOptions.None);
        }

        public int LastIndexOf(string source, string value)
        {
            return LastIndexOf(source, value, CompareOptions.None);
        }

        public int LastIndexOf(string source, char value, CompareOptions options)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return LastIndexOf(source, MemoryMarshal.CreateReadOnlySpan(ref value, 1), options);
        }

        public int LastIndexOf(string source, string value, CompareOptions options)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }
            if (value == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            return LastIndexOf(source.AsSpan(), value.AsSpan(), options);
        }

        public int LastIndexOf(string source, char value, int startIndex)
        {
            return LastIndexOf(source, value, startIndex, startIndex + 1, CompareOptions.None);
        }

        public int LastIndexOf(string source, string value, int startIndex)
        {
            return LastIndexOf(source, value, startIndex, startIndex + 1, CompareOptions.None);
        }

        public int LastIndexOf(string source, char value, int startIndex, CompareOptions options)
        {
            return LastIndexOf(source, value, startIndex, startIndex + 1, options);
        }

        public int LastIndexOf(string source, string value, int startIndex, CompareOptions options)
        {
            return LastIndexOf(source, value, startIndex, startIndex + 1, options);
        }

        public int LastIndexOf(string source, char value, int startIndex, int count)
        {
            return LastIndexOf(source, value, startIndex, count, CompareOptions.None);
        }

        public int LastIndexOf(string source, string value, int startIndex, int count)
        {
            return LastIndexOf(source, value, startIndex, count, CompareOptions.None);
        }

        public int LastIndexOf(string source, char value, int startIndex, int count, CompareOptions options)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

        TryAgain:

            // Previous versions of the Framework special-cased empty 'source' to allow startIndex = -1 or startIndex = 0,
            // ignoring 'count' and short-circuiting the entire operation. We'll silently fix up the 'count' parameter
            // if this occurs.
            //
            // See the comments just before string.IndexOf(string) for more information on how these computations are
            // performed.

            if ((uint)startIndex >= (uint)source.Length)
            {
                if (startIndex == -1 && source.Length == 0)
                {
                    count = 0; // normalize
                }
                else if (startIndex == source.Length)
                {
                    // The caller likely had an off-by-one error when invoking the API. The Framework has historically
                    // allowed for this and tried to fix up the parameters, so we'll continue to do so for compat.

                    startIndex--;
                    if (count > 0)
                    {
                        count--;
                    }

                    goto TryAgain; // guaranteed never to loop more than once
                }
                else
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
                }
            }

            startIndex = startIndex - count + 1; // this will be the actual index where we begin our search

            if (!source.TryGetSpan(startIndex, count, out ReadOnlySpan<char> sourceSpan))
            {
                ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
            }

            int retVal = LastIndexOf(sourceSpan, MemoryMarshal.CreateReadOnlySpan(ref value, 1), options);
            if (retVal >= 0)
            {
                retVal += startIndex;
            }
            return retVal;
        }

        public int LastIndexOf(string source, string value, int startIndex, int count, CompareOptions options)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }
            if (value == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

        TryAgain:

            // Previous versions of the Framework special-cased empty 'source' to allow startIndex = -1 or startIndex = 0,
            // ignoring 'count' and short-circuiting the entire operation. We'll silently fix up the 'count' parameter
            // if this occurs.
            //
            // See the comments just before string.IndexOf(string) for more information on how these computations are
            // performed.

            if ((uint)startIndex >= (uint)source.Length)
            {
                if (startIndex == -1 && source.Length == 0)
                {
                    count = 0; // normalize
                }
                else if (startIndex == source.Length)
                {
                    // The caller likely had an off-by-one error when invoking the API. The Framework has historically
                    // allowed for this and tried to fix up the parameters, so we'll continue to do so for compat.

                    startIndex--;
                    if (count > 0)
                    {
                        count--;
                    }

                    goto TryAgain; // guaranteed never to loop more than once
                }
                else
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
                }
            }

            startIndex = startIndex - count + 1; // this will be the actual index where we begin our search

            if (!source.TryGetSpan(startIndex, count, out ReadOnlySpan<char> sourceSpan))
            {
                ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
            }

            int retVal = LastIndexOf(sourceSpan, value, options);
            if (retVal >= 0)
            {
                retVal += startIndex;
            }
            return retVal;
        }

        /// <summary>
        /// Searches for the last occurrence of a substring within a source string.
        /// </summary>
        /// <param name="source">The string to search within.</param>
        /// <param name="value">The substring to locate within <paramref name="source"/>.</param>
        /// <param name="options">The <see cref="CompareOptions"/> to use during the search.</param>
        /// <returns>
        /// The zero-based index into <paramref name="source"/> where the substring <paramref name="value"/>
        /// last appears; or -1 if <paramref name="value"/> cannot be found within <paramref name="source"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="options"/> contains an unsupported combination of flags.
        /// </exception>
        public unsafe int LastIndexOf(ReadOnlySpan<char> source, ReadOnlySpan<char> value, CompareOptions options = CompareOptions.None)
        {
            if ((options & ValidIndexMaskOffFlags) == 0)
            {
                // Common case: caller is attempting to perform a linguistic search.
                // Pass the flags down to NLS or ICU unless we're running in invariant
                // mode, at which point we normalize the flags to Orginal[IgnoreCase].

                if (!GlobalizationMode.Invariant)
                {
                    if (value.IsEmpty)
                    {
                        return source.Length; // Empty target string trivially occurs at the last index of every search space.
                    }
                    else
                    {
                        return IndexOfCore(source, value, options, null /* matchLengthPtr */, fromBeginning: false);
                    }
                }
                else if ((options & CompareOptions.IgnoreCase) == 0)
                {
                    goto ReturnOrdinal;
                }
                else
                {
                    goto ReturnOrdinalIgnoreCase;
                }
            }
            else
            {
                // Less common case: caller is attempting to perform non-linguistic comparison,
                // or an invalid combination of flags was supplied.

                if (options == CompareOptions.Ordinal)
                {
                    goto ReturnOrdinal;
                }
                else if (options == CompareOptions.OrdinalIgnoreCase)
                {
                    goto ReturnOrdinalIgnoreCase;
                }
                else
                {
                    throw new ArgumentException(
                        paramName: nameof(options),
                        message: SR.Argument_InvalidFlag);
                }
            }

        ReturnOrdinal:
            return source.LastIndexOf(value);

        ReturnOrdinalIgnoreCase:
            return IndexOfOrdinalIgnoreCase(source, value, fromBeginning: false);
        }

        /// <summary>
        /// Searches for the last occurrence of a <see cref="Rune"/> within a source string.
        /// </summary>
        /// <param name="source">The string to search within.</param>
        /// <param name="value">The <see cref="Rune"/> to locate within <paramref name="source"/>.</param>
        /// <param name="options">The <see cref="CompareOptions"/> to use during the search.</param>
        /// <returns>
        /// The zero-based index into <paramref name="source"/> where <paramref name="value"/>
        /// last appears; or -1 if <paramref name="value"/> cannot be found within <paramref name="source"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="options"/> contains an unsupported combination of flags.
        /// </exception>
        public unsafe int LastIndexOf(ReadOnlySpan<char> source, Rune value, CompareOptions options = CompareOptions.None)
        {
            Span<char> valueAsUtf16 = stackalloc char[Rune.MaxUtf16CharsPerRune];
            int charCount = value.EncodeToUtf16(valueAsUtf16);
            return LastIndexOf(source, valueAsUtf16.Slice(0, charCount), options);
        }

        internal static int LastIndexOfOrdinal(string source, string value, int startIndex, int count, bool ignoreCase)
        {
            Debug.Assert(!string.IsNullOrEmpty(source));
            Debug.Assert(value != null);

            if (GlobalizationMode.Invariant)
            {
                return InvariantLastIndexOf(source, value, startIndex, count, ignoreCase);
            }

            // For ordinal (non-linguistic) comparisons, an empty target string is always
            // found at the end of the search space, and a non-empty target string
            // can never be found within an empty search space. This assumption is not
            // valid for linguistic comparisons, including InvariantCulture comparisons.

            if (value.Length == 0)
            {
                return startIndex + 1; // startIndex is the index of the last char to include in the search space
            }

            if (count == 0)
            {
                return -1;
            }

            return GlobalizationMode.UseNls ?
                NlsLastIndexOfOrdinalCore(source, value, startIndex, count, ignoreCase) :
                IcuLastIndexOfOrdinalCore(source, value, startIndex, count, ignoreCase);
        }

        /// <summary>
        /// Gets the SortKey for the given string with the given options.
        /// </summary>
        public SortKey GetSortKey(string source, CompareOptions options)
        {
            if (GlobalizationMode.Invariant)
            {
                return InvariantCreateSortKey(source, options);
            }

            return CreateSortKeyCore(source, options);
        }

        public SortKey GetSortKey(string source)
        {
            if (GlobalizationMode.Invariant)
            {
                return InvariantCreateSortKey(source, CompareOptions.None);
            }

            return CreateSortKeyCore(source, CompareOptions.None);
        }

        private SortKey CreateSortKeyCore(string source, CompareOptions options) =>
            GlobalizationMode.UseNls ?
                NlsCreateSortKey(source, options) :
                IcuCreateSortKey(source, options);

        /// <summary>
        /// Computes a sort key over the specified input.
        /// </summary>
        /// <param name="source">The text over which to compute the sort key.</param>
        /// <param name="destination">The buffer into which to write the resulting sort key bytes.</param>
        /// <param name="options">The <see cref="CompareOptions"/> used for computing the sort key.</param>
        /// <returns>The number of bytes written to <paramref name="destination"/>.</returns>
        /// <remarks>
        /// Use <see cref="GetSortKeyLength"/> to query the required size of <paramref name="destination"/>.
        /// It is acceptable to provide a larger-than-necessary output buffer to this method.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// <paramref name="destination"/> is too small to contain the resulting sort key;
        /// or <paramref name="options"/> contains an unsupported flag;
        /// or <paramref name="source"/> cannot be processed using the desired <see cref="CompareOptions"/>
        /// under the current <see cref="CompareInfo"/>.
        /// </exception>
        public int GetSortKey(ReadOnlySpan<char> source, Span<byte> destination, CompareOptions options = CompareOptions.None)
        {
            if ((options & ValidCompareMaskOffFlags) != 0)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidFlag, ExceptionArgument.options);
            }

            if (GlobalizationMode.Invariant)
            {
                return InvariantGetSortKey(source, destination, options);
            }
            else
            {
                return GetSortKeyCore(source, destination, options);
            }
        }

        private int GetSortKeyCore(ReadOnlySpan<char> source, Span<byte> destination, CompareOptions options) =>
           GlobalizationMode.UseNls ?
               NlsGetSortKey(source, destination, options) :
               IcuGetSortKey(source, destination, options);

        /// <summary>
        /// Returns the length (in bytes) of the sort key that would be produced from the specified input.
        /// </summary>
        /// <param name="source">The text over which to compute the sort key.</param>
        /// <param name="options">The <see cref="CompareOptions"/> used for computing the sort key.</param>
        /// <returns>The length (in bytes) of the sort key.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="options"/> contains an unsupported flag;
        /// or <paramref name="source"/> cannot be processed using the desired <see cref="CompareOptions"/>
        /// under the current <see cref="CompareInfo"/>.
        /// </exception>
        public int GetSortKeyLength(ReadOnlySpan<char> source, CompareOptions options = CompareOptions.None)
        {
            if ((options & ValidCompareMaskOffFlags) != 0)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidFlag, ExceptionArgument.options);
            }

            if (GlobalizationMode.Invariant)
            {
                return InvariantGetSortKeyLength(source, options);
            }
            else
            {
                return GetSortKeyLengthCore(source, options);
            }
        }

        private int GetSortKeyLengthCore(ReadOnlySpan<char> source, CompareOptions options) =>
          GlobalizationMode.UseNls ?
              NlsGetSortKeyLength(source, options) :
              IcuGetSortKeyLength(source, options);

        public override bool Equals(object? value)
        {
            return value is CompareInfo otherCompareInfo
                && Name == otherCompareInfo.Name;
        }

        public override int GetHashCode() => Name.GetHashCode();

        /// <summary>
        /// This method performs the equivalent of of creating a Sortkey for a string from CompareInfo,
        /// then generates a randomized hashcode value from the sort key.
        ///
        /// The hash code is guaranteed to be the same for string A and B where A.Equals(B) is true and both
        /// the CompareInfo and the CompareOptions are the same. If two different CompareInfo objects
        /// treat the string the same way, this implementation will treat them differently (the same way that
        /// Sortkey does at the moment).
        /// </summary>
        public int GetHashCode(string source, CompareOptions options)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return GetHashCode(source.AsSpan(), options);
        }

        public int GetHashCode(ReadOnlySpan<char> source, CompareOptions options)
        {
            if ((options & ValidCompareMaskOffFlags) == 0)
            {
                // Common case: caller is attempting to get a linguistic sort key.
                // Pass the flags down to NLS or ICU unless we're running in invariant
                // mode, at which point we normalize the flags to Orginal[IgnoreCase].

                if (!GlobalizationMode.Invariant)
                {
                    return GetHashCodeOfStringCore(source, options);
                }
                else if ((options & CompareOptions.IgnoreCase) == 0)
                {
                    goto ReturnOrdinal;
                }
                else
                {
                    goto ReturnOrdinalIgnoreCase;
                }
            }
            else
            {
                // Less common case: caller is attempting to get a non-linguistic sort key,
                // or an invalid combination of flags was supplied.

                if (options == CompareOptions.Ordinal)
                {
                    goto ReturnOrdinal;
                }
                else if (options == CompareOptions.OrdinalIgnoreCase)
                {
                    goto ReturnOrdinalIgnoreCase;
                }
                else
                {
                    ThrowCompareOptionsCheckFailed(options);
                }
            }

        ReturnOrdinal:
            return string.GetHashCode(source);

        ReturnOrdinalIgnoreCase:
            return string.GetHashCodeOrdinalIgnoreCase(source);
        }

        private unsafe int GetHashCodeOfStringCore(ReadOnlySpan<char> source, CompareOptions options) =>
            GlobalizationMode.UseNls ?
                NlsGetHashCodeOfString(source, options) :
                IcuGetHashCodeOfString(source, options);

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
                                                                        (byte)(CultureInfo.LOCALE_INVARIANT >> 24),
                                                                        (byte)((CultureInfo.LOCALE_INVARIANT & 0x00FF0000) >> 16),
                                                                        (byte)((CultureInfo.LOCALE_INVARIANT & 0x0000FF00) >> 8),
                                                                        (byte)(CultureInfo.LOCALE_INVARIANT & 0xFF)));
                    }
                    else
                    {
                        m_SortVersion = GlobalizationMode.UseNls ? NlsGetSortVersion() : IcuGetSortVersion();
                    }
                }

                return m_SortVersion;
            }
        }

        public int LCID => CultureInfo.GetCultureInfo(Name).LCID;
    }
}
