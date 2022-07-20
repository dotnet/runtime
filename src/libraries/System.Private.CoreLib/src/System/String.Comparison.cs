// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Unicode;

namespace System
{
    public partial class String
    {
        //
        // Search/Query methods
        //

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool EqualsHelper(string strA, string strB)
        {
            Debug.Assert(strA != null);
            Debug.Assert(strB != null);
            Debug.Assert(strA.Length == strB.Length);

            return SpanHelpers.SequenceEqual(
                    ref Unsafe.As<char, byte>(ref strA.GetRawStringData()),
                    ref Unsafe.As<char, byte>(ref strB.GetRawStringData()),
                    ((uint)strA.Length) * sizeof(char));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CompareOrdinalHelper(string strA, int indexA, int countA, string strB, int indexB, int countB)
        {
            Debug.Assert(strA != null);
            Debug.Assert(strB != null);
            Debug.Assert(indexA >= 0 && indexB >= 0);
            Debug.Assert(countA >= 0 && countB >= 0);
            Debug.Assert(indexA + countA <= strA.Length && indexB + countB <= strB.Length);

            return SpanHelpers.SequenceCompareTo(
                ref Unsafe.Add(ref strA.GetRawStringData(), (nint)(uint)indexA /* force zero-extension */), countA,
                ref Unsafe.Add(ref strB.GetRawStringData(), (nint)(uint)indexB /* force zero-extension */), countB);
        }

        internal static bool EqualsOrdinalIgnoreCase(string? strA, string? strB)
        {
            if (ReferenceEquals(strA, strB))
            {
                return true;
            }

            if (strA is null || strB is null)
            {
                return false;
            }

            if (strA.Length != strB.Length)
            {
                return false;
            }

            return EqualsOrdinalIgnoreCaseNoLengthCheck(strA, strB);
        }

        private static bool EqualsOrdinalIgnoreCaseNoLengthCheck(string strA, string strB)
        {
            Debug.Assert(strA.Length == strB.Length);

            return Ordinal.EqualsIgnoreCase(ref strA.GetRawStringData(), ref strB.GetRawStringData(), strB.Length);
        }

        private static unsafe int CompareOrdinalHelper(string strA, string strB)
        {
            Debug.Assert(strA != null);
            Debug.Assert(strB != null);

            // NOTE: This may be subject to change if eliminating the check
            // in the callers makes them small enough to be inlined
            Debug.Assert(strA._firstChar == strB._firstChar,
                "For performance reasons, callers of this method should " +
                "check/short-circuit beforehand if the first char is the same.");

            int length = Math.Min(strA.Length, strB.Length);

            fixed (char* ap = &strA._firstChar) fixed (char* bp = &strB._firstChar)
            {
                char* a = ap;
                char* b = bp;

                // Check if the second chars are different here
                // The reason we check if _firstChar is different is because
                // it's the most common case and allows us to avoid a method call
                // to here.
                // The reason we check if the second char is different is because
                // if the first two chars the same we can increment by 4 bytes,
                // leaving us word-aligned on both 32-bit (12 bytes into the string)
                // and 64-bit (16 bytes) platforms.

                // For empty strings, the second char will be null due to padding.
                // The start of the string is the type pointer + string length, which
                // takes up 8 bytes on 32-bit, 12 on x64. For empty strings the null
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
#if TARGET_64BIT
                while (length >= 12)
                {
                    if (*(long*)a != *(long*)b) goto DiffOffset0;
                    if (*(long*)(a + 4) != *(long*)(b + 4)) goto DiffOffset4;
                    if (*(long*)(a + 8) != *(long*)(b + 8)) goto DiffOffset8;
                    length -= 12; a += 12; b += 12;
                }
#else // TARGET_64BIT
                while (length >= 10)
                {
                    if (*(int*)a != *(int*)b) goto DiffOffset0;
                    if (*(int*)(a + 2) != *(int*)(b + 2)) goto DiffOffset2;
                    if (*(int*)(a + 4) != *(int*)(b + 4)) goto DiffOffset4;
                    if (*(int*)(a + 6) != *(int*)(b + 6)) goto DiffOffset6;
                    if (*(int*)(a + 8) != *(int*)(b + 8)) goto DiffOffset8;
                    length -= 10; a += 10; b += 10;
                }
#endif // TARGET_64BIT

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

#if TARGET_64BIT
            DiffOffset8: a += 4; b += 4;
            DiffOffset4: a += 4; b += 4;
#else // TARGET_64BIT
                // Use jumps instead of falling through, since
                // otherwise going to DiffOffset8 will involve
                // 8 add instructions before getting to DiffNextInt
                DiffOffset8: a += 8; b += 8; goto DiffOffset0;
                DiffOffset6: a += 6; b += 6; goto DiffOffset0;
                DiffOffset4: a += 2; b += 2;
                DiffOffset2: a += 2; b += 2;
#endif // TARGET_64BIT

            DiffOffset0:
                // If we reached here, we already see a difference in the unrolled loop above
#if TARGET_64BIT
                if (*(int*)a == *(int*)b)
                {
                    a += 2; b += 2;
                }
#endif // TARGET_64BIT

            DiffNextInt:
                if (*a != *b) return *a - *b;

                DiffOffset1:
                Debug.Assert(*(a + 1) != *(b + 1), "This char must be different if we reach here!");
                return *(a + 1) - *(b + 1);
            }
        }

        // Provides a culture-correct string comparison. StrA is compared to StrB
        // to determine whether it is lexicographically less, equal, or greater, and then returns
        // either a negative integer, 0, or a positive integer; respectively.
        //
        public static int Compare(string? strA, string? strB)
        {
            return Compare(strA, strB, StringComparison.CurrentCulture);
        }

        // Provides a culture-correct string comparison. strA is compared to strB
        // to determine whether it is lexicographically less, equal, or greater, and then a
        // negative integer, 0, or a positive integer is returned; respectively.
        // The case-sensitive option is set by ignoreCase
        //
        public static int Compare(string? strA, string? strB, bool ignoreCase)
        {
            StringComparison comparisonType = ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture;
            return Compare(strA, strB, comparisonType);
        }

        // Provides a more flexible function for string comparison. See StringComparison
        // for meaning of different comparisonType.
        public static int Compare(string? strA, string? strB, StringComparison comparisonType)
        {
            if (object.ReferenceEquals(strA, strB))
            {
                CheckStringComparison(comparisonType);
                return 0;
            }

            // They can't both be null at this point.
            if (strA == null)
            {
                CheckStringComparison(comparisonType);
                return -1;
            }
            if (strB == null)
            {
                CheckStringComparison(comparisonType);
                return 1;
            }

            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, strB, GetCaseCompareOfComparisonCulture(comparisonType));

                case StringComparison.InvariantCulture:
                case StringComparison.InvariantCultureIgnoreCase:
                    return CompareInfo.Invariant.Compare(strA, strB, GetCaseCompareOfComparisonCulture(comparisonType));

                case StringComparison.Ordinal:
                    // Most common case: first character is different.
                    // Returns false for empty strings.
                    if (strA._firstChar != strB._firstChar)
                    {
                        return strA._firstChar - strB._firstChar;
                    }

                    return CompareOrdinalHelper(strA, strB);

                case StringComparison.OrdinalIgnoreCase:
                    return Ordinal.CompareStringIgnoreCase(ref strA.GetRawStringData(), strA.Length, ref strB.GetRawStringData(), strB.Length);

                default:
                    throw new ArgumentException(SR.NotSupported_StringComparison, nameof(comparisonType));
            }
        }

