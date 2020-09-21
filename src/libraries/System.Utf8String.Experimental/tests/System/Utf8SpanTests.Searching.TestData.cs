// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;

using static System.Tests.Utf8TestUtilities;

using ustring = System.Utf8String;

namespace System.Text.Tests
{
    public partial class Utf8SpanTests
    {
        public static IEnumerable<object[]> TryFindData_Char_Ordinal()
        {
            foreach (TryFindTestData entry in TryFindData_All())
            {
                if (!entry.Options.HasFlag(TryFindTestDataOptions.TestOrdinal) || entry.Options.HasFlag(TryFindTestDataOptions.TestIgnoreCaseOnly))
                {
                    continue;
                }

                if (!TryParseSearchTermAsChar(entry.SearchTerm, out char searchChar))
                {
                    continue;
                }

                yield return new object[]
                {
                    entry.Source,
                    searchChar,
                    entry.ExpectedFirstMatch,
                    entry.ExpectedLastMatch,
                };
            }
        }

        public static IEnumerable<object[]> TryFindData_Char_WithComparison()
        {
            foreach (TryFindTestData entry in TryFindData_All())
            {
                if (!TryParseSearchTermAsChar(entry.SearchTerm, out char searchChar))
                {
                    continue;
                }

                if (entry.Options.HasFlag(TryFindTestDataOptions.TestOrdinal))
                {
                    if (!entry.Options.HasFlag(TryFindTestDataOptions.TestIgnoreCaseOnly))
                    {
                        yield return new object[]
                        {
                            entry.Source,
                            searchChar,
                            StringComparison.Ordinal,
                            null /* culture */,
                            entry.ExpectedFirstMatch,
                            entry.ExpectedLastMatch,
                        };
                    }
                    if (!entry.Options.HasFlag(TryFindTestDataOptions.TestCaseSensitiveOnly))
                    {
                        yield return new object[]
                        {
                            entry.Source,
                            searchChar,
                            StringComparison.OrdinalIgnoreCase,
                            null /* culture */,
                            entry.ExpectedFirstMatch,
                            entry.ExpectedLastMatch,
                        };
                    }
                }

                foreach (CultureInfo culture in entry.AdditionalCultures ?? Array.Empty<CultureInfo>())
                {
                    if (culture == CultureInfo.InvariantCulture)
                    {
                        if (!entry.Options.HasFlag(TryFindTestDataOptions.TestIgnoreCaseOnly))
                        {
                            yield return new object[]
                            {
                            entry.Source,
                            searchChar,
                            StringComparison.InvariantCulture,
                            null /* culture */,
                            entry.ExpectedFirstMatch,
                            entry.ExpectedLastMatch,
                            };
                        }
                        if (!entry.Options.HasFlag(TryFindTestDataOptions.TestCaseSensitiveOnly))
                        {
                            yield return new object[]
                            {
                            entry.Source,
                            searchChar,
                            StringComparison.InvariantCultureIgnoreCase,
                            null /* culture */,
                            entry.ExpectedFirstMatch,
                            entry.ExpectedLastMatch,
                            };
                        }
                    }

                    if (!entry.Options.HasFlag(TryFindTestDataOptions.TestIgnoreCaseOnly))
                    {
                        yield return new object[]
                        {
                            entry.Source,
                            searchChar,
                            StringComparison.CurrentCulture,
                            culture,
                            entry.ExpectedFirstMatch,
                            entry.ExpectedLastMatch,
                        };
                    }
                    if (!entry.Options.HasFlag(TryFindTestDataOptions.TestCaseSensitiveOnly))
                    {
                        yield return new object[]
                        {
                            entry.Source,
                            searchChar,
                            StringComparison.CurrentCultureIgnoreCase,
                            culture,
                            entry.ExpectedFirstMatch,
                            entry.ExpectedLastMatch,
                        };
                    }
                }
            }
        }

