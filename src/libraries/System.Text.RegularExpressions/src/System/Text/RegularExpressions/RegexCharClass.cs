// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Text.RegularExpressions
{
    // The main function of RegexCharClass is as a builder to turn ranges, characters and
    // Unicode categories into a single string.  This string is used as a black box
    // representation of a character class by the rest of Regex.  The format is as follows.
    //
    // Char index   Use
    //      0       Flags - currently this only holds the "negate" flag
    //      1       length of the string representing the "set" portion, e.g. [a-z0-9] only has a "set"
    //      2       length of the string representing the "category" portion, e.g. [\p{Lu}] only has a "category"
    //      3...m   The set.  These are a series of ranges which define the characters included in the set.
    //              To determine if a given character is in the set, we binary search over this set of ranges
    //              and see where the character should go.  Based on whether the ending index is odd or even,
    //              we know if the character is in the set.
    //      m+1...n The categories.  This is a list of UnicodeCategory enum values which describe categories
    //              included in this class. These can either be individual values (either UnicodeCategory - 1
    //              for inclusive values, or -1 - UnicodeCategory for exclusive values), or a "group", which
    //              is a contiguous sequence of such values surrounded by \0 values; all values in the group
    //              have the same positive/negative orientation.

    /// <summary>Provides the "set of Unicode chars" functionality used by the regexp engine.</summary>
    internal sealed partial class RegexCharClass
    {
        // Constants
        internal const int FlagsIndex = 0;
        internal const int SetLengthIndex = 1;
        internal const int CategoryLengthIndex = 2;
        internal const int SetStartIndex = 3; // must be odd for subsequent logic to work

        internal const char LastChar = '\uFFFF';

        internal const short SpaceConst = 100;
        private const short NotSpaceConst = -100;

        private const string InternalRegexIgnoreCase = "__InternalRegexIgnoreCase__";
        private const string SpaceCategories = "\x64";
        private const string NotSpaceCategories = "\uFF9C";
        private const string WordCategories = "\u0000\u0002\u0004\u0005\u0003\u0001\u0006\u0009\u0013\u0000";
        private const string NotWordCategories = "\u0000\uFFFE\uFFFC\uFFFB\uFFFD\uFFFF\uFFFA\uFFF7\uFFED\u0000";

        internal const string SpaceClass = "\u0000\u0000\u0001\u0064"; // \s
        internal const string NotSpaceClass = "\u0000\u0000\u0001\uFF9C"; // \S
        internal const string NegatedSpaceClass = "\u0001\0\u0001d"; // [^\s]
        internal const string WordClass = "\u0000\u0000\u000A\u0000\u0002\u0004\u0005\u0003\u0001\u0006\u0009\u0013\u0000"; // \w
        internal const string NotWordClass = "\u0000\u0000\u000A\u0000\uFFFE\uFFFC\uFFFB\uFFFD\uFFFF\uFFFA\uFFF7\uFFED\u0000"; // \W
        internal const string NegatedWordClass = "\u0001\0\n\0\u0002\u0004\u0005\u0003\u0001\u0006\t\u0013\0"; // [^\w]
        internal const string DigitClass = "\u0000\u0000\u0001\u0009"; // \d
        internal const string NotDigitClass = "\u0000\u0000\u0001\uFFF7"; // \D
        internal const string NegatedDigitClass = "\u0001\0\u0001\t"; // [^\d]
        internal const string ControlClass = "\0\0\u0001\u000f"; // \p{Cc}
        internal const string NotControlClass = "\0\0\u0001\ufff1"; // \P{Cc}
        internal const string LetterClass = "\0\0\a\0\u0002\u0004\u0005\u0003\u0001\0"; // \p{L}
        internal const string NotLetterClass = "\0\0\u0007\0\ufffe\ufffc\ufffb\ufffd\uffff\0"; // \P{L}
        internal const string LetterOrDigitClass = "\0\0\b\0\u0002\u0004\u0005\u0003\u0001\0\t"; // [\p{L}\d]
        internal const string NotLetterOrDigitClass = "\u0001\0\b\0\u0002\u0004\u0005\u0003\u0001\0\t"; // [^\p{L}\d]
        internal const string LowerClass = "\0\0\u0001\u0002"; // \p{Ll}
        internal const string NotLowerClass = "\0\0\u0001\ufffe"; // \P{Ll}
        internal const string UpperClass = "\0\0\u0001\u0001"; // \p{Lu}
        internal const string NotUpperClass = "\0\0\u0001\uffff"; // \P{Lu}
        internal const string NumberClass = "\0\0\u0005\0\t\n\v\0"; // \p{N}
        internal const string NotNumberClass = "\0\0\u0005\0\ufff7\ufff6\ufff5\0"; // \P{N}
        internal const string PunctuationClass = "\0\0\t\0\u0013\u0014\u0016\u0019\u0015\u0018\u0017\0"; // \p{P}
        internal const string NotPunctuationClass = "\0\0\u0009\0\uffed\uffec\uffea\uffe7\uffeb\uffe8\uffe9\0"; // \P{P}
        internal const string SeparatorClass = "\0\0\u0005\0\r\u000e\f\0"; // \p{Z}
        internal const string NotSeparatorClass = "\0\0\u0005\0\ufff3\ufff2\ufff4\0"; // \P{Z}
        internal const string SymbolClass = "\0\0\u0006\0\u001b\u001c\u001a\u001d\0"; // \p{S}
        internal const string NotSymbolClass = "\0\0\u0006\0\uffe5\uffe4\uffe6\uffe3\0"; // \P{S}
        internal const string AsciiLetterClass = "\0\u0004\0A[a{"; // [A-Za-z]
        internal const string NotAsciiLetterClass = "\u0001\u0004\0A[a{"; // [^A-Za-z]
        internal const string AsciiLetterOrDigitClass = "\0\u0006\00:A[a{"; // [A-Za-z0-9]
        internal const string NotAsciiLetterOrDigitClass = "\u0001\u0006\00:A[a{"; // [^A-Za-z0-9]
        internal const string HexDigitClass = "\0\u0006\00:AGag"; // [A-Fa-f0-9]
        internal const string NotHexDigitClass = "\u0001\u0006\00:AGag"; // [^A-Fa-f0-9]
        internal const string HexDigitUpperClass = "\0\u0004\00:AG"; // [A-F0-9]
        internal const string NotHexDigitUpperClass = "\u0001\u0004\00:AG"; // [A-F0-9]
        internal const string HexDigitLowerClass = "\0\u0004\00:ag"; // [a-f0-9]
        internal const string NotHexDigitLowerClass = "\u0001\u0004\00:ag"; // [a-f0-9]

        private const string ECMASpaceRanges = "\u0009\u000E\u0020\u0021";
        private const string NotECMASpaceRanges = "\0\u0009\u000E\u0020\u0021";
        private const string ECMAWordRanges = "\u0030\u003A\u0041\u005B\u005F\u0060\u0061\u007B\u0130\u0131";
        private const string NotECMAWordRanges = "\0\u0030\u003A\u0041\u005B\u005F\u0060\u0061\u007B\u0130\u0131";
        private const string ECMADigitRanges = "\u0030\u003A";
        private const string NotECMADigitRanges = "\0\u0030\u003A";

        internal const string ECMASpaceClass = "\x00\x04\x00" + ECMASpaceRanges;
        internal const string NotECMASpaceClass = "\x01\x04\x00" + ECMASpaceRanges;
        internal const string ECMAWordClass = "\x00\x0A\x00" + ECMAWordRanges;
        internal const string NotECMAWordClass = "\x01\x0A\x00" + ECMAWordRanges;
        internal const string ECMADigitClass = "\x00\x02\x00" + ECMADigitRanges;
        internal const string NotECMADigitClass = "\x01\x02\x00" + ECMADigitRanges;

        internal const string NotNewLineClass = "\x01\x02\x00\x0A\x0B";

        internal const string AnyClass = "\x00\x01\x00\x00";
        private const string EmptyClass = "\x00\x00\x00";

        // Sets regularly used as a canonical way to express the equivalent of '.' with Singleline when Singleline isn't in use.
        internal const string WordNotWordClass = "\u0000\u0000\u0014\u0000\u0002\u0004\u0005\u0003\u0001\u0006\u0009\u0013\u0000\u0000\uFFFE\uFFFC\uFFFB\uFFFD\uFFFF\uFFFA\uFFF7\uFFED\u0000";
        internal const string NotWordWordClass = "\u0000\u0000\u0014\u0000\uFFFE\uFFFC\uFFFB\uFFFD\uFFFF\uFFFA\uFFF7\uFFED\u0000\u0000\u0002\u0004\u0005\u0003\u0001\u0006\u0009\u0013\u0000";
        internal const string DigitNotDigitClass = "\u0000\u0000\u0002\u0009\uFFF7";
        internal const string NotDigitDigitClass = "\u0000\u0000\u0002\uFFF7\u0009";
        internal const string SpaceNotSpaceClass = "\u0000\u0000\u0002\u0064\uFF9C";
        internal const string NotSpaceSpaceClass = "\u0000\u0000\u0002\uFF9C\u0064";

        // UnicodeCategory is zero based, so we add one to each value and subtract it off later
        private const int DefinedCategoriesCapacity = 38;
        private static readonly Dictionary<string, string> s_definedCategories = new Dictionary<string, string>(DefinedCategoriesCapacity)
        {
            // Others
            { "Cc", "\u000F" }, // UnicodeCategory.Control + 1
            { "Cf", "\u0010" }, // UnicodeCategory.Format + 1
            { "Cn", "\u001E" }, // UnicodeCategory.OtherNotAssigned + 1
            { "Co", "\u0012" }, // UnicodeCategory.PrivateUse + 1
            { "Cs", "\u0011" }, // UnicodeCategory.Surrogate + 1
            { "C", "\u0000\u000F\u0010\u001E\u0012\u0011\u0000" },

            // Letters
            { "Ll", "\u0002" }, // UnicodeCategory.LowercaseLetter + 1
            { "Lm", "\u0004" }, // UnicodeCategory.ModifierLetter + 1
            { "Lo", "\u0005" }, // UnicodeCategory.OtherLetter + 1
            { "Lt", "\u0003" }, // UnicodeCategory.TitlecaseLetter + 1
            { "Lu", "\u0001" }, // UnicodeCategory.UppercaseLetter + 1
            { "L", "\u0000\u0002\u0004\u0005\u0003\u0001\u0000" },

            // InternalRegexIgnoreCase = {LowercaseLetter} OR {TitlecaseLetter} OR {UppercaseLetter}
            // !!!This category should only ever be used in conjunction with RegexOptions.IgnoreCase code paths!!!
            { InternalRegexIgnoreCase, "\u0000\u0002\u0003\u0001\u0000" },

            // Marks
            { "Mc", "\u0007" }, // UnicodeCategory.SpacingCombiningMark + 1
            { "Me", "\u0008" }, // UnicodeCategory.EnclosingMark + 1
            { "Mn", "\u0006" }, // UnicodeCategory.NonSpacingMark + 1
            { "M", "\u0000\u0007\u0008\u0006\u0000" },

            // Numbers
            { "Nd", "\u0009" }, // UnicodeCategory.DecimalDigitNumber + 1
            { "Nl", "\u000A" }, // UnicodeCategory.LetterNumber + 1
            { "No", "\u000B" }, // UnicodeCategory.OtherNumber + 1
            { "N", "\u0000\u0009\u000A\u000B\u0000" },

            // Punctuation
            { "Pc", "\u0013" }, // UnicodeCategory.ConnectorPunctuation + 1
            { "Pd", "\u0014" }, // UnicodeCategory.DashPunctuation + 1
            { "Pe", "\u0016" }, // UnicodeCategory.ClosePunctuation + 1
            { "Po", "\u0019" }, // UnicodeCategory.OtherPunctuation + 1
            { "Ps", "\u0015" }, // UnicodeCategory.OpenPunctuation + 1
            { "Pf", "\u0018" }, // UnicodeCategory.FinalQuotePunctuation + 1
            { "Pi", "\u0017" }, // UnicodeCategory.InitialQuotePunctuation + 1
            { "P", "\u0000\u0013\u0014\u0016\u0019\u0015\u0018\u0017\u0000" },

            // Symbols
            { "Sc", "\u001B" }, // UnicodeCategory.CurrencySymbol + 1
            { "Sk", "\u001C" }, // UnicodeCategory.ModifierSymbol + 1
            { "Sm", "\u001A" }, // UnicodeCategory.MathSymbol + 1
            { "So", "\u001D" }, // UnicodeCategory.OtherSymbol + 1
            { "S", "\u0000\u001B\u001C\u001A\u001D\u0000" },

            // Separators
            { "Zl", "\u000D" }, // UnicodeCategory.LineSeparator + 1
            { "Zp", "\u000E" }, // UnicodeCategory.ParagraphSeparator + 1
            { "Zs", "\u000C" }, // UnicodeCategory.SpaceSeparator + 1
            { "Z", "\u0000\u000D\u000E\u000C\u0000" },
        };

        private static readonly char[] s_whitespaceChars =
            ['\u0009', '\u000A', '\u000B', '\u000C', '\u000D',
             '\u0020', '\u0085', '\u00A0', '\u1680', '\u2000',
             '\u2001', '\u2002', '\u2003', '\u2004', '\u2005',
             '\u2006', '\u2007', '\u2008', '\u2009', '\u200A',
             '\u2028', '\u2029', '\u202F', '\u205F', '\u3000'];

        private List<(char First, char Last)>? _rangelist;
        private StringBuilder? _categories;
        private RegexCharClass? _subtractor;
        private bool _negate;
        private RegexCaseBehavior _caseBehavior;

#if DEBUG
        static RegexCharClass()
        {
            // Make sure the initial capacity for s_definedCategories is correct
            Debug.Assert(
                s_definedCategories.Count == DefinedCategoriesCapacity,
                $"Expected (s_definedCategories.Count): {s_definedCategories.Count}, Actual (DefinedCategoriesCapacity): {DefinedCategoriesCapacity}");

            // Make sure the s_propTable is correctly ordered
            int len = s_propTable.Length;
            for (int i = 0; i < len - 1; i++)
                Debug.Assert(string.Compare(s_propTable[i][0], s_propTable[i + 1][0], StringComparison.Ordinal) < 0, $"RegexCharClass s_propTable is out of order at ({s_propTable[i][0]}, {s_propTable[i + 1][0]})");

            // Make sure character information is in sync with Unicode data.
            var whitespaceSet = new HashSet<char>(s_whitespaceChars);
            for (int i = 0; i <= char.MaxValue; i++)
            {
                Debug.Assert(whitespaceSet.Contains((char)i) == char.IsWhiteSpace((char)i));
            }
        }
#endif

        /// <summary>
        /// Creates an empty character class.
        /// </summary>
        public RegexCharClass()
        {
        }

        private RegexCharClass(bool negate, List<(char First, char Last)>? ranges, StringBuilder? categories, RegexCharClass? subtraction)
        {
            _rangelist = ranges;
            _categories = categories;
            _negate = negate;
            _subtractor = subtraction;
        }

        public bool CanMerge => !_negate && _subtractor == null;

        public bool Negate
        {
            set { _negate = value; }
        }

        public void AddChar(char c) => AddRange(c, c);

        public void AddNotChar(char c)
        {
            if (c == 0)
            {
                AddRange((char)1, LastChar);
            }
            else if (c == LastChar)
            {
                AddRange((char)0, (char)(LastChar - 1));
            }
            else
            {
                AddRange((char)0, (char)(c - 1));
                AddRange((char)(c + 1), LastChar);
            }
        }

        /// <summary>
        /// Adds a regex char class
        /// </summary>
        public void AddCharClass(RegexCharClass cc)
        {
            Debug.Assert(cc.CanMerge && CanMerge, "Both character classes added together must be able to merge");

            int ccRangeCount = cc._rangelist?.Count ?? 0;

            if (ccRangeCount != 0)
            {
                EnsureRangeList().AddRange(cc._rangelist!);
            }

            if (cc._categories != null)
            {
                EnsureCategories().Append(cc._categories);
            }
        }

        /// <summary>Adds a regex char class if the classes are mergeable.</summary>
        public bool TryAddCharClass(RegexCharClass cc)
        {
            if (cc.CanMerge && CanMerge)
            {
                AddCharClass(cc);
                return true;
            }

            return false;
        }

        private StringBuilder EnsureCategories() =>
            _categories ??= new StringBuilder();

        private List<(char First, char Last)> EnsureRangeList() =>
            _rangelist ??= new List<(char First, char Last)>(6);

        /// <summary>
        /// Adds ranges (specified by their range string representation) to the class.
        /// </summary>
        private void AddRanges(ReadOnlySpan<char> set)
        {
            Debug.Assert(!set.IsEmpty);

            List<(char First, char Last)> rangeList = EnsureRangeList();

            int i;
            for (i = 0; i < set.Length - 1; i += 2)
            {
                rangeList.Add((set[i], (char)(set[i + 1] - 1)));
            }

            if (i < set.Length)
            {
                rangeList.Add((set[i], LastChar));
            }
        }

        public void AddSubtraction(RegexCharClass sub)
        {
            Debug.Assert(_subtractor == null, "Can't add two subtractions to a char class. ");
            _subtractor = sub;
        }

        /// <summary>
        /// Adds a single range of characters to the class.
        /// </summary>
        public void AddRange(char first, char last) =>
            EnsureRangeList().Add((first, last));

        public void AddCategoryFromName(string categoryName, bool invert, bool caseInsensitive, string pattern, int currentPos)
        {
            if (s_definedCategories.TryGetValue(categoryName, out string? category) &&
                !categoryName.Equals(InternalRegexIgnoreCase))
            {
                if (caseInsensitive && (categoryName.Equals("Ll") || categoryName.Equals("Lu") || categoryName.Equals("Lt")))
                {
                    // when RegexOptions.IgnoreCase is specified then {Ll}, {Lu}, and {Lt} cases should all match
                    category = s_definedCategories[InternalRegexIgnoreCase];
                }

                StringBuilder categories = EnsureCategories();
                if (invert)
                {
                    // Negate category
                    for (int i = 0; i < category.Length; i++)
                    {
                        short ch = (short)category[i];
                        categories.Append((char)-ch);
                    }
                }
                else
                {
                    categories.Append(category);
                }
            }
            else
            {
                AddRanges(RangesFromProperty(categoryName, invert, pattern, currentPos));
            }
        }

        private void AddCategory(string category) => EnsureCategories().Append(category);

        /// <summary>
        /// Adds to the class any case-equivalence versions of characters already
        /// in the class. Used for case-insensitivity.
        /// </summary>
        public void AddCaseEquivalences(CultureInfo culture)
        {
            List<(char First, char Last)>? rangeList = _rangelist;
            if (rangeList != null)
            {
                int count = rangeList.Count;
                for (int i = 0; i < count; i++)
                {
                    (char First, char Last) range = rangeList[i];
                    if (range.First == range.Last)
                    {
                        if (RegexCaseEquivalences.TryFindCaseEquivalencesForCharWithIBehavior(range.First, culture, ref _caseBehavior, out ReadOnlySpan<char> equivalences))
                        {
                            foreach (char equivalence in equivalences)
                            {
                                AddChar(equivalence);
                            }
                        }
                    }
                    else
                    {
                        AddCaseEquivalenceRange(range.First, range.Last, culture);
                    }
                }
            }
        }

        /// <summary>
        /// For a single range that's in the set, adds any additional ranges
        /// necessary to ensure that lowercase equivalents are also included.
        /// </summary>
        private void AddCaseEquivalenceRange(char chMin, char chMax, CultureInfo culture)
        {
            for (int i = chMin; i <= chMax; i++)
            {
                if (RegexCaseEquivalences.TryFindCaseEquivalencesForCharWithIBehavior((char)i, culture, ref _caseBehavior, out ReadOnlySpan<char> equivalences))
                {
                    foreach (char equivalence in equivalences)
                    {
                        AddChar(equivalence);
                    }
                }
            }
        }

        public void AddWord(bool ecma, bool negate)
        {
            if (ecma)
            {
                AddRanges((negate ? NotECMAWordRanges : ECMAWordRanges).AsSpan());
            }
            else
            {
                AddCategory(negate ? NotWordCategories : WordCategories);
            }
        }

        public void AddSpace(bool ecma, bool negate)
        {
            if (ecma)
            {
                AddRanges((negate ? NotECMASpaceRanges : ECMASpaceRanges).AsSpan());
            }
            else
            {
                AddCategory(negate ? NotSpaceCategories : SpaceCategories);
            }
        }

        public void AddDigit(bool ecma, bool negate, string pattern, int currentPos)
        {
            if (ecma)
            {
                AddRanges((negate ? NotECMADigitRanges : ECMADigitRanges).AsSpan());
            }
            else
            {
                AddCategoryFromName("Nd", negate, caseInsensitive: false, pattern, currentPos);
            }
        }

        public static string ConvertOldStringsToClass(string set, string category)
        {
            bool startsWithNulls = set.Length >= 2 && set[0] == '\0' && set[1] == '\0';
            int strLength = SetStartIndex + set.Length + category.Length;
            if (startsWithNulls)
            {
                strLength -= 2;
            }

            return
#if NET
                string
#else
                StringExtensions
#endif
                .Create(strLength, (set, category, startsWithNulls), static (span, state) =>
            {
                int index;

                if (state.startsWithNulls)
                {
                    span[FlagsIndex] = (char)0x1;
                    span[SetLengthIndex] = (char)(state.set.Length - 2);
                    span[CategoryLengthIndex] = (char)state.category.Length;
                    state.set.AsSpan(2).CopyTo(span.Slice(SetStartIndex));
                    index = SetStartIndex + state.set.Length - 2;
                }
                else
                {
                    span[FlagsIndex] = '\0';
                    span[SetLengthIndex] = (char)state.set.Length;
                    span[CategoryLengthIndex] = (char)state.category.Length;
                    state.set.AsSpan().CopyTo(span.Slice(SetStartIndex));
                    index = SetStartIndex + state.set.Length;
                }

                state.category.AsSpan().CopyTo(span.Slice(index));
            });
        }

        /// <summary>
        /// Returns the char
        /// </summary>
        public static char SingletonChar(string set)
        {
            Debug.Assert(IsSingleton(set) || IsSingletonInverse(set), "Tried to get the singleton char out of a non singleton character class");
            return set[SetStartIndex];
        }

        public static bool IsMergeable(string charClass) =>
            charClass != null &&
            !IsNegated(charClass) &&
            !IsSubtraction(charClass);

        public static bool IsEmpty(string charClass) =>
            charClass[CategoryLengthIndex] == 0 &&
            charClass[SetLengthIndex] == 0 &&
            !IsNegated(charClass) &&
            !IsSubtraction(charClass);

        /// <summary><c>true</c> if the set contains a single character only</summary>
        /// <remarks>
        /// This will happen not only from character classes manually written to contain a single character,
        /// but much more frequently by the implementation/parser itself, e.g. when looking for \n as part of
        /// finding the end of a line, when processing an alternation like "hello|hithere" where the first
        /// character of both options is the same, etc.
        /// </remarks>
        public static bool IsSingleton(string set) =>
            set[CategoryLengthIndex] == 0 &&
            set[SetLengthIndex] == 2 &&
            !IsNegated(set) &&
            !IsSubtraction(set) &&
            (set[SetStartIndex] == LastChar || set[SetStartIndex] + 1 == set[SetStartIndex + 1]);

        public static bool IsSingletonInverse(string set) =>
            set[CategoryLengthIndex] == 0 &&
            set[SetLengthIndex] == 2 &&
            IsNegated(set) &&
            !IsSubtraction(set) &&
            (set[SetStartIndex] == LastChar || set[SetStartIndex] + 1 == set[SetStartIndex + 1]);

        /// <summary>
        /// Gets the categories from a set if the set is only categories (no ranges, no subtraction),
        /// they all share the same negation status (not doing so is rare), and they all fit in the destination span.
        /// </summary>
        /// <param name="set">The character class to examine.</param>
        /// <param name="categories">The destination span into which the categories should be written.</param>
        /// <param name="numCategories">The number of categories written to <paramref name="categories"/>.</param>
        /// <param name="negated">false if the categories written to <paramref name="categories"/> represent inclusions; true if they represent exclusions.</param>
        /// <returns>true if the categories could be retrieved; otherwise, false.</returns>
        public static bool TryGetOnlyCategories(string set, Span<UnicodeCategory> categories, out int numCategories, out bool negated)
        {
            negated = false;
            numCategories = 0;
            bool sawFirstCategory = false;

            // Require that the character class has no ranges, has no subtraction, and has categories.
            int categoryLength = set[CategoryLengthIndex];
            if (categoryLength == 0 || set[SetLengthIndex] != 0 || IsSubtraction(set))
            {
                return false;
            }

            // Loop through all categories, storing them into the categories span.
            int categoryEnd = SetStartIndex + set[CategoryLengthIndex];
            for (int pos = SetStartIndex; pos < categoryEnd; pos++)
            {
                // Get the next category value.
                short c = (short)set[pos];
                if (c > 0)
                {
                    // It's a positive (inclusive) value.  Make sure all previous categories seen are also positive.
                    // Also make sure it's not the fake space category, which consumers don't handle as it's
                    // not a real UnicodeCategory.
                    if ((sawFirstCategory && negated) ||
                        c == SpaceConst ||
                        numCategories == categories.Length)
                    {
                        return false;
                    }

                    sawFirstCategory = true;
                    categories[numCategories++] = (UnicodeCategory)(c - 1);
                }
                else if (c < 0)
                {
                    // It's a negative (exclusive) value.  Make sure all previous categories seen are also negative.
                    // Also make sure it's not the fake non-space category, which consumers don't handle as it's
                    // not a real UnicodeCategory.
                    if ((sawFirstCategory && !negated) ||
                        c == NotSpaceConst ||
                        numCategories == categories.Length)
                    {
                        return false;
                    }

                    sawFirstCategory = true;
                    negated = true;
                    categories[numCategories++] = (UnicodeCategory)(-1 - c);
                }
                else // c == 0
                {
                    // It's the start of a group. Every value in the group needs to have the same orientation.
                    // We stop when we hit the next 0.
                    c = (short)set[++pos];
                    Debug.Assert(c != 0);
                    if (c > 0)
                    {
                        if (sawFirstCategory && negated)
                        {
                            return false;
                        }
                        sawFirstCategory = true;

                        do
                        {
                            if (numCategories == categories.Length)
                            {
                                return false;
                            }

                            categories[numCategories++] = (UnicodeCategory)(c - 1);
                            c = (short)set[++pos];
                        }
                        while (c != 0);
                    }
                    else
                    {
                        if (sawFirstCategory && !negated)
                        {
                            return false;
                        }
                        negated = true;
                        sawFirstCategory = true;

                        do
                        {
                            if (numCategories == categories.Length)
                            {
                                return false;
                            }

                            categories[numCategories++] = (UnicodeCategory)(-1 - c);
                            c = (short)set[++pos];
                        }
                        while (c != 0);
                    }
                }
            }

            // Factor in whether the entire character class is itself negated.
            negated ^= IsNegated(set);
            return true;
        }

        /// <summary>Attempts to get a single range stored in the set.</summary>
        /// <param name="set">The set.</param>
        /// <param name="lowInclusive">The inclusive lower-bound of the range, if available.</param>
        /// <param name="highInclusive">The inclusive upper-bound of the range, if available.</param>
        /// <returns>true if the set contained a single range; otherwise, false.</returns>
        /// <remarks>
        /// <paramref name="lowInclusive"/> and <paramref name="highInclusive"/> will be equal if the
        /// range is a singleton or singleton inverse. The range will need to be negated by the caller
        /// if <see cref="IsNegated(string)"/> is true.
        /// </remarks>
        public static bool TryGetSingleRange(string set, out char lowInclusive, out char highInclusive)
        {
            if (set[CategoryLengthIndex] == 0 && // must not have any categories
                set.Length == SetStartIndex + set[SetLengthIndex]) // and no subtraction
            {
                switch ((int)set[SetLengthIndex])
                {
                    case 1:
                        lowInclusive = set[SetStartIndex];
                        highInclusive = LastChar;
                        return true;

                    case 2:
                        lowInclusive = set[SetStartIndex];
                        highInclusive = (char)(set[SetStartIndex + 1] - 1);
                        return true;
                }
            }

            lowInclusive = highInclusive = '\0';
            return false;
        }

        /// <summary>Attempts to get two ranges stored in the set.  The set may be negated.</summary>
        /// <param name="set">The set.</param>
        /// <param name="range0">The first result range.</param>
        /// <param name="range1">The second result range.</param>
        /// <returns>true if the set contained exactly two ranges; otherwise, false.</returns>
        public static bool TryGetDoubleRange(
            string set,
            out (char LowInclusive, char HighInclusive) range0,
            out (char LowInclusive, char HighInclusive) range1)
        {
            if (set[CategoryLengthIndex] == 0 && // must not have any categories
                set.Length == SetStartIndex + set[SetLengthIndex]) // and no subtraction
            {
                int setLength = set[SetLengthIndex];
                if (setLength is 3 or 4)
                {
                    range0 = (set[SetStartIndex], (char)(set[SetStartIndex + 1] - 1));
                    range1 = (set[SetStartIndex + 2], setLength == 3 ? LastChar : (char)(set[SetStartIndex + 3] - 1));
                    return true;
                }
            }

            range0 = range1 = ('\0', '\0');
            return false;
        }

        /// <summary>Gets all of the characters in the specified set, storing them into the provided span.</summary>
        /// <param name="set">The character class.</param>
        /// <param name="chars">The span into which the chars should be stored.</param>
        /// <returns>
        /// The number of stored chars.  If they won't all fit, 0 is returned.
        /// If 0 is returned, no assumptions can be made about the characters.
        /// </returns>
        /// <remarks>
        /// Only considers character classes that only contain sets (no categories),
        /// just simple sets containing starting/ending pairs (subtraction from those pairs
        /// is factored in, however).The returned characters may be negated: if IsNegated(set)
        /// is false, then the returned characters are the only ones that match; if it returns
        /// true, then the returned characters are the only ones that don't match.
        /// </remarks>
        public static int GetSetChars(string set, Span<char> chars)
        {
            // We get the characters by enumerating the set portion, so we validate that it's
            // set up to enable that, e.g. no categories.
            if (!CanEasilyEnumerateSetContents(set, out bool hasSubtraction))
            {
                return 0;
            }

            // Negation with subtraction is too cumbersome to reason about efficiently.
            if (hasSubtraction && IsNegated(set))
            {
                return 0;
            }

            // Iterate through the pairs of ranges, storing each value in each range
            // into the supplied span.  If they all won't fit, we give up and return 0.
            // Otherwise we return the number found.  Note that we don't bother to handle
            // the corner case where the last range's upper bound is LastChar (\uFFFF),
            // based on it a) complicating things, and b) it being really unlikely to
            // be part of a small set.
            int setLength = set[SetLengthIndex];
            int count = 0, evaluated = 0;
            for (int i = SetStartIndex; i < SetStartIndex + setLength; i += 2)
            {
                int curSetEnd = set[i + 1];
                for (int c = set[i]; c < curSetEnd; c++)
                {
                    // Keep track of how many characters we've checked. This could work
                    // just comparing count rather than evaluated, but we also want to
                    // limit how much work is done here, which we can do by constraining
                    // the number of checks to the size of the storage provided.
                    if (++evaluated > chars.Length)
                    {
                        return 0;
                    }

                    // If the set is all ranges but has a subtracted class,
                    // validate the char is actually in the set prior to storing it:
                    // it might be in the subtracted range.
                    if (hasSubtraction && !CharInClass((char)c, set))
                    {
                        continue;
                    }

                    Debug.Assert(count <= evaluated);
                    chars[count++] = (char)c;
                }
            }

            return count;
        }

        /// <summary>
        /// Determines whether two sets may overlap.
        /// </summary>
        /// <returns>false if the two sets do not overlap; true if they may.</returns>
        /// <remarks>
        /// If the method returns false, the caller can be sure the sets do not overlap.
        /// If the method returns true, it's still possible the sets don't overlap.
        /// </remarks>
        public static bool MayOverlap(string set1, string set2)
        {
            // If the sets are identical, there's obviously overlap.
            if (set1 == set2)
            {
                return true;
            }

            // If either set is all-inclusive, there's overlap by definition (unless
            // the other set is empty, but that's so rare it's not worth checking.)
            if (set1 == AnyClass || set2 == AnyClass)
            {
                return true;
            }

            // If one set is negated and the other one isn't, we're in one of two situations:
            // - The remainder of the sets are identical, in which case these are inverses of
            //   each other, and they don't overlap.
            // - The remainder of the sets aren't identical, in which case there's very likely
            //   overlap, and it's not worth spending more time investigating.
            bool set1Negated = IsNegated(set1);
            bool set2Negated = IsNegated(set2);
            if (set1Negated != set2Negated)
            {
                return !set1.AsSpan(1).SequenceEqual(set2.AsSpan(1));
            }

            // If the sets are negated, since they're not equal, there's almost certainly overlap.
            Debug.Assert(set1Negated == set2Negated);
            if (set1Negated)
            {
                return true;
            }

            // Special-case some known, common classes that don't overlap.
            if (KnownDistinctSets(set1, set2) ||
                KnownDistinctSets(set2, set1))
            {
                return false;
            }

            // If both sets are composed of only Unicode categories, we can compare most cases fairly easily.
            Span<UnicodeCategory> categories1 = stackalloc UnicodeCategory[16], categories2 = stackalloc UnicodeCategory[16];
            if (TryGetOnlyCategories(set1, categories1, out int numCategories1, out bool negated1) &&
                TryGetOnlyCategories(set2, categories2, out int numCategories2, out bool negated2))
            {
                // Check for the case of the sets being negated versions of the same single category,
                // e.g. \d and \D, in which case they don't overlap.
                if (numCategories1 == 1 && numCategories2 == 1 &&
                    categories1[0] == categories2[0] &&
                    negated1 != negated2)
                {
                    return false;
                }

                // Otherwise, if either is negated, just assume they may overlap.
                if (negated1 || negated2)
                {
                    return true;
                }

                // Check if any category is the same between the two. We've limited the number of elements in the spans
                // to a small number, so we just do the easy thing of comparing the full product.
                foreach (UnicodeCategory cat1 in categories1.Slice(0, numCategories1))
                {
                    foreach (UnicodeCategory cat2 in categories2.Slice(0, numCategories2))
                    {
                        if (cat1 == cat2)
                        {
                            return true;
                        }
                    }
                }

                // If we got here, the two sets are disjoint.
                return false;
            }

            // If set2 can be easily enumerated (e.g. no unicode categories), then enumerate it and
            // check if any of its members are in set1.  Otherwise, the same for set1.
            if (CanEasilyEnumerateSetContents(set2))
            {
                return MayOverlapByEnumeration(set1, set2);
            }
            else if (CanEasilyEnumerateSetContents(set1))
            {
                return MayOverlapByEnumeration(set2, set1);
            }

            // Assume that everything else might overlap.  In the future if it proved impactful, we could be more accurate here,
            // at the expense of more computation time.
            return true;

            static bool KnownDistinctSets(string set1, string set2) =>
                // Because of how the set strings are constructed, these known distinct sets aren't handled by our
                // more general UnicodeCategory logic.
                set1 switch
                {
                    SpaceClass => set2 is NotSpaceClass or DigitClass or WordClass,
                    ECMASpaceClass => set2 is NotECMASpaceClass or ECMADigitClass or ECMAWordClass,
                    WordClass => set2 is NotWordClass,
                    ECMAWordClass => set2 is NotECMAWordClass,
                    _ => false,
                };

            static bool MayOverlapByEnumeration(string set1, string set2)
            {
                Debug.Assert(!IsNegated(set1) && !IsNegated(set2));
                for (int i = SetStartIndex; i < SetStartIndex + set2[SetLengthIndex]; i += 2)
                {
                    int curSetEnd = set2[i + 1];
                    for (int c = set2[i]; c < curSetEnd; c++)
                    {
                        if (CharInClass((char)c, set1))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether the specified set is a named set with a reasonably small count
        /// of Unicode characters.
        /// </summary>
        /// <param name="set">The set description.</param>
        /// <param name="chars">The chars that make up the known set.</param>
        /// <param name="negated">Whether the <paramref name="chars"/> need to be negated.</param>
        /// <param name="description">A description suitable for use in C# code as a variable name.</param>
        public static bool IsUnicodeCategoryOfSmallCharCount(string set, [NotNullWhen(true)] out char[]? chars, out bool negated, [NotNullWhen(true)] out string? description)
        {
            switch (set)
            {
                case SpaceClass:
                case NotSpaceClass:
                    chars = s_whitespaceChars;
                    negated = set == NotSpaceClass;
                    description = "whitespace";
                    return true;
            }

            chars = default;
            negated = false;
            description = null;
            return false;
        }

        /// <summary>Gets whether the specified character participates in case conversion.</summary>
        /// <remarks>
        /// This method is used to perform operations as if they were case-sensitive even if they're
        /// specified as being case-insensitive.  Such a reduction can be applied when the only character
        /// that would lower-case to the one being searched for / compared against is that character itself.
        /// </remarks>
        public static bool ParticipatesInCaseConversion(int comparison)
        {
            Debug.Assert((uint)comparison <= char.MaxValue);

            switch (char.GetUnicodeCategory((char)comparison))
            {
                case UnicodeCategory.ClosePunctuation:
                case UnicodeCategory.ConnectorPunctuation:
                case UnicodeCategory.Control:
                case UnicodeCategory.DashPunctuation:
                case UnicodeCategory.DecimalDigitNumber:
                case UnicodeCategory.FinalQuotePunctuation:
                case UnicodeCategory.InitialQuotePunctuation:
                case UnicodeCategory.LineSeparator:
                case UnicodeCategory.OpenPunctuation:
                case UnicodeCategory.OtherNumber:
                case UnicodeCategory.OtherPunctuation:
                case UnicodeCategory.ParagraphSeparator:
                case UnicodeCategory.SpaceSeparator:
                    // All chars in these categories meet the criteria that the only way
                    // `char.ToLower(toTest, AnyCulture) == charInAboveCategory` is when
                    // toTest == charInAboveCategory.
                    return false;

                default:
                    // We don't know (without testing the character against every other
                    // character), so assume it does.
                    return true;
            }
        }

        /// <summary>Gets whether the specified span participates in case conversion.</summary>
        /// <remarks>The span participates in case conversion if any of its characters do.</remarks>
        public static bool ParticipatesInCaseConversion(ReadOnlySpan<char> s)
        {
            foreach (char c in s)
            {
                if (ParticipatesInCaseConversion(c))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Gets whether the set description string is for two ASCII letters that case to each other under OrdinalIgnoreCase rules.</summary>
        public static bool SetContainsAsciiOrdinalIgnoreCaseCharacter(string set, Span<char> twoChars)
        {
            Debug.Assert(twoChars.Length >= 2);
            return
                !IsNegated(set) &&
                GetSetChars(set, twoChars) == 2 &&
                twoChars[0] < 128 &&
                twoChars[1] < 128 &&
                twoChars[0] != twoChars[1] &&
                char.IsLetter(twoChars[0]) &&
                char.IsLetter(twoChars[1]) &&
                (twoChars[0] | 0x20) == (twoChars[1] | 0x20);
        }

        /// <summary>Gets whether we can iterate through the set list pairs in order to completely enumerate the set's contents.</summary>
        /// <remarks>This may enumerate negated characters if the set is negated.  This will return false if the set has subtraction.</remarks>
        private static bool CanEasilyEnumerateSetContents(string set) =>
            CanEasilyEnumerateSetContents(set, out bool hasSubtraction) &&
            !hasSubtraction;

        /// <summary>Gets whether we can iterate through the set list pairs in order to completely enumerate the set's contents.</summary>
        /// <remarks>This may enumerate negated characters if the set is negated, and it may be an overestimate if the set contains subtraction.</remarks>
        private static bool CanEasilyEnumerateSetContents(string set, out bool hasSubtraction)
        {
            hasSubtraction = IsSubtraction(set);
            return
                set.Length > SetStartIndex &&
                set[SetLengthIndex] > 0 &&
                set[SetLengthIndex] % 2 == 0 &&
                set[CategoryLengthIndex] == 0;
        }

        /// <summary>Provides results from <see cref="Analyze"/>.</summary>
        internal struct CharClassAnalysisResults
        {
            /// <summary>true if the set contains only ranges; false if it contains Unicode categories and/or subtraction.</summary>
            public bool OnlyRanges;
            /// <summary>true if we know for sure that the set contains only ASCII values; otherwise, false.</summary>
            /// <remarks>This can only be true if <see cref="OnlyRanges"/> is true.</remarks>
            public bool ContainsOnlyAscii;
            /// <summary>true if we know for sure that the set doesn't contain any ASCII values; otherwise, false.</summary>
            /// <remarks>This can only be true if <see cref="OnlyRanges"/> is true.</remarks>
            public bool ContainsNoAscii;
            /// <summary>true if we know for sure that all ASCII values are in the set; otherwise, false.</summary>
            /// <remarks>This can only be true if <see cref="OnlyRanges"/> is true.</remarks>
            public bool AllAsciiContained;
            /// <summary>true if we know for sure that all non-ASCII values are in the set; otherwise, false.</summary>
            /// <remarks>This can only be true if <see cref="OnlyRanges"/> is true.</remarks>
            public bool AllNonAsciiContained;
            /// <summary>The inclusive lower bound.</summary>
            /// <remarks>This is only valid if <see cref="OnlyRanges"/> is true.</remarks>
            public int LowerBoundInclusiveIfOnlyRanges;
            /// <summary>The exclusive upper bound.</summary>
            /// <remarks>This is only valid if <see cref="OnlyRanges"/> is true.</remarks>
            public int UpperBoundExclusiveIfOnlyRanges;
        }

        /// <summary>Analyzes the set to determine some basic properties that can be used to optimize usage.</summary>
        internal static CharClassAnalysisResults Analyze(string set)
        {
            bool isNegated = IsNegated(set);

            // The analysis is performed based entirely on ranges contained within the set.
            // Thus, we require that it can be "easily enumerated", meaning it contains only
            // ranges (and more specifically those with both the lower inclusive and upper
            // exclusive bounds specified). We also permit the set to contain a subtracted
            // character class, as for non-negated sets, that can only narrow what's permitted,
            // and the analysis can be performed on the overestimate of the set prior to subtraction.
            // However, negation is performed before subtraction, which means we can't trust
            // the ranges to inform AllNonAsciiContained and AllAsciiContained, as the subtraction
            // could create holes in those.  As such, while we can permit subtraction for non-negated
            // sets, for negated sets, we need to bail.
            if (!CanEasilyEnumerateSetContents(set, out bool hasSubtraction) ||
                (isNegated && hasSubtraction))
            {
                // We can't make any strong claims about the set.
                return default;
            }

            char firstValueInclusive = set[SetStartIndex];
            char lastValueExclusive = set[SetStartIndex + set[SetLengthIndex] - 1];

            if (isNegated)
            {
                // We're negated: if the upper bound of the range is ASCII, that means everything
                // above it is actually included, meaning all non-ASCII are in the class.
                // Similarly if the lower bound is non-ASCII, that means in a negated world
                // everything ASCII is included.
                Debug.Assert(!hasSubtraction);
                return new CharClassAnalysisResults
                {
                    OnlyRanges = true,
                    AllNonAsciiContained = lastValueExclusive <= 128,
                    AllAsciiContained = firstValueInclusive >= 128,
                    ContainsNoAscii = firstValueInclusive == 0 && set[SetStartIndex + 1] >= 128,
                    ContainsOnlyAscii = false,
                    LowerBoundInclusiveIfOnlyRanges = firstValueInclusive,
                    UpperBoundExclusiveIfOnlyRanges = lastValueExclusive,
                };
            }

            // If the upper bound is ASCII, that means everything included in the class is ASCII.
            // Similarly if the lower bound is non-ASCII, that means no ASCII is in the class.
            return new CharClassAnalysisResults
            {
                OnlyRanges = true,
                AllNonAsciiContained = false,
                AllAsciiContained = firstValueInclusive == 0 && set[SetStartIndex + 1] >= 128 && !hasSubtraction,
                ContainsOnlyAscii = lastValueExclusive <= 128,
                ContainsNoAscii = firstValueInclusive >= 128,
                LowerBoundInclusiveIfOnlyRanges = firstValueInclusive,
                UpperBoundExclusiveIfOnlyRanges = lastValueExclusive,
            };
        }

        internal static bool IsSubtraction(string charClass) =>
            charClass.Length > SetStartIndex +
            charClass[CategoryLengthIndex] +
            charClass[SetLengthIndex];

        internal static bool IsNegated(string set) => set[FlagsIndex] == 1;

        internal static bool IsNegated(string set, int setOffset) => set[FlagsIndex + setOffset] == 1;

        public static bool IsECMAWordChar(char ch) =>
            // According to ECMA-262, \s, \S, ., ^, and $ use Unicode-based interpretations of
            // whitespace and newline, while \d, \D\, \w, \W, \b, and \B use ASCII-only
            // interpretations of digit, word character, and word boundary.  In other words,
            // no special treatment of Unicode ZERO WIDTH NON-JOINER (ZWNJ U+200C) and
            // ZERO WIDTH JOINER (ZWJ U+200D) is required for ECMA word boundaries.
            ((((uint)ch - 'A') & ~0x20) < 26) || // ASCII letter
            (((uint)ch - '0') < 10) || // digit
            ch == '_' || // underscore
            ch == '\u0130'; // latin capital letter I with dot above

        /// <summary>16 bytes, representing the chars 0 through 127, with a 1 for a bit where that char is a word char.</summary>
        private static ReadOnlySpan<byte> WordCharAsciiLookup =>
        [
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x03,
            0xFE, 0xFF, 0xFF, 0x87, 0xFE, 0xFF, 0xFF, 0x07
        ];

        /// <summary>Mask of Unicode categories that combine to form [\\w]</summary>
        private const int WordCategoriesMask =
            1 << (int)UnicodeCategory.UppercaseLetter |
            1 << (int)UnicodeCategory.LowercaseLetter |
            1 << (int)UnicodeCategory.TitlecaseLetter |
            1 << (int)UnicodeCategory.ModifierLetter |
            1 << (int)UnicodeCategory.OtherLetter |
            1 << (int)UnicodeCategory.NonSpacingMark |
            1 << (int)UnicodeCategory.DecimalDigitNumber |
            1 << (int)UnicodeCategory.ConnectorPunctuation;

        /// <summary>Determines whether a character is considered a word character for the purposes of testing the \w set.</summary>
        public static bool IsWordChar(char ch)
        {
            // This is the same as IsBoundaryWordChar, except that IsBoundaryWordChar also
            // returns true for \u200c and \u200d.

            // Bitmap for whether each character 0 through 127 is in [\\w]
            ReadOnlySpan<byte> ascii = WordCharAsciiLookup;

            // If the char is ASCII, look it up in the bitmap. Otherwise, query its Unicode category.
            int chDiv8 = ch >> 3;
            return (uint)chDiv8 < (uint)ascii.Length ?
                (ascii[chDiv8] & (1 << (ch & 0x7))) != 0 :
                (WordCategoriesMask & (1 << (int)CharUnicodeInfo.GetUnicodeCategory(ch))) != 0;
        }

        /// <summary>Determines whether the characters that match the specified set are known to all be word characters.</summary>
        public static bool IsKnownWordClassSubset(string set)
        {
            // Check for common sets that we know to be subsets of \w.
            if (set is
                WordClass or DigitClass or LetterClass or LetterOrDigitClass or
                AsciiLetterClass or AsciiLetterOrDigitClass or
                HexDigitClass or HexDigitUpperClass or HexDigitLowerClass)
            {
                return true;
            }

            // Check for sets composed of Unicode categories that are part of \w.
            Span<UnicodeCategory> categories = stackalloc UnicodeCategory[16];
            if (TryGetOnlyCategories(set, categories, out int numCategories, out bool negated) && !negated)
            {
                foreach (UnicodeCategory cat in categories.Slice(0, numCategories))
                {
                    if (!IsWordCategory(cat))
                    {
                        return false;
                    }
                }

                return true;
            }

            // If we can enumerate every character in the set quickly, do so, checking to see whether they're all in \w.
            if (CanEasilyEnumerateSetContents(set))
            {
                for (int i = SetStartIndex; i < SetStartIndex + set[SetLengthIndex]; i += 2)
                {
                    int curSetEnd = set[i + 1];
                    for (int c = set[i]; c < curSetEnd; c++)
                    {
                        if (!CharInClass((char)c, WordClass))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            // Unlikely to be a subset of \w, and we don't know for sure.
            return false;
        }

        /// <summary>Determines whether a character is considered a word character for the purposes of testing a word character boundary.</summary>
        public static bool IsBoundaryWordChar(char ch)
        {
            // According to UTS#18 Unicode Regular Expressions (http://www.unicode.org/reports/tr18/)
            // RL 1.4 Simple Word Boundaries  The class of <word_character> includes all Alphabetic
            // values from the Unicode character database, from UnicodeData.txt [UData], plus the U+200C
            // ZERO WIDTH NON-JOINER and U+200D ZERO WIDTH JOINER.
            const char ZeroWidthNonJoiner = '\u200C', ZeroWidthJoiner = '\u200D';

            // Bitmap for whether each character 0 through 127 is in [\\w]
            ReadOnlySpan<byte> ascii = WordCharAsciiLookup;

            // If the char is ASCII, look it up in the bitmap. Otherwise, query its Unicode category.
            int chDiv8 = ch >> 3;
            return (uint)chDiv8 < (uint)ascii.Length ?
                (ascii[chDiv8] & (1 << (ch & 0x7))) != 0 :
                (IsWordCategory(CharUnicodeInfo.GetUnicodeCategory(ch)) ||
                 (ch == ZeroWidthJoiner | ch == ZeroWidthNonJoiner));
        }

        private static bool IsWordCategory(UnicodeCategory category) =>
            (WordCategoriesMask & (1 << (int)category)) != 0;

        /// <summary>Determines whether the 'a' and 'b' values differ by only a single bit, setting that bit in 'mask'.</summary>
        /// <remarks>This isn't specific to RegexCharClass; it's just a convenient place to host it.</remarks>
        public static bool DifferByOneBit(char a, char b, out int mask)
        {
            mask = a ^ b;
            return BitOperations.IsPow2(mask);
        }

        /// <summary>Determines a character's membership in a character class (via the string representation of the class).</summary>
        /// <param name="ch">The character.</param>
        /// <param name="set">The string representation of the character class.</param>
        /// <param name="asciiLazyCache">A lazily-populated cache for ASCII results stored in a 256-bit array.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CharInClass(char ch, string set, ref uint[]? asciiLazyCache)
        {
            // The uint[] contains 8 ints, or 256 bits.  These are laid out as pairs, where the first bit in the pair
            // says whether the second bit in the pair has already been computed.  Once a value is computed, it's never
            // changed, so since Int32s are written/read atomically, we can trust the value bit if we see that the known bit
            // has been set.  If the known bit hasn't been set, then we proceed to look it up, and then swap in the result.
            const int CacheArrayLength = 8;
            Debug.Assert(asciiLazyCache is null || asciiLazyCache.Length == CacheArrayLength, "set lookup should be able to store two bits for each of the first 128 characters");

            // If the value is ASCII and already has an answer for this value, use it.
            if (asciiLazyCache is uint[] cache)
            {
                int index = ch >> 4;
                if ((uint)index < (uint)cache.Length)
                {
                    Debug.Assert(ch < 128);
                    uint current = cache[index];
                    uint bit = 1u << ((ch & 0xF) << 1);
                    if ((current & bit) != 0)
                    {
                        return (current & (bit << 1)) != 0;
                    }
                }
            }

            // For ASCII, lazily initialize. For non-ASCII, just compute the value.
            return ch < 128 ?
                InitializeValue(ch, set, ref asciiLazyCache) :
                CharInClassRecursive(ch, set, 0);

            static bool InitializeValue(char ch, string set, ref uint[]? asciiLazyCache)
            {
                // (After warm-up, we should find ourselves rarely getting here.)
                Debug.Assert(ch < 128);

                // Compute the result and determine which bits to write back to the array and "or" the bits back in a thread-safe manner.
                bool isInClass = CharInClass(ch, set);
                uint bitsToSet = 1u << ((ch & 0xF) << 1);
                if (isInClass)
                {
                    bitsToSet |= bitsToSet << 1;
                }

                uint[]? cache = asciiLazyCache ?? Interlocked.CompareExchange(ref asciiLazyCache, new uint[CacheArrayLength], null) ?? asciiLazyCache;
#if NET
                Interlocked
#else
                InterlockedExtensions
#endif
                    .Or(ref cache[ch >> 4], bitsToSet);

                // Return the computed value.
                return isInClass;
            }
        }

        /// <summary>
        /// Determines a character's membership in a character class (via the string representation of the class).
        /// </summary>
        public static bool CharInClass(char ch, string set) =>
            CharInClassRecursive(ch, set, 0);

        private static bool CharInClassRecursive(char ch, string set, int start)
        {
            int setLength = set[start + SetLengthIndex];
            int categoryLength = set[start + CategoryLengthIndex];
            int endPosition = start + SetStartIndex + setLength + categoryLength;

            bool inClass = CharInClassInternal(ch, set, start, setLength, categoryLength);

            // Note that we apply the negation *before* performing the subtraction.  This is because
            // the negation only applies to the first char class, not the entire subtraction.
            if (IsNegated(set, start))
            {
                inClass = !inClass;
            }

            // Subtract if necessary
            if (inClass && set.Length > endPosition)
            {
                inClass = !CharInClassRecursive(ch, set, endPosition);
            }

            return inClass;
        }

        /// <summary>
        /// Determines a character's membership in a character class (via the
        /// string representation of the class).
        /// </summary>
        private static bool CharInClassInternal(char ch, string set, int start, int setLength, int categoryLength)
        {
            int min = start + SetStartIndex;
            int max = min + setLength;

            while (min != max)
            {
                int mid = (min + max) >> 1;
                if (ch < set[mid])
                {
                    max = mid;
                }
                else
                {
                    min = mid + 1;
                }
            }

            // The starting position of the set within the character class determines
            // whether what an odd or even ending position means.  If the start is odd,
            // an *even* ending position means the character was in the set.  With recursive
            // subtractions in the mix, the starting position = start+SetStartIndex.  Since we know that
            // SetStartIndex is odd, we can simplify it out of the equation.  But if it changes we need to
            // reverse this check.
            Debug.Assert((SetStartIndex & 0x1) == 1, "If SetStartIndex is not odd, the calculation below this will be reversed");
            if ((min & 0x1) == (start & 0x1))
            {
                return true;
            }

            if (categoryLength == 0)
            {
                return false;
            }

            return CharInCategory(ch, set.AsSpan(SetStartIndex + start + setLength, categoryLength));
        }

        private static bool CharInCategory(char ch, ReadOnlySpan<char> categorySetSegment)
        {
            UnicodeCategory chcategory = char.GetUnicodeCategory(ch);

            for (int i = 0; i < categorySetSegment.Length; i++)
            {
                int curcat = (short)categorySetSegment[i];

                if (curcat == 0)
                {
                    // zero is our marker for a group of categories - treated as a unit
                    if (CharInCategoryGroup(chcategory, categorySetSegment, ref i))
                    {
                        return true;
                    }
                }
                else if (curcat > 0)
                {
                    // greater than zero is a positive case

                    if (curcat == SpaceConst)
                    {
                        if (char.IsWhiteSpace(ch))
                        {
                            return true;
                        }
                    }
                    else if (chcategory == (UnicodeCategory)(curcat - 1))
                    {
                        return true;
                    }
                }
                else
                {
                    // less than zero is a negative case
                    if (curcat == NotSpaceConst)
                    {
                        if (!char.IsWhiteSpace(ch))
                        {
                            return true;
                        }
                    }
                    else if (chcategory != (UnicodeCategory)(-1 - curcat))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// This is used for categories which are composed of other categories - L, N, Z, W...
        /// These groups need special treatment when they are negated
        /// </summary>
        private static bool CharInCategoryGroup(UnicodeCategory chcategory, ReadOnlySpan<char> category, ref int i)
        {
            int pos = i + 1;
            int curcat = (short)category[pos];
            bool result;

            if (curcat > 0)
            {
                // positive case - the character must be in ANY of the categories in the group
                result = false;
                do
                {
                    result |= chcategory == (UnicodeCategory)(curcat - 1);
                    curcat = (short)category[++pos];
                }
                while (curcat != 0);
            }
            else
            {
                // negative case - the character must be in NONE of the categories in the group
                Debug.Assert(curcat < 0);
                result = true;
                do
                {
                    result &= chcategory != (UnicodeCategory)(-1 - curcat);
                    curcat = (short)category[++pos];
                }
                while (curcat != 0);
            }

            i = pos;
            return result;
        }

        public static RegexCharClass Parse(string charClass) => ParseRecursive(charClass, 0);

        private static RegexCharClass ParseRecursive(string charClass, int start)
        {
            int setLength = charClass[start + SetLengthIndex];
            int categoryLength = charClass[start + CategoryLengthIndex];
            int endPosition = start + SetStartIndex + setLength + categoryLength;

            int i = start + SetStartIndex;
            int end = i + setLength;

            List<(char First, char Last)>? ranges = ComputeRanges(charClass.AsSpan(start));

            RegexCharClass? sub = null;
            if (charClass.Length > endPosition)
            {
                sub = ParseRecursive(charClass, endPosition);
            }

            StringBuilder? categoriesBuilder = null;
            if (categoryLength > 0)
            {
                categoriesBuilder = new StringBuilder().Append(charClass.AsSpan(end, categoryLength));
            }

            return new RegexCharClass(IsNegated(charClass, start), ranges, categoriesBuilder, sub);
        }

        /// <summary>Computes a list of all of the character ranges in the set string.</summary>
        public static List<(char First, char Last)>? ComputeRanges(ReadOnlySpan<char> set)
        {
            int setLength = set[SetLengthIndex];
            int i = SetStartIndex;
            int end = i + setLength;

            List<(char First, char Last)>? ranges = null;
            if (setLength > 0)
            {
                ranges = new List<(char First, char Last)>(setLength);
                while (i < end)
                {
                    char first = set[i];
                    i++;

                    char last = i < end ? (char)(set[i] - 1) : LastChar;
                    i++;

                    ranges.Add((first, last));
                }
            }

            return ranges;
        }

        /// <summary>Cache of character class strings for single ASCII characters.</summary>
        private static readonly string[] s_asciiStrings = new string[128];
        /// <summary>Cache of character class strings for pairs of upper/lower-case ASCII letters.</summary>
        private static readonly string[] s_asciiLetterPairStrings = new string[26];

        /// <summary>Creates a set string for a single character.</summary>
        /// <param name="c">The character for which to create the set.</param>
        /// <returns>The create set string.</returns>
        public static string OneToStringClass(char c)
            => CharsToStringClass([c]);

        internal static string CharsToStringClass(ReadOnlySpan<char> chars)
        {
#if DEBUG
            // Make sure they're all sorted with no duplicates
            for (int index = 0; index < chars.Length - 1; index++)
            {
                Debug.Assert(chars[index] < chars[index + 1]);
            }
#endif

            switch (chars.Length)
            {
                case 0:
                    // If there aren't any chars, just return an empty class.
                    return EmptyClass;

                case 1:
                    // Special-case ASCII characters to avoid the computation/allocation in this very common case.
                    if (chars[0] < 128)
                    {
                        string[] asciiStrings = s_asciiStrings;
                        if (chars[0] < asciiStrings.Length)
                        {
                            return asciiStrings[chars[0]] ??= $"\0\u0002\0{chars[0]}{(char)(chars[0] + 1)}";
                        }
                    }
                    break;

                case 2:
                    // Special-case cased ASCII letter pairs to avoid the computation/allocation in this very common case.
                    int masked0 = chars[0] | 0x20;
                    if ((uint)(masked0 - 'a') <= 'z' - 'a' && masked0 == (chars[1] | 0x20))
                    {
                        return s_asciiLetterPairStrings[masked0 - 'a'] ??= $"\0\u0004\0{(char)(masked0 & ~0x20)}{(char)((masked0 & ~0x20) + 1)}{(char)masked0}{(char)(masked0 + 1)}";
                    }
                    break;
            }

            // Count how many characters there actually are.  All but the very last possible
            // char value will have two characters, one for the inclusive beginning of range
            // and one for the exclusive end of range.
            int count = chars.Length * 2;
            if (chars[chars.Length - 1] == LastChar)
            {
                count--;
            }

            // Get the pointer/length of the span to be able to pass it into string.Create.
            ReadOnlySpan<char> tmpChars = chars; // avoid address exposing the span and impacting the other code in the method that uses it
#if NET
            return string.Create(SetStartIndex + count, tmpChars, static (span, chars) =>
            {
                // Fill in the set string
                span[FlagsIndex] = (char)0;
                span[SetLengthIndex] = (char)(span.Length - SetStartIndex);
                span[CategoryLengthIndex] = (char)0;
                int i = SetStartIndex;
                foreach (char c in chars)
                {
                    span[i++] = c;
                    if (c != LastChar)
                    {
                        span[i++] = (char)(c + 1);
                    }
                }
                Debug.Assert(i == span.Length);
            });
#else
            unsafe
            {
                return StringExtensions.Create(SetStartIndex + count, (IntPtr)(&tmpChars), static (span, charsPtr) =>
                {
                    // Fill in the set string
                    span[FlagsIndex] = (char)0;
                    span[SetLengthIndex] = (char)(span.Length - SetStartIndex);
                    span[CategoryLengthIndex] = (char)0;
                    int i = SetStartIndex;
                    ReadOnlySpan<char> chars = *(ReadOnlySpan<char>*)charsPtr;
                    foreach (char c in chars)
                    {
                        span[i++] = c;
                        if (c != LastChar)
                        {
                            span[i++] = (char)(c + 1);
                        }
                    }
                    Debug.Assert(i == span.Length);
                });
            }
#endif
        }


        /// <summary>
        /// Constructs the string representation of the class.
        /// </summary>
        public string ToStringClass()
        {
            var vsb = new ValueStringBuilder(stackalloc char[256]);
            ToStringClass(ref vsb);
            return vsb.ToString();
        }

        private void ToStringClass(ref ValueStringBuilder vsb)
        {
            Canonicalize();

            int initialLength = vsb.Length;
            int categoriesLength = _categories?.Length ?? 0;
            Span<char> headerSpan = vsb.AppendSpan(SetStartIndex);
            headerSpan[FlagsIndex] = (char)(_negate ? 1 : 0);
            headerSpan[SetLengthIndex] = '\0'; // (will be replaced once we know how long a range we've added)
            headerSpan[CategoryLengthIndex] = (char)categoriesLength;

            // Append ranges
            List<(char First, char Last)>? rangelist = _rangelist;
            if (rangelist != null)
            {
                for (int i = 0; i < rangelist.Count; i++)
                {
                    (char First, char Last) currentRange = rangelist[i];
                    vsb.Append(currentRange.First);
                    if (currentRange.Last != LastChar)
                    {
                        vsb.Append((char)(currentRange.Last + 1));
                    }
                }
            }

            // Update the range length.  The ValueStringBuilder may have already had some
            // contents (if this is a subtactor), so we need to offset by the initial length.
            vsb[initialLength + SetLengthIndex] = (char)(vsb.Length - initialLength - SetStartIndex);

            // Append categories
            if (categoriesLength != 0)
            {
                foreach (ReadOnlyMemory<char> chunk in _categories!.GetChunks())
                {
                    vsb.Append(chunk.Span);
                }
            }

            // Append a subtractor if there is one.
            _subtractor?.ToStringClass(ref vsb);
        }

        /// <summary>
        /// Logic to reduce a character class to a unique, sorted form.
        /// </summary>
        private void Canonicalize()
        {
            List<(char First, char Last)>? rangelist = _rangelist;
            if (rangelist != null)
            {
                // Find and eliminate overlapping or abutting ranges.
                if (rangelist.Count > 1)
                {
                    rangelist.Sort((x, y) => x.First.CompareTo(y.First));

                    bool done = false;
                    int j = 0;

                    for (int i = 1; ; i++)
                    {
                        char last;
                        for (last = rangelist[j].Last; ; i++)
                        {
                            if (i == rangelist.Count || last == LastChar)
                            {
                                done = true;
                                break;
                            }

                            (char First, char Last) currentRange;
                            if ((currentRange = rangelist[i]).First > last + 1)
                            {
                                break;
                            }

                            if (last < currentRange.Last)
                            {
                                last = currentRange.Last;
                            }
                        }

                        rangelist[j] = (rangelist[j].First, last);

                        j++;

                        if (done)
                        {
                            break;
                        }

                        if (j < i)
                        {
                            rangelist[j] = rangelist[i];
                        }
                    }

                    rangelist.RemoveRange(j, rangelist.Count - j);
                }

                // If the class now represents a single negated range, but does so by including every
                // other character, invert it to produce a normalized form with a single range.  This
                // is valuable for subsequent optimizations in most of the engines.
                if (!_negate &&
                    _subtractor is null &&
                    (_categories is null || _categories.Length == 0))
                {
                    if (rangelist.Count == 2)
                    {
                        // There are two ranges in the list.  See if there's one missing range between them.
                        // Such a range might be as small as a single character.
                        if (rangelist[0].First == 0 &&
                            rangelist[1].Last == LastChar &&
                            rangelist[0].Last < rangelist[1].First - 1)
                        {
                            rangelist[0] = ((char)(rangelist[0].Last + 1), (char)(rangelist[1].First - 1));
                            rangelist.RemoveAt(1);
                            _negate = true;
                        }
                    }
                    else if (rangelist.Count == 1)
                    {
                        if (rangelist[0].First == 0)
                        {
                            // There's only one range in the list.  Does it include everything but the last char?
                            if (rangelist[0].Last == LastChar - 1)
                            {
                                rangelist[0] = (LastChar, LastChar);
                                _negate = true;
                            }
                        }
                        else if (rangelist[0].First == 1)
                        {
                            // Or everything but the first char?
                            if (rangelist[0].Last == LastChar)
                            {
                                rangelist[0] = ('\0', '\0');
                                _negate = true;
                            }
                        }
                    }
                }

                // If the class now has a range that includes everything, and if it doesn't have subtraction,
                // we can remove all of its categories, as they're duplicative (the set already includes everything).
                if (!_negate &&
                    _subtractor is null &&
                    _categories?.Length > 0 &&
                    rangelist.Count == 1 && rangelist[0].First == 0 && rangelist[0].Last == LastChar)
                {
                    _categories.Clear();
                }

                // If there's only a single character omitted from ranges, if there's no subtractor, and if there are categories,
                // see if that character is in the categories.  If it is, then we can replace whole thing with a complete "any" range.
                // If it's not, then we can remove the categories, as they're only duplicating the rest of the range, turning the set
                // into a "not one". This primarily helps in the case of a synthesized set from analysis that ends up combining '.' with
                // categories, as we want to reduce that set down to either [^\n] or [\0-\uFFFF]. (This can be extrapolated to any number
                // of missing characters; in fact, categories in general are superfluous and the entire set can be represented as ranges.
                // But categories serve as a space optimization, and we strike a balance between testing many characters and the time/complexity
                // it takes to do so.  Thus, we limit this to the common case of a single missing character.)
                if (!_negate &&
                    _subtractor is null &&
                    _categories?.Length > 0 &&
                    rangelist.Count == 2 && rangelist[0].First == 0 && rangelist[0].Last + 2 == rangelist[1].First && rangelist[1].Last == LastChar)
                {
                    var vsb = new ValueStringBuilder(stackalloc char[256]);
                    foreach (ReadOnlyMemory<char> chunk in _categories!.GetChunks())
                    {
                        vsb.Append(chunk.Span);
                    }

                    if (CharInCategory((char)(rangelist[0].Last + 1), vsb.AsSpan()))
                    {
                        rangelist.RemoveAt(1);
                        rangelist[0] = ('\0', LastChar);
                    }
                    else
                    {
                        _negate = true;
                        rangelist.RemoveAt(1);
                        char notOne = (char)(rangelist[0].Last + 1);
                        rangelist[0] = (notOne, notOne);
                    }
                    _categories.Clear();

                    vsb.Dispose();
                }
            }
        }

        private static ReadOnlySpan<char> RangesFromProperty(string capname, bool invert, string pattern, int currentPos)
        {
            int min = 0;
            int max = s_propTable.Length;
            while (min != max)
            {
                int mid = (min + max) / 2;
                int res = string.Compare(capname, s_propTable[mid][0], StringComparison.Ordinal);
                if (res < 0)
                {
                    max = mid;
                }
                else if (res > 0)
                {
                    min = mid + 1;
                }
                else
                {
                    string set = s_propTable[mid][1];
                    Debug.Assert(!string.IsNullOrEmpty(set), "Found a null/empty element in RegexCharClass prop table");
                    return
                        !invert ? set.AsSpan() :
                        set[0] == '\0' ? set.AsSpan(1) :
                        ("\0" + set).AsSpan();
                }
            }

            throw new RegexParseException(RegexParseError.UnrecognizedUnicodeProperty, currentPos,
                SR.Format(SR.MakeException, pattern, currentPos, SR.Format(SR.UnrecognizedUnicodeProperty, capname)));
        }

#if DEBUG || !SYSTEM_TEXT_REGULAREXPRESSIONS
        private static readonly string[] CategoryIdToName = PopulateCategoryIdToName();

        private static string[] PopulateCategoryIdToName()
        {
            // Populate category reverse lookup used for diagnostic output

            var temp = new List<KeyValuePair<string, string>>(s_definedCategories);
            temp.RemoveAll(kvp => kvp.Value.Length != 1);
            temp.Sort((kvp1, kvp2) => ((short)kvp1.Value[0]).CompareTo((short)kvp2.Value[0]));
            return temp.ConvertAll(kvp => kvp.Key).ToArray();
        }

        /// <summary>
        /// Produces a human-readable description for a set string.
        /// </summary>
        /// <param name="set">The set string to describe.</param>
        /// <param name="forceBrackets">Whether to force brackets around the description even for single characters.</param>
        public static string DescribeSet(string set, bool forceBrackets = false)
        {
            int setLength = set[SetLengthIndex];
            int categoryLength = set[CategoryLengthIndex];
            int endPosition = SetStartIndex + setLength + categoryLength;
            bool negated = IsNegated(set);
            Span<char> scratch = stackalloc char[32];

            // Special-case set of a single character to output that character without set square brackets.
            if (!forceBrackets && // brackets not forced
                !negated && // no negation
                categoryLength == 0 && // no categories
                endPosition >= set.Length && // no subtraction
                setLength == 2 && // don't bother handling the case of the single character being 0xFFFF, in which case setLength would be 1
                set[SetStartIndex] + 1 == set[SetStartIndex + 1])
            {
                return DescribeChar(set[SetStartIndex]);
            }

            int index = SetStartIndex;
            char ch1;
            char ch2;
            StringBuilder desc = new StringBuilder().Append('[');

            void RenderRanges()
            {
                int rangesEnd = SetStartIndex + set[SetLengthIndex];
                while (index < rangesEnd)
                {
                    ch1 = set[index];
                    if (index + 1 < rangesEnd)
                    {
                        ch2 = (char)(set[index + 1] - 1);
                        index += 2;
                    }
                    else
                    {
                        ch2 = LastChar;
                        index++;
                    }

                    desc.Append(DescribeChar(ch1));

                    if (ch2 != ch1)
                    {
                        if (ch1 + 1 != ch2)
                        {
                            desc.Append('-');
                        }

                        desc.Append(DescribeChar(ch2));
                    }
                }
            }

            // Special-case sets where the description will be more succinct by rendering it as negated, e.g. where
            // there are fewer gaps between ranges than there are ranges.  This is the case when the first range
            // includes \0 and the last range includes 0xFFFF, and typically occurs for sets that were actually
            // initially negated but ended up as non-negated from various transforms along the way.
            if (categoryLength == 0 && // no categories
                endPosition >= set.Length && // no subtraction
                setLength % 2 == 1 && // odd number of values because the last range won't include an upper bound
                set[index] == 0)
            {
                // We now have an odd number of values structures as:
                //     0,end0,start1,end1,start2,end2,...,startN
                // Rather than walking the pairs starting from index 0, we walk pairs starting from index 1 (creating a range from end0 to start1),
                // since we're creating ranges from the gaps.
                index++;
                desc.Append('^');
                RenderRanges();
                return desc.Append(']').ToString();
            }

            if (negated)
            {
                desc.Append('^');
            }

            RenderRanges();

            while (index < SetStartIndex + set[SetLengthIndex] + set[CategoryLengthIndex])
            {
                ch1 = set[index];
                if (ch1 == 0)
                {
                    const char GroupChar = '\0';
                    int lastindex = set.IndexOf(GroupChar, index + 1);
                    if (lastindex != -1)
                    {
                        ReadOnlySpan<char> group = set.AsSpan(index, lastindex - index + 1);
                        switch (group)
                        {
                            case WordCategories:
                                desc.Append(@"\w");
                                break;

                            case NotWordCategories:
                                desc.Append(@"\W");
                                break;

                            default:
                                // The inverse of a group as created by AddCategoryFromName simply negates every character as a 16-bit value.
                                Span<char> invertedGroup = group.Length <= scratch.Length ? scratch.Slice(0, group.Length) : new char[group.Length];
                                for (int i = 0; i < group.Length; i++)
                                {
                                    invertedGroup[i] = (char)-(short)group[i];
                                }

                                // Determine whether the group is a known Unicode category, e.g. \p{Mc}, or group of categories, e.g. \p{L},
                                // or the inverse of those.
                                foreach (KeyValuePair<string, string> kvp in s_definedCategories)
                                {
                                    bool equalsGroup = group.SequenceEqual(kvp.Value.AsSpan());
                                    if (equalsGroup || invertedGroup.SequenceEqual(kvp.Value.AsSpan()))
                                    {
                                        desc.Append(equalsGroup ? @"\p{" : @"\P{").Append(kvp.Key).Append('}');
                                        break;
                                    }
                                }
                                break;
                        }

                        index = lastindex;
                    }
                }
                else
                {
                    desc.Append(DescribeCategory(ch1));
                }

                index++;
            }

            if (set.Length > endPosition)
            {
                desc.Append('-').Append(DescribeSet(set.Substring(endPosition), forceBrackets: true));
            }

            return desc.Append(']').ToString();
        }

        /// <summary>Produces a human-readable description for a single character.</summary>
        public static string DescribeChar(char ch) =>
            ch switch
            {
                '\0' => @"\0",
                '\a' => "\\a",
                '\b' => "\\b",
                '\t' => "\\t",
                '\r' => "\\r",
                '\v' => "\\v",
                '\f' => "\\f",
                '\n' => "\\n",
                '\\' => "\\\\",
                '-'  => "\\-",
                >= ' ' and <= '~' => ch.ToString(),
                _ => $"\\u{(uint)ch:X4}"
            };

        private static string DescribeCategory(char ch) =>
            (short)ch switch
            {
                SpaceConst => @"\s",
                NotSpaceConst => @"\S",
                (short)(UnicodeCategory.DecimalDigitNumber + 1) => @"\d",
                -(short)(UnicodeCategory.DecimalDigitNumber + 1) => @"\D",
                < 0 => $"\\P{{{CategoryIdToName[-(short)ch - 1]}}}",
                _ => $"\\p{{{CategoryIdToName[ch - 1]}}}",
            };
#endif
    }
}