        // Provides a culture-correct string comparison. strA is compared to strB
        // to determine whether it is lexicographically less, equal, or greater, and then a
        // negative integer, 0, or a positive integer is returned; respectively.
        //
        public static int Compare(string? strA, string? strB, CultureInfo? culture, CompareOptions options)
        {
            CultureInfo compareCulture = culture ?? CultureInfo.CurrentCulture;
            return compareCulture.CompareInfo.Compare(strA, strB, options);
        }

        // Provides a culture-correct string comparison. strA is compared to strB
        // to determine whether it is lexicographically less, equal, or greater, and then a
        // negative integer, 0, or a positive integer is returned; respectively.
        // The case-sensitive option is set by ignoreCase, and the culture is set
        // by culture
        //
        public static int Compare(string? strA, string? strB, bool ignoreCase, CultureInfo? culture)
        {
            CompareOptions options = ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None;
            return Compare(strA, strB, culture, options);
        }

        // Determines whether two string regions match.  The substring of strA beginning
        // at indexA of given length is compared with the substring of strB
        // beginning at indexB of the same length.
        //
        public static int Compare(string? strA, int indexA, string? strB, int indexB, int length)
        {
            // NOTE: It's important we call the boolean overload, and not the StringComparison
            // one. The two have some subtly different behavior (see notes in the former).
            return Compare(strA, indexA, strB, indexB, length, ignoreCase: false);
        }