        public static IEnumerable<object[]> TryFindData_Rune_Ordinal()
        {
            foreach (TryFindTestData entry in TryFindData_All())
            {
                if (!entry.Options.HasFlag(TryFindTestDataOptions.TestOrdinal) || entry.Options.HasFlag(TryFindTestDataOptions.TestIgnoreCaseOnly))
                {
                    continue;
                }

                if (!TryParseSearchTermAsRune(entry.SearchTerm, out Rune searchRune))
                {
                    continue;
                }

                yield return new object[]
                {
                    entry.Source,
                    searchRune,
                    entry.ExpectedFirstMatch,
                    entry.ExpectedLastMatch,
                };
            }
        }

        public static IEnumerable<object[]> TryFindData_Rune_WithComparison()
        {
            foreach (TryFindTestData entry in TryFindData_All())
            {
                if (!TryParseSearchTermAsRune(entry.SearchTerm, out Rune searchRune))
                {
                    continue;
                }

                if (entry.Options.HasFlag(TryFindTestDataOptions.TestOrdinal))
                {
                    if (!entry.Options.HasFlag(TryFindTestDataOptions.TestIgnoreCaseOnly))
                    {
                        yield return new object[]
                        {
                            entry.Source,
                            searchRune,
                            StringComparison.Ordinal,
                            null /* culture */,
                            entry.ExpectedFirstMatch,
                            entry.ExpectedLastMatch,
                        };
                    }
                    if (!entry.Options.HasFlag(TryFindTestDataOptions.TestCaseSensitiveOnly))
                    {
                        yield return new object[]
                        {
                            entry.Source,
                            searchRune,
                            StringComparison.OrdinalIgnoreCase,
                            null /* culture */,
                            entry.ExpectedFirstMatch,
                            entry.ExpectedLastMatch,
                        };
                    }
                }

                foreach (CultureInfo culture in entry.AdditionalCultures ?? Array.Empty<CultureInfo>())
                {
                    if (culture == CultureInfo.InvariantCulture)
                    {
                        if (!entry.Options.HasFlag(TryFindTestDataOptions.TestIgnoreCaseOnly))
                        {
                            yield return new object[]
                            {
                                entry.Source,
                                searchRune,
                                StringComparison.InvariantCulture,
                                null /* culture */,
                                entry.ExpectedFirstMatch,
                                entry.ExpectedLastMatch,
                            };
                        }
                        if (!entry.Options.HasFlag(TryFindTestDataOptions.TestCaseSensitiveOnly))
                        {
                            yield return new object[]
                            {
                                entry.Source,
                                searchRune,
                                StringComparison.InvariantCultureIgnoreCase,
                                null /* culture */,
                                entry.ExpectedFirstMatch,
                                entry.ExpectedLastMatch,
                            };
                        }
                    }

                    if (!entry.Options.HasFlag(TryFindTestDataOptions.TestIgnoreCaseOnly))
                    {
                        yield return new object[]
                        {
                            entry.Source,
                            searchRune,
                            StringComparison.CurrentCulture,
                            culture,
                            entry.ExpectedFirstMatch,
                            entry.ExpectedLastMatch,
                        };
                    }
                    if (!entry.Options.HasFlag(TryFindTestDataOptions.TestCaseSensitiveOnly))
                    {
                        yield return new object[]
                        {
                            entry.Source,
                            searchRune,
                            StringComparison.CurrentCultureIgnoreCase,
                            culture,
                            entry.ExpectedFirstMatch,
                            entry.ExpectedLastMatch,
                        };
                    }
                }
            }
        }

        public static IEnumerable<object[]> TryFindData_Utf8Span_Ordinal()
        {
            foreach (TryFindTestData entry in TryFindData_All())
            {
                if (!entry.Options.HasFlag(TryFindTestDataOptions.TestOrdinal) || entry.Options.HasFlag(TryFindTestDataOptions.TestIgnoreCaseOnly))
                {
                    continue;
                }

                if (!TryParseSearchTermAsUtf8String(entry.SearchTerm, out ustring searchTerm))
                {
                    continue;
                }

                yield return new object[]
                {
                    entry.Source,
                    searchTerm,
                    entry.ExpectedFirstMatch,
                    entry.ExpectedLastMatch,
                };
            }
        }

