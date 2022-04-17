// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.CompilerServices;

namespace System
{
    public partial class String
    {
        public bool Contains(string value)
        {
            if (value == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);

            return SpanHelpers.IndexOf(
                ref _firstChar,
                Length,
                ref value._firstChar,
                value.Length) >= 0;
        }

        public bool Contains(string value, StringComparison comparisonType)
        {
#pragma warning disable CA2249 // Consider using 'string.Contains' instead of 'string.IndexOf'... this is the implementation of Contains!
            return IndexOf(value, comparisonType) >= 0;
#pragma warning restore CA2249
        }

        public bool Contains(char value) => SpanHelpers.Contains(ref _firstChar, value, Length);

        public bool Contains(char value, StringComparison comparisonType)
        {
            return IndexOf(value, comparisonType) != -1;
        }

        // Returns the index of the first occurrence of a specified character in the current instance.
        // The search starts at startIndex and runs thorough the next count characters.
        //
        public int IndexOf(char value) => SpanHelpers.IndexOf(ref _firstChar, value, Length);

        public int IndexOf(char value, int startIndex)
        {
            return IndexOf(value, startIndex, Length - startIndex);
        }

        public int IndexOf(char value, StringComparison comparisonType)
        {
            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.IndexOf(this, value, GetCaseCompareOfComparisonCulture(comparisonType));

                case StringComparison.InvariantCulture:
                case StringComparison.InvariantCultureIgnoreCase:
                    return CompareInfo.Invariant.IndexOf(this, value, GetCaseCompareOfComparisonCulture(comparisonType));

                case StringComparison.Ordinal:
                    return IndexOf(value);

                case StringComparison.OrdinalIgnoreCase:
                    return CompareInfo.Invariant.IndexOf(this, value, CompareOptions.OrdinalIgnoreCase);

                default:
                    throw new ArgumentException(SR.NotSupported_StringComparison, nameof(comparisonType));
            }
        }