        // Determines whether two string regions match.  The substring of strA beginning
        // at indexA of given length is compared with the substring of strB
        // beginning at indexB of the same length.  Case sensitivity is determined by the ignoreCase boolean.
        //
        public static int Compare(string? strA, int indexA, string? strB, int indexB, int length, bool ignoreCase)
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

            CompareOptions options = ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None;
            return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, indexA, lengthA, strB, indexB, lengthB, options);
        }

        // Determines whether two string regions match.  The substring of strA beginning
        // at indexA of length length is compared with the substring of strB
        // beginning at indexB of the same length.  Case sensitivity is determined by the ignoreCase boolean,
        // and the culture is set by culture.
        //
        public static int Compare(string? strA, int indexA, string? strB, int indexB, int length, bool ignoreCase, CultureInfo? culture)
        {
            CompareOptions options = ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None;
            return Compare(strA, indexA, strB, indexB, length, culture, options);
        }

        // Determines whether two string regions match.  The substring of strA beginning
        // at indexA of length length is compared with the substring of strB
        // beginning at indexB of the same length.
        //
        public static int Compare(string? strA, int indexA, string? strB, int indexB, int length, CultureInfo? culture, CompareOptions options)
        {
            CultureInfo compareCulture = culture ?? CultureInfo.CurrentCulture;
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

            return compareCulture.CompareInfo.Compare(strA, indexA, lengthA, strB, indexB, lengthB, options);
        }

        public static int Compare(string? strA, int indexA, string? strB, int indexB, int length, StringComparison comparisonType)
        {
            CheckStringComparison(comparisonType);

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
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_NegativeLength);
            }

            if (indexA < 0 || indexB < 0)
            {
                string paramName = indexA < 0 ? nameof(indexA) : nameof(indexB);
                throw new ArgumentOutOfRangeException(paramName, SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }

            if (strA.Length - indexA < 0 || strB.Length - indexB < 0)
            {
                string paramName = strA.Length - indexA < 0 ? nameof(indexA) : nameof(indexB);
                throw new ArgumentOutOfRangeException(paramName, SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }

            if (length == 0 || (object.ReferenceEquals(strA, strB) && indexA == indexB))
            {
                return 0;
            }

            int lengthA = Math.Min(length, strA.Length - indexA);
            int lengthB = Math.Min(length, strB.Length - indexB);

            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, indexA, lengthA, strB, indexB, lengthB, GetCaseCompareOfComparisonCulture(comparisonType));

                case StringComparison.InvariantCulture:
                case StringComparison.InvariantCultureIgnoreCase:
                    return CompareInfo.Invariant.Compare(strA, indexA, lengthA, strB, indexB, lengthB, GetCaseCompareOfComparisonCulture(comparisonType));

                case StringComparison.Ordinal:
                    return CompareOrdinalHelper(strA, indexA, lengthA, strB, indexB, lengthB);

                default:
                    Debug.Assert(comparisonType == StringComparison.OrdinalIgnoreCase); // CheckStringComparison validated these earlier
                    return Ordinal.CompareStringIgnoreCase(ref Unsafe.Add(ref strA.GetRawStringData(), indexA), lengthA, ref Unsafe.Add(ref strB.GetRawStringData(), indexB), lengthB);
            }
        }

        // Compares strA and strB using an ordinal (code-point) comparison.
        //
        public static int CompareOrdinal(string? strA, string? strB)
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
            if (strA._firstChar != strB._firstChar)
            {
                return strA._firstChar - strB._firstChar;
            }

            return CompareOrdinalHelper(strA, strB);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int CompareOrdinal(ReadOnlySpan<char> strA, ReadOnlySpan<char> strB)
            => SpanHelpers.SequenceCompareTo(ref MemoryMarshal.GetReference(strA), strA.Length, ref MemoryMarshal.GetReference(strB), strB.Length);

        // Compares strA and strB using an ordinal (code-point) comparison.
        //
        public static int CompareOrdinal(string? strA, int indexA, string? strB, int indexB, int length)
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
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_NegativeCount);
            }

            if (indexA < 0 || indexB < 0)
            {
                string paramName = indexA < 0 ? nameof(indexA) : nameof(indexB);
                throw new ArgumentOutOfRangeException(paramName, SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }

            int lengthA = Math.Min(length, strA.Length - indexA);
            int lengthB = Math.Min(length, strB.Length - indexB);

            if (lengthA < 0 || lengthB < 0)
            {
                string paramName = lengthA < 0 ? nameof(indexA) : nameof(indexB);
                throw new ArgumentOutOfRangeException(paramName, SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }

            if (length == 0 || (object.ReferenceEquals(strA, strB) && indexA == indexB))
            {
                return 0;
            }

            return CompareOrdinalHelper(strA, indexA, lengthA, strB, indexB, lengthB);
        }

        // Compares this String to another String (cast as object), returning an integer that
        // indicates the relationship. This method returns a value less than 0 if this is less than value, 0
        // if this is equal to value, or a value greater than 0 if this is greater than value.
        //
        public int CompareTo(object? value)
        {
            if (value == null)
            {
                return 1;
            }

            if (!(value is string other))
            {
                throw new ArgumentException(SR.Arg_MustBeString);
            }

            return CompareTo(other); // will call the string-based overload
        }

        // Determines the sorting relation of StrB to the current instance.
        //
        public int CompareTo(string? strB)
        {
            return string.Compare(this, strB, StringComparison.CurrentCulture);
        }

        // Determines whether a specified string is a suffix of the current instance.
        //
        // The case-sensitive and culture-sensitive option is set by options,
        // and the default culture is used.
        //
        public bool EndsWith(string value)
        {
            return EndsWith(value, StringComparison.CurrentCulture);
        }

        public bool EndsWith(string value, StringComparison comparisonType)
        {
            ArgumentNullException.ThrowIfNull(value);

            if ((object)this == (object)value)
            {
                CheckStringComparison(comparisonType);
                return true;
            }

            if (value.Length == 0)
            {
                CheckStringComparison(comparisonType);
                return true;
            }

            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.IsSuffix(this, value, GetCaseCompareOfComparisonCulture(comparisonType));

                case StringComparison.InvariantCulture:
                case StringComparison.InvariantCultureIgnoreCase:
                    return CompareInfo.Invariant.IsSuffix(this, value, GetCaseCompareOfComparisonCulture(comparisonType));

                case StringComparison.Ordinal:
                    int offset = this.Length - value.Length;
                    return (uint)offset <= (uint)this.Length && this.AsSpan(offset).SequenceEqual(value);

                case StringComparison.OrdinalIgnoreCase:
                    return this.Length < value.Length ?
                            false :
                            (Ordinal.CompareStringIgnoreCase(ref Unsafe.Add(ref this.GetRawStringData(), this.Length - value.Length), value.Length, ref value.GetRawStringData(), value.Length) == 0);

                default:
                    throw new ArgumentException(SR.NotSupported_StringComparison, nameof(comparisonType));
            }
        }

        public bool EndsWith(string value, bool ignoreCase, CultureInfo? culture)
        {
            ArgumentNullException.ThrowIfNull(value);

            if ((object)this == (object)value)
            {
                return true;
            }

            CultureInfo referenceCulture = culture ?? CultureInfo.CurrentCulture;
            return referenceCulture.CompareInfo.IsSuffix(this, value, ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None);
        }

        public bool EndsWith(char value)
        {
            // If the string is empty, *(&_firstChar + length - 1) will deref within
            // the _stringLength field, which will be all-zero. We must forbid '\0'
            // from going down the optimized code path because otherwise empty strings
            // would appear to end with '\0', which is incorrect.
            // n.b. This optimization relies on the layout of string and is not valid
            // for other data types like char[] or Span<char>.
            if (RuntimeHelpers.IsKnownConstant(value) && value != '\0')
            {
                // deref Length now to front-load the null check; also take this time to zero-extend
                // n.b. (localLength - 1) could be negative!
                nuint localLength = (uint)Length;
                return Unsafe.Add(ref _firstChar, (nint)localLength - 1) == value;
            }

            int lastPos = Length - 1;
            return ((uint)lastPos < (uint)Length) && this[lastPos] == value;
        }

        // Determines whether two strings match.
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (object.ReferenceEquals(this, obj))
                return true;

            if (!(obj is string str))
                return false;

            if (this.Length != str.Length)
                return false;

            return EqualsHelper(this, str);
        }

        // Determines whether two strings match.
        [Intrinsic] // Unrolled and vectorized for half-constant input
        public bool Equals([NotNullWhen(true)] string? value)
        {
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

        [Intrinsic] // Unrolled and vectorized for half-constant input (Ordinal)
        public bool Equals([NotNullWhen(true)] string? value, StringComparison comparisonType)
        {
            if (object.ReferenceEquals(this, value))
            {
                CheckStringComparison(comparisonType);
                return true;
            }

            if (value is null)
            {
                CheckStringComparison(comparisonType);
                return false;
            }

            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.Compare(this, value, GetCaseCompareOfComparisonCulture(comparisonType)) == 0;

                case StringComparison.InvariantCulture:
                case StringComparison.InvariantCultureIgnoreCase:
                    return CompareInfo.Invariant.Compare(this, value, GetCaseCompareOfComparisonCulture(comparisonType)) == 0;

                case StringComparison.Ordinal:
                    if (this.Length != value.Length)
                        return false;
                    return EqualsHelper(this, value);

                case StringComparison.OrdinalIgnoreCase:
                    if (this.Length != value.Length)
                        return false;

                    return EqualsOrdinalIgnoreCaseNoLengthCheck(this, value);

                default:
                    throw new ArgumentException(SR.NotSupported_StringComparison, nameof(comparisonType));
            }
        }

        // Determines whether two Strings match.
        [Intrinsic] // Unrolled and vectorized for half-constant input
        public static bool Equals(string? a, string? b)
        {
            if (object.ReferenceEquals(a, b))
            {
                return true;
            }

            if (a is null || b is null || a.Length != b.Length)
            {
                return false;
            }

            return EqualsHelper(a, b);
        }

        [Intrinsic] // Unrolled and vectorized for half-constant input (Ordinal)
        public static bool Equals(string? a, string? b, StringComparison comparisonType)
        {
            if (object.ReferenceEquals(a, b))
            {
                CheckStringComparison(comparisonType);
                return true;
            }

            if (a is null || b is null)
            {
                CheckStringComparison(comparisonType);
                return false;
            }

            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.Compare(a, b, GetCaseCompareOfComparisonCulture(comparisonType)) == 0;

                case StringComparison.InvariantCulture:
                case StringComparison.InvariantCultureIgnoreCase:
                    return CompareInfo.Invariant.Compare(a, b, GetCaseCompareOfComparisonCulture(comparisonType)) == 0;

                case StringComparison.Ordinal:
                    if (a.Length != b.Length)
                        return false;
                    return EqualsHelper(a, b);

                case StringComparison.OrdinalIgnoreCase:
                    if (a.Length != b.Length)
                        return false;

                    return EqualsOrdinalIgnoreCaseNoLengthCheck(a, b);

                default:
                    throw new ArgumentException(SR.NotSupported_StringComparison, nameof(comparisonType));
            }
        }

        public static bool operator ==(string? a, string? b) => string.Equals(a, b);

        public static bool operator !=(string? a, string? b) => !string.Equals(a, b);

        // Gets a hash code for this string.  If strings A and B are such that A.Equals(B), then
        // they will return the same hash code.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            ulong seed = Marvin.DefaultSeed;

            // Multiplication below will not overflow since going from positive Int32 to UInt32.
            return Marvin.ComputeHash32(ref Unsafe.As<char, byte>(ref _firstChar), (uint)_stringLength * 2 /* in bytes, not chars */, (uint)seed, (uint)(seed >> 32));
        }

        // Gets a hash code for this string and this comparison. If strings A and B and comparison C are such
        // that string.Equals(A, B, C), then they will return the same hash code with this comparison C.
        public int GetHashCode(StringComparison comparisonType) => StringComparer.FromComparison(comparisonType).GetHashCode(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetHashCodeOrdinalIgnoreCase()
        {
            ulong seed = Marvin.DefaultSeed;
            return Marvin.ComputeHash32OrdinalIgnoreCase(ref _firstChar, _stringLength /* in chars, not bytes */, (uint)seed, (uint)(seed >> 32));
        }

        // A span-based equivalent of String.GetHashCode(). Computes an ordinal hash code.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetHashCode(ReadOnlySpan<char> value)
        {
            ulong seed = Marvin.DefaultSeed;

            // Multiplication below will not overflow since going from positive Int32 to UInt32.
            return Marvin.ComputeHash32(ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(value)), (uint)value.Length * 2 /* in bytes, not chars */, (uint)seed, (uint)(seed >> 32));
        }

        // A span-based equivalent of String.GetHashCode(StringComparison). Uses the specified comparison type.
        public static int GetHashCode(ReadOnlySpan<char> value, StringComparison comparisonType)
        {
            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.GetHashCode(value, GetCaseCompareOfComparisonCulture(comparisonType));

                case StringComparison.InvariantCulture:
                case StringComparison.InvariantCultureIgnoreCase:
                    return CompareInfo.Invariant.GetHashCode(value, GetCaseCompareOfComparisonCulture(comparisonType));

                case StringComparison.Ordinal:
                    return GetHashCode(value);

                case StringComparison.OrdinalIgnoreCase:
                    return GetHashCodeOrdinalIgnoreCase(value);

                default:
                    ThrowHelper.ThrowArgumentException(ExceptionResource.NotSupported_StringComparison, ExceptionArgument.comparisonType);
                    Debug.Fail("Should not reach this point.");
                    return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetHashCodeOrdinalIgnoreCase(ReadOnlySpan<char> value)
        {
            ulong seed = Marvin.DefaultSeed;
            return Marvin.ComputeHash32OrdinalIgnoreCase(ref MemoryMarshal.GetReference(value), value.Length /* in chars, not bytes */, (uint)seed, (uint)(seed >> 32));
        }

        // Use this if and only if 'Denial of Service' attacks are not a concern (i.e. never used for free-form user input),
        // or are otherwise mitigated
        internal unsafe int GetNonRandomizedHashCode()
        {
            fixed (char* src = &_firstChar)
            {
                Debug.Assert(src[this.Length] == '\0', "src[this.Length] == '\\0'");
                Debug.Assert(((int)src) % 4 == 0, "Managed string should start at 4 bytes boundary");

                uint hash1 = (5381 << 16) + 5381;
                uint hash2 = hash1;

                uint* ptr = (uint*)src;
                int length = this.Length;

                while (length > 2)
                {
                    length -= 4;
                    // Where length is 4n-1 (e.g. 3,7,11,15,19) this additionally consumes the null terminator
                    hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptr[0];
                    hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[1];
                    ptr += 2;
                }

                if (length > 0)
                {
                    // Where length is 4n-3 (e.g. 1,5,9,13,17) this additionally consumes the null terminator
                    hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[0];
                }

                return (int)(hash1 + (hash2 * 1566083941));
            }
        }

        internal unsafe int GetNonRandomizedHashCodeOrdinalIgnoreCase()
        {
            uint hash1 = (5381 << 16) + 5381;
            uint hash2 = hash1;

            fixed (char* src = &_firstChar)
            {
                Debug.Assert(src[this.Length] == '\0', "src[this.Length] == '\\0'");
                Debug.Assert(((int) src) % 4 == 0, "Managed string should start at 4 bytes boundary");

                uint* ptr = (uint*) src;
                int length = this.Length;

                // We "normalize to lowercase" every char by ORing with 0x0020. This casts
                // a very wide net because it will change, e.g., '^' to '~'. But that should
                // be ok because we expect this to be very rare in practice.
                const uint NormalizeToLowercase = 0x0020_0020u; // valid both for big-endian and for little-endian

                while (length > 2)
                {
                    uint p0 = ptr[0];
                    uint p1 = ptr[1];
                    if (!Utf16Utility.AllCharsInUInt32AreAscii(p0 | p1))
                    {
                        goto NotAscii;
                    }

                    length -= 4;
                    // Where length is 4n-1 (e.g. 3,7,11,15,19) this additionally consumes the null terminator
                    hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ (p0 | NormalizeToLowercase);
                    hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ (p1 | NormalizeToLowercase);
                    ptr += 2;
                }

                if (length > 0)
                {
                    uint p0 = ptr[0];
                    if (!Utf16Utility.AllCharsInUInt32AreAscii(p0))
                    {
                        goto NotAscii;
                    }

                    // Where length is 4n-3 (e.g. 1,5,9,13,17) this additionally consumes the null terminator
                    hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ (p0 | NormalizeToLowercase);
                }
            }

            return (int)(hash1 + (hash2 * 1566083941));

        NotAscii:
            return GetNonRandomizedHashCodeOrdinalIgnoreCaseSlow(this);

            static int GetNonRandomizedHashCodeOrdinalIgnoreCaseSlow(string str)
            {
                int length = str.Length;
                char[]? borrowedArr = null;
                // Important: leave an additional space for '\0'
                Span<char> scratch = (uint)length < 64 ?
                    stackalloc char[64] : (borrowedArr = ArrayPool<char>.Shared.Rent(length + 1));

                int charsWritten = System.Globalization.Ordinal.ToUpperOrdinal(str, scratch);
                Debug.Assert(charsWritten == length);
                scratch[length] = '\0';

                const uint NormalizeToLowercase = 0x0020_0020u;
                uint hash1 = (5381 << 16) + 5381;
                uint hash2 = hash1;

                // Duplicate the main loop, can be removed once JIT gets "Loop Unswitching" optimization
                fixed (char* src = scratch)
                {
                    uint* ptr = (uint*)src;
                    while (length > 2)
                    {
                        length -= 4;
                        hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ (ptr[0] | NormalizeToLowercase);
                        hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ (ptr[1] | NormalizeToLowercase);
                        ptr += 2;
                    }

                    if (length > 0)
                    {
                        hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ (ptr[0] | NormalizeToLowercase);
                    }
                }

                if (borrowedArr != null)
                {
                    ArrayPool<char>.Shared.Return(borrowedArr);
                }
                return (int)(hash1 + (hash2 * 1566083941));
            }
        }

        // Determines whether a specified string is a prefix of the current instance
        //
        public bool StartsWith(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            return StartsWith(value, StringComparison.CurrentCulture);
        }

        [Intrinsic] // Unrolled and vectorized for half-constant input (Ordinal)
        public bool StartsWith(string value, StringComparison comparisonType)
        {
            ArgumentNullException.ThrowIfNull(value);

            if ((object)this == (object)value)
            {
                CheckStringComparison(comparisonType);
                return true;
            }

            if (value.Length == 0)
            {
                CheckStringComparison(comparisonType);
                return true;
            }

            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.IsPrefix(this, value, GetCaseCompareOfComparisonCulture(comparisonType));

                case StringComparison.InvariantCulture:
                case StringComparison.InvariantCultureIgnoreCase:
                    return CompareInfo.Invariant.IsPrefix(this, value, GetCaseCompareOfComparisonCulture(comparisonType));

                case StringComparison.Ordinal:
                    if (this.Length < value.Length || _firstChar != value._firstChar)
                    {
                        return false;
                    }
                    return (value.Length == 1) ?
                            true :                 // First char is the same and thats all there is to compare
                            SpanHelpers.SequenceEqual(
                                ref Unsafe.As<char, byte>(ref this.GetRawStringData()),
                                ref Unsafe.As<char, byte>(ref value.GetRawStringData()),
                                ((nuint)value.Length) * 2);

                case StringComparison.OrdinalIgnoreCase:
                    if (this.Length < value.Length)
                    {
                        return false;
                    }
                    return Ordinal.EqualsIgnoreCase(ref this.GetRawStringData(), ref value.GetRawStringData(), value.Length);

                default:
                    throw new ArgumentException(SR.NotSupported_StringComparison, nameof(comparisonType));
            }
        }

        public bool StartsWith(string value, bool ignoreCase, CultureInfo? culture)
        {
            ArgumentNullException.ThrowIfNull(value);

            if ((object)this == (object)value)
            {
                return true;
            }

            CultureInfo referenceCulture = culture ?? CultureInfo.CurrentCulture;
            return referenceCulture.CompareInfo.IsPrefix(this, value, ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None);
        }

        public bool StartsWith(char value)
        {
            // If the string is empty, _firstChar will contain the null terminator.
            // We forbid '\0' from going down the optimized code path because otherwise
            // empty strings would appear to begin with '\0', which is incorrect.
            // n.b. This optimization relies on the layout of string and is not valid
            // for other data types like char[] or Span<char>.
            if (RuntimeHelpers.IsKnownConstant(value) && value != '\0')
            {
                return _firstChar == value;
            }

            return Length != 0 && _firstChar == value;
        }

        internal static void CheckStringComparison(StringComparison comparisonType)
        {
            // Single comparison to check if comparisonType is within [CurrentCulture .. OrdinalIgnoreCase]
            if ((uint)comparisonType > (uint)StringComparison.OrdinalIgnoreCase)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.NotSupported_StringComparison, ExceptionArgument.comparisonType);
            }
        }

        internal static CompareOptions GetCaseCompareOfComparisonCulture(StringComparison comparisonType)
        {
            Debug.Assert((uint)comparisonType <= (uint)StringComparison.OrdinalIgnoreCase);

            // Culture enums can be & with CompareOptions.IgnoreCase 0x01 to extract if IgnoreCase or CompareOptions.None 0x00
            //
            // CompareOptions.None                          0x00
            // CompareOptions.IgnoreCase                    0x01
            //
            // StringComparison.CurrentCulture:             0x00
            // StringComparison.InvariantCulture:           0x02
            // StringComparison.Ordinal                     0x04
            //
            // StringComparison.CurrentCultureIgnoreCase:   0x01
            // StringComparison.InvariantCultureIgnoreCase: 0x03
            // StringComparison.OrdinalIgnoreCase           0x05

            return (CompareOptions)((int)comparisonType & (int)CompareOptions.IgnoreCase);
        }

        private static CompareOptions GetCompareOptionsFromOrdinalStringComparison(StringComparison comparisonType)
        {
            Debug.Assert(comparisonType == StringComparison.Ordinal || comparisonType == StringComparison.OrdinalIgnoreCase);

            // StringComparison.Ordinal (0x04) --> CompareOptions.Ordinal (0x4000_0000)
            // StringComparison.OrdinalIgnoreCase (0x05) -> CompareOptions.OrdinalIgnoreCase (0x1000_0000)

            int ct = (int)comparisonType;
            return (CompareOptions)((ct & -ct) << 28); // neg and shl
        }
    }
}