        public static IEnumerable<object[]> TryFindData_Utf8Span_WithComparison()
        {
            foreach (TryFindTestData entry in TryFindData_All())
            {
                if (!TryParseSearchTermAsUtf8String(entry.SearchTerm, out ustring searchTerm))
                {
                    continue;
                }

                if (entry.Options.HasFlag(TryFindTestDataOptions.TestOrdinal))
                {
                    if (!entry.Options.HasFlag(TryFindTestDataOptions.TestIgnoreCaseOnly))
                    {
                        yield return new object[]
                        {
                            entry.Source,
                            searchTerm,
                            StringComparison.Ordinal,
                            null /* culture */,
                            entry.ExpectedFirstMatch,
                            entry.ExpectedLastMatch,
                        };
                    }
                    if (!entry.Options.HasFlag(TryFindTestDataOptions.TestCaseSensitiveOnly))
                    {
                        yield return new object[]
                        {
                            entry.Source,
                            searchTerm,
                            StringComparison.OrdinalIgnoreCase,
                            null /* culture */,
                            entry.ExpectedFirstMatch,
                            entry.ExpectedLastMatch,
                        };
                    }
                }

                foreach (CultureInfo culture in entry.AdditionalCultures ?? Array.Empty<CultureInfo>())
                {
                    if (culture == CultureInfo.InvariantCulture)
                    {
                        if (!entry.Options.HasFlag(TryFindTestDataOptions.TestIgnoreCaseOnly))
                        {
                            yield return new object[]
                            {
                                entry.Source,
                                searchTerm,
                                StringComparison.InvariantCulture,
                                null /* culture */,
                                entry.ExpectedFirstMatch,
                                entry.ExpectedLastMatch,
                            };
                        }
                        if (!entry.Options.HasFlag(TryFindTestDataOptions.TestCaseSensitiveOnly))
                        {
                            yield return new object[]
                            {
                                entry.Source,
                                searchTerm,
                                StringComparison.InvariantCultureIgnoreCase,
                                null /* culture */,
                                entry.ExpectedFirstMatch,
                                entry.ExpectedLastMatch,
                            };
                        }
                    }

                    if (!entry.Options.HasFlag(TryFindTestDataOptions.TestIgnoreCaseOnly))
                    {
                        yield return new object[]
                        {
                            entry.Source,
                            searchTerm,
                            StringComparison.CurrentCulture,
                            culture,
                            entry.ExpectedFirstMatch,
                            entry.ExpectedLastMatch,
                        };
                    }
                    if (!entry.Options.HasFlag(TryFindTestDataOptions.TestCaseSensitiveOnly))
                    {
                        yield return new object[]
                        {
                            entry.Source,
                            searchTerm,
                            StringComparison.CurrentCultureIgnoreCase,
                            culture,
                            entry.ExpectedFirstMatch,
                            entry.ExpectedLastMatch,
                        };
                    }
                }
            }
        }