        public unsafe int IndexOf(char value, int startIndex, int count)
        {
            if ((uint)startIndex > (uint)Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }

            if ((uint)count > (uint)(Length - startIndex))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_Count);
            }

            int result = SpanHelpers.IndexOf(ref Unsafe.Add(ref _firstChar, startIndex), value, count);

            return result < 0 ? result : result + startIndex;
        }

        // Returns the index of the first occurrence of any specified character in the current instance.
        // The search starts at startIndex and runs to startIndex + count - 1.
        //
        public int IndexOfAny(char[] anyOf)
        {
            if (anyOf is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.anyOf);
            }

            return new ReadOnlySpan<char>(ref _firstChar, Length).IndexOfAny(anyOf);
        }

        public int IndexOfAny(char[] anyOf, int startIndex)
        {
            return IndexOfAny(anyOf, startIndex, Length - startIndex);
        }

        public int IndexOfAny(char[] anyOf, int startIndex, int count)
        {
            if (anyOf is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.anyOf);
            }

            if ((uint)startIndex > (uint)Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }

            if ((uint)count > (uint)(Length - startIndex))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_Count);
            }

            int result = new ReadOnlySpan<char>(ref Unsafe.Add(ref _firstChar, startIndex), count).IndexOfAny(anyOf);

            return result < 0 ? result : result + startIndex;
        }

       /*
        * IndexOf, LastIndexOf, Contains, StartsWith, and EndsWith
        * ========================================================
        *
        * Given a search string 'searchString', a target string 'value' to locate within the search string, and a comparer
        * 'comparer', we ask the comparer to generate a set S of tuples '(startPos, endPos)' for which the below expression
        * returns true:
        *
        * >> bool result = searchString.Substring(startPos, endPos - startPos).Equals(value, comparer);
        *
        * If the generated set S is empty (i.e., there is no combination of values 'startPos' and 'endPos' which makes the
        * above expression evaluate to true), then we say "'searchString' does not contain 'value'", and the expression
        * "searchString.Contains(value, comparer)" should evaluate to false. If the set S is non-empty, then we say
        * "'searchString' contains 'value'", and the expression "searchString.Contains(value, comparer)" should
        * evaluate to true.
        *
        * n.b. There may be other tuples '(startPos, endPos)' *not* present in the generated set S for which the above
        * expression evaluates to true. We discount the existence of these values. Allowing any such values to factor
        * into the logic below could result in splitting the search string in a manner inappropriate for the culture
        * rules of the specified comparer. For the remainder of this discussion, when we refer to 'startPos' and
        * 'endPos', we consider only tuples '(startPos, endPos)' as they may be present in the generated set S.
        *
        * Given a 'searchString', 'value', and 'comparer', the behavior of the IndexOf method is that it finds the
        * smallest possible 'endPos' for which there exists any corresponding 'startPos' which makes the above
        * expression evaluate to true, then it returns any 'startPos' within that subset. For example:
        *
        * let searchString = "<ZWJ><ZWJ>hihi" (where <ZWJ> = U+200D ZERO WIDTH JOINER, a weightless code point)
        * let value = "hi"
        * let comparer = a linguistic culture-invariant comparer (e.g., StringComparison.InvariantCulture)
        * then S = { (0, 4), (1, 4), (2, 4), (4, 6) }
        * so the expression "<ZWJ><ZWJ>hihi".IndexOf("hi", comparer) can evaluate to any of { 0, 1, 2 }.
        *
        * n.b. ordinal comparers (e.g., StringComparison.Ordinal and StringComparison.OrdinalIgnoreCase) do not
        * exhibit this ambiguity, as any given 'startPos' or 'endPos' will appear at most exactly once across
        * all entries from set S. With the above example, S = { (2, 4), (4, 6) }, so IndexOf = 2 unambiguously.
        *
        * There exists a relationship between IndexOf and StartsWith. If there exists in set S any entry with
        * the tuple values (startPos = 0, endPos = <anything>), we say "'searchString' starts with 'value'", and
        * the expression "searchString.StartsWith(value, comparer)" should evaluate to true. If there exists
        * no such entry in set S, then we say "'searchString' does not start with 'value'", and the expression
        * "searchString.StartsWith(value, comparer)" should evaluate to false.
        *
        * LastIndexOf and EndsWith have a similar relationship as IndexOf and StartsWith. The behavior of the
        * LastIndexOf method is that it finds the largest possible 'endPos' for which there exists any corresponding
        * 'startPos' which makes the expression evaluate to true, then it returns any 'startPos' within that
        * subset. For example:
        *
        * let searchString = "hi<ZWJ><ZWJ>hi" (this is slightly modified from the earlier example)
        * let value = "hi"
        * let comparer = StringComparison.InvariantCulture
        * then S = { (0, 2), (0, 3), (0, 4), (2, 6), (3, 6), (4, 6) }
        * so the expression "hi<ZWJ><ZWJ>hi".LastIndexOf("hi", comparer) can evaluate to any of { 2, 3, 4 }.
        *
        * If there exists in set S any entry with the tuple values (startPos = <anything>, endPos = searchString.Length),
        * we say "'searchString' ends with 'value'", and the expression "searchString.EndsWith(value, comparer)"
        * should evaluate to true. If there exists no such entry in set S, then we say "'searchString' does not
        * start with 'value'", and the expression "searchString.EndsWith(value, comparer)" should evaluate to false.
        *
        * There are overloads of IndexOf and LastIndexOf which take an offset and length in order to constrain the
        * search space to a substring of the original search string.
        *
        * For LastIndexOf specifially, overloads which take a 'startIndex' and 'count' behave differently
        * than their IndexOf counterparts. 'startIndex' is the index of the last char element that should
        * be considered when performing the search. For example, if startIndex = 4, then the caller is
        * indicating "when finding the match I want you to include the char element at index 4, but not
        * any char elements past that point."
        *
        *                        idx = 0123456 ("abcdefg".Length = 7)
        * So, if the search string is "abcdefg", startIndex = 5 and count = 3, then the search space will
        *                                 ~~~    be the substring "def", as highlighted to the left.
        * Essentially: "the search space should be of length 3 chars and should end *just after* the char
        * element at index 5."
        *
        * Since this behavior can introduce off-by-one errors in the boundary cases, we allow startIndex = -1
        * with a zero-length 'searchString' (treated as equivalent to startIndex = 0), and we allow
        * startIndex = searchString.Length (treated as equivalent to startIndex = searchString.Length - 1).
        *
        * Note also that this behavior can introduce errors when dealing with UTF-16 surrogate pairs.
        * If the search string is the 3 chars "[BMP][HI][LO]", startIndex = 1 and count = 2, then the
        *                                      ~~~~~~~~~       search space wil be the substring "[BMP][ HI]".
        * This means that the char [HI] is incorrectly seen as a standalone high surrogate, which could
        * lead to incorrect matching behavior, or it could cause LastIndexOf to incorrectly report that
        * a zero-weight character could appear between the [HI] and [LO] chars.
        */

        public int IndexOf(string value)
        {
            return IndexOf(value, StringComparison.CurrentCulture);
        }

        public int IndexOf(string value, int startIndex)
        {
            return IndexOf(value, startIndex, StringComparison.CurrentCulture);
        }

        public int IndexOf(string value, int startIndex, int count)
        {
            return IndexOf(value, startIndex, count, StringComparison.CurrentCulture);
        }

        public int IndexOf(string value, StringComparison comparisonType)
        {
            return IndexOf(value, 0, Length, comparisonType);
        }

        public int IndexOf(string value, int startIndex, StringComparison comparisonType)
        {
            return IndexOf(value, startIndex, Length - startIndex, comparisonType);
        }

        public int IndexOf(string value, int startIndex, int count, StringComparison comparisonType)
        {
            // Parameter checking will be done by CompareInfo.IndexOf.

            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.IndexOf(this, value, startIndex, count, GetCaseCompareOfComparisonCulture(comparisonType));

                case StringComparison.InvariantCulture:
                case StringComparison.InvariantCultureIgnoreCase:
                    return CompareInfo.Invariant.IndexOf(this, value, startIndex, count, GetCaseCompareOfComparisonCulture(comparisonType));

                case StringComparison.Ordinal:
                case StringComparison.OrdinalIgnoreCase:
                    return Ordinal.IndexOf(this, value, startIndex, count, comparisonType == StringComparison.OrdinalIgnoreCase);

                default:
                    throw (value is null)
                        ? new ArgumentNullException(nameof(value))
                        : new ArgumentException(SR.NotSupported_StringComparison, nameof(comparisonType));
            }
        }

        // Returns the index of the last occurrence of a specified character in the current instance.
        // The search starts at startIndex and runs backwards to startIndex - count + 1.
        // The character at position startIndex is included in the search.  startIndex is the larger
        // index within the string.
        //
        public int LastIndexOf(char value) => SpanHelpers.LastIndexOf(ref _firstChar, value, Length);

        public int LastIndexOf(char value, int startIndex)
        {
            return LastIndexOf(value, startIndex, startIndex + 1);
        }

        public unsafe int LastIndexOf(char value, int startIndex, int count)
        {
            if (Length == 0)
            {
                return -1;
            }

            if ((uint)startIndex >= (uint)Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_IndexMustBeLess);
            }

            if ((uint)count > (uint)startIndex + 1)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_Count);
            }

            int startSearchAt = startIndex + 1 - count;
            int result = SpanHelpers.LastIndexOf(ref Unsafe.Add(ref _firstChar, startSearchAt), value, count);

            return result < 0 ? result : result + startSearchAt;
        }

        // Returns the index of the last occurrence of any specified character in the current instance.
        // The search starts at startIndex and runs backwards to startIndex - count + 1.
        // The character at position startIndex is included in the search.  startIndex is the larger
        // index within the string.
        //
        public int LastIndexOfAny(char[] anyOf)
        {
            if (anyOf is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.anyOf);
            }

            return new ReadOnlySpan<char>(ref _firstChar, Length).LastIndexOfAny(anyOf);
        }

        public int LastIndexOfAny(char[] anyOf, int startIndex)
        {
            return LastIndexOfAny(anyOf, startIndex, startIndex + 1);
        }

        public unsafe int LastIndexOfAny(char[] anyOf, int startIndex, int count)
        {
            if (anyOf is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.anyOf);
            }

            if (Length == 0)
            {
                return -1;
            }

            if ((uint)startIndex >= (uint)Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_IndexMustBeLess);
            }

            if ((count < 0) || ((count - 1) > startIndex))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_Count);
            }

            int startSearchAt = startIndex + 1 - count;
            int result = new ReadOnlySpan<char>(ref Unsafe.Add(ref _firstChar, startSearchAt), count).LastIndexOfAny(anyOf);

            return result < 0 ? result : result + startSearchAt;
        }

        // Returns the index of the last occurrence of any character in value in the current instance.
        // The search starts at startIndex and runs backwards to startIndex - count + 1.
        // The character at position startIndex is included in the search.  startIndex is the larger
        // index within the string.
        //
        public int LastIndexOf(string value)
        {
            return LastIndexOf(value, Length - 1, Length, StringComparison.CurrentCulture);
        }

        public int LastIndexOf(string value, int startIndex)
        {
            return LastIndexOf(value, startIndex, startIndex + 1, StringComparison.CurrentCulture);
        }

        public int LastIndexOf(string value, int startIndex, int count)
        {
            return LastIndexOf(value, startIndex, count, StringComparison.CurrentCulture);
        }

        public int LastIndexOf(string value, StringComparison comparisonType)
        {
            return LastIndexOf(value, Length - 1, Length, comparisonType);
        }

        public int LastIndexOf(string value, int startIndex, StringComparison comparisonType)
        {
            return LastIndexOf(value, startIndex, startIndex + 1, comparisonType);
        }

        public int LastIndexOf(string value, int startIndex, int count, StringComparison comparisonType)
        {
            // Parameter checking will be done by CompareInfo.LastIndexOf.

            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.LastIndexOf(this, value, startIndex, count, GetCaseCompareOfComparisonCulture(comparisonType));

                case StringComparison.InvariantCulture:
                case StringComparison.InvariantCultureIgnoreCase:
                    return CompareInfo.Invariant.LastIndexOf(this, value, startIndex, count, GetCaseCompareOfComparisonCulture(comparisonType));

                case StringComparison.Ordinal:
                case StringComparison.OrdinalIgnoreCase:
                    return CompareInfo.Invariant.LastIndexOf(this, value, startIndex, count, GetCompareOptionsFromOrdinalStringComparison(comparisonType));

                default:
                    throw (value is null)
                        ? new ArgumentNullException(nameof(value))
                        : new ArgumentException(SR.NotSupported_StringComparison, nameof(comparisonType));
            }
        }
    }
}