        private static IEnumerable<TryFindTestData> TryFindData_All()
        {
            CultureInfo inv = CultureInfo.InvariantCulture;
            CultureInfo en_US = CultureInfo.GetCultureInfo("en-US");
            CultureInfo tr_TR = CultureInfo.GetCultureInfo("tr-TR");
            CultureInfo hu_HU = CultureInfo.GetCultureInfo("hu-HU");

            TryFindTestData[] testDataEntries = new TryFindTestData[]
            {
                new TryFindTestData
                {
                    // Searching for the empty string within the empty string should result in 0..0 / ^0..^0 across all comparers and all cultures
                    Source = null,
                    SearchTerm = null,
                    Options = TryFindTestDataOptions.TestOrdinal,
                    AdditionalCultures = new CultureInfo[] { inv, en_US, tr_TR, hu_HU },
                    ExpectedFirstMatch = 0..0,
                    ExpectedLastMatch = ^0..^0,
                },
                new TryFindTestData
                {
                    // Searching for the empty string within a non-empty string should result in 0..0 / ^0..^0 across all comparers and all cultures
                    Source = u8("Hello"),
                    SearchTerm = null,
                    Options = TryFindTestDataOptions.TestOrdinal,
                    AdditionalCultures = new CultureInfo[] { inv, en_US, tr_TR, hu_HU },
                    ExpectedFirstMatch = 0..0,
                    ExpectedLastMatch = ^0..^0,
                },
                new TryFindTestData
                {
                    // Searching for a non-empty string within an empty string should fail across all comparers and all cultures
                    Source = null,
                    SearchTerm = u8("Hello"),
                    Options = TryFindTestDataOptions.TestOrdinal,
                    AdditionalCultures = new CultureInfo[] { inv, en_US, tr_TR, hu_HU },
                    ExpectedFirstMatch = null,
                    ExpectedLastMatch = null,
                },
                new TryFindTestData
                {
                    // Searching for the null terminator shouldn't match unless the input contains a null terminator
                    Source = u8("Hello"),
                    SearchTerm = '\0',
                    Options = TryFindTestDataOptions.TestOrdinal,
                    AdditionalCultures = null,
                    ExpectedFirstMatch = null,
                    ExpectedLastMatch = null,
                },
                new TryFindTestData
                {
                    // Searching for the null terminator shouldn't match unless the input contains a null terminator
                    Source = u8("H\0ell\0o"),
                    SearchTerm = '\0',
                    Options = TryFindTestDataOptions.TestOrdinal,
                    AdditionalCultures = null,
                    ExpectedFirstMatch = 1..2,
                    ExpectedLastMatch = ^2..^1,
                },
                new TryFindTestData
                {
                    // Simple ASCII search with success (case-sensitive)
                    Source = u8("Hello"),
                    SearchTerm = 'l',
                    Options = TryFindTestDataOptions.TestOrdinal | TryFindTestDataOptions.TestCaseSensitiveOnly,
                    AdditionalCultures = new CultureInfo[] { inv },
                    ExpectedFirstMatch = 2..3,
                    ExpectedLastMatch = 3..4,
                },
                new TryFindTestData
                {
                    // Simple ASCII search with failure (case-sensitive)
                    Source = u8("Hello"),
                    SearchTerm = 'L',
                    Options = TryFindTestDataOptions.TestOrdinal | TryFindTestDataOptions.TestCaseSensitiveOnly,
                    AdditionalCultures = new CultureInfo[] { inv },
                    ExpectedFirstMatch = null,
                    ExpectedLastMatch = null,
                },
                new TryFindTestData
                {
                    // Simple ASCII search with success (case-insensitive)
                    Source = u8("Hello"),
                    SearchTerm = 'L',
                    Options = TryFindTestDataOptions.TestOrdinal | TryFindTestDataOptions.TestIgnoreCaseOnly,
                    AdditionalCultures = new CultureInfo[] { inv },
                    ExpectedFirstMatch = 2..3,
                    ExpectedLastMatch = 3..4,
                },
                new TryFindTestData
                {
                    // U+1F600 GRINNING FACE, should match an exact Rune search
                    Source = u8("x\U0001F600y"),
                    SearchTerm = new Rune(0x1F600),
                    Options = TryFindTestDataOptions.TestOrdinal,
                    AdditionalCultures = new CultureInfo[] { inv },
                    ExpectedFirstMatch = 1..5,
                    ExpectedLastMatch = 1..5,
                },
                new TryFindTestData
                {
                    // U+1F600 GRINNING FACE, shouldn't match looking for individual UTF-16 surrogate chars
                    Source = u8("x\ud83d\ude00y"),
                    SearchTerm = '\ud83d',
                    Options = TryFindTestDataOptions.TestOrdinal,
                    AdditionalCultures = new CultureInfo[] { inv },
                    ExpectedFirstMatch = null,
                    ExpectedLastMatch = null,
                },
                new TryFindTestData
                {
                    // U+1F600 GRINNING FACE, shouldn't match on the standalone [ F0 ] byte that begins the multi-byte sequence
                    Source = u8("x\ud83d\ude00y"),
                    SearchTerm = '\u00f0',
                    Options = TryFindTestDataOptions.TestOrdinal,
                    AdditionalCultures = new CultureInfo[] { inv },
                    ExpectedFirstMatch = null,
                    ExpectedLastMatch = null,
                },
                new TryFindTestData
                {
                    // hu_HU shouldn't match "d" within "dz"
                    Source = u8("ab_dz_ba"),
                    SearchTerm = 'd',
                    Options = TryFindTestDataOptions.None,
                    AdditionalCultures = new CultureInfo[] { hu_HU },
                    ExpectedFirstMatch = null,
                    ExpectedLastMatch = null,
                },
                new TryFindTestData
                {
                    // Turkish I, case-sensitive
                    Source = u8("\u0069\u0130\u0131\u0049"), // iİıI
                    SearchTerm = 'i',
                    Options = TryFindTestDataOptions.TestCaseSensitiveOnly,
                    AdditionalCultures = new CultureInfo[] { tr_TR },
                    ExpectedFirstMatch = 0..1,
                    ExpectedLastMatch = 0..1,
                },
                new TryFindTestData
                {
                    // Turkish I, case-insensitive
                    Source = u8("\u0069\u0130\u0131\u0049"), // iİıI
                    SearchTerm = 'i',
                    Options = TryFindTestDataOptions.TestIgnoreCaseOnly,
                    AdditionalCultures = new CultureInfo[] { tr_TR },
                    ExpectedFirstMatch = 0..1,
                    ExpectedLastMatch = 1..3,
                },
                new TryFindTestData
                {
                    // denormalized forms, no match
                    Source = u8("a\u0308e\u0308A\u0308E\u0308"), // äëÄË (denormalized)
                    SearchTerm = 'e', // shouldn't match letter paired with diacritic
                    Options = TryFindTestDataOptions.None,
                    AdditionalCultures = new CultureInfo[] { inv },
                    ExpectedFirstMatch = null,
                    ExpectedLastMatch = null,
                },
                new TryFindTestData
                {
                    // denormalized forms, case-sensitive
                    Source = u8("a\u0308e\u0308A\u0308E\u0308"), // äëÄË (denormalized)
                    SearchTerm = '\u00eb', // ë, normalized form
                    Options = TryFindTestDataOptions.TestCaseSensitiveOnly,
                    AdditionalCultures = new CultureInfo[] { inv },
                    ExpectedFirstMatch = 3..6,
                    ExpectedLastMatch = 3..6,
                },
                new TryFindTestData
                {
                    // denormalized forms, case-insensitive
                    Source = u8("a\u0308e\u0308A\u0308E\u0308"), // äëÄË (denormalized)
                    SearchTerm = '\u00eb', // ë, normalized form
                    Options = TryFindTestDataOptions.TestIgnoreCaseOnly,
                    AdditionalCultures = new CultureInfo[] { inv },
                    ExpectedFirstMatch = 3..6,
                    ExpectedLastMatch = ^3..,
                },
            };

            return testDataEntries;
        }

        public class TryFindTestData
        {
            public ustring Source;
            public object SearchTerm;
            public TryFindTestDataOptions Options;
            public CultureInfo[] AdditionalCultures;
            public Range? ExpectedFirstMatch;
            public Range? ExpectedLastMatch;
        }

        [Flags]
        public enum TryFindTestDataOptions
        {
            None = 0,
            TestOrdinal = 1 << 0,
            TestCaseSensitiveOnly = 1 << 1,
            TestIgnoreCaseOnly = 2 << 1,
        }
    }
}
