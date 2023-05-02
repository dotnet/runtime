// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

// NOTE: The logic in this file is largely a duplicate of logic in RegexCompiler, emitting C# instead of MSIL.
// Most changes made to this file should be kept in sync, so far as bug fixes and relevant optimizations
// are concerned.

namespace System.Text.RegularExpressions.Generator
{
    public partial class RegexGenerator
    {
        /// <summary>Escapes '&amp;', '&lt;' and '&gt;' characters. We aren't using HtmlEncode as that would also escape single and double quotes.</summary>
        private static string EscapeXmlComment(string text) =>
            text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        /// <summary>Emits the definition of the partial method. This method just delegates to the property cache on the generated Regex-derived type.</summary>
        private static void EmitRegexPartialMethod(RegexMethod regexMethod, IndentedTextWriter writer)
        {
            // Emit the namespace.
            RegexType? parent = regexMethod.DeclaringType;
            if (!string.IsNullOrWhiteSpace(parent.Namespace))
            {
                writer.WriteLine($"namespace {parent.Namespace}");
                writer.WriteLine("{");
                writer.Indent++;
            }

            // Emit containing types.
            var parentClasses = new Stack<string>();
            while (parent is not null)
            {
                parentClasses.Push($"partial {parent.Keyword} {parent.Name}");
                parent = parent.Parent;
            }
            while (parentClasses.Count != 0)
            {
                writer.WriteLine($"{parentClasses.Pop()}");
                writer.WriteLine("{");
                writer.Indent++;
            }

            // Emit the partial method definition.
            writer.WriteLine("/// <remarks>");
            writer.WriteLine("/// Pattern explanation:<br/>");
            writer.WriteLine("/// <code>");
            DescribeExpressionAsXmlComment(writer, regexMethod.Tree.Root.Child(0), regexMethod); // skip implicit root capture
            writer.WriteLine("/// </code>");
            writer.WriteLine("/// </remarks>");
            writer.WriteLine($"[global::System.CodeDom.Compiler.{s_generatedCodeAttribute}]");
            writer.WriteLine($"{regexMethod.Modifiers} global::System.Text.RegularExpressions.Regex {regexMethod.MethodName}() => global::{GeneratedNamespace}.{regexMethod.GeneratedName}.Instance;");

            // Unwind all scopes
            while (writer.Indent != 0)
            {
                writer.Indent--;
                writer.WriteLine("}");
            }
        }

        /// <summary>Emits the Regex-derived type for a method where we're unable to generate custom code.</summary>
        private static void EmitRegexLimitedBoilerplate(
            IndentedTextWriter writer, RegexMethod rm, string reason, LanguageVersion langVer)
        {
            string visibility;
            if (langVer >= LanguageVersion.CSharp11)
            {
                visibility = "file";
                writer.WriteLine($"/// <summary>Caches a <see cref=\"Regex\"/> instance for the {rm.MethodName} method.</summary>");
            }
            else
            {
                visibility = "internal";
                writer.WriteLine($"/// <summary>This class supports generated regexes and should not be used by other code directly.</summary>");
            }
            writer.WriteLine($"/// <remarks>A custom Regex-derived type could not be generated because {reason}.</remarks>");
            writer.WriteLine($"[{s_generatedCodeAttribute}]");
            writer.WriteLine($"{visibility} sealed class {rm.GeneratedName} : Regex");
            writer.WriteLine($"{{");
            writer.WriteLine($"    /// <summary>Cached, thread-safe singleton instance.</summary>");
            writer.Write($"    internal static readonly Regex Instance = ");
            writer.WriteLine(
                rm.MatchTimeout is not null ? $"new({Literal(rm.Pattern)}, {Literal(rm.Options)}, {GetTimeoutExpression(rm.MatchTimeout.Value)});" :
                rm.Options != 0 ? $"new({Literal(rm.Pattern)}, {Literal(rm.Options)});" :
                $"new({Literal(rm.Pattern)});");
            writer.WriteLine($"}}");
        }

        /// <summary>Name of the helper type field that indicates the process-wide default timeout.</summary>
        private const string DefaultTimeoutFieldName = "s_defaultTimeout";

        /// <summary>Name of the helper type field that indicates whether <see cref="DefaultTimeoutFieldName"/> is non-infinite.</summary>
        private const string HasDefaultTimeoutFieldName = "s_hasTimeout";

        /// <summary>Emits the Regex-derived type for a method whose RunnerFactory implementation was generated into <paramref name="runnerFactoryImplementation"/>.</summary>
        private static void EmitRegexDerivedImplementation(
            IndentedTextWriter writer, RegexMethod rm, string runnerFactoryImplementation, bool allowUnsafe)
        {
            writer.WriteLine($"/// <summary>Custom <see cref=\"Regex\"/>-derived type for the {rm.MethodName} method.</summary>");
            writer.WriteLine($"[{s_generatedCodeAttribute}]");
            if (allowUnsafe)
            {
                writer.WriteLine($"[SkipLocalsInit]");
            }
            writer.WriteLine($"file sealed class {rm.GeneratedName} : Regex");
            writer.WriteLine($"{{");
            writer.WriteLine($"    /// <summary>Cached, thread-safe singleton instance.</summary>");
            writer.WriteLine($"    internal static readonly {rm.GeneratedName} Instance = new();");
            writer.WriteLine($"");
            writer.WriteLine($"    /// <summary>Initializes the instance.</summary>");
            writer.WriteLine($"    private {rm.GeneratedName}()");
            writer.WriteLine($"    {{");
            writer.WriteLine($"        base.pattern = {Literal(rm.Pattern)};");
            writer.WriteLine($"        base.roptions = {Literal(rm.Options)};");
            if (rm.MatchTimeout is not null)
            {
                writer.WriteLine($"        base.internalMatchTimeout = {GetTimeoutExpression(rm.MatchTimeout.Value)};");
            }
            else
            {
                writer.WriteLine($"        ValidateMatchTimeout({HelpersTypeName}.{DefaultTimeoutFieldName});");
                writer.WriteLine($"        base.internalMatchTimeout = {HelpersTypeName}.{DefaultTimeoutFieldName};");
            }
            writer.WriteLine($"        base.factory = new RunnerFactory();");
            if (rm.Tree.CaptureNumberSparseMapping is not null)
            {
                writer.Write("        base.Caps = new Hashtable {");
                AppendHashtableContents(writer, rm.Tree.CaptureNumberSparseMapping.Cast<DictionaryEntry>().OrderBy(de => de.Key as int?));
                writer.WriteLine($" }};");
            }
            if (rm.Tree.CaptureNameToNumberMapping is not null)
            {
                writer.Write("        base.CapNames = new Hashtable {");
                AppendHashtableContents(writer, rm.Tree.CaptureNameToNumberMapping.Cast<DictionaryEntry>().OrderBy(de => de.Key as string, StringComparer.Ordinal));
                writer.WriteLine($" }};");
            }
            if (rm.Tree.CaptureNames is not null)
            {
                writer.Write("        base.capslist = new string[] {");
                string separator = "";
                foreach (string s in rm.Tree.CaptureNames)
                {
                    writer.Write(separator);
                    writer.Write(Literal(s));
                    separator = ", ";
                }
                writer.WriteLine($" }};");
            }
            writer.WriteLine($"        base.capsize = {rm.Tree.CaptureCount};");
            writer.WriteLine($"    }}");
            writer.WriteLine(runnerFactoryImplementation);
            writer.WriteLine($"}}");

            static void AppendHashtableContents(IndentedTextWriter writer, IEnumerable<DictionaryEntry> contents)
            {
                string separator = "";
                foreach (DictionaryEntry en in contents)
                {
                    writer.Write(separator);
                    separator = ", ";

                    writer.Write(" { ");
                    if (en.Key is int key)
                    {
                        writer.Write(key);
                    }
                    else
                    {
                        writer.Write($"\"{en.Key}\"");
                    }
                    writer.Write($", {en.Value} }} ");
                }
            }
        }

        /// <summary>Emits the code for the RunnerFactory.  This is the actual logic for the regular expression.</summary>
        private static void EmitRegexDerivedTypeRunnerFactory(IndentedTextWriter writer, RegexMethod rm, Dictionary<string, string[]> requiredHelpers, bool checkOverflow)
        {
            void EnterCheckOverflow()
            {
                if (checkOverflow)
                {
                    writer.WriteLine($"unchecked");
                    writer.WriteLine($"{{");
                    writer.Indent++;
                }
            }

            void ExitCheckOverflow()
            {
                if (checkOverflow)
                {
                    writer.Indent--;
                    writer.WriteLine($"}}");
                }
            }

            writer.WriteLine($"/// <summary>Provides a factory for creating <see cref=\"RegexRunner\"/> instances to be used by methods on <see cref=\"Regex\"/>.</summary>");
            writer.WriteLine($"private sealed class RunnerFactory : RegexRunnerFactory");
            writer.WriteLine($"{{");
            writer.WriteLine($"    /// <summary>Creates an instance of a <see cref=\"RegexRunner\"/> used by methods on <see cref=\"Regex\"/>.</summary>");
            writer.WriteLine($"    protected override RegexRunner CreateInstance() => new Runner();");
            writer.WriteLine();
            writer.WriteLine($"    /// <summary>Provides the runner that contains the custom logic implementing the specified regular expression.</summary>");
            writer.WriteLine($"    private sealed class Runner : RegexRunner");
            writer.WriteLine($"    {{");
            if (rm.MatchTimeout is null)
            {
                // We need to emit timeout checks for everything other than the developer explicitly setting Timeout.Infinite.
                // In the common case where a timeout isn't specified, we need to at run-time check whether a process-wide
                // default timeout has been specified, so we emit a static readonly TimeSpan to store the default value
                // and a static readonly bool to store whether that value is non-infinite (the latter enables the JIT
                // to remove all timeout checks as part of tiering if the default is infinite).
                const string DefaultTimeoutHelpers = nameof(DefaultTimeoutHelpers);
                if (!requiredHelpers.ContainsKey(DefaultTimeoutHelpers))
                {
                    requiredHelpers.Add(DefaultTimeoutHelpers, new string[]
                    {
                        $"/// <summary>Default timeout value set in <see cref=\"AppContext\"/>, or <see cref=\"Regex.InfiniteMatchTimeout\"/> if none was set.</summary>",
                        $"internal static readonly TimeSpan {DefaultTimeoutFieldName} = AppContext.GetData(\"REGEX_DEFAULT_MATCH_TIMEOUT\") is TimeSpan timeout ? timeout : Regex.InfiniteMatchTimeout;",
                        $"",
                        $"/// <summary>Whether <see cref=\"{DefaultTimeoutFieldName}\"/> is non-infinite.</summary>",
                        $"internal static readonly bool {HasDefaultTimeoutFieldName} = {DefaultTimeoutFieldName} != Regex.InfiniteMatchTimeout;",
                    });
                }
            }
            writer.WriteLine($"        /// <summary>Scan the <paramref name=\"inputSpan\"/> starting from base.runtextstart for the next match.</summary>");
            writer.WriteLine($"        /// <param name=\"inputSpan\">The text being scanned by the regular expression.</param>");
            writer.WriteLine($"        protected override void Scan(ReadOnlySpan<char> inputSpan)");
            writer.WriteLine($"        {{");
            writer.Indent += 3;
            EnterCheckOverflow();
            (bool needsTryFind, bool needsTryMatch) = EmitScan(writer, rm);
            ExitCheckOverflow();
            writer.Indent -= 3;
            writer.WriteLine($"        }}");
            if (needsTryFind)
            {
                writer.WriteLine();
                writer.WriteLine($"        /// <summary>Search <paramref name=\"inputSpan\"/> starting from base.runtextpos for the next location a match could possibly start.</summary>");
                writer.WriteLine($"        /// <param name=\"inputSpan\">The text being scanned by the regular expression.</param>");
                writer.WriteLine($"        /// <returns>true if a possible match was found; false if no more matches are possible.</returns>");
                writer.WriteLine($"        private bool TryFindNextPossibleStartingPosition(ReadOnlySpan<char> inputSpan)");
                writer.WriteLine($"        {{");
                writer.Indent += 3;
                EnterCheckOverflow();
                EmitTryFindNextPossibleStartingPosition(writer, rm, requiredHelpers, checkOverflow);
                ExitCheckOverflow();
                writer.Indent -= 3;
                writer.WriteLine($"        }}");
            }
            if (needsTryMatch)
            {
                writer.WriteLine();
                writer.WriteLine($"        /// <summary>Determine whether <paramref name=\"inputSpan\"/> at base.runtextpos is a match for the regular expression.</summary>");
                writer.WriteLine($"        /// <param name=\"inputSpan\">The text being scanned by the regular expression.</param>");
                writer.WriteLine($"        /// <returns>true if the regular expression matches at the current position; otherwise, false.</returns>");
                writer.WriteLine($"        private bool TryMatchAtCurrentPosition(ReadOnlySpan<char> inputSpan)");
                writer.WriteLine($"        {{");
                writer.Indent += 3;
                EnterCheckOverflow();
                EmitTryMatchAtCurrentPosition(writer, rm, requiredHelpers, checkOverflow);
                ExitCheckOverflow();
                writer.Indent -= 3;
                writer.WriteLine($"        }}");
            }
            writer.WriteLine($"    }}");
            writer.WriteLine($"}}");
        }

        /// <summary>Gets a C# expression representing the specified timeout value.</summary>
        private static string GetTimeoutExpression(int matchTimeout) =>
            matchTimeout == Timeout.Infinite ?
                "Regex.InfiniteMatchTimeout" :
                $"TimeSpan.FromMilliseconds({matchTimeout.ToString(CultureInfo.InvariantCulture)})";

        /// <summary>Adds the IsWordChar helper to the required helpers collection.</summary>
        private static void AddIsWordCharHelper(Dictionary<string, string[]> requiredHelpers)
        {
            const string IsWordChar = nameof(IsWordChar);
            if (!requiredHelpers.ContainsKey(IsWordChar))
            {
                requiredHelpers.Add(IsWordChar, new string[]
                {
                    "/// <summary>Determines whether the character is part of the [\\w] set.</summary>",
                    "[MethodImpl(MethodImplOptions.AggressiveInlining)]",
                    "internal static bool IsWordChar(char ch)",
                    "{",
                    "    // Mask of Unicode categories that combine to form [\\w]",
                    "    const int WordCategoriesMask =",
                    "        1 << (int)UnicodeCategory.UppercaseLetter |",
                    "        1 << (int)UnicodeCategory.LowercaseLetter |",
                    "        1 << (int)UnicodeCategory.TitlecaseLetter |",
                    "        1 << (int)UnicodeCategory.ModifierLetter |",
                    "        1 << (int)UnicodeCategory.OtherLetter |",
                    "        1 << (int)UnicodeCategory.NonSpacingMark |",
                    "        1 << (int)UnicodeCategory.DecimalDigitNumber |",
                    "        1 << (int)UnicodeCategory.ConnectorPunctuation;",
                    "",
                    "    // Bitmap for whether each character 0 through 127 is in [\\w]",
                    "    ReadOnlySpan<byte> ascii = new byte[]",
                    "    {",
                    "        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x03,",
                    "        0xFE, 0xFF, 0xFF, 0x87, 0xFE, 0xFF, 0xFF, 0x07",
                    "    };",
                    "",
                    "    // If the char is ASCII, look it up in the bitmap. Otherwise, query its Unicode category.",
                    "    int chDiv8 = ch >> 3;",
                    "    return (uint)chDiv8 < (uint)ascii.Length ?",
                    "        (ascii[chDiv8] & (1 << (ch & 0x7))) != 0 :",
                    "        (WordCategoriesMask & (1 << (int)CharUnicodeInfo.GetUnicodeCategory(ch))) != 0;",
                    "}",
                });
            }
        }

        /// <summary>Adds the IsBoundary helper to the required helpers collection.</summary>
        private static void AddIsBoundaryHelper(Dictionary<string, string[]> requiredHelpers, bool checkOverflow)
        {
            const string IsBoundary = nameof(IsBoundary);
            if (!requiredHelpers.ContainsKey(IsBoundary))
            {
                string uncheckedKeyword = checkOverflow ? "unchecked" : "";
                requiredHelpers.Add(IsBoundary, new string[]
                {
                    $"/// <summary>Determines whether the specified index is a boundary.</summary>",
                    $"[MethodImpl(MethodImplOptions.AggressiveInlining)]",
                    $"internal static bool IsBoundary(ReadOnlySpan<char> inputSpan, int index)",
                    $"{{",
                    $"    int indexMinus1 = index - 1;",
                    $"    return {uncheckedKeyword}((uint)indexMinus1 < (uint)inputSpan.Length && IsBoundaryWordChar(inputSpan[indexMinus1])) !=",
                    $"           {uncheckedKeyword}((uint)index < (uint)inputSpan.Length && IsBoundaryWordChar(inputSpan[index]));",
                    $"",
                    $"    static bool IsBoundaryWordChar(char ch) => IsWordChar(ch) || (ch == '\\u200C' | ch == '\\u200D');",
                    $"}}",
                });

                AddIsWordCharHelper(requiredHelpers);
            }
        }

        /// <summary>Adds the IsECMABoundary helper to the required helpers collection.</summary>
        private static void AddIsECMABoundaryHelper(Dictionary<string, string[]> requiredHelpers, bool checkOverflow)
        {
            const string IsECMABoundary = nameof(IsECMABoundary);
            if (!requiredHelpers.ContainsKey(IsECMABoundary))
            {
                string uncheckedKeyword = checkOverflow ? "unchecked" : "";
                requiredHelpers.Add(IsECMABoundary, new string[]
                {
                    $"/// <summary>Determines whether the specified index is a boundary (ECMAScript).</summary>",
                    $"[MethodImpl(MethodImplOptions.AggressiveInlining)]",
                    $"internal static bool IsECMABoundary(ReadOnlySpan<char> inputSpan, int index)",
                    $"{{",
                    $"    int indexMinus1 = index - 1;",
                    $"    return {uncheckedKeyword}((uint)indexMinus1 < (uint)inputSpan.Length && IsECMAWordChar(inputSpan[indexMinus1])) !=",
                    $"           {uncheckedKeyword}((uint)index < (uint)inputSpan.Length && IsECMAWordChar(inputSpan[index]));",
                    $"",
                    $"    static bool IsECMAWordChar(char ch) =>",
                    $"        char.IsAsciiLetterOrDigit(ch) ||",
                    $"        ch == '_' ||",
                    $"        ch == '\\u0130'; // latin capital letter I with dot above",
                    $"}}",
                });
            }
        }

        /// <summary>Adds an IndexOfAnyValues instance declaration to the required helpers collection if the chars are ASCII.</summary>
        private static string EmitIndexOfAnyValuesOrLiteral(ReadOnlySpan<char> chars, Dictionary<string, string[]> requiredHelpers)
        {
            // IndexOfAnyValues<char> is faster than a regular IndexOfAny("abcd") for sets of 4/5 values iff they are ASCII.
            // Only emit IndexOfAnyValues instances when we know they'll be faster to avoid increasing the startup cost too much.
            Debug.Assert(chars.Length is 4 or 5);

            return RegexCharClass.IsAscii(chars)
                ? EmitIndexOfAnyValues(chars.ToArray(), requiredHelpers)
                : Literal(chars.ToString());
        }

        /// <summary>Adds an IndexOfAnyValues instance declaration to the required helpers collection.</summary>
        private static string EmitIndexOfAnyValues(char[] asciiChars, Dictionary<string, string[]> requiredHelpers)
        {
            Debug.Assert(RegexCharClass.IsAscii(asciiChars));

            // The set of ASCII characters can be represented as a 128-bit bitmap. Use the 16-byte hex string as the key.
            byte[] bitmap = new byte[16];
            foreach (char c in asciiChars)
            {
                bitmap[c >> 3] |= (byte)(1 << (c & 7));
            }

            string hexBitmap = BitConverter.ToString(bitmap).Replace("-", string.Empty);

            string fieldName = hexBitmap switch
            {
                "FFFFFFFF000000000000000000000080" => "s_asciiControl",
                "000000000000FF030000000000000000" => "s_asciiDigits",
                "0000000000000000FEFFFF07FEFFFF07" => "s_asciiLetters",
                "000000000000FF03FEFFFF07FEFFFF07" => "s_asciiLettersAndDigits",
                "000000000000FF037E0000007E000000" => "s_asciiHexDigits",
                "000000000000FF03000000007E000000" => "s_asciiHexDigitsLower",
                "000000000000FF037E00000000000000" => "s_asciiHexDigitsUpper",
                "00000000EEF7008C010000B800000028" => "s_asciiPunctuation",
                "00000000010000000000000000000000" => "s_asciiSeparators",
                "00000000100800700000004001000050" => "s_asciiSymbols",
                "003E0000010000000000000000000000" => "s_asciiWhiteSpace",
                "000000000000FF03FEFFFF87FEFFFF07" => "s_asciiWordChars",

                "00000000FFFFFFFFFFFFFFFFFFFFFF7F" => "s_asciiExceptControl",
                "FFFFFFFFFFFF00FCFFFFFFFFFFFFFFFF" => "s_asciiExceptDigits",
                "FFFFFFFFFFFFFFFF010000F8010000F8" => "s_asciiExceptLetters",
                "FFFFFFFFFFFF00FC010000F8010000F8" => "s_asciiExceptLettersAndDigits",
                "FFFFFFFFFFFFFFFFFFFFFFFF010000F8" => "s_asciiExceptLower",
                "FFFFFFFF1108FF73FEFFFF47FFFFFFD7" => "s_asciiExceptPunctuation",
                "FFFFFFFFFEFFFFFFFFFFFFFFFFFFFFFF" => "s_asciiExceptSeparators",
                "FFFFFFFFEFF7FF8FFFFFFFBFFEFFFFAF" => "s_asciiExceptSymbols",
                "FFFFFFFFFFFFFFFF010000F8FFFFFFFF" => "s_asciiExceptUpper",
                "FFC1FFFFFEFFFFFFFFFFFFFFFFFFFFFF" => "s_asciiExceptWhiteSpace",
                "FFFFFFFFFFFF00FC01000078010000F8" => "s_asciiExceptWordChars",

                _ => $"s_ascii_{hexBitmap.TrimStart('0')}"
            };

            if (!requiredHelpers.ContainsKey(fieldName))
            {
                Array.Sort(asciiChars);

                string setLiteral = Literal(new string(asciiChars));

                requiredHelpers.Add(fieldName, new string[]
                {
                    $"/// <summary>Supports searching for characters in or not in {EscapeXmlComment(setLiteral)}.</summary>",
                    $"internal static readonly IndexOfAnyValues<char> {fieldName} = IndexOfAnyValues.Create({setLiteral});",
                });
            }

            return $"{HelpersTypeName}.{fieldName}";
        }

        private static string EmitIndexOfAnyCustomHelper(string set, Dictionary<string, string[]> requiredHelpers, bool checkOverflow)
        {
            // In order to optimize the search for ASCII characters, we use IndexOfAnyValues to vectorize a search
            // for those characters plus anything non-ASCII (if we find something non-ASCII, we'll fall back to
            // a sequential walk).  In order to do that search, we actually build up a set for all of the ASCII
            // characters _not_ contained in the set, and then do a search for the inverse of that, which will be
            // all of the target ASCII characters and all of non-ASCII.
            var asciiChars = new List<char>();
            for (int i = 0; i < 128; i++)
            {
                if (!RegexCharClass.CharInClass((char)i, set))
                {
                    asciiChars.Add((char)i);
                }
            }

            // If this is a known set, use a predetermined simple name for the helper.
            string? helperName = set switch
            {
                RegexCharClass.DigitClass => "IndexOfAnyDigit",
                RegexCharClass.ControlClass => "IndexOfAnyControl",
                RegexCharClass.LetterClass => "IndexOfAnyLetter",
                RegexCharClass.LetterOrDigitClass => "IndexOfAnyLetterOrDigit",
                RegexCharClass.LowerClass => "IndexOfAnyLower",
                RegexCharClass.NumberClass => "IndexOfAnyNumber",
                RegexCharClass.PunctuationClass => "IndexOfAnyPunctuation",
                RegexCharClass.SeparatorClass => "IndexOfAnySeparator",
                RegexCharClass.SpaceClass => "IndexOfAnyWhiteSpace",
                RegexCharClass.SymbolClass => "IndexOfAnySymbol",
                RegexCharClass.UpperClass => "IndexOfAnyUpper",
                RegexCharClass.WordClass => "IndexOfAnyWordChar",

                RegexCharClass.NotDigitClass => "IndexOfAnyExceptDigit",
                RegexCharClass.NotControlClass => "IndexOfAnyExceptControl",
                RegexCharClass.NotLetterClass => "IndexOfAnyExceptLetter",
                RegexCharClass.NotLetterOrDigitClass => "IndexOfAnyExceptLetterOrDigit",
                RegexCharClass.NotLowerClass => "IndexOfAnyExceptLower",
                RegexCharClass.NotNumberClass => "IndexOfAnyExceptNumber",
                RegexCharClass.NotPunctuationClass => "IndexOfAnyExceptPunctuation",
                RegexCharClass.NotSeparatorClass => "IndexOfAnyExceptSeparator",
                RegexCharClass.NotSpaceClass => "IndexOfAnyExceptWhiteSpace",
                RegexCharClass.NotSymbolClass => "IndexOfAnyExceptSymbol",
                RegexCharClass.NotUpperClass => "IndexOfAnyExceptUpper",
                RegexCharClass.NotWordClass => "IndexOfAnyExceptWordChar",

                _ => null,
            };

            // If this set is just from a few Unicode categories, derive a name from the categories.
            if (helperName is null)
            {
                Span<UnicodeCategory> categories = stackalloc UnicodeCategory[5]; // arbitrary limit to keep names from being too unwieldy
                if (RegexCharClass.TryGetOnlyCategories(set, categories, out int numCategories, out bool negatedCategory))
                {
                    helperName = $"IndexOfAny{(negatedCategory ? "Except" : "")}{string.Concat(categories.Slice(0, numCategories).ToArray().Select(c => c.ToString()))}";
                }
            }

            // As a final fallback, manufacture a name unique to the full set description.
            if (helperName is null)
            {
                using (SHA256 sha = SHA256.Create())
                {
#pragma warning disable CA1850 // SHA256.HashData isn't available on netstandard2.0
                    helperName = $"IndexOfNonAsciiOrAny_{BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(set))).Replace("-", "")}";
#pragma warning restore CA1850
                }
            }

            if (!requiredHelpers.ContainsKey(helperName))
            {
                var additionalDeclarations = new HashSet<string>();
                string matchExpr = MatchCharacterClass("span[i]", set, negate: false, additionalDeclarations, requiredHelpers);

                var lines = new List<string>();
                lines.Add($"/// <summary>Finds the next index of any character that matches {EscapeXmlComment(DescribeSet(set))}.</summary>");
                lines.Add($"[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                lines.Add($"internal static int {helperName}(this ReadOnlySpan<char> span)");
                lines.Add($"{{");
                int uncheckedStart = lines.Count;
                lines.Add(asciiChars.Count == 128 ?
                          $"    int i = span.IndexOfAnyExceptInRange('\0', '\u007f');" :
                          $"    int i = span.IndexOfAnyExcept({EmitIndexOfAnyValues(asciiChars.ToArray(), requiredHelpers)});");
                lines.Add($"    if ((uint)i < (uint)span.Length)");
                lines.Add($"    {{");
                lines.Add($"        if (char.IsAscii(span[i]))");
                lines.Add($"        {{");
                lines.Add($"            return i;");
                lines.Add($"        }}");
                lines.Add($"");
                if (additionalDeclarations.Count > 0)
                {
                    lines.AddRange(additionalDeclarations.Select(s => $"        {s}"));
                }
                lines.Add($"        do");
                lines.Add($"        {{");
                lines.Add($"            if ({matchExpr})");
                lines.Add($"            {{");
                lines.Add($"                return i;");
                lines.Add($"            }}");
                lines.Add($"            i++;");
                lines.Add($"        }}");
                lines.Add($"        while ((uint)i < (uint)span.Length);");
                lines.Add($"    }}");
                lines.Add($"");
                lines.Add($"    return -1;");
                lines.Add($"}}");

                if (checkOverflow)
                {
                    lines.Insert(uncheckedStart, "    unchecked");
                    lines.Insert(uncheckedStart + 1, "    {");
                    for (int i = uncheckedStart + 2; i < lines.Count - 1; i++)
                    {
                        lines[i] = $"    {lines[i]}";
                    }
                    lines.Insert(lines.Count - 1, "    }");
                }

                requiredHelpers.Add(helperName, lines.ToArray());
            }

            return helperName;
        }

        /// <summary>Emits the body of the Scan method override.</summary>
        private static (bool NeedsTryFind, bool NeedsTryMatch) EmitScan(IndentedTextWriter writer, RegexMethod rm)
        {
            bool rtl = (rm.Options & RegexOptions.RightToLeft) != 0;
            bool needsTryFind = false, needsTryMatch = false;
            RegexNode root = rm.Tree.Root.Child(0);

            // We can always emit our most general purpose scan loop, but there are common situations we can easily check
            // for where we can emit simpler/better code instead.
            if (root.Kind is RegexNodeKind.Empty)
            {
                // Emit a capture for the current position of length 0.  This is rare to see with a real-world pattern,
                // but it's very common as part of exploring the source generator, because it's what you get when you
                // start out with an empty pattern.
                writer.WriteLine("// The pattern matches the empty string.");
                writer.WriteLine($"int pos = base.runtextpos;");
                writer.WriteLine($"base.Capture(0, pos, pos);");
            }
            else if (root.Kind is RegexNodeKind.Nothing)
            {
                // Emit nothing.  This is rare in production and not something to we need optimize for, but as with
                // empty, it's helpful as a learning exposition tool.
                writer.WriteLine("// The pattern never matches anything.");
            }
            else if (root.Kind is RegexNodeKind.Multi or RegexNodeKind.One or RegexNodeKind.Notone or RegexNodeKind.Set)
            {
                // If the whole expression is just one or more characters, we can rely on the FindOptimizations spitting out
                // an IndexOf that will find the exact sequence or not, and we don't need to do additional checking beyond that.
                needsTryFind = true;
                using (EmitBlock(writer, "if (TryFindNextPossibleStartingPosition(inputSpan))"))
                {
                    writer.WriteLine("// The search in TryFindNextPossibleStartingPosition performed the entire match.");
                    writer.WriteLine($"int start = base.runtextpos;");
                    writer.WriteLine($"int end = base.runtextpos = start {(!rtl ? "+" : "-")} {(root.Kind == RegexNodeKind.Multi ? root.Str!.Length : 1)};");
                    writer.WriteLine($"base.Capture(0, start, end);");
                }
            }
            else if (rm.Tree.FindOptimizations.FindMode is
                    FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning or
                    FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Start or
                    FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Start or
                    FindNextStartingPositionMode.LeadingAnchor_RightToLeft_End)
            {
                // If the expression is anchored in such a way that there's one and only one possible position that can match,
                // we don't need a scan loop, just a single check and match.
                needsTryFind = needsTryMatch = true;
                writer.WriteLine("// The pattern is anchored.  Validate the current position and try to match at it only.");
                using (EmitBlock(writer, "if (TryFindNextPossibleStartingPosition(inputSpan) && !TryMatchAtCurrentPosition(inputSpan))"))
                {
                    writer.WriteLine($"base.runtextpos = {(!rtl ? "inputSpan.Length" : "0")};");
                }
            }
            else
            {
                // Emit the general purpose scan loop. At this point, we always need TryMatchAtCurrentPosition.  If we have any
                // information that will enable TryFindNextPossibleStartingPosition to help narrow down the search, we need it,
                // too, but otherwise it can be skipped.
                needsTryMatch = true;
                needsTryFind =
                    rm.Tree.FindOptimizations.FindMode != FindNextStartingPositionMode.NoSearch ||
                    rm.Tree.FindOptimizations.MinRequiredLength != 0 ||
                    rm.Tree.FindOptimizations.LeadingAnchor != RegexNodeKind.Unknown ||
                    rm.Tree.FindOptimizations.TrailingAnchor != RegexNodeKind.Unknown;

                writer.WriteLine("// Search until we can't find a valid starting position, we find a match, or we reach the end of the input.");
                writer.Write("while (");
                if (needsTryFind)
                {
                    writer.WriteLine("TryFindNextPossibleStartingPosition(inputSpan) &&");
                    writer.Write("       ");
                }
                writer.WriteLine("!TryMatchAtCurrentPosition(inputSpan) &&");
                writer.WriteLine($"       base.runtextpos != {(!rtl ? "inputSpan.Length" : "0")})");
                using (EmitBlock(writer, null))
                {
                    writer.WriteLine($"base.runtextpos{(!rtl ? "++" : "--")};");

                    // Check the timeout at least once per failed starting location, as finding the next location and
                    // attempting a match at that location could do work at least linear in the length of the input.
                    EmitTimeoutCheckIfNeeded(writer, rm, appendNewLineIfTimeoutEmitted: false);
                }
            }

            return (needsTryFind, needsTryMatch);
        }

        /// <summary>Emits the body of the TryFindNextPossibleStartingPosition.</summary>
        private static void EmitTryFindNextPossibleStartingPosition(IndentedTextWriter writer, RegexMethod rm, Dictionary<string, string[]> requiredHelpers, bool checkOverflow)
        {
            RegexOptions options = rm.Options;
            RegexTree regexTree = rm.Tree;
            bool rtl = (options & RegexOptions.RightToLeft) != 0;

            // In some cases, we need to emit declarations at the beginning of the method, but we only discover we need them later.
            // To handle that, we build up a collection of all the declarations to include, track where they should be inserted,
            // and then insert them at that position once everything else has been output.
            var additionalDeclarations = new HashSet<string>();

            // Emit locals initialization
            writer.WriteLine("int pos = base.runtextpos;");
            writer.Flush();
            int additionalDeclarationsPosition = ((StringWriter)writer.InnerWriter).GetStringBuilder().Length;
            int additionalDeclarationsIndent = writer.Indent;
            writer.WriteLine();

            const string NoMatchFound = "NoMatchFound";
            bool findEndsInAlwaysReturningTrue = false;
            bool noMatchFoundLabelNeeded = false;

            // Generate length check.  If the input isn't long enough to possibly match, fail quickly.
            // It's rare for min required length to be 0, so we don't bother special-casing the check,
            // especially since we want the "return false" code regardless.
            int minRequiredLength = rm.Tree.FindOptimizations.MinRequiredLength;
            Debug.Assert(minRequiredLength >= 0);
            FinishEmitBlock clause = default;
            if (minRequiredLength > 0)
            {
                writer.WriteLine(minRequiredLength == 1 ?
                    "// Empty matches aren't possible." :
                    $"// Any possible match is at least {minRequiredLength} characters.");
                clause = EmitBlock(writer, (minRequiredLength, rtl) switch
                {
                    (1, false) => "if ((uint)pos < (uint)inputSpan.Length)",
                    (_, false) => $"if (pos <= inputSpan.Length - {minRequiredLength})",
                    (1, true) => "if (pos > 0)",
                    (_, true) => $"if (pos >= {minRequiredLength})",
                });
            }
            using (clause)
            {
                // Emit any anchors.
                if (!EmitAnchors())
                {
                    // Either anchors weren't specified, or they don't completely root all matches to a specific location.

                    // Emit the code for whatever find mode has been determined.
                    switch (regexTree.FindOptimizations.FindMode)
                    {
                        case FindNextStartingPositionMode.LeadingString_LeftToRight:
                        case FindNextStartingPositionMode.LeadingString_OrdinalIgnoreCase_LeftToRight:
                        case FindNextStartingPositionMode.FixedDistanceString_LeftToRight:
                            EmitIndexOf_LeftToRight();
                            break;

                        case FindNextStartingPositionMode.LeadingString_RightToLeft:
                            EmitIndexOf_RightToLeft();
                            break;

                        case FindNextStartingPositionMode.LeadingSet_LeftToRight:
                        case FindNextStartingPositionMode.FixedDistanceSets_LeftToRight:
                            EmitFixedSet_LeftToRight();
                            break;

                        case FindNextStartingPositionMode.LeadingSet_RightToLeft:
                            EmitFixedSet_RightToLeft();
                            break;

                        case FindNextStartingPositionMode.LiteralAfterLoop_LeftToRight:
                            EmitLiteralAfterAtomicLoop();
                            break;

                        default:
                            Debug.Fail($"Unexpected mode: {regexTree.FindOptimizations.FindMode}");
                            goto case FindNextStartingPositionMode.NoSearch;

                        case FindNextStartingPositionMode.NoSearch:
                            writer.WriteLine("return true;");
                            findEndsInAlwaysReturningTrue = true;
                            break;
                    }
                }
            }

            // If the main path is guaranteed to end in a "return true;" and nothing is going to
            // jump past it, we don't need a "return false;" path.
            if (minRequiredLength > 0 || !findEndsInAlwaysReturningTrue || noMatchFoundLabelNeeded)
            {
                writer.WriteLine();
                writer.WriteLine("// No match found.");
                if (noMatchFoundLabelNeeded)
                {
                    writer.WriteLine($"{NoMatchFound}:");
                }
                writer.WriteLine($"base.runtextpos = {(!rtl ? "inputSpan.Length" : "0")};");
                writer.WriteLine("return false;");
            }

            // We're done.  Patch up any additional declarations.
            ReplaceAdditionalDeclarations(writer, additionalDeclarations, additionalDeclarationsPosition, additionalDeclarationsIndent);
            return;

            // Emit a goto for the specified label.
            void Goto(string label) => writer.WriteLine($"goto {label};");

            // Emits any anchors.  Returns true if the anchor roots any match to a specific location and thus no further
            // searching is required; otherwise, false.
            bool EmitAnchors()
            {
                // Anchors that fully implement TryFindNextPossibleStartingPosition, with a check that leads to immediate success or failure determination.
                switch (regexTree.FindOptimizations.FindMode)
                {
                    case FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning:
                        writer.WriteLine("// The pattern leads with a beginning (\\A) anchor.");
                        using (EmitBlock(writer, "if (pos == 0)"))
                        {
                            // If we're at the beginning, we're at a possible match location.  Otherwise,
                            // we'll never be, so fail immediately.
                            writer.WriteLine("return true;");
                        }
                        return true;

                    case FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Start:
                    case FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Start:
                        writer.Write($"// The pattern leads with a start (\\G) anchor");
                        if (regexTree.FindOptimizations.FindMode == FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Start)
                        {
                            writer.Write(" when processed right to left.");
                        }
                        writer.WriteLine(".");
                        using (EmitBlock(writer, "if (pos == base.runtextstart)"))
                        {
                            // For both left-to-right and right-to-left, if we're  currently at the start,
                            // we're at a possible match location.  Otherwise, because we've already moved
                            // beyond it, we'll never be, so fail immediately.
                            writer.WriteLine("return true;");
                        }
                        return true;

                    case FindNextStartingPositionMode.LeadingAnchor_LeftToRight_EndZ:
                        writer.WriteLine("// The pattern leads with an end (\\Z) anchor.");
                        using (EmitBlock(writer, "if (pos < inputSpan.Length - 1)"))
                        {
                            // If we're not currently at the end (or a newline just before it), skip ahead
                            // since nothing until then can possibly match.
                            writer.WriteLine("base.runtextpos = inputSpan.Length - 1;");
                        }
                        writer.WriteLine("return true;");
                        findEndsInAlwaysReturningTrue = true;
                        return true;

                    case FindNextStartingPositionMode.LeadingAnchor_LeftToRight_End:
                        writer.WriteLine("// The pattern leads with an end (\\z) anchor.");
                        using (EmitBlock(writer, "if (pos < inputSpan.Length)"))
                        {
                            // If we're not currently at the end (or a newline just before it), skip ahead
                            // since nothing until then can possibly match.
                            writer.WriteLine("base.runtextpos = inputSpan.Length;");
                        }
                        writer.WriteLine("return true;");
                        findEndsInAlwaysReturningTrue = true;
                        return true;

                    case FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Beginning:
                        writer.WriteLine("// The pattern leads with a beginning (\\A) anchor when processed right to left.");
                        using (EmitBlock(writer, "if (pos != 0)"))
                        {
                            // If we're not currently at the beginning, skip ahead (or, rather, backwards)
                            // since nothing until then can possibly match. (We're iterating from the end
                            // to the beginning in RightToLeft mode.)
                            writer.WriteLine("base.runtextpos = 0;");
                        }
                        writer.WriteLine("return true;");
                        findEndsInAlwaysReturningTrue = true;
                        return true;

                    case FindNextStartingPositionMode.LeadingAnchor_RightToLeft_EndZ:
                        writer.WriteLine("// The pattern leads with an end (\\Z) anchor when processed right to left.");
                        using (EmitBlock(writer, "if (pos >= inputSpan.Length - 1 && ((uint)pos >= (uint)inputSpan.Length || inputSpan[pos] == '\\n'))"))
                        {
                            // If we're currently at the end, we're at a valid position to try.  Otherwise,
                            // we'll never be (we're iterating from end to beginning), so fail immediately.
                            writer.WriteLine("return true;");
                        }
                        return true;

                    case FindNextStartingPositionMode.LeadingAnchor_RightToLeft_End:
                        writer.WriteLine("// The pattern leads with an end (\\z) anchor when processed right to left.");
                        using (EmitBlock(writer, "if (pos >= inputSpan.Length)"))
                        {
                            // If we're currently at the end, we're at a valid position to try.  Otherwise,
                            // we'll never be (we're iterating from end to beginning), so fail immediately.
                            writer.WriteLine("return true;");
                        }
                        return true;

                    case FindNextStartingPositionMode.TrailingAnchor_FixedLength_LeftToRight_EndZ:
                        // Jump to the end, minus the min required length, which in this case is actually the fixed length, minus 1 (for a possible ending \n).
                        writer.WriteLine($"// The pattern has a trailing end (\\Z) anchor, and any possible match is exactly {regexTree.FindOptimizations.MinRequiredLength} characters.");
                        using (EmitBlock(writer, $"if (pos < inputSpan.Length - {regexTree.FindOptimizations.MinRequiredLength + 1})"))
                        {
                            writer.WriteLine($"base.runtextpos = inputSpan.Length - {regexTree.FindOptimizations.MinRequiredLength + 1};");
                        }
                        writer.WriteLine("return true;");
                        findEndsInAlwaysReturningTrue = true;
                        return true;

                    case FindNextStartingPositionMode.TrailingAnchor_FixedLength_LeftToRight_End:
                        // Jump to the end, minus the min required length, which in this case is actually the fixed length.
                        writer.WriteLine($"// The pattern has a trailing end (\\z) anchor, and any possible match is exactly {regexTree.FindOptimizations.MinRequiredLength} characters.");
                        using (EmitBlock(writer, $"if (pos < inputSpan.Length - {regexTree.FindOptimizations.MinRequiredLength})"))
                        {
                            writer.WriteLine($"base.runtextpos = inputSpan.Length - {regexTree.FindOptimizations.MinRequiredLength};");
                        }
                        writer.WriteLine("return true;");
                        findEndsInAlwaysReturningTrue = true;
                        return true;
                }

                // Now handle anchors that boost the position but may not determine immediate success or failure.

                switch (regexTree.FindOptimizations.LeadingAnchor)
                {
                    case RegexNodeKind.Bol:
                        // Optimize the handling of a Beginning-Of-Line (BOL) anchor.  BOL is special, in that unlike
                        // other anchors like Beginning, there are potentially multiple places a BOL can match.  So unlike
                        // the other anchors, which all skip all subsequent processing if found, with BOL we just use it
                        // to boost our position to the next line, and then continue normally with any searches.
                        writer.WriteLine($"// The pattern has a leading beginning-of-line anchor.");
                        using (EmitBlock(writer, "if (pos > 0 && inputSpan[pos - 1] != '\\n')"))
                        {
                            writer.WriteLine("int newlinePos = inputSpan.Slice(pos).IndexOf('\\n');");
                            using (EmitBlock(writer, "if ((uint)newlinePos > inputSpan.Length - pos - 1)"))
                            {
                                noMatchFoundLabelNeeded = true;
                                Goto(NoMatchFound);
                            }
                            writer.WriteLine("pos += newlinePos + 1;");
                            writer.WriteLine();

                            // We've updated the position.  Make sure there's still enough room in the input for a possible match.
                            using (EmitBlock(writer, minRequiredLength switch
                            {
                                0 => "if (pos > inputSpan.Length)",
                                1 => "if (pos >= inputSpan.Length)",
                                _ => $"if (pos > inputSpan.Length - {minRequiredLength})"
                            }))
                            {
                                noMatchFoundLabelNeeded = true;
                                Goto(NoMatchFound);
                            }
                        }
                        writer.WriteLine();
                        break;
                }

                switch (regexTree.FindOptimizations.TrailingAnchor)
                {
                    case RegexNodeKind.End when regexTree.FindOptimizations.MaxPossibleLength is int maxLength:
                        writer.WriteLine($"// The pattern has a trailing end (\\z) anchor, and any possible match is no more than {maxLength} characters.");
                        using (EmitBlock(writer, $"if (pos < inputSpan.Length - {maxLength})"))
                        {
                            writer.WriteLine($"pos = inputSpan.Length - {maxLength};");
                        }
                        writer.WriteLine();
                        break;

                    case RegexNodeKind.EndZ when regexTree.FindOptimizations.MaxPossibleLength is int maxLength:
                        writer.WriteLine($"// The pattern has a trailing end (\\Z) anchor, and any possible match is no more than {maxLength} characters.");
                        using (EmitBlock(writer, $"if (pos < inputSpan.Length - {maxLength + 1})"))
                        {
                            writer.WriteLine($"pos = inputSpan.Length - {maxLength + 1};");
                        }
                        writer.WriteLine();
                        break;
                }

                return false;
            }

            // Emits a case-sensitive left-to-right search for a substring.
            void EmitIndexOf_LeftToRight()
            {
                RegexFindOptimizations opts = regexTree.FindOptimizations;

                string substring = "", stringComparison = "", offset = "", offsetDescription = "";

                switch (opts.FindMode)
                {
                    case FindNextStartingPositionMode.LeadingString_LeftToRight:
                        substring = regexTree.FindOptimizations.LeadingPrefix;
                        offsetDescription = "at the beginning of the pattern";
                        Debug.Assert(!string.IsNullOrEmpty(substring));
                        break;

                    case FindNextStartingPositionMode.LeadingString_OrdinalIgnoreCase_LeftToRight:
                        substring = regexTree.FindOptimizations.LeadingPrefix;
                        stringComparison = ", StringComparison.OrdinalIgnoreCase";
                        offsetDescription = "ordinal case-insensitive at the beginning of the pattern";
                        Debug.Assert(!string.IsNullOrEmpty(substring));
                        break;

                    case FindNextStartingPositionMode.FixedDistanceString_LeftToRight:
                        Debug.Assert(!string.IsNullOrEmpty(regexTree.FindOptimizations.FixedDistanceLiteral.String));
                        substring = regexTree.FindOptimizations.FixedDistanceLiteral.String;
                        if (regexTree.FindOptimizations.FixedDistanceLiteral is { Distance: > 0 } literal)
                        {
                            offset = $" + {literal.Distance}";
                            offsetDescription = $" at index {literal.Distance} in the pattern";
                        }
                        break;

                    default:
                        Debug.Fail($"Unexpected mode: {opts.FindMode}");
                        break;
                }

                writer.WriteLine($"// The pattern has the literal {Literal(substring)} {offsetDescription}. Find the next occurrence.");
                writer.WriteLine($"// If it can't be found, there's no match.");
                writer.WriteLine($"int i = inputSpan.Slice(pos{offset}).IndexOf({Literal(substring)}{stringComparison});");
                using (EmitBlock(writer, "if (i >= 0)"))
                {
                    writer.WriteLine("base.runtextpos = pos + i;");
                    writer.WriteLine("return true;");
                }
            }

            // Emits a case-sensitive right-to-left search for a substring.
            void EmitIndexOf_RightToLeft()
            {
                string prefix = regexTree.FindOptimizations.LeadingPrefix;

                writer.WriteLine($"// The pattern begins with a literal {Literal(prefix)}. Find the next occurrence right-to-left.");
                writer.WriteLine($"// If it can't be found, there's no match.");
                writer.WriteLine($"pos = inputSpan.Slice(0, pos).LastIndexOf({Literal(prefix)});");
                using (EmitBlock(writer, "if (pos >= 0)"))
                {
                    writer.WriteLine($"base.runtextpos = pos + {prefix.Length};");
                    writer.WriteLine($"return true;");
                }
            }

            // Emits a search for a set at a fixed position from the start of the pattern,
            // and potentially other sets at other fixed positions in the pattern.
            void EmitFixedSet_LeftToRight()
            {
                Debug.Assert(regexTree.FindOptimizations.FixedDistanceSets is { Count: > 0 });

                List<RegexFindOptimizations.FixedDistanceSet>? sets = regexTree.FindOptimizations.FixedDistanceSets;
                RegexFindOptimizations.FixedDistanceSet primarySet = sets![0];
                const int MaxSets = 4;
                int setsToUse = Math.Min(sets.Count, MaxSets);

                writer.WriteLine(primarySet.Distance == 0 ?
                   $"// The pattern begins with {DescribeSet(primarySet.Set)}." :
                   $"// The pattern matches {DescribeSet(primarySet.Set)} at index {primarySet.Distance}.");
                writer.WriteLine($"// Find the next occurrence. If it can't be found, there's no match.");

                // Use IndexOf{Any} to accelerate the skip loop via vectorization to match the first prefix.
                // But we avoid using it for the relatively common case of the starting set being '.', aka anything other than
                // a newline, as it's very rare to have long, uninterrupted sequences of newlines. And we avoid using it
                // for the case of the starting set being anything (e.g. '.' with SingleLine), as in that case it'll always match
                // the first char.
                int setIndex = 0;
                bool canUseIndexOf =
                    primarySet.Set != RegexCharClass.NotNewLineClass &&
                    primarySet.Set != RegexCharClass.AnyClass;
                bool needLoop = !canUseIndexOf || setsToUse > 1;

                FinishEmitBlock loopBlock = default;
                if (needLoop)
                {
                    writer.WriteLine("ReadOnlySpan<char> span = inputSpan.Slice(pos);");
                    string upperBound = "span.Length" + (setsToUse > 1 || primarySet.Distance != 0 ? $" - {minRequiredLength - 1}" : "");
                    loopBlock = EmitBlock(writer, $"for (int i = 0; i < {upperBound}; i++)");
                }

                if (canUseIndexOf)
                {
                    string span = needLoop ?
                        "span" :
                        "inputSpan.Slice(pos)";

                    span = (needLoop, primarySet.Distance) switch
                    {
                        (false, 0) => span,
                        (true, 0) => $"{span}.Slice(i)",
                        (false, _) => $"{span}.Slice({primarySet.Distance})",
                        (true, _) => $"{span}.Slice(i + {primarySet.Distance})",
                    };

                    Debug.Assert(!primarySet.Negated || (primarySet.Chars is null && primarySet.AsciiSet is null));

                    string indexOf =
                        primarySet.Chars is not null ? primarySet.Chars.Length switch
                        {
                            1 => $"{span}.IndexOf({Literal(primarySet.Chars[0])})",
                            2 => $"{span}.IndexOfAny({Literal(primarySet.Chars[0])}, {Literal(primarySet.Chars[1])})",
                            3 => $"{span}.IndexOfAny({Literal(primarySet.Chars[0])}, {Literal(primarySet.Chars[1])}, {Literal(primarySet.Chars[2])})",
                            _ => $"{span}.IndexOfAny({EmitIndexOfAnyValuesOrLiteral(primarySet.Chars, requiredHelpers)})",
                        } :
                        primarySet.AsciiSet is not null ? $"{span}.IndexOfAny({EmitIndexOfAnyValues(primarySet.AsciiSet, requiredHelpers)})" :
                        primarySet.Range is not null ? (primarySet.Range.Value.LowInclusive == primarySet.Range.Value.HighInclusive, primarySet.Negated) switch
                        {
                            (false, false) => $"{span}.IndexOfAnyInRange({Literal(primarySet.Range.Value.LowInclusive)}, {Literal(primarySet.Range.Value.HighInclusive)})",
                            (true, false) => $"{span}.IndexOf({Literal(primarySet.Range.Value.LowInclusive)})",
                            (false, true) => $"{span}.IndexOfAnyExceptInRange({Literal(primarySet.Range.Value.LowInclusive)}, {Literal(primarySet.Range.Value.HighInclusive)})",
                            (true, true) => $"{span}.IndexOfAnyExcept({Literal(primarySet.Range.Value.LowInclusive)})",
                        } :
                        $"{span}.{EmitIndexOfAnyCustomHelper(primarySet.Set, requiredHelpers, checkOverflow)}()";

                    if (needLoop)
                    {
                        writer.WriteLine($"int indexOfPos = {indexOf};");
                        using (EmitBlock(writer, "if (indexOfPos < 0)"))
                        {
                            noMatchFoundLabelNeeded = true;
                            Goto(NoMatchFound);
                        }
                        writer.WriteLine("i += indexOfPos;");
                        writer.WriteLine();

                        if (setsToUse > 1)
                        {
                            // Of the remaining sets we're going to check, find the maximum distance of any of them.
                            // If it's further than the primary set we checked, we need a bounds check.
                            int maxDistance = sets[1].Distance;
                            for (int i = 2; i < setsToUse; i++)
                            {
                                maxDistance = Math.Max(maxDistance, sets[i].Distance);
                            }
                            if (maxDistance > primarySet.Distance)
                            {
                                int numRemainingSets = setsToUse - 1;
                                writer.WriteLine($"// The primary set being searched for was found. {numRemainingSets} more set{(numRemainingSets > 1 ? "s" : "")} will be checked so as");
                                writer.WriteLine($"// to minimize the number of places TryMatchAtCurrentPosition is run unnecessarily.");
                                writer.WriteLine($"// Make sure {(numRemainingSets > 1 ? "they fit" : "it fits")} in the remainder of the input.");
                                using (EmitBlock(writer, $"if ((uint)(i + {maxDistance}) >= (uint)span.Length)"))
                                {
                                    noMatchFoundLabelNeeded = true;
                                    Goto(NoMatchFound);
                                }
                                writer.WriteLine();
                            }
                        }
                    }
                    else
                    {
                        writer.WriteLine($"int i = {indexOf};");
                        using (EmitBlock(writer, "if (i >= 0)"))
                        {
                            writer.WriteLine("base.runtextpos = pos + i;");
                            writer.WriteLine("return true;");
                        }
                    }

                    setIndex = 1;
                }

                if (needLoop)
                {
                    Debug.Assert(setIndex == 0 || setIndex == 1);
                    bool hasCharClassConditions = false;
                    if (setIndex < setsToUse)
                    {
                        // if (CharInClass(textSpan[i + charClassIndex], prefix[0], "...") &&
                        //     ...)
                        Debug.Assert(needLoop);
                        int start = setIndex;
                        for (; setIndex < setsToUse; setIndex++)
                        {
                            string spanIndex = $"span[i{(sets[setIndex].Distance > 0 ? $" + {sets[setIndex].Distance}" : "")}]";
                            string charInClassExpr = MatchCharacterClass(spanIndex, sets[setIndex].Set, negate: false, additionalDeclarations, requiredHelpers);

                            if (setIndex == start)
                            {
                                writer.Write($"if ({charInClassExpr}");
                            }
                            else
                            {
                                writer.WriteLine(" &&");
                                writer.Write($"    {charInClassExpr}");
                            }
                        }
                        writer.WriteLine(")");
                        hasCharClassConditions = true;
                    }

                    using (hasCharClassConditions ? EmitBlock(writer, null) : default)
                    {
                        writer.WriteLine("base.runtextpos = pos + i;");
                        writer.WriteLine("return true;");
                    }
                }

                loopBlock.Dispose();
            }

            // Emits a right-to-left search for a set at a fixed position from the start of the pattern.
            // (Currently that position will always be a distance of 0, meaning the start of the pattern itself.)
            void EmitFixedSet_RightToLeft()
            {
                Debug.Assert(regexTree.FindOptimizations.FixedDistanceSets is { Count: > 0 });

                RegexFindOptimizations.FixedDistanceSet set = regexTree.FindOptimizations.FixedDistanceSets![0];
                Debug.Assert(set.Distance == 0);

                writer.WriteLine($"// The pattern begins with {DescribeSet(set.Set)}.");
                writer.WriteLine($"// Find the next occurrence. If it can't be found, there's no match.");

                if (set.Chars is { Length: 1 })
                {
                    writer.WriteLine($"pos = inputSpan.Slice(0, pos).LastIndexOf({Literal(set.Chars[0])});");
                    using (EmitBlock(writer, "if (pos >= 0)"))
                    {
                        writer.WriteLine("base.runtextpos = pos + 1;");
                        writer.WriteLine("return true;");
                    }
                }
                else
                {
                    using (EmitBlock(writer, "while ((uint)--pos < (uint)inputSpan.Length)"))
                    {
                        using (EmitBlock(writer, $"if ({MatchCharacterClass("inputSpan[pos]", set.Set, negate: false, additionalDeclarations, requiredHelpers)})"))
                        {
                            writer.WriteLine("base.runtextpos = pos + 1;");
                            writer.WriteLine("return true;");
                        }
                    }
                }
            }

            // Emits a search for a literal following a leading atomic single-character loop.
            void EmitLiteralAfterAtomicLoop()
            {
                Debug.Assert(regexTree.FindOptimizations.LiteralAfterLoop is not null);
                (RegexNode LoopNode, (char Char, string? String, char[]? Chars) Literal) target = regexTree.FindOptimizations.LiteralAfterLoop.Value;

                Debug.Assert(target.LoopNode.Kind is RegexNodeKind.Setloop or RegexNodeKind.Setlazy or RegexNodeKind.Setloopatomic);
                Debug.Assert(target.LoopNode.N == int.MaxValue);

                writer.Write($"// The pattern begins with an atomic loop for {DescribeSet(target.LoopNode.Str!)}, followed by ");
                writer.WriteLine(
                    target.Literal.String is not null ? $"the string {Literal(target.Literal.String)}." :
                    target.Literal.Chars is not null ? $"one of the characters {Literal(new string(target.Literal.Chars))}" :
                    $"the character {Literal(target.Literal.Char)}.");
                writer.WriteLine($"// Search for the literal, and then walk backwards to the beginning of the loop.");

                FinishEmitBlock block = default;
                if (target.LoopNode.M > 0)
                {
                    // If there's no lower bound on the loop, then once we find the literal, we know we have a valid starting position to try.
                    // If there is a lower bound, then we need a loop, as we could find the literal but it might not be prefixed with enough
                    // appropriate characters to satisfy the minimum bound.
                    block = EmitBlock(writer, "while (true)");
                }
                using (block)
                {
                    writer.WriteLine($"ReadOnlySpan<char> slice = inputSpan.Slice(pos);");
                    writer.WriteLine();

                    // Find the literal.  If we can't find it, we're done searching.
                    writer.Write("int i = slice.");
                    writer.WriteLine(
                        target.Literal.String is string literalString ? $"IndexOf({Literal(literalString)});" :
                        target.Literal.Chars is not char[] literalChars ? $"IndexOf({Literal(target.Literal.Char)});" :
                        literalChars.Length switch
                        {
                            2 => $"IndexOfAny({Literal(literalChars[0])}, {Literal(literalChars[1])});",
                            3 => $"IndexOfAny({Literal(literalChars[0])}, {Literal(literalChars[1])}, {Literal(literalChars[2])});",
                            _ => $"IndexOfAny({EmitIndexOfAnyValuesOrLiteral(literalChars, requiredHelpers)});",
                        });

                    FinishEmitBlock indexOfFoundBlock = default;
                    if (target.LoopNode.M > 0)
                    {
                        using (EmitBlock(writer, $"if (i < 0)"))
                        {
                            writer.WriteLine("break;");
                        }
                        writer.WriteLine();
                    }
                    else
                    {
                        indexOfFoundBlock = EmitBlock(writer, $"if (i >= 0)");
                    }

                    // We found the literal.  Walk backwards from it finding as many matches as we can against the loop.
                    writer.WriteLine("int prev = i - 1;");
                    using (EmitBlock(writer, $"while ((uint)prev < (uint)slice.Length && {MatchCharacterClass("slice[prev]", target.LoopNode.Str!, negate: false, additionalDeclarations, requiredHelpers)})"))
                    {
                        writer.WriteLine("prev--;");
                    }
                    writer.WriteLine();

                    if (target.LoopNode.M > 0)
                    {
                        // If we found fewer than needed, loop around to try again.  The loop doesn't overlap with the literal,
                        // so we can start from after the last place the literal matched.
                        using (EmitBlock(writer, $"if ((i - prev - 1) < {target.LoopNode.M})"))
                        {
                            writer.WriteLine("pos += i + 1;");
                            writer.WriteLine("continue;");
                        }
                        writer.WriteLine();
                    }

                    // We have a winner.  The starting position is just after the last position that failed to match the loop.
                    // We also store the position after the loop into runtrackpos (an extra, unused field on RegexRunner) in order
                    // to communicate this position to the match algorithm such that it can skip the loop.
                    writer.WriteLine("base.runtextpos = pos + prev + 1;");
                    writer.WriteLine("base.runtrackpos = pos + i;");
                    writer.WriteLine("return true;");

                    indexOfFoundBlock.Dispose();
                }
            }
        }

        /// <summary>Emits the body of the TryMatchAtCurrentPosition.</summary>
        private static void EmitTryMatchAtCurrentPosition(IndentedTextWriter writer, RegexMethod rm, Dictionary<string, string[]> requiredHelpers, bool checkOverflow)
        {
            // In .NET Framework and up through .NET Core 3.1, the code generated for RegexOptions.Compiled was effectively an unrolled
            // version of what RegexInterpreter would process.  The RegexNode tree would be turned into a series of opcodes via
            // RegexWriter; the interpreter would then sit in a loop processing those opcodes, and the RegexCompiler iterated through the
            // opcodes generating code for each equivalent to what the interpreter would do albeit with some decisions made at compile-time
            // rather than at run-time.  This approach, however, lead to complicated code that wasn't pay-for-play (e.g. a big backtracking
            // jump table that all compilations went through even if there was no backtracking), that didn't factor in the shape of the
            // tree (e.g. it's difficult to add optimizations based on interactions between nodes in the graph), and that didn't read well
            // when decompiled from IL to C# or when directly emitted as C# as part of a source generator.
            //
            // This implementation is instead based on directly walking the RegexNode tree and outputting code for each node in the graph.
            // A dedicated for each kind of RegexNode emits the code necessary to handle that node's processing, including recursively
            // calling the relevant function for any of its children nodes.  Backtracking is handled not via a giant jump table, but instead
            // by emitting direct jumps to each backtracking construct.  This is achieved by having all match failures jump to a "done"
            // label that can be changed by a previous emitter, e.g. before EmitLoop returns, it ensures that "doneLabel" is set to the
            // label that code should jump back to when backtracking.  That way, a subsequent EmitXx function doesn't need to know exactly
            // where to jump: it simply always jumps to "doneLabel" on match failure, and "doneLabel" is always configured to point to
            // the right location.  In an expression without backtracking, or before any backtracking constructs have been encountered,
            // "doneLabel" is simply the final return location from the TryMatchAtCurrentPosition method that will undo any captures and exit, signaling to
            // the calling scan loop that nothing was matched.

            // Arbitrary limit for unrolling vs creating a loop.  We want to balance size in the generated
            // code with other costs, like the (small) overhead of slicing to create the temp span to iterate.
            const int MaxUnrollSize = 16;

            RegexOptions options = (RegexOptions)rm.Options;
            RegexTree regexTree = rm.Tree;

            // Helper to define names.  Names start unadorned, but as soon as there's repetition,
            // they begin to have a numbered suffix.
            Dictionary<string, int> usedNames = new();

            // Every RegexTree is rooted in the implicit Capture for the whole expression.
            // Skip the Capture node. We handle the implicit root capture specially.
            RegexNode node = regexTree.Root;
            Debug.Assert(node.Kind == RegexNodeKind.Capture, "Every generated tree should begin with a capture node");
            Debug.Assert(node.ChildCount() == 1, "Capture nodes should have one child");
            node = node.Child(0);

            // In some cases, we need to emit declarations at the beginning of the method, but we only discover we need them later.
            // To handle that, we build up a collection of all the declarations to include, track where they should be inserted,
            // and then insert them at that position once everything else has been output.
            HashSet<string> additionalDeclarations = new();
            Dictionary<string, string[]> additionalLocalFunctions = new();

            // Declare some locals.
            string sliceSpan = "slice";
            writer.WriteLine("int pos = base.runtextpos;");
            writer.WriteLine($"int matchStart = pos;");
            writer.Flush();
            int additionalDeclarationsPosition = ((StringWriter)writer.InnerWriter).GetStringBuilder().Length;
            int additionalDeclarationsIndent = writer.Indent;

            // The implementation tries to use const indexes into the span wherever possible, which we can do
            // for all fixed-length constructs.  In such cases (e.g. single chars, repeaters, strings, etc.)
            // we know at any point in the regex exactly how far into it we are, and we can use that to index
            // into the span created at the beginning of the routine to begin at exactly where we're starting
            // in the input.  When we encounter a variable-length construct, we transfer the static value to
            // pos, slicing the inputSpan appropriately, and then zero out the static position.
            int sliceStaticPos = 0;
            SliceInputSpan(defineLocal: true);
            writer.WriteLine();

            // doneLabel starts out as the top-level label for the whole expression failing to match.  However,
            // it may be changed by the processing of a node to point to whereever subsequent match failures
            // should jump to, in support of backtracking or other constructs.  For example, before emitting
            // the code for a branch N, an alternation will set the doneLabel to point to the label for
            // processing the next branch N+1: that way, any failures in the branch N's processing will
            // implicitly end up jumping to the right location without needing to know in what context it's used.
            string doneLabel = ReserveName("NoMatch");
            string topLevelDoneLabel = doneLabel;

            // Check whether there are captures anywhere in the expression. If there isn't, we can skip all
            // the boilerplate logic around uncapturing, as there won't be anything to uncapture.
            bool expressionHasCaptures = rm.Analysis.MayContainCapture(node);

            // Emit the code for all nodes in the tree.
            EmitNode(node);

            // If we fall through to this place in the code, we've successfully matched the expression.
            writer.WriteLine();
            writer.WriteLine("// The input matched.");
            if (sliceStaticPos > 0)
            {
                EmitAdd(writer, "pos", sliceStaticPos); // TransferSliceStaticPosToPos would also slice, which isn't needed here
            }
            writer.WriteLine("base.runtextpos = pos;");
            writer.WriteLine("base.Capture(0, matchStart, pos);");
            writer.WriteLine("return true;");

            // We're done with the match.

            // Patch up any additional declarations.
            ReplaceAdditionalDeclarations(writer, additionalDeclarations, additionalDeclarationsPosition, additionalDeclarationsIndent);

            // And emit any required helpers.
            if (additionalLocalFunctions.Count != 0)
            {
                foreach (KeyValuePair<string, string[]> localFunctions in additionalLocalFunctions.OrderBy(k => k.Key))
                {
                    writer.WriteLine();
                    foreach (string line in localFunctions.Value)
                    {
                        writer.WriteLine(line);
                    }
                }
            }

            return;

            // Helper to create a name guaranteed to be unique within the function.
            string ReserveName(string prefix)
            {
                usedNames.TryGetValue(prefix, out int count);
                usedNames[prefix] = count + 1;
                return count == 0 ? prefix : $"{prefix}{count}";
            }

            // Helper to emit a label.  As of C# 10, labels aren't statements of their own and need to adorn a following statement;
            // if a label appears just before a closing brace, then, it's a compilation error.  To avoid issues there, this by
            // default implements a blank statement (a semicolon) after each label, but individual uses can opt-out of the semicolon
            // when it's known the label will always be followed by a statement.
            void MarkLabel(string label, bool emitSemicolon = true) => writer.WriteLine($"{label}:{(emitSemicolon ? ";" : "")}");

            // Gets whether calling Goto(label) will result in exiting the match method.
            bool GotoWillExitMatch(string label) => label == topLevelDoneLabel;

            // Emits a goto to jump to the specified label.  However, if the specified label is the top-level done label indicating
            // that the entire match has failed, we instead emit our epilogue, uncapturing if necessary and returning out of TryMatchAtCurrentPosition.
            void Goto(string label)
            {
                if (GotoWillExitMatch(label))
                {
                    // We only get here in the code if the whole expression fails to match and jumps to
                    // the original value of doneLabel.
                    if (expressionHasCaptures)
                    {
                        EmitUncaptureUntil("0");
                    }
                    writer.WriteLine("return false; // The input didn't match.");
                }
                else
                {
                    writer.WriteLine($"goto {label};");
                }
            }

            // Emits a case or default line followed by an indented body.
            void CaseGoto(string clause, string label)
            {
                writer.WriteLine(clause);
                writer.Indent++;
                Goto(label);
                writer.Indent--;
            }

            // Slices the inputSpan starting at pos until end and stores it into slice.
            void SliceInputSpan(bool defineLocal = false)
            {
                if (defineLocal)
                {
                    writer.Write("ReadOnlySpan<char> ");
                }
                writer.WriteLine($"{sliceSpan} = inputSpan.Slice(pos);");
            }

            // Emits the sum of a constant and a value from a local.
            string Sum(int constant, string? local = null) =>
                local is null ? constant.ToString(CultureInfo.InvariantCulture) :
                constant == 0 ? local :
                $"{constant} + {local}";

            // Emits a check that the span is large enough at the currently known static position to handle the required additional length.
            void EmitSpanLengthCheck(int requiredLength, string? dynamicRequiredLength = null)
            {
                Debug.Assert(requiredLength > 0);
                using (EmitBlock(writer, $"if ({SpanLengthCheck(requiredLength, dynamicRequiredLength)})"))
                {
                    Goto(doneLabel);
                }
            }

            // Returns a length check for the current span slice.  The check returns true if
            // the span isn't long enough for the specified length.
            string SpanLengthCheck(int requiredLength, string? dynamicRequiredLength = null) =>
                dynamicRequiredLength is null && sliceStaticPos + requiredLength == 1 ? $"{sliceSpan}.IsEmpty" :
                $"(uint){sliceSpan}.Length < {Sum(sliceStaticPos + requiredLength, dynamicRequiredLength)}";

            // Adds the value of sliceStaticPos into the pos local, slices slice by the corresponding amount,
            // and zeros out sliceStaticPos.
            void TransferSliceStaticPosToPos(bool forceSliceReload = false)
            {
                if (sliceStaticPos > 0)
                {
                    EmitAdd(writer, "pos", sliceStaticPos);
                    sliceStaticPos = 0;
                    SliceInputSpan();
                }
                else if (forceSliceReload)
                {
                    SliceInputSpan();
                }
            }

            // Emits the code for an alternation.
            void EmitAlternation(RegexNode node)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Alternate, $"Unexpected type: {node.Kind}");
                Debug.Assert(node.ChildCount() >= 2, $"Expected at least 2 children, found {node.ChildCount()}");

                int childCount = node.ChildCount();
                Debug.Assert(childCount >= 2);

                string originalDoneLabel = doneLabel;

                // Both atomic and non-atomic are supported.  While a parent RegexNode.Atomic node will itself
                // successfully prevent backtracking into this child node, we can emit better / cheaper code
                // for an Alternate when it is atomic, so we still take it into account here.
                Debug.Assert(node.Parent is not null);
                bool isAtomic = rm.Analysis.IsAtomicByAncestor(node);

                // If no child branch overlaps with another child branch, we can emit more streamlined code
                // that avoids checking unnecessary branches, e.g. with abc|def|ghi if the next character in
                // the input is 'a', we needn't try the def or ghi branches.  A simple, relatively common case
                // of this is if every branch begins with a specific, unique character, in which case
                // the whole alternation can be treated as a simple switch, so we special-case that. However,
                // we can't goto _into_ switch cases, which means we can't use this approach if there's any
                // possibility of backtracking into the alternation.
                bool useSwitchedBranches = false;
                if ((node.Options & RegexOptions.RightToLeft) == 0)
                {
                    useSwitchedBranches = isAtomic;
                    if (!useSwitchedBranches)
                    {
                        useSwitchedBranches = true;
                        for (int i = 0; i < childCount; i++)
                        {
                            if (rm.Analysis.MayBacktrack(node.Child(i)))
                            {
                                useSwitchedBranches = false;
                                break;
                            }
                        }
                    }
                }

                // Detect whether every branch begins with one or more unique characters.
                const int SetCharsSize = 5; // arbitrary limit (for IgnoreCase, we want this to be at least 3 to handle the vast majority of values)
                Span<char> setChars = stackalloc char[SetCharsSize];
                if (useSwitchedBranches)
                {
                    // Iterate through every branch, seeing if we can easily find a starting One, Multi, or small Set.
                    // If we can, extract its starting char (or multiple in the case of a set), validate that all such
                    // starting characters are unique relative to all the branches.
                    var seenChars = new HashSet<char>();
                    for (int i = 0; i < childCount && useSwitchedBranches; i++)
                    {
                        // If it's not a One, Multi, or Set, we can't apply this optimization.
                        if (node.Child(i).FindBranchOneMultiOrSetStart() is not RegexNode oneMultiOrSet)
                        {
                            useSwitchedBranches = false;
                            break;
                        }

                        // If it's a One or a Multi, get the first character and add it to the set.
                        // If it was already in the set, we can't apply this optimization.
                        if (oneMultiOrSet.Kind is RegexNodeKind.One or RegexNodeKind.Multi)
                        {
                            if (!seenChars.Add(oneMultiOrSet.FirstCharOfOneOrMulti()))
                            {
                                useSwitchedBranches = false;
                                break;
                            }
                        }
                        else
                        {
                            // The branch begins with a set.  Make sure it's a set of only a few characters
                            // and get them.  If we can't, we can't apply this optimization.
                            Debug.Assert(oneMultiOrSet.Kind is RegexNodeKind.Set);
                            int numChars;
                            if (RegexCharClass.IsNegated(oneMultiOrSet.Str!) ||
                                (numChars = RegexCharClass.GetSetChars(oneMultiOrSet.Str!, setChars)) == 0)
                            {
                                useSwitchedBranches = false;
                                break;
                            }

                            // Check to make sure each of the chars is unique relative to all other branches examined.
                            foreach (char c in setChars.Slice(0, numChars))
                            {
                                if (!seenChars.Add(c))
                                {
                                    useSwitchedBranches = false;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (useSwitchedBranches)
                {
                    // Note: This optimization does not exist with RegexOptions.Compiled.  Here we rely on the
                    // C# compiler to lower the C# switch statement with appropriate optimizations. In some
                    // cases there are enough branches that the compiler will emit a jump table.  In others
                    // it'll optimize the order of checks in order to minimize the total number in the worst
                    // case.  In any case, we get easier to read and reason about C#.
                    EmitSwitchedBranches();
                }
                else
                {
                    EmitAllBranches();
                }
                return;

                // Emits the code for a switch-based alternation of non-overlapping branches.
                void EmitSwitchedBranches()
                {
                    // We need at least 1 remaining character in the span, for the char to switch on.
                    EmitSpanLengthCheck(1);
                    writer.WriteLine();

                    // Emit a switch statement on the first char of each branch.
                    using (EmitBlock(writer, $"switch ({sliceSpan}[{sliceStaticPos++}])"))
                    {
                        Span<char> setChars = stackalloc char[SetCharsSize]; // needs to be same size as detection check in caller
                        int startingSliceStaticPos = sliceStaticPos;

                        // Emit a case for each branch.
                        for (int i = 0; i < childCount; i++)
                        {
                            sliceStaticPos = startingSliceStaticPos;

                            RegexNode child = node.Child(i);
                            Debug.Assert(child.Kind is RegexNodeKind.One or RegexNodeKind.Multi or RegexNodeKind.Set or RegexNodeKind.Concatenate, DescribeNode(child, rm));
                            Debug.Assert(child.Kind is not RegexNodeKind.Concatenate || (child.ChildCount() >= 2 && child.Child(0).Kind is RegexNodeKind.One or RegexNodeKind.Multi or RegexNodeKind.Set));

                            RegexNode? childStart = child.FindBranchOneMultiOrSetStart();
                            Debug.Assert(childStart is not null, "Unexpectedly couldn't find the branch starting node.");

                            if (childStart.Kind is RegexNodeKind.Set)
                            {
                                int numChars = RegexCharClass.GetSetChars(childStart.Str!, setChars);
                                Debug.Assert(numChars != 0);
                                writer.WriteLine($"case {string.Join(" or ", setChars.Slice(0, numChars).ToArray().Select(Literal))}:");
                            }
                            else
                            {
                                writer.WriteLine($"case {Literal(childStart.FirstCharOfOneOrMulti())}:");
                            }
                            writer.Indent++;

                            // Emit the code for the branch, without the first character that was already matched in the switch.
                            switch (child.Kind)
                            {
                                case RegexNodeKind.Multi:
                                    EmitNode(CloneMultiWithoutFirstChar(child));
                                    writer.WriteLine();
                                    break;

                                case RegexNodeKind.Concatenate:
                                    var newConcat = new RegexNode(RegexNodeKind.Concatenate, child.Options);
                                    if (childStart.Kind == RegexNodeKind.Multi)
                                    {
                                        newConcat.AddChild(CloneMultiWithoutFirstChar(childStart));
                                    }
                                    int concatChildCount = child.ChildCount();
                                    for (int j = 1; j < concatChildCount; j++)
                                    {
                                        newConcat.AddChild(child.Child(j));
                                    }
                                    EmitNode(newConcat.Reduce());
                                    writer.WriteLine();
                                    break;

                                    static RegexNode CloneMultiWithoutFirstChar(RegexNode node)
                                    {
                                        Debug.Assert(node.Kind is RegexNodeKind.Multi);
                                        Debug.Assert(node.Str!.Length >= 2);
                                        return node.Str!.Length == 2 ?
                                            new RegexNode(RegexNodeKind.One, node.Options, node.Str![1]) :
                                            new RegexNode(RegexNodeKind.Multi, node.Options, node.Str!.Substring(1));
                                    }
                            }

                            // This is only ever used for atomic alternations, so we can simply reset the doneLabel
                            // after emitting the child, as nothing will backtrack here (and we need to reset it
                            // so that all branches see the original).
                            doneLabel = originalDoneLabel;

                            // If we get here in the generated code, the branch completed successfully.
                            // Before jumping to the end, we need to zero out sliceStaticPos, so that no
                            // matter what the value is after the branch, whatever follows the alternate
                            // will see the same sliceStaticPos.
                            TransferSliceStaticPosToPos();
                            writer.WriteLine($"break;");
                            writer.WriteLine();

                            writer.Indent--;
                        }

                        // Default branch if the character didn't match the start of any branches.
                        CaseGoto("default:", doneLabel);
                    }
                }

                void EmitAllBranches()
                {
                    // Label to jump to when any branch completes successfully.
                    string matchLabel = ReserveName("AlternationMatch");

                    // Save off pos.  We'll need to reset this each time a branch fails.
                    string startingPos = ReserveName("alternation_starting_pos");
                    bool canUseLocalsForAllState = !isAtomic && !rm.Analysis.IsInLoop(node);
                    if (canUseLocalsForAllState)
                    {
                        // Because of how control flow and definite assignment works in the C# compiler, we can end
                        // up in situations where backtracking by hopping between labels leads the compiler to see
                        // things as not definitely assigned even if in practice they will be.  To avoid compilation
                        // errors with such complicated patterns we need to ensure the locals are declared and
                        // initialized at the beginning of the method.
                        additionalDeclarations.Add($"int {startingPos} = 0;");
                        writer.WriteLine($"{startingPos} = pos;");
                    }
                    else
                    {
                        writer.WriteLine($"int {startingPos} = pos;");
                    }
                    int startingSliceStaticPos = sliceStaticPos;

                    // We need to be able to undo captures in two situations:
                    // - If a branch of the alternation itself contains captures, then if that branch
                    //   fails to match, any captures from that branch until that failure point need to
                    //   be uncaptured prior to jumping to the next branch.
                    // - If the expression after the alternation contains captures, then failures
                    //   to match in those expressions could trigger backtracking back into the
                    //   alternation, and thus we need uncapture any of them.
                    // As such, if the alternation contains captures or if it's not atomic, we need
                    // to grab the current crawl position so we can unwind back to it when necessary.
                    // We can do all of the uncapturing as part of falling through to the next branch.
                    // If we fail in a branch, then such uncapturing will unwind back to the position
                    // at the start of the alternation.  If we fail after the alternation, and the
                    // matched branch didn't contain any backtracking, then the failure will end up
                    // jumping to the next branch, which will unwind the captures.  And if we fail after
                    // the alternation and the matched branch did contain backtracking, that backtracking
                    // construct is responsible for unwinding back to its starting crawl position. If
                    // it eventually ends up failing, that failure will result in jumping to the next branch
                    // of the alternation, which will again dutifully unwind the remaining captures until
                    // what they were at the start of the alternation.  Of course, if there are no captures
                    // anywhere in the regex, we don't have to do any of that.
                    string? startingCapturePos = null;
                    if (expressionHasCaptures && (rm.Analysis.MayContainCapture(node) || !isAtomic))
                    {
                        startingCapturePos = ReserveName("alternation_starting_capturepos");
                        if (canUseLocalsForAllState)
                        {
                            additionalDeclarations.Add($"int {startingCapturePos} = 0;");
                            writer.WriteLine($"{startingCapturePos} = base.Crawlpos();");
                        }
                        else
                        {
                            writer.WriteLine($"int {startingCapturePos} = base.Crawlpos();");
                        }
                    }
                    writer.WriteLine();

                    // After executing the alternation, subsequent matching may fail, at which point execution
                    // will need to backtrack to the alternation.  We emit a branching table at the end of the
                    // alternation, with a label that will be left as the "doneLabel" upon exiting emitting the
                    // alternation.  The branch table is populated with an entry for each branch of the alternation,
                    // containing either the label for the last backtracking construct in the branch if such a construct
                    // existed (in which case the doneLabel upon emitting that node will be different from before it)
                    // or the label for the next branch.
                    var labelMap = new string[childCount];
                    string backtrackLabel = ReserveName("AlternationBacktrack");
                    string? currentBranch = null;
                    if (canUseLocalsForAllState)
                    {
                        // We're not atomic, so we'll have to handle backtracking, but we're not inside of a loop,
                        // so we can store the current branch in a local rather than pushing it on to the backtracking
                        // stack (if we were in a loop, such a local couldn't be used as it could be overwritten by
                        // a subsequent iteration of that outer loop).
                        currentBranch = ReserveName("alternation_branch");
                        additionalDeclarations.Add($"int {currentBranch} = 0;");
                    }

                    for (int i = 0; i < childCount; i++)
                    {
                        // If the alternation isn't atomic, backtracking may require our jump table jumping back
                        // into these branches, so we can't use actual scopes, as that would hide the labels.
                        using (EmitBlock(writer, $"// Branch {i}", faux: !isAtomic))
                        {
                            bool isLastBranch = i == childCount - 1;

                            string? nextBranch = null;
                            if (!isLastBranch)
                            {
                                // Failure to match any branch other than the last one should result
                                // in jumping to process the next branch.
                                nextBranch = ReserveName("AlternationBranch");
                                doneLabel = nextBranch;
                            }
                            else
                            {
                                // Failure to match the last branch is equivalent to failing to match
                                // the whole alternation, which means those failures should jump to
                                // what "doneLabel" was defined as when starting the alternation.
                                doneLabel = originalDoneLabel;
                            }

                            // Emit the code for each branch.
                            EmitNode(node.Child(i));
                            writer.WriteLine();

                            // Add this branch to the backtracking table.  At this point, either the child
                            // had backtracking constructs, in which case doneLabel points to the last one
                            // and that's where we'll want to jump to, or it doesn't, in which case doneLabel
                            // still points to the nextBranch, which similarly is where we'll want to jump to.
                            if (!isAtomic)
                            {
                                // If we're inside of a loop, push the state we need to preserve on to the
                                // the backtracking stack.  If we're not inside of a loop, simply ensure all
                                // the relevant state is stored in our locals.
                                if (currentBranch is null)
                                {
                                    EmitStackPush(startingCapturePos is not null ?
                                        new[] { i.ToString(), startingPos, startingCapturePos } :
                                        new[] { i.ToString(), startingPos });
                                }
                                else
                                {
                                    writer.WriteLine($"{currentBranch} = {i};");
                                }
                            }
                            labelMap[i] = doneLabel;

                            // If we get here in the generated code, the branch completed successfully.
                            // Before jumping to the end, we need to zero out sliceStaticPos, so that no
                            // matter what the value is after the branch, whatever follows the alternate
                            // will see the same sliceStaticPos.
                            TransferSliceStaticPosToPos();
                            if (!isLastBranch || !isAtomic)
                            {
                                // If this isn't the last branch, we're about to output a reset section,
                                // and if this isn't atomic, there will be a backtracking section before
                                // the end of the method.  In both of those cases, we've successfully
                                // matched and need to skip over that code.  If, however, this is the
                                // last branch and this is an atomic alternation, we can just fall
                                // through to the successfully matched location.
                                Goto(matchLabel);
                            }

                            // Reset state for next branch and loop around to generate it.  This includes
                            // setting pos back to what it was at the beginning of the alternation,
                            // updating slice to be the full length it was, and if there's a capture that
                            // needs to be reset, uncapturing it.
                            if (!isLastBranch)
                            {
                                writer.WriteLine();
                                MarkLabel(nextBranch!, emitSemicolon: false);
                                writer.WriteLine($"pos = {startingPos};");
                                SliceInputSpan();
                                sliceStaticPos = startingSliceStaticPos;
                                if (startingCapturePos is not null)
                                {
                                    EmitUncaptureUntil(startingCapturePos);
                                }
                            }
                        }

                        writer.WriteLine();
                    }

                    // We should never fall through to this location in the generated code.  Either
                    // a branch succeeded in matching and jumped to the end, or a branch failed in
                    // matching and jumped to the next branch location.  We only get to this code
                    // if backtracking occurs and the code explicitly jumps here based on our setting
                    // "doneLabel" to the label for this section.  Thus, we only need to emit it if
                    // something can backtrack to us, which can't happen if we're inside of an atomic
                    // node. Thus, emit the backtracking section only if we're non-atomic.
                    if (isAtomic)
                    {
                        doneLabel = originalDoneLabel;
                    }
                    else
                    {
                        doneLabel = backtrackLabel;
                        MarkLabel(backtrackLabel, emitSemicolon: false);

                        // We're backtracking.  Check the timeout.
                        EmitTimeoutCheckIfNeeded(writer, rm);

                        string switchClause;
                        if (currentBranch is null)
                        {
                            // We're in a loop, so we use the backtracking stack to persist our state. Pop it off.
                            EmitStackPop(startingCapturePos is not null ?
                                new[] { startingCapturePos, startingPos } :
                                new[] { startingPos });
                            switchClause = StackPop();
                        }
                        else
                        {
                            // We're not in a loop, so our locals already store the state we need.
                            switchClause = currentBranch;
                        }
                        using (EmitBlock(writer, $"switch ({switchClause})"))
                        {
                            for (int i = 0; i < labelMap.Length; i++)
                            {
                                CaseGoto($"case {i}:", labelMap[i]);
                            }
                        }
                        writer.WriteLine();
                    }

                    // Successfully completed the alternate.
                    MarkLabel(matchLabel);
                    Debug.Assert(sliceStaticPos == 0);
                }
            }

            // Emits the code to handle a backreference.
            void EmitBackreference(RegexNode node)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Backreference, $"Unexpected type: {node.Kind}");

                int capnum = RegexParser.MapCaptureNumber(node.M, rm.Tree.CaptureNumberSparseMapping);

                if (sliceStaticPos > 0)
                {
                    TransferSliceStaticPosToPos();
                    writer.WriteLine();
                }

                // If the specified capture hasn't yet captured anything, fail to match... except when using RegexOptions.ECMAScript,
                // in which case per ECMA 262 section 21.2.2.9 the backreference should succeed.
                if ((node.Options & RegexOptions.ECMAScript) != 0)
                {
                    writer.WriteLine($"// If the {DescribeCapture(node.M, rm)} hasn't matched, the backreference matches with RegexOptions.ECMAScript rules.");
                    using (EmitBlock(writer, $"if (base.IsMatched({capnum}))"))
                    {
                        EmitWhenHasCapture();
                    }
                }
                else
                {
                    writer.WriteLine($"// If the {DescribeCapture(node.M, rm)} hasn't matched, the backreference doesn't match.");
                    using (EmitBlock(writer, $"if (!base.IsMatched({capnum}))"))
                    {
                        Goto(doneLabel);
                    }
                    writer.WriteLine();
                    EmitWhenHasCapture();
                }

                void EmitWhenHasCapture()
                {
                    writer.WriteLine("// Get the captured text.  If it doesn't match at the current position, the backreference doesn't match.");

                    additionalDeclarations.Add("int matchLength = 0;");
                    writer.WriteLine($"matchLength = base.MatchLength({capnum});");

                    // Validate that the remaining length of the slice is sufficient
                    // to possibly match, and then do a SequenceEqual against the matched text.
                    if ((node.Options & RegexOptions.RightToLeft) == 0)
                    {
                        writer.WriteLine($"if ({sliceSpan}.Length < matchLength || ");
                        using (EmitBlock(writer, $"    !inputSpan.Slice(base.MatchIndex({capnum}), matchLength).SequenceEqual({sliceSpan}.Slice(0, matchLength)))"))
                        {
                            Goto(doneLabel);
                        }

                        writer.WriteLine();
                        writer.WriteLine($"pos += matchLength;");
                    }
                    else
                    {
                        writer.WriteLine($"if (pos < matchLength || ");
                        using (EmitBlock(writer, $"    !inputSpan.Slice(base.MatchIndex({capnum}), matchLength).SequenceEqual(inputSpan.Slice(pos - matchLength, matchLength)))"))
                        {
                            Goto(doneLabel);
                        }

                        writer.WriteLine();
                        writer.WriteLine($"pos -= matchLength;");
                    }
                    SliceInputSpan();
                }
            }

            // Emits the code for an if(backreference)-then-else conditional.
            void EmitBackreferenceConditional(RegexNode node)
            {
                Debug.Assert(node.Kind is RegexNodeKind.BackreferenceConditional, $"Unexpected type: {node.Kind}");
                Debug.Assert(node.ChildCount() == 2, $"Expected 2 children, found {node.ChildCount()}");

                // We're branching in a complicated fashion.  Make sure sliceStaticPos is 0.
                TransferSliceStaticPosToPos();

                // Get the capture number to test.
                int capnum = RegexParser.MapCaptureNumber(node.M, rm.Tree.CaptureNumberSparseMapping);

                // Get the "yes" branch and the "no" branch.  The "no" branch is optional in syntax and is thus
                // somewhat likely to be Empty.
                RegexNode yesBranch = node.Child(0);
                RegexNode? noBranch = node.Child(1) is { Kind: not RegexNodeKind.Empty } childNo ? childNo : null;
                string originalDoneLabel = doneLabel;

                // If the child branches might backtrack, we can't emit the branches inside constructs that
                // require braces, e.g. if/else, even though that would yield more idiomatic output.
                // But if we know for certain they won't backtrack, we can output the nicer code.
                if (rm.Analysis.IsAtomicByAncestor(node) || (!rm.Analysis.MayBacktrack(yesBranch) && (noBranch is null || !rm.Analysis.MayBacktrack(noBranch))))
                {
                    using (EmitBlock(writer, $"if (base.IsMatched({capnum}))"))
                    {
                        writer.WriteLine($"// The {DescribeCapture(node.M, rm)} captured a value.  Match the first branch.");
                        EmitNode(yesBranch);
                        writer.WriteLine();
                        TransferSliceStaticPosToPos(); // make sure sliceStaticPos is 0 after each branch
                    }

                    if (noBranch is not null)
                    {
                        using (EmitBlock(writer, $"else"))
                        {
                            writer.WriteLine($"// Otherwise, match the second branch.");
                            EmitNode(noBranch);
                            writer.WriteLine();
                            TransferSliceStaticPosToPos(); // make sure sliceStaticPos is 0 after each branch
                        }
                    }

                    doneLabel = originalDoneLabel; // atomicity
                    return;
                }

                string refNotMatched = ReserveName("ConditionalBackreferenceNotMatched");
                string endConditional = ReserveName("ConditionalBackreferenceEnd");

                // As with alternations, we have potentially multiple branches, each of which may contain
                // backtracking constructs, but the expression after the conditional needs a single target
                // to backtrack to.  So, we expose a single Backtrack label and track which branch was
                // followed in this resumeAt local.
                string resumeAt = ReserveName("conditionalbackreference_branch");
                bool isInLoop = rm.Analysis.IsInLoop(node);
                if (isInLoop)
                {
                    writer.WriteLine($"int {resumeAt};");
                }
                else
                {
                    additionalDeclarations.Add($"int {resumeAt} = 0;");
                }

                // While it would be nicely readable to use an if/else block, if the branches contain
                // anything that triggers backtracking, labels will end up being defined, and if they're
                // inside the scope block for the if or else, that will prevent jumping to them from
                // elsewhere.  So we implement the if/else with labels and gotos manually.
                // Check to see if the specified capture number was captured.
                using (EmitBlock(writer, $"if (!base.IsMatched({capnum}))"))
                {
                    Goto(refNotMatched);
                }
                writer.WriteLine();

                // The specified capture was captured.  Run the "yes" branch.
                // If it successfully matches, jump to the end.
                EmitNode(yesBranch);
                writer.WriteLine();
                TransferSliceStaticPosToPos(); // make sure sliceStaticPos is 0 after each branch
                string postYesDoneLabel = doneLabel;
                if (postYesDoneLabel != originalDoneLabel || isInLoop)
                {
                    writer.WriteLine($"{resumeAt} = 0;");
                }

                bool needsEndConditional = postYesDoneLabel != originalDoneLabel || noBranch is not null;
                if (needsEndConditional)
                {
                    Goto(endConditional);
                    writer.WriteLine();
                }

                MarkLabel(refNotMatched);
                string postNoDoneLabel = originalDoneLabel;
                if (noBranch is not null)
                {
                    // Output the no branch.
                    doneLabel = originalDoneLabel;
                    EmitNode(noBranch);
                    writer.WriteLine();
                    TransferSliceStaticPosToPos(); // make sure sliceStaticPos is 0 after each branch
                    postNoDoneLabel = doneLabel;
                    if (postNoDoneLabel != originalDoneLabel || isInLoop)
                    {
                        writer.WriteLine($"{resumeAt} = 1;");
                    }
                }
                else
                {
                    // There's only a yes branch.  If it's going to cause us to output a backtracking
                    // label but code may not end up taking the yes branch path, we need to emit a resumeAt
                    // that will cause the backtracking to immediately pass through this node.
                    if (postYesDoneLabel != originalDoneLabel || isInLoop)
                    {
                        writer.WriteLine($"{resumeAt} = 2;");
                    }
                }

                // If either the yes branch or the no branch contained backtracking, subsequent expressions
                // might try to backtrack to here, so output a backtracking map based on resumeAt.
                bool hasBacktracking = postYesDoneLabel != originalDoneLabel || postNoDoneLabel != originalDoneLabel;
                if (hasBacktracking)
                {
                    // Skip the backtracking section.
                    Goto(endConditional);
                    writer.WriteLine();

                    // Backtrack section
                    string backtrack = ReserveName("ConditionalBackreferenceBacktrack");
                    doneLabel = backtrack;
                    MarkLabel(backtrack, emitSemicolon: false);

                    // Pop from the stack the branch that was used and jump back to its backtracking location.
                    // If we're not in a loop, though, we won't have pushed it on to the stack as nothing will
                    // have been able to overwrite it in the interim, so we can just trust the value already in
                    // the local.
                    if (isInLoop)
                    {
                        EmitStackPop(resumeAt);
                    }
                    using (EmitBlock(writer, $"switch ({resumeAt})"))
                    {
                        if (postYesDoneLabel != originalDoneLabel)
                        {
                            CaseGoto("case 0:", postYesDoneLabel);
                        }

                        if (postNoDoneLabel != originalDoneLabel)
                        {
                            CaseGoto("case 1:", postNoDoneLabel);
                        }

                        CaseGoto("default:", originalDoneLabel);
                    }
                }

                if (needsEndConditional)
                {
                    MarkLabel(endConditional);
                }

                if (hasBacktracking && isInLoop)
                {
                    // We're not atomic and at least one of the yes or no branches contained backtracking constructs,
                    // so finish outputting our backtracking logic, which involves pushing onto the stack which
                    // branch to backtrack into.  If we're not in a loop, though, nothing else can overwrite this local
                    // in the interim, so we can avoid pushing it.
                    EmitStackPush(resumeAt);
                }
            }

            // Emits the code for an if(expression)-then-else conditional.
            void EmitExpressionConditional(RegexNode node)
            {
                Debug.Assert(node.Kind is RegexNodeKind.ExpressionConditional, $"Unexpected type: {node.Kind}");
                Debug.Assert(node.ChildCount() == 3, $"Expected 3 children, found {node.ChildCount()}");

                bool isAtomic = rm.Analysis.IsAtomicByAncestor(node);

                // We're branching in a complicated fashion.  Make sure sliceStaticPos is 0.
                TransferSliceStaticPosToPos();

                // The first child node is the condition expression.  If this matches, then we branch to the "yes" branch.
                // If it doesn't match, then we branch to the optional "no" branch if it exists, or simply skip the "yes"
                // branch, otherwise. The condition is treated as a positive lookaround.
                RegexNode condition = node.Child(0);

                // Get the "yes" branch and the "no" branch.  The "no" branch is optional in syntax and is thus
                // somewhat likely to be Empty.
                RegexNode yesBranch = node.Child(1);
                RegexNode? noBranch = node.Child(2) is { Kind: not RegexNodeKind.Empty } childNo ? childNo : null;
                string originalDoneLabel = doneLabel;

                string expressionNotMatched = ReserveName("ConditionalExpressionNotMatched");
                string endConditional = ReserveName("ConditionalExpressionEnd");

                // As with alternations, we have potentially multiple branches, each of which may contain
                // backtracking constructs, but the expression after the condition needs a single target
                // to backtrack to.  So, we expose a single Backtrack label and track which branch was
                // followed in this resumeAt local.
                bool isInLoop = false;
                string resumeAt = ReserveName("conditionalexpression_branch");
                if (!isAtomic)
                {
                    isInLoop = rm.Analysis.IsInLoop(node);
                    if (isInLoop)
                    {
                        writer.WriteLine($"int {resumeAt} = 0;");
                    }
                    else
                    {
                        additionalDeclarations.Add($"int {resumeAt} = 0;");
                    }
                }

                // If the condition expression has captures, we'll need to uncapture them in the case of no match.
                string? startingCapturePos = null;
                if (rm.Analysis.MayContainCapture(condition))
                {
                    startingCapturePos = ReserveName("conditionalexpression_starting_capturepos");
                    writer.WriteLine($"int {startingCapturePos} = base.Crawlpos();");
                }

                // Emit the condition expression.  Route any failures to after the yes branch.  This code is almost
                // the same as for a positive lookaround; however, a positive lookaround only needs to reset the position
                // on a successful match, as a failed match fails the whole expression; here, we need to reset the
                // position on completion, regardless of whether the match is successful or not.
                doneLabel = expressionNotMatched;

                // Save off pos.  We'll need to reset this upon successful completion of the lookaround.
                string startingPos = ReserveName("conditionalexpression_starting_pos");
                writer.WriteLine($"int {startingPos} = pos;");
                writer.WriteLine();
                int startingSliceStaticPos = sliceStaticPos;

                // Emit the child. The condition expression is a zero-width assertion, which is atomic,
                // so prevent backtracking into it.
                writer.WriteLine("// Condition:");
                EmitNode(condition);
                writer.WriteLine();
                doneLabel = originalDoneLabel;

                // After the condition completes successfully, reset the text positions.
                // Do not reset captures, which persist beyond the lookaround.
                writer.WriteLine("// Condition matched:");
                writer.WriteLine($"pos = {startingPos};");
                SliceInputSpan();
                sliceStaticPos = startingSliceStaticPos;
                writer.WriteLine();

                // The expression matched.  Run the "yes" branch. If it successfully matches, jump to the end.
                EmitNode(yesBranch);
                writer.WriteLine();
                TransferSliceStaticPosToPos(); // make sure sliceStaticPos is 0 after each branch
                string postYesDoneLabel = doneLabel;
                if (!isAtomic && postYesDoneLabel != originalDoneLabel)
                {
                    writer.WriteLine($"{resumeAt} = 0;");
                }
                Goto(endConditional);
                writer.WriteLine();

                // After the condition completes unsuccessfully, reset the text positions
                // _and_ reset captures, which should not persist when the whole expression failed.
                writer.WriteLine("// Condition did not match:");
                MarkLabel(expressionNotMatched, emitSemicolon: false);
                writer.WriteLine($"pos = {startingPos};");
                SliceInputSpan();
                sliceStaticPos = startingSliceStaticPos;
                if (startingCapturePos is not null)
                {
                    EmitUncaptureUntil(startingCapturePos);
                }
                writer.WriteLine();

                string postNoDoneLabel = originalDoneLabel;
                if (noBranch is not null)
                {
                    // Output the no branch.
                    doneLabel = originalDoneLabel;
                    EmitNode(noBranch);
                    writer.WriteLine();
                    TransferSliceStaticPosToPos(); // make sure sliceStaticPos is 0 after each branch
                    postNoDoneLabel = doneLabel;
                    if (!isAtomic && postNoDoneLabel != originalDoneLabel)
                    {
                        writer.WriteLine($"{resumeAt} = 1;");
                    }
                }
                else
                {
                    // There's only a yes branch.  If it's going to cause us to output a backtracking
                    // label but code may not end up taking the yes branch path, we need to emit a resumeAt
                    // that will cause the backtracking to immediately pass through this node.
                    if (!isAtomic && postYesDoneLabel != originalDoneLabel)
                    {
                        writer.WriteLine($"{resumeAt} = 2;");
                    }
                }

                // If either the yes branch or the no branch contained backtracking, subsequent expressions
                // might try to backtrack to here, so output a backtracking map based on resumeAt.
                if (isAtomic || (postYesDoneLabel == originalDoneLabel && postNoDoneLabel == originalDoneLabel))
                {
                    doneLabel = originalDoneLabel;
                    MarkLabel(endConditional);
                }
                else
                {
                    // Skip the backtracking section.
                    Goto(endConditional);
                    writer.WriteLine();

                    string backtrack = ReserveName("ConditionalExpressionBacktrack");
                    doneLabel = backtrack;
                    MarkLabel(backtrack, emitSemicolon: false);

                    if (isInLoop)
                    {
                        // If we're not in a loop, the local will maintain its value until backtracking occurs.
                        // If we are in a loop, multiple iterations need their own value, so we need to use the stack.
                        EmitStackPop(resumeAt);
                    }

                    using (EmitBlock(writer, $"switch ({resumeAt})"))
                    {
                        if (postYesDoneLabel != originalDoneLabel)
                        {
                            CaseGoto("case 0:", postYesDoneLabel);
                        }

                        if (postNoDoneLabel != originalDoneLabel)
                        {
                            CaseGoto("case 1:", postNoDoneLabel);
                        }

                        CaseGoto("default:", originalDoneLabel);
                    }

                    MarkLabel(endConditional, emitSemicolon: !isInLoop);
                    if (isInLoop)
                    {
                        EmitStackPush(resumeAt);
                    }
                }
            }

            // Emits the code for a Capture node.
            void EmitCapture(RegexNode node, RegexNode? subsequent = null)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Capture, $"Unexpected type: {node.Kind}");
                Debug.Assert(node.ChildCount() == 1, $"Expected 1 child, found {node.ChildCount()}");

                int capnum = RegexParser.MapCaptureNumber(node.M, rm.Tree.CaptureNumberSparseMapping);
                int uncapnum = RegexParser.MapCaptureNumber(node.N, rm.Tree.CaptureNumberSparseMapping);
                bool isAtomic = rm.Analysis.IsAtomicByAncestor(node);
                bool isInLoop = rm.Analysis.IsInLoop(node);

                TransferSliceStaticPosToPos();
                string startingPos = ReserveName("capture_starting_pos");
                if (isInLoop)
                {
                    writer.WriteLine($"int {startingPos} = pos;");
                }
                else
                {
                    additionalDeclarations.Add($"int {startingPos} = 0;");
                    writer.WriteLine($"{startingPos} = pos;");
                }
                writer.WriteLine();

                RegexNode child = node.Child(0);

                if (uncapnum != -1)
                {
                    using (EmitBlock(writer, $"if (!base.IsMatched({uncapnum}))"))
                    {
                        Goto(doneLabel);
                    }
                    writer.WriteLine();
                }

                // Emit child node.
                string originalDoneLabel = doneLabel;
                EmitNode(child, subsequent);
                bool childBacktracks = doneLabel != originalDoneLabel;

                writer.WriteLine();
                TransferSliceStaticPosToPos();
                if (uncapnum == -1)
                {
                    writer.WriteLine($"base.Capture({capnum}, {startingPos}, pos);");
                }
                else
                {
                    writer.WriteLine($"base.TransferCapture({capnum}, {uncapnum}, {startingPos}, pos);");
                }

                if (isAtomic || !childBacktracks)
                {
                    // If the capture is atomic and nothing can backtrack into it, we're done.
                    // Similarly, even if the capture isn't atomic, if the captured expression
                    // doesn't do any backtracking, we're done.
                    doneLabel = originalDoneLabel;
                }
                else
                {
                    // We're not atomic and the child node backtracks.  When it does, we need
                    // to ensure that the starting position for the capture is appropriately
                    // reset to what it was initially (it could have changed as part of being
                    // in a loop or similar).  So, we emit a backtracking section that
                    // pushes/pops the starting position before falling through.
                    writer.WriteLine();

                    if (isInLoop)
                    {
                        // If we're in a loop, different iterations of the loop need their own
                        // starting position, so push it on to the stack.  If we're not in a loop,
                        // the local will maintain its value and will suffice.
                        EmitStackPush(startingPos);
                    }

                    // Skip past the backtracking section
                    string end = ReserveName("CaptureSkipBacktrack");
                    Goto(end);
                    writer.WriteLine();

                    // Emit a backtracking section that restores the capture's state and then jumps to the previous done label
                    string backtrack = ReserveName($"CaptureBacktrack");
                    MarkLabel(backtrack, emitSemicolon: false);
                    if (isInLoop)
                    {
                        EmitStackPop(startingPos);
                    }
                    Goto(doneLabel);
                    writer.WriteLine();

                    doneLabel = backtrack;
                    MarkLabel(end);
                }
            }

            // Emits the code to handle a positive lookaround assertion. This is a positive lookahead
            // for left-to-right and a positive lookbehind for right-to-left.
            void EmitPositiveLookaroundAssertion(RegexNode node)
            {
                Debug.Assert(node.Kind is RegexNodeKind.PositiveLookaround, $"Unexpected type: {node.Kind}");
                Debug.Assert(node.ChildCount() == 1, $"Expected 1 child, found {node.ChildCount()}");

                if (rm.Analysis.HasRightToLeft)
                {
                    // Lookarounds are the only places in the node tree where we might change direction,
                    // i.e. where we might go from RegexOptions.None to RegexOptions.RightToLeft, or vice
                    // versa.  This is because lookbehinds are implemented by making the whole subgraph be
                    // RegexOptions.RightToLeft and reversed.  Since we use static position to optimize left-to-right
                    // and don't use it in support of right-to-left, we need to resync the static position
                    // to the current position when entering a lookaround, just in case we're changing direction.
                    TransferSliceStaticPosToPos(forceSliceReload: true);
                }

                // Save off pos.  We'll need to reset this upon successful completion of the lookaround.
                string startingPos = ReserveName((node.Options & RegexOptions.RightToLeft) != 0 ? "positivelookbehind_starting_pos" : "positivelookahead_starting_pos");
                writer.WriteLine($"int {startingPos} = pos;");
                writer.WriteLine();
                int startingSliceStaticPos = sliceStaticPos;

                // Check for timeout. Lookarounds result in re-processing the same input, so while not
                // technically backtracking, it's appropriate to have a timeout check.
                EmitTimeoutCheckIfNeeded(writer, rm);

                // Emit the child.
                RegexNode child = node.Child(0);
                if (rm.Analysis.MayBacktrack(child))
                {
                    // Lookarounds are implicitly atomic, so we need to emit the node as atomic if it might backtrack.
                    EmitAtomic(node, null);
                }
                else
                {
                    EmitNode(child);
                }

                // After the child completes successfully, reset the text positions.
                // Do not reset captures, which persist beyond the lookaround.
                writer.WriteLine();
                writer.WriteLine($"pos = {startingPos};");
                SliceInputSpan();
                sliceStaticPos = startingSliceStaticPos;
            }

            // Emits the code to handle a negative lookaround assertion. This is a negative lookahead
            // for left-to-right and a negative lookbehind for right-to-left.
            void EmitNegativeLookaroundAssertion(RegexNode node)
            {
                Debug.Assert(node.Kind is RegexNodeKind.NegativeLookaround, $"Unexpected type: {node.Kind}");
                Debug.Assert(node.ChildCount() == 1, $"Expected 1 child, found {node.ChildCount()}");

                if (rm.Analysis.HasRightToLeft)
                {
                    // Lookarounds are the only places in the node tree where we might change direction,
                    // i.e. where we might go from RegexOptions.None to RegexOptions.RightToLeft, or vice
                    // versa.  This is because lookbehinds are implemented by making the whole subgraph be
                    // RegexOptions.RightToLeft and reversed.  Since we use static position to optimize left-to-right
                    // and don't use it in support of right-to-left, we need to resync the static position
                    // to the current position when entering a lookaround, just in case we're changing direction.
                    TransferSliceStaticPosToPos(forceSliceReload: true);
                }

                string originalDoneLabel = doneLabel;

                // Save off pos.  We'll need to reset this upon successful completion of the lookaround.
                string startingPos = ReserveName((node.Options & RegexOptions.RightToLeft) != 0 ? "negativelookbehind_starting_pos" : "negativelookahead_starting_pos");
                writer.WriteLine($"int {startingPos} = pos;");
                int startingSliceStaticPos = sliceStaticPos;

                string negativeLookaroundDoneLabel = ReserveName("NegativeLookaroundMatch");
                doneLabel = negativeLookaroundDoneLabel;

                // Check for timeout. Lookarounds result in re-processing the same input, so while not
                // technically backtracking, it's appropriate to have a timeout check.
                EmitTimeoutCheckIfNeeded(writer, rm);

                // Emit the child.
                RegexNode child = node.Child(0);
                if (rm.Analysis.MayBacktrack(child))
                {
                    // Lookarounds are implicitly atomic, so we need to emit the node as atomic if it might backtrack.
                    EmitAtomic(node, null);
                }
                else
                {
                    EmitNode(child);
                }

                // If the generated code ends up here, it matched the lookaround, which actually
                // means failure for a _negative_ lookaround, so we need to jump to the original done.
                writer.WriteLine();
                Goto(originalDoneLabel);
                writer.WriteLine();

                // Failures (success for a negative lookaround) jump here.
                MarkLabel(negativeLookaroundDoneLabel, emitSemicolon: false);

                // After the child completes in failure (success for negative lookaround), reset the text positions.
                writer.WriteLine($"pos = {startingPos};");
                SliceInputSpan();
                sliceStaticPos = startingSliceStaticPos;

                doneLabel = originalDoneLabel;
            }

            // Emits the code for the node.
            void EmitNode(RegexNode node, RegexNode? subsequent = null, bool emitLengthChecksIfRequired = true)
            {
                // Before we handle general-purpose matching logic for nodes, handle any special-casing.
                if (rm.Tree.FindOptimizations.FindMode == FindNextStartingPositionMode.LiteralAfterLoop_LeftToRight &&
                    rm.Tree.FindOptimizations.LiteralAfterLoop?.LoopNode == node)
                {
                    Debug.Assert(sliceStaticPos == 0, "This should be the first node and thus static position shouldn't have advanced.");
                    writer.WriteLine("// Skip loop already matched in TryFindNextPossibleStartingPosition.");
                    writer.WriteLine("pos = base.runtrackpos;");
                    SliceInputSpan();
                    return;
                }

                if (!StackHelper.TryEnsureSufficientExecutionStack())
                {
                    StackHelper.CallOnEmptyStack(EmitNode, node, subsequent, emitLengthChecksIfRequired);
                    return;
                }

                if ((node.Options & RegexOptions.RightToLeft) != 0)
                {
                    // RightToLeft doesn't take advantage of static positions.  While RightToLeft won't update static
                    // positions, a previous operation may have left us with a non-zero one.  Make sure it's zero'd out
                    // such that pos and slice are up-to-date.  Note that RightToLeft also shouldn't use the slice span,
                    // as it's not kept up-to-date; any RightToLeft implementation that wants to use it must first update
                    // it from pos.
                    TransferSliceStaticPosToPos();
                }

                // Separate out several node types that, for conciseness, don't need a header nor scope written into the source.
                // Effectively these either evaporate, are completely self-explanatory, or only exist for their children to be rendered.
                switch (node.Kind)
                {
                    // Nothing is written for an empty.
                    case RegexNodeKind.Empty:
                        return;

                    // A single-line goto for a failure doesn't need a scope or comment.
                    case RegexNodeKind.Nothing:
                        Goto(doneLabel);
                        return;

                    // Skip atomic nodes that wrap non-backtracking children; in such a case there's nothing to be made atomic.
                    case RegexNodeKind.Atomic when !rm.Analysis.MayBacktrack(node.Child(0)):
                        EmitNode(node.Child(0));
                        return;

                    // Concatenate is a simplification in the node tree so that a series of children can be represented as one.
                    // We don't need its presence visible in the source.
                    case RegexNodeKind.Concatenate:
                        EmitConcatenation(node, subsequent, emitLengthChecksIfRequired);
                        return;
                }

                // For everything else, output a comment about what the node is.
                writer.WriteLine($"// {DescribeNode(node, rm)}");

                // Separate out several node types that, for conciseness, don't need a scope written into the source as they're
                // always a single statement / block.
                switch (node.Kind)
                {
                    case RegexNodeKind.Beginning:
                    case RegexNodeKind.Start:
                    case RegexNodeKind.Bol:
                    case RegexNodeKind.Eol:
                    case RegexNodeKind.End:
                    case RegexNodeKind.EndZ:
                        EmitAnchors(node);
                        return;

                    case RegexNodeKind.Boundary:
                    case RegexNodeKind.NonBoundary:
                    case RegexNodeKind.ECMABoundary:
                    case RegexNodeKind.NonECMABoundary:
                        EmitBoundary(node);
                        return;

                    case RegexNodeKind.One:
                    case RegexNodeKind.Notone:
                    case RegexNodeKind.Set:
                        EmitSingleChar(node, emitLengthChecksIfRequired);
                        return;

                    case RegexNodeKind.Multi when (node.Options & RegexOptions.RightToLeft) == 0:
                        EmitMultiChar(node, emitLengthChecksIfRequired);
                        return;

                    case RegexNodeKind.UpdateBumpalong:
                        EmitUpdateBumpalong(node);
                        return;
                }

                // For everything else, put the node's code into its own scope, purely for readability. If the node contains labels
                // that may need to be visible outside of its scope, the scope is still emitted for clarity but is commented out.
                using (EmitBlock(writer, null, faux: rm.Analysis.MayBacktrack(node)))
                {
                    switch (node.Kind)
                    {
                        case RegexNodeKind.Multi:
                            EmitMultiChar(node, emitLengthChecksIfRequired);
                            return;

                        case RegexNodeKind.Oneloop:
                        case RegexNodeKind.Notoneloop:
                        case RegexNodeKind.Setloop:
                            EmitSingleCharLoop(node, subsequent, emitLengthChecksIfRequired);
                            return;

                        case RegexNodeKind.Onelazy:
                        case RegexNodeKind.Notonelazy:
                        case RegexNodeKind.Setlazy:
                            EmitSingleCharLazy(node, subsequent, emitLengthChecksIfRequired);
                            return;

                        case RegexNodeKind.Oneloopatomic:
                        case RegexNodeKind.Notoneloopatomic:
                        case RegexNodeKind.Setloopatomic:
                            EmitSingleCharAtomicLoop(node, emitLengthChecksIfRequired);
                            return;

                        case RegexNodeKind.Loop:
                            EmitLoop(node);
                            return;

                        case RegexNodeKind.Lazyloop:
                            EmitLazy(node);
                            return;

                        case RegexNodeKind.Alternate:
                            EmitAlternation(node);
                            return;

                        case RegexNodeKind.Backreference:
                            EmitBackreference(node);
                            return;

                        case RegexNodeKind.BackreferenceConditional:
                            EmitBackreferenceConditional(node);
                            return;

                        case RegexNodeKind.ExpressionConditional:
                            EmitExpressionConditional(node);
                            return;

                        case RegexNodeKind.Atomic:
                            Debug.Assert(rm.Analysis.MayBacktrack(node.Child(0)));
                            EmitAtomic(node, subsequent);
                            return;

                        case RegexNodeKind.Capture:
                            EmitCapture(node, subsequent);
                            return;

                        case RegexNodeKind.PositiveLookaround:
                            EmitPositiveLookaroundAssertion(node);
                            return;

                        case RegexNodeKind.NegativeLookaround:
                            EmitNegativeLookaroundAssertion(node);
                            return;
                    }
                }

                // All nodes should have been handled.
                Debug.Fail($"Unexpected node type: {node.Kind}");
            }

            // Emits the node for an atomic.
            void EmitAtomic(RegexNode node, RegexNode? subsequent)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Atomic or RegexNodeKind.PositiveLookaround or RegexNodeKind.NegativeLookaround, $"Unexpected type: {node.Kind}");
                Debug.Assert(node.ChildCount() == 1, $"Expected 1 child, found {node.ChildCount()}");
                Debug.Assert(rm.Analysis.MayBacktrack(node.Child(0)), "Expected child to potentially backtrack");

                // Grab the current done label and the current backtracking position.  The purpose of the atomic node
                // is to ensure that nodes after it that might backtrack skip over the atomic, which means after
                // rendering the atomic's child, we need to reset the label so that subsequent backtracking doesn't
                // see any label left set by the atomic's child.  We also need to reset the backtracking stack position
                // so that the state on the stack remains consistent.
                string originalDoneLabel = doneLabel;
                additionalDeclarations.Add("int stackpos = 0;");
                string startingStackpos = ReserveName("atomic_stackpos");
                writer.WriteLine($"int {startingStackpos} = stackpos;");
                writer.WriteLine();

                // Emit the child.
                EmitNode(node.Child(0), subsequent);
                writer.WriteLine();

                // Reset the stack position and done label.
                writer.WriteLine($"stackpos = {startingStackpos};");
                doneLabel = originalDoneLabel;
            }

            // Emits the code to handle updating base.runtextpos to pos in response to
            // an UpdateBumpalong node.  This is used when we want to inform the scan loop that
            // it should bump from this location rather than from the original location.
            void EmitUpdateBumpalong(RegexNode node)
            {
                Debug.Assert(node.Kind is RegexNodeKind.UpdateBumpalong, $"Unexpected type: {node.Kind}");

                TransferSliceStaticPosToPos();
                using (EmitBlock(writer, "if (base.runtextpos < pos)"))
                {
                    writer.WriteLine("base.runtextpos = pos;");
                }
            }

            // Emits code for a concatenation
            void EmitConcatenation(RegexNode node, RegexNode? subsequent, bool emitLengthChecksIfRequired)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Concatenate, $"Unexpected type: {node.Kind}");
                Debug.Assert(node.ChildCount() >= 2, $"Expected at least 2 children, found {node.ChildCount()}");

                // Emit the code for each child one after the other.
                string? prevDescription = null;
                int childCount = node.ChildCount();
                for (int i = 0; i < childCount; i++)
                {
                    // If we can find a subsequence of fixed-length children, we can emit a length check once for that sequence
                    // and then skip the individual length checks for each.  We can also discover case-insensitive sequences that
                    // can be checked efficiently with methods like StartsWith. We also want to minimize the repetition of if blocks,
                    // and so we try to emit a series of clauses all part of the same if block rather than one if block per child.
                    if ((node.Options & RegexOptions.RightToLeft) == 0 &&
                        emitLengthChecksIfRequired &&
                        node.TryGetJoinableLengthCheckChildRange(i, out int requiredLength, out int exclusiveEnd))
                    {
                        bool wroteClauses = true;
                        writer.Write($"if ({SpanLengthCheck(requiredLength)}");

                        while (i < exclusiveEnd)
                        {
                            for (; i < exclusiveEnd; i++)
                            {
                                void WritePrefix()
                                {
                                    if (wroteClauses)
                                    {
                                        writer.WriteLine(prevDescription is not null ? $" || // {prevDescription}" : " ||");
                                        writer.Write("    ");
                                    }
                                    else
                                    {
                                        writer.Write("if (");
                                    }
                                }

                                RegexNode child = node.Child(i);
                                if (node.TryGetOrdinalCaseInsensitiveString(i, exclusiveEnd, out int nodesConsumed, out string? caseInsensitiveString))
                                {
                                    WritePrefix();
                                    string sourceSpan = sliceStaticPos > 0 ? $"{sliceSpan}.Slice({sliceStaticPos})" : sliceSpan;
                                    writer.Write($"!{sourceSpan}.StartsWith({Literal(caseInsensitiveString)}, StringComparison.OrdinalIgnoreCase)");
                                    prevDescription = $"Match the string {Literal(caseInsensitiveString)} (ordinal case-insensitive)";
                                    wroteClauses = true;

                                    sliceStaticPos += caseInsensitiveString.Length;
                                    i += nodesConsumed - 1;
                                }
                                else if (child.Kind is RegexNodeKind.Multi)
                                {
                                    WritePrefix();
                                    EmitMultiCharString(child.Str!, emitLengthCheck: false, clauseOnly: true, rightToLeft: false);
                                    prevDescription = DescribeNode(child, rm);
                                    wroteClauses = true;
                                }
                                else if ((child.IsOneFamily || child.IsNotoneFamily || child.IsSetFamily) &&
                                         child.M == child.N &&
                                         child.M <= MaxUnrollSize)
                                {
                                    int repeatCount = child.Kind is RegexNodeKind.One or RegexNodeKind.Notone or RegexNodeKind.Set ? 1 : child.M;
                                    for (int c = 0; c < repeatCount; c++)
                                    {
                                        WritePrefix();
                                        EmitSingleChar(child, emitLengthCheck: false, clauseOnly: true);
                                        prevDescription = c == 0 ? DescribeNode(child, rm) : null;
                                        wroteClauses = true;
                                    }
                                }
                                else break;
                            }

                            if (wroteClauses)
                            {
                                writer.WriteLine(prevDescription is not null ? $") // {prevDescription}" : ")");
                                using (EmitBlock(writer, null))
                                {
                                    Goto(doneLabel);
                                }
                                if (i < childCount)
                                {
                                    writer.WriteLine();
                                }

                                wroteClauses = false;
                                prevDescription = null;
                            }

                            if (i < exclusiveEnd)
                            {
                                EmitNode(node.Child(i), GetSubsequentOrDefault(i, node, subsequent), emitLengthChecksIfRequired: false);
                                if (i < childCount - 1)
                                {
                                    writer.WriteLine();
                                }

                                i++;
                            }
                        }

                        i--;
                        continue;
                    }

                    EmitNode(node.Child(i), GetSubsequentOrDefault(i, node, subsequent), emitLengthChecksIfRequired: emitLengthChecksIfRequired);
                    if (i < childCount - 1)
                    {
                        writer.WriteLine();
                    }
                }

                // Gets the node to treat as the subsequent one to node.Child(index)
                static RegexNode? GetSubsequentOrDefault(int index, RegexNode node, RegexNode? defaultNode)
                {
                    int childCount = node.ChildCount();
                    for (int i = index + 1; i < childCount; i++)
                    {
                        RegexNode next = node.Child(i);
                        if (next.Kind is not RegexNodeKind.UpdateBumpalong) // skip node types that don't have a semantic impact
                        {
                            return next;
                        }
                    }

                    return defaultNode;
                }
            }

            // Emits the code to handle a single-character match.
            void EmitSingleChar(RegexNode node, bool emitLengthCheck = true, string? offset = null, bool clauseOnly = false)
            {
                Debug.Assert(node.IsOneFamily || node.IsNotoneFamily || node.IsSetFamily, $"Unexpected type: {node.Kind}");

                bool rtl = (node.Options & RegexOptions.RightToLeft) != 0;
                Debug.Assert(!rtl || offset is null);
                Debug.Assert(!rtl || !clauseOnly);

                string expr = !rtl ?
                    $"{sliceSpan}[{Sum(sliceStaticPos, offset)}]" :
                     "inputSpan[pos - 1]";

                if (node.IsSetFamily)
                {
                    expr = MatchCharacterClass(expr, node.Str!, negate: true, additionalDeclarations, requiredHelpers);
                }
                else
                {
                    expr = $"{expr} {(node.IsOneFamily ? "!=" : "==")} {Literal(node.Ch)}";
                }

                if (clauseOnly)
                {
                    writer.Write(expr);
                }
                else
                {
                    string clause =
                        !emitLengthCheck ? $"if ({expr})" :
                        !rtl ? $"if ({SpanLengthCheck(1, offset)} || {expr})" :
                        $"if ((uint)(pos - 1) >= inputSpan.Length || {expr})";

                    using (EmitBlock(writer, clause))
                    {
                        Goto(doneLabel);
                    }
                }

                if (!rtl)
                {
                    sliceStaticPos++;
                }
                else
                {
                    writer.WriteLine("pos--;");
                }
            }

            // Emits the code to handle a boundary check on a character.
            void EmitBoundary(RegexNode node)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Boundary or RegexNodeKind.NonBoundary or RegexNodeKind.ECMABoundary or RegexNodeKind.NonECMABoundary, $"Unexpected kind: {node.Kind}");

                string call;
                if (node.Kind is RegexNodeKind.Boundary or RegexNodeKind.NonBoundary)
                {
                    call = node.Kind is RegexNodeKind.Boundary ?
                        $"!{HelpersTypeName}.IsBoundary" :
                        $"{HelpersTypeName}.IsBoundary";
                    AddIsBoundaryHelper(requiredHelpers, checkOverflow);
                }
                else
                {
                    call = node.Kind is RegexNodeKind.ECMABoundary ?
                        $"!{HelpersTypeName}.IsECMABoundary" :
                        $"{HelpersTypeName}.IsECMABoundary";
                    AddIsECMABoundaryHelper(requiredHelpers, checkOverflow);
                }

                using (EmitBlock(writer, $"if ({call}(inputSpan, pos{(sliceStaticPos > 0 ? $" + {sliceStaticPos}" : "")}))"))
                {
                    Goto(doneLabel);
                }
            }

            // Emits the code to handle various anchors.
            void EmitAnchors(RegexNode node)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Beginning or RegexNodeKind.Start or RegexNodeKind.Bol or RegexNodeKind.End or RegexNodeKind.EndZ or RegexNodeKind.Eol, $"Unexpected type: {node.Kind}");
                Debug.Assert((node.Options & RegexOptions.RightToLeft) == 0 || sliceStaticPos == 0);
                Debug.Assert(sliceStaticPos >= 0);

                switch (node.Kind)
                {
                    case RegexNodeKind.Beginning:
                    case RegexNodeKind.Start:
                        if (sliceStaticPos > 0)
                        {
                            // If we statically know we've already matched part of the regex, there's no way we're at the
                            // beginning or start, as we've already progressed past it.
                            Goto(doneLabel);
                        }
                        else
                        {
                            using (EmitBlock(writer, node.Kind == RegexNodeKind.Beginning ?
                                "if (pos != 0)" :
                                "if (pos != base.runtextstart)"))
                            {
                                Goto(doneLabel);
                            }
                        }
                        break;

                    case RegexNodeKind.Bol:
                        using (EmitBlock(writer, sliceStaticPos > 0 ?
                            $"if ({sliceSpan}[{sliceStaticPos - 1}] != '\\n')" :
                            $"if (pos > 0 && inputSpan[pos - 1] != '\\n')"))
                        {
                            Goto(doneLabel);
                        }
                        break;

                    case RegexNodeKind.End:
                        using (EmitBlock(writer, sliceStaticPos > 0 ?
                            $"if ({sliceStaticPos} < {sliceSpan}.Length)" :
                            "if ((uint)pos < (uint)inputSpan.Length)"))
                        {
                            Goto(doneLabel);
                        }
                        break;

                    case RegexNodeKind.EndZ:
                        using (EmitBlock(writer, sliceStaticPos > 0 ?
                            $"if ({sliceStaticPos + 1} < {sliceSpan}.Length || ({sliceStaticPos} < {sliceSpan}.Length && {sliceSpan}[{sliceStaticPos}] != '\\n'))" :
                            "if (pos < inputSpan.Length - 1 || ((uint)pos < (uint)inputSpan.Length && inputSpan[pos] != '\\n'))"))
                        {
                            Goto(doneLabel);
                        }
                        break;

                    case RegexNodeKind.Eol:
                        using (EmitBlock(writer, sliceStaticPos > 0 ?
                            $"if ({sliceStaticPos} < {sliceSpan}.Length && {sliceSpan}[{sliceStaticPos}] != '\\n')" :
                            "if ((uint)pos < (uint)inputSpan.Length && inputSpan[pos] != '\\n')"))
                        {
                            Goto(doneLabel);
                        }
                        break;
                }
            }

            // Emits the code to handle a multiple-character match.
            void EmitMultiChar(RegexNode node, bool emitLengthCheck)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Multi, $"Unexpected type: {node.Kind}");
                Debug.Assert(node.Str is not null);
                EmitMultiCharString(node.Str, emitLengthCheck, clauseOnly: false, (node.Options & RegexOptions.RightToLeft) != 0);
            }

            void EmitMultiCharString(string str, bool emitLengthCheck, bool clauseOnly, bool rightToLeft)
            {
                Debug.Assert(str.Length >= 2);
                Debug.Assert(!clauseOnly || (!emitLengthCheck && !rightToLeft));

                if (rightToLeft)
                {
                    Debug.Assert(emitLengthCheck);
                    using (EmitBlock(writer, $"if ((uint)(pos - {str.Length}) >= inputSpan.Length)"))
                    {
                        Goto(doneLabel);
                    }
                    writer.WriteLine();

                    using (EmitBlock(writer, $"for (int i = 0; i < {str.Length}; i++)"))
                    {
                        using (EmitBlock(writer, $"if (inputSpan[--pos] != {Literal(str)}[{str.Length - 1} - i])"))
                        {
                            Goto(doneLabel);
                        }
                    }

                    return;
                }

                string sourceSpan = sliceStaticPos > 0 ? $"{sliceSpan}.Slice({sliceStaticPos})" : sliceSpan;
                string clause = $"!{sourceSpan}.StartsWith({Literal(str)})";
                if (clauseOnly)
                {
                    writer.Write(clause);
                }
                else
                {
                    using (EmitBlock(writer, $"if ({clause})"))
                    {
                        Goto(doneLabel);
                    }
                }

                sliceStaticPos += str.Length;
            }

            void EmitSingleCharLoop(RegexNode node, RegexNode? subsequent = null, bool emitLengthChecksIfRequired = true)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Oneloop or RegexNodeKind.Notoneloop or RegexNodeKind.Setloop, $"Unexpected type: {node.Kind}");

                // If this is actually atomic based on its parent, emit it as atomic instead; no backtracking necessary.
                if (rm.Analysis.IsAtomicByAncestor(node))
                {
                    EmitSingleCharAtomicLoop(node);
                    return;
                }

                // If this is actually a repeater, emit that instead; no backtracking necessary.
                if (node.M == node.N)
                {
                    EmitSingleCharRepeater(node, emitLengthChecksIfRequired);
                    return;
                }

                // Emit backtracking around an atomic single char loop.  We can then implement the backtracking
                // as an afterthought, since we know exactly how many characters are accepted by each iteration
                // of the wrapped loop (1) and that there's nothing captured by the loop.

                Debug.Assert(node.M < node.N);
                string backtrackingLabel = ReserveName("CharLoopBacktrack");
                string endLoop = ReserveName("CharLoopEnd");
                string startingPos = ReserveName("charloop_starting_pos");
                string endingPos = ReserveName("charloop_ending_pos");
                additionalDeclarations.Add($"int {startingPos} = 0, {endingPos} = 0;");
                bool rtl = (node.Options & RegexOptions.RightToLeft) != 0;
                bool isInLoop = rm.Analysis.IsInLoop(node);

                // We're about to enter a loop, so ensure our text position is 0.
                TransferSliceStaticPosToPos();

                // Grab the current position, then emit the loop as atomic, and then
                // grab the current position again.  Even though we emit the loop without
                // knowledge of backtracking, we can layer it on top by just walking back
                // through the individual characters (a benefit of the loop matching exactly
                // one character per iteration, no possible captures within the loop, etc.)
                writer.WriteLine($"{startingPos} = pos;");
                writer.WriteLine();

                EmitSingleCharAtomicLoop(node);
                writer.WriteLine();

                TransferSliceStaticPosToPos();
                writer.WriteLine($"{endingPos} = pos;");
                EmitAdd(writer, startingPos, !rtl ? node.M : -node.M);
                Goto(endLoop);
                writer.WriteLine();

                // Backtracking section. Subsequent failures will jump to here, at which
                // point we decrement the matched count as long as it's above the minimum
                // required, and try again by flowing to everything that comes after this.
                MarkLabel(backtrackingLabel, emitSemicolon: false);
                string? capturePos = null;
                if (isInLoop)
                {
                    // This loop is inside of another loop, which means we persist state
                    // on the backtracking stack rather than relying on locals to always
                    // hold the right state (if we didn't do that, another iteration of the
                    // outer loop could have resulted in the locals being overwritten).
                    // Pop the relevant state from the stack.
                    if (expressionHasCaptures)
                    {
                        EmitUncaptureUntil(StackPop());
                    }
                    EmitStackPop(endingPos, startingPos);
                }
                else if (expressionHasCaptures)
                {
                    // Since we're not in a loop, we're using a local to track the crawl position.
                    // Unwind back to the position we were at prior to running the code after this loop.
                    capturePos = ReserveName("charloop_capture_pos");
                    additionalDeclarations.Add($"int {capturePos} = 0;");
                    EmitUncaptureUntil(capturePos);
                }
                writer.WriteLine();

                // We're backtracking.  Check the timeout.
                EmitTimeoutCheckIfNeeded(writer, rm);

                if (!rtl &&
                    node.N > 1 && // no point in using IndexOf for small loops, in particular optionals
                    subsequent?.FindStartingLiteralNode() is RegexNode literalNode &&
                    TryEmitIndexOf(requiredHelpers, literalNode, useLast: true, negate: false, out int literalLength, out string? indexOfExpr))
                {
                    writer.WriteLine($"if ({startingPos} >= {endingPos} ||");

                    string setEndingPosCondition = $"    ({endingPos} = inputSpan.Slice({startingPos}, ";
                    setEndingPosCondition = literalLength > 1 ?
                        $"{setEndingPosCondition}Math.Min(inputSpan.Length, {endingPos} + {literalLength - 1}) - {startingPos})" :
                        $"{setEndingPosCondition}{endingPos} - {startingPos})";

                    using (EmitBlock(writer, $"{setEndingPosCondition}.{indexOfExpr}) < 0)"))
                    {
                        Goto(doneLabel);
                    }
                    writer.WriteLine($"{endingPos} += {startingPos};");
                    writer.WriteLine($"pos = {endingPos};");
                }
                else
                {
                    using (EmitBlock(writer, $"if ({startingPos} {(!rtl ? ">=" : "<=")} {endingPos})"))
                    {
                        Goto(doneLabel);
                    }
                    writer.WriteLine(!rtl ? $"pos = --{endingPos};" : $"pos = ++{endingPos};");
                }

                if (!rtl)
                {
                    SliceInputSpan();
                }
                writer.WriteLine();

                MarkLabel(endLoop, emitSemicolon: false);
                if (isInLoop)
                {
                    // We're in a loop and thus can't rely on locals correctly holding the state we
                    // need (the locals could be overwritten by a subsequent iteration).  Push the state
                    // on to the backtracking stack.
                    EmitStackPush(expressionHasCaptures ?
                        new[] { startingPos, endingPos, "base.Crawlpos()" } :
                        new[] { startingPos, endingPos });
                }
                else if (capturePos is not null)
                {
                    // We're not in a loop and so can trust our locals.  Store the current capture position
                    // into the capture position local; we'll uncapture back to this when backtracking to
                    // remove any captures from after this loop that we need to throw away.
                    writer.WriteLine($"{capturePos} = base.Crawlpos();");
                }

                doneLabel = backtrackingLabel; // leave set to the backtracking label for all subsequent nodes
            }

            void EmitSingleCharLazy(RegexNode node, RegexNode? subsequent = null, bool emitLengthChecksIfRequired = true)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Onelazy or RegexNodeKind.Notonelazy or RegexNodeKind.Setlazy, $"Unexpected type: {node.Kind}");

                // Emit the min iterations as a repeater.  Any failures here don't necessitate backtracking,
                // as the lazy itself failed to match, and there's no backtracking possible by the individual
                // characters/iterations themselves.
                if (node.M > 0)
                {
                    EmitSingleCharRepeater(node, emitLengthChecksIfRequired);
                }

                // If the whole thing was actually that repeater, we're done. Similarly, if this is actually an atomic
                // lazy loop, nothing will ever backtrack into this node, so we never need to iterate more than the minimum.
                if (node.M == node.N || rm.Analysis.IsAtomicByAncestor(node))
                {
                    return;
                }

                if (node.M > 0)
                {
                    // We emitted a repeater to handle the required iterations; add a newline after it.
                    writer.WriteLine();
                }

                Debug.Assert(node.M < node.N);

                // We now need to match one character at a time, each time allowing the remainder of the expression
                // to try to match, and only matching another character if the subsequent expression fails to match.

                // We're about to enter a loop, so ensure our text position is 0.
                TransferSliceStaticPosToPos();

                // If the loop isn't unbounded, track the number of iterations and the max number to allow.
                string? iterationCount = null;
                string? maxIterations = null;
                if (node.N != int.MaxValue)
                {
                    maxIterations = $"{node.N - node.M}";

                    iterationCount = ReserveName("lazyloop_iteration");
                    writer.WriteLine($"int {iterationCount} = 0;");
                }

                // Track the current crawl position.  Upon backtracking, we'll unwind any captures beyond this point.
                string? capturePos = null;
                if (expressionHasCaptures)
                {
                    capturePos = ReserveName("lazyloop_capturepos");
                    additionalDeclarations.Add($"int {capturePos} = 0;");
                }

                // Track the current pos.  Each time we backtrack, we'll reset to the stored position, which
                // is also incremented each time we match another character in the loop.
                string startingPos = ReserveName("lazyloop_pos");
                additionalDeclarations.Add($"int {startingPos} = 0;");
                writer.WriteLine($"{startingPos} = pos;");

                // Skip the backtracking section for the initial subsequent matching.  We've already matched the
                // minimum number of iterations, which means we can successfully match with zero additional iterations.
                string endLoopLabel = ReserveName("LazyLoopEnd");
                Goto(endLoopLabel);
                writer.WriteLine();

                // Backtracking section. Subsequent failures will jump to here.
                string backtrackingLabel = ReserveName("LazyLoopBacktrack");
                MarkLabel(backtrackingLabel, emitSemicolon: false);

                // Uncapture any captures if the expression has any.  It's possible the captures it has
                // are before this node, in which case this is wasted effort, but still functionally correct.
                if (capturePos is not null)
                {
                    EmitUncaptureUntil(capturePos);
                }

                // If there's a max number of iterations, see if we've exceeded the maximum number of characters
                // to match.  If we haven't, increment the iteration count.
                if (maxIterations is not null)
                {
                    using (EmitBlock(writer, $"if ({iterationCount} >= {maxIterations})"))
                    {
                        Goto(doneLabel);
                    }
                    writer.WriteLine($"{iterationCount}++;");
                }

                // We're backtracking.  Check the timeout.
                EmitTimeoutCheckIfNeeded(writer, rm);

                // Now match the next item in the lazy loop.  We need to reset the pos to the position
                // just after the last character in this loop was matched, and we need to store the resulting position
                // for the next time we backtrack.
                writer.WriteLine($"pos = {startingPos};");
                SliceInputSpan();
                EmitSingleChar(node);
                TransferSliceStaticPosToPos();

                // Now that we've appropriately advanced by one character and are set for what comes after the loop,
                // see if we can skip ahead more iterations by doing a search for a following literal.
                if ((node.Options & RegexOptions.RightToLeft) == 0)
                {
                    if (iterationCount is null &&
                        node.Kind is RegexNodeKind.Notonelazy &&
                        subsequent?.FindStartingLiteral(4) is RegexNode.StartingLiteralData literal && // 5 == max optimized by IndexOfAny, and we need to reserve 1 for node.Ch
                        !literal.Negated && // not negated; can't search for both the node.Ch and a negated subsequent char with an IndexOf* method
                        (literal.String is not null ||
                         literal.SetChars is not null ||
                         (literal.AsciiChars is not null && node.Ch < 128) || // for ASCII sets, only allow when the target can be efficiently included in the set
                         literal.Range.LowInclusive == literal.Range.HighInclusive ||
                         (literal.Range.LowInclusive <= node.Ch && node.Ch <= literal.Range.HighInclusive))) // for ranges, only allow when the range overlaps with the target, since there's no accelerated way to search for the union
                    {
                        // e.g. "<[^>]*?>"

                        // Whether the not'd character matches the subsequent literal. This impacts whether we need to search
                        // for both or just the literal, as well as what assumptions we can make once a match is found.
                        bool overlap;

                        // This lazy loop will consume all characters other than node.Ch until the subsequent literal.
                        // We can implement it to search for either that char or the literal, whichever comes first.
                        if (literal.String is not null) // string literal
                        {
                            overlap = literal.String[0] == node.Ch;
                            writer.WriteLine(overlap ?
                                $"{startingPos} = {sliceSpan}.IndexOf({Literal(node.Ch)});" :
                                $"{startingPos} = {sliceSpan}.IndexOfAny({Literal(node.Ch)}, {Literal(literal.String[0])});");
                        }
                        else if (literal.SetChars is not null) // set literal
                        {
                            overlap = literal.SetChars.Contains(node.Ch);
                            writer.WriteLine((overlap, literal.SetChars.Length) switch
                            {
                                (true, 2) => $"{startingPos} = {sliceSpan}.IndexOfAny({Literal(literal.SetChars[0])}, {Literal(literal.SetChars[1])});",
                                (true, 3) => $"{startingPos} = {sliceSpan}.IndexOfAny({Literal(literal.SetChars[0])}, {Literal(literal.SetChars[1])}, {Literal(literal.SetChars[2])});",
                                (true, _) => $"{startingPos} = {sliceSpan}.IndexOfAny({EmitIndexOfAnyValuesOrLiteral(literal.SetChars.AsSpan(), requiredHelpers)});",

                                (false, 2) => $"{startingPos} = {sliceSpan}.IndexOfAny({Literal(node.Ch)}, {Literal(literal.SetChars[0])}, {Literal(literal.SetChars[1])});",
                                (false, _) => $"{startingPos} = {sliceSpan}.IndexOfAny({EmitIndexOfAnyValuesOrLiteral($"{node.Ch}{literal.SetChars}".AsSpan(), requiredHelpers)});",
                            });
                        }
                        else if (literal.AsciiChars is not null) // set of only ASCII characters
                        {
                            char[] asciiChars = literal.AsciiChars;
                            overlap = asciiChars.Contains(node.Ch);
                            if (!overlap)
                            {
                                Debug.Assert(node.Ch < 128);
                                Array.Resize(ref asciiChars, asciiChars.Length + 1);
                                asciiChars[asciiChars.Length - 1] = node.Ch;
                            }
                            writer.WriteLine($"{startingPos} = {sliceSpan}.IndexOfAny({EmitIndexOfAnyValues(asciiChars, requiredHelpers)});");
                        }
                        else if (literal.Range.LowInclusive == literal.Range.HighInclusive) // single char from a RegexNode.One
                        {
                            overlap = literal.Range.LowInclusive == node.Ch;
                            writer.WriteLine(overlap ?
                                $"{startingPos} = {sliceSpan}.IndexOf({Literal(node.Ch)});" :
                                $"{startingPos} = {sliceSpan}.IndexOfAny({Literal(node.Ch)}, {Literal(literal.Range.LowInclusive)});");
                        }
                        else // char range
                        {
                            overlap = true;
                            writer.WriteLine($"{startingPos} = {sliceSpan}.IndexOfAnyInRange({Literal(literal.Range.LowInclusive)}, {Literal(literal.Range.HighInclusive)});");
                        }

                        // If the search didn't find anything, fail the match.  If it did find something, then we need to consider whether
                        // that something is the loop character.  If it's not, we've successfully backtracked to the next lazy location
                        // where we should evaluate the rest of the pattern.  If it does match, then we need to consider whether there's
                        // overlap between the loop character and the literal.  If there is overlap, this is also a place to check.  But
                        // if there's not overlap, and if the found character is the loop character, we also want to fail the match here
                        // and now, as this means the loop ends before it gets to what needs to come after the loop, and thus the pattern
                        // can't possibly match here.
                        using (EmitBlock(writer, overlap ?
                            $"if ({startingPos} < 0)" :
                            $"if ((uint){startingPos} >= (uint){sliceSpan}.Length || {sliceSpan}[{startingPos}] == {Literal(node.Ch)})"))
                        {
                            Goto(doneLabel);
                        }

                        writer.WriteLine($"pos += {startingPos};");
                        SliceInputSpan();
                    }
                    else if (iterationCount is null &&
                        node.Kind is RegexNodeKind.Setlazy &&
                        node.Str == RegexCharClass.AnyClass &&
                        subsequent?.FindStartingLiteralNode() is RegexNode literal2 &&
                        TryEmitIndexOf(requiredHelpers, literal2, useLast: false, negate: false, out _, out string? indexOfExpr))
                    {
                        // e.g. ".*?string" with RegexOptions.Singleline
                        // This lazy loop will consume all characters until the subsequent literal. If the subsequent literal
                        // isn't found, the loop fails. We can implement it to just search for that literal.
                        writer.WriteLine($"{startingPos} = {sliceSpan}.{indexOfExpr};");
                        using (EmitBlock(writer, $"if ({startingPos} < 0)"))
                        {
                            Goto(doneLabel);
                        }
                        writer.WriteLine($"pos += {startingPos};");
                        SliceInputSpan();
                    }
                }

                // Store the position we've left off at in case we need to iterate again.
                writer.WriteLine($"{startingPos} = pos;");

                // Update the done label for everything that comes after this node.  This is done after we emit the single char
                // matching, as that failing indicates the loop itself has failed to match.
                string originalDoneLabel = doneLabel;
                doneLabel = backtrackingLabel; // leave set to the backtracking label for all subsequent nodes

                writer.WriteLine();
                bool isInLoop = rm.Analysis.IsInLoop(node);

                MarkLabel(endLoopLabel, emitSemicolon: !(capturePos is not null || isInLoop));
                if (capturePos is not null)
                {
                    writer.WriteLine($"{capturePos} = base.Crawlpos();");
                }

                // If this loop is itself not in another loop, nothing more needs to be done:
                // upon backtracking, locals being used by this loop will have retained their
                // values and be up-to-date.  But if this loop is inside another loop, multiple
                // iterations of this loop each need their own state, so we need to use the stack
                // to hold it, and we need a dedicated backtracking section to handle restoring
                // that state before jumping back into the loop itself.
                if (isInLoop)
                {
                    writer.WriteLine();

                    // Store the loop's state.
                    EmitStackPush(
                        capturePos is not null && iterationCount is not null ? new[] { startingPos, capturePos, iterationCount } :
                        capturePos is not null ? new[] { startingPos, capturePos } :
                        iterationCount is not null ? new[] { startingPos, iterationCount } :
                        new[] { startingPos });

                    // Skip past the backtracking section.
                    string end = ReserveName("LazyLoopSkipBacktrack");
                    Goto(end);
                    writer.WriteLine();

                    // Emit a backtracking section that restores the loop's state and then jumps to the previous done label.
                    string backtrack = ReserveName("CharLazyBacktrack");
                    MarkLabel(backtrack, emitSemicolon: false);

                    // Restore the loop's state.
                    EmitStackPop(
                        capturePos is not null && iterationCount is not null ? new[] { iterationCount, capturePos, startingPos } :
                        capturePos is not null ? new[] { capturePos, startingPos } :
                        iterationCount is not null ? new[] { iterationCount, startingPos } :
                        new[] { startingPos });

                    Goto(doneLabel);
                    writer.WriteLine();

                    doneLabel = backtrack;
                    MarkLabel(end);
                }
            }

            void EmitLazy(RegexNode node)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Lazyloop, $"Unexpected type: {node.Kind}");
                Debug.Assert(node.M < int.MaxValue, $"Unexpected M={node.M}");
                Debug.Assert(node.N >= node.M, $"Unexpected M={node.M}, N={node.N}");
                Debug.Assert(node.ChildCount() == 1, $"Expected 1 child, found {node.ChildCount()}");

                RegexNode child = node.Child(0);
                int minIterations = node.M;
                int maxIterations = node.N;
                string originalDoneLabel = doneLabel;

                // If this is actually a repeater, reuse the loop implementation, as a loop and a lazy loop
                // both need to greedily consume up to their min iteration count and are identical in
                // behavior when min == max.
                if (minIterations == maxIterations)
                {
                    EmitLoop(node);
                    return;
                }

                // We should only be here if the lazy loop isn't atomic due to an ancestor, as the optimizer should
                // in such a case have lowered the loop's upper bound to its lower bound, at which point it would
                // have been handled by the above delegation to EmitLoop.  However, if the optimizer missed doing so,
                // this loop could still be considered atomic by ancestor by its parent nodes, in which case we want
                // to make sure the code emitted here conforms (e.g. doesn't leave any state erroneously on the stack).
                // So, we assert it's not atomic, but still handle that case.
                bool isAtomic = rm.Analysis.IsAtomicByAncestor(node);
                Debug.Assert(!isAtomic, "An atomic lazy should have had its upper bound lowered to its lower bound.");

                // We might loop any number of times.  In order to ensure this loop and subsequent code sees sliceStaticPos
                // the same regardless, we always need it to contain the same value, and the easiest such value is 0.
                // So, we transfer sliceStaticPos to pos, and ensure that any path out of here has sliceStaticPos as 0.
                TransferSliceStaticPosToPos();

                string body = ReserveName("LazyLoopBody");
                string endLoop = ReserveName("LazyLoopEnd");

                string iterationCount = ReserveName("lazyloop_iteration");
                additionalDeclarations.Add($"int {iterationCount} = 0;");
                writer.WriteLine($"{iterationCount} = 0;");

                // Loops that match empty iterations need additional checks in place to prevent infinitely matching (since
                // you could end up looping an infinite number of times at the same location).  We can avoid those
                // additional checks if we can prove that the loop can never match empty, which we can do by computing
                // the minimum length of the child; only if it's 0 might iterations be empty.
                bool iterationMayBeEmpty = child.ComputeMinLength() == 0;
                string? startingPos = null, sawEmpty = null;
                if (iterationMayBeEmpty)
                {
                    startingPos = ReserveName("lazyloop_starting_pos");
                    sawEmpty = ReserveName("lazyloop_empty_seen");
                    writer.WriteLine($"int {startingPos} = pos, {sawEmpty} = 0; // the lazy loop may match empty iterations");
                }

                // If the min count is 0, start out by jumping right to what's after the loop.  Backtracking
                // will then bring us back in to do further iterations.
                if (minIterations == 0)
                {
                    Goto(endLoop);
                }
                writer.WriteLine();

                // Iteration body
                MarkLabel(body, emitSemicolon: isAtomic);

                // In case iterations are backtracked through and unwound, we need to store the current position (so that
                // matching can resume from that location), the current crawl position if captures are possible (so that
                // we can uncapture back to that position), and both the starting position from the iteration we're leaving
                // and whether we've seen an empty iteration (if iterations may be empty).  Since there can be multiple
                // iterations, this state needs to be stored on to the backtracking stack.
                if (!isAtomic)
                {
                    int entriesPerIteration = 1/*pos*/ + (iterationMayBeEmpty ? 2/*startingPos+sawEmpty*/ : 0) + (expressionHasCaptures ? 1/*Crawlpos*/ : 0);
                    EmitStackPush(
                        expressionHasCaptures && iterationMayBeEmpty ? new[] { "pos", startingPos!, sawEmpty!, "base.Crawlpos()" } :
                        iterationMayBeEmpty ? new[] { "pos", startingPos!, sawEmpty! } :
                        expressionHasCaptures ? new[] { "pos", "base.Crawlpos()" } :
                        new[] { "pos" });

                    if (iterationMayBeEmpty)
                    {
                        // We need to store the current pos so we can compare it against pos after the iteration, in order to
                        // determine whether the iteration was empty.
                        writer.WriteLine($"{startingPos} = pos;");
                    }

                    // Proactively increase the number of iterations.  We do this prior to the match rather than once
                    // we know it's successful, because we need to decrement it as part of a failed match when
                    // backtracking; it's thus simpler to just always decrement it as part of a failed match, even
                    // when initially greedily matching the loop, which then requires we increment it before trying.
                    writer.WriteLine($"{iterationCount}++;");

                    // Last but not least, we need to set the doneLabel that a failed match of the body will jump to.
                    // Such an iteration match failure may or may not fail the whole operation, depending on whether
                    // we've already matched the minimum required iterations, so we need to jump to a location that
                    // will make that determination.
                    string iterationFailedLabel = ReserveName("LazyLoopIterationNoMatch");
                    doneLabel = iterationFailedLabel;

                    // Finally, emit the child.
                    Debug.Assert(sliceStaticPos == 0);
                    writer.WriteLine();
                    EmitNode(child);
                    writer.WriteLine();
                    TransferSliceStaticPosToPos(); // ensure sliceStaticPos remains 0
                    if (doneLabel == iterationFailedLabel)
                    {
                        doneLabel = originalDoneLabel;
                    }

                    // Loop condition.  Continue iterating if we've not yet reached the minimum.  We just successfully
                    // matched an iteration, so the only reason we'd need to forcefully loop around again is if the
                    // minimum were at least 2.
                    if (minIterations >= 2)
                    {
                        writer.WriteLine($"// The lazy loop requires a minimum of {minIterations} iterations. If that many haven't yet matched, loop now.");
                        using (EmitBlock(writer, $"if ({CountIsLessThan(iterationCount, minIterations)})"))
                        {
                            Goto(body);
                        }
                    }

                    if (iterationMayBeEmpty)
                    {
                        // If the last iteration was empty, we need to prevent further iteration from this point
                        // unless we backtrack out of this iteration.
                        writer.WriteLine("// If the iteration successfully matched zero-length input, record that an empty iteration was seen.");
                        using (EmitBlock(writer, $"if (pos == {startingPos})"))
                        {
                            writer.WriteLine($"{sawEmpty} = 1; // true");
                        }
                        writer.WriteLine();
                    }

                    // We matched the next iteration.  Jump to the subsequent code.
                    Goto(endLoop);
                    writer.WriteLine();

                    // Now handle what happens when an iteration fails (and since a lazy loop only executes an iteration
                    // when it's required to satisfy the loop by definition of being lazy, the loop is failing).  We need
                    // to reset state to what it was before just that iteration started.  That includes resetting pos and
                    // clearing out any captures from that iteration.
                    writer.WriteLine("// The lazy loop iteration failed to match.");
                    MarkLabel(iterationFailedLabel, emitSemicolon: false);
                    if (doneLabel != originalDoneLabel || !GotoWillExitMatch(originalDoneLabel)) // we don't need to back anything out if we're about to exit TryMatchAtCurrentPosition anyway.
                    {
                        // Fail this loop iteration, including popping state off the backtracking stack that was pushed
                        // on as part of the failing iteration.
                        writer.WriteLine($"{iterationCount}--;");
                        if (expressionHasCaptures)
                        {
                            EmitUncaptureUntil(StackPop());
                        }
                        EmitStackPop(iterationMayBeEmpty ?
                            new[] { sawEmpty!, startingPos!, "pos" } :
                            new[] { "pos" });
                        SliceInputSpan();

                        // If the loop's child doesn't backtrack, then this loop has failed.
                        // If the loop's child does backtrack, we need to backtrack back into the previous iteration if there was one.
                        if (doneLabel == originalDoneLabel)
                        {
                            // Since the only reason we'd end up revisiting previous iterations of the lazy loop is if the child had backtracking constructs
                            // we'd backtrack into, and the child doesn't, the whole loop is failed and done. If we successfully processed any iterations,
                            // we thus need to pop all of the state we pushed onto the stack for those iterations, as we're exiting out to the parent who
                            // will expect the stack to be cleared of any child state.
                            Debug.Assert(entriesPerIteration >= 1);
                            writer.WriteLine(entriesPerIteration > 1 ?
                                $"stackpos -= {iterationCount} * {entriesPerIteration};" :
                                $"stackpos -= {iterationCount};");
                        }
                        else
                        {
                            // The child has backtracking constructs.  If we have no successful iterations previously processed, just bail.
                            // If we do have successful iterations previously processed, however, we need to backtrack back into the last one.
                            using (EmitBlock(writer, $"if ({iterationCount} > 0)"))
                            {
                                writer.WriteLine($"// The lazy loop matched at least one iteration; backtrack into the last one.");
                                if (iterationMayBeEmpty)
                                {
                                    // If we saw empty, it must have been in the most recent iteration, as we wouldn't have
                                    // allowed additional iterations after one that was empty.  Thus, we reset it back to
                                    // false prior to backtracking / undoing that iteration.
                                    writer.WriteLine($"{sawEmpty} = 0; // false");
                                }
                                Goto(doneLabel);
                            }
                            writer.WriteLine();
                        }
                    }
                    Goto(originalDoneLabel);
                    writer.WriteLine();

                    MarkLabel(endLoop, emitSemicolon: false);

                    // If the lazy loop is not atomic, then subsequent code may backtrack back into this lazy loop, either
                    // causing it to add additional iterations, or backtracking into existing iterations and potentially
                    // unwinding them.  We need to do a timeout check, and then determine whether to branch back to add more
                    // iterations (if we haven't hit the loop's maximum iteration count and haven't seen an empty iteration)
                    // or unwind by branching back to the last backtracking location.  Either way, we need a dedicated
                    // backtracking section that a subsequent construct will see as its backtracking target.

                    // We need to ensure that some state (e.g. iteration count) is persisted if we're backtracked to.
                    // We also need to push the current position, so that subsequent iterations pick up at the right
                    // point (and subsequent expressions are almost certain to have changed the current pos). However,
                    // if we're not inside of a loop, the other local's used for this construct are sufficient, as nothing
                    // else will overwrite them between now and when backtracking occurs.  If, however, we are inside
                    // of another loop, then any number of iterations might have such state that needs to be stored,
                    // and thus it needs to be pushed on to the backtracking stack.
                    bool isInLoop = rm.Analysis.IsInLoop(node);
                    EmitStackPush(
                        !isInLoop ? (expressionHasCaptures ? new[] { "pos", "base.Crawlpos()" } : new[] { "pos" }) :
                        iterationMayBeEmpty ? (expressionHasCaptures ? new[] { "pos", iterationCount, startingPos!, sawEmpty!, "base.Crawlpos()" } : new[] { "pos", iterationCount, startingPos!, sawEmpty! }) :
                        expressionHasCaptures ? new[] { "pos", iterationCount, "base.Crawlpos()"} :
                        new[] { "pos", iterationCount });

                    string skipBacktrack = ReserveName("LazyLoopSkipBacktrack");
                    Goto(skipBacktrack);
                    writer.WriteLine();

                    // Emit a backtracking section that checks the timeout, restores the loop's state, and jumps to
                    // the appropriate label.
                    string backtrack = ReserveName($"LazyLoopBacktrack");
                    MarkLabel(backtrack, emitSemicolon: false);

                    // We're backtracking.  Check the timeout.
                    EmitTimeoutCheckIfNeeded(writer, rm);

                    if (expressionHasCaptures)
                    {
                        EmitUncaptureUntil(StackPop());
                    }
                    EmitStackPop(
                        !isInLoop ? new[] { "pos" } :
                        iterationMayBeEmpty ? new[] { sawEmpty!, startingPos!, iterationCount, "pos" } :
                        new[] { iterationCount, "pos" });
                    SliceInputSpan();

                    // Determine where to branch, either back to the lazy loop body to add an additional iteration,
                    // or to the last backtracking label.
                    if (maxIterations != int.MaxValue || iterationMayBeEmpty)
                    {
                        FinishEmitBlock clause;

                        writer.WriteLine();
                        if (maxIterations == int.MaxValue)
                        {
                            // If the last iteration matched empty, backtrack.
                            writer.WriteLine("// If the last iteration matched empty, don't continue lazily iterating. Instead, backtrack.");
                            clause = EmitBlock(writer, $"if ({sawEmpty} != 0)");
                        }
                        else if (iterationMayBeEmpty)
                        {
                            // If the last iteration matched empty or if we've reached our upper bound, backtrack.
                            writer.WriteLine($"// If the upper bound {maxIterations} has already been reached, or if the last");
                            writer.WriteLine($"// iteration matched empty, don't continue lazily iterating. Instead, backtrack.");
                            clause = EmitBlock(writer, $"if ({CountIsGreaterThanOrEqualTo(iterationCount, maxIterations)} || {sawEmpty} != 0)");
                        }
                        else
                        {
                            // If we've reached our upper bound, backtrack.
                            writer.WriteLine($"// If the upper bound {maxIterations} has already been reached,");
                            writer.WriteLine($"// don't continue lazily iterating. Instead, backtrack.");
                            clause = EmitBlock(writer, $"if ({CountIsGreaterThanOrEqualTo(iterationCount, maxIterations)})");
                        }

                        using (clause)
                        {
                            if (iterationMayBeEmpty)
                            {
                                // If we saw empty, it must have been in the most recent iteration, as we wouldn't have
                                // allowed additional iterations after one that was empty.  Thus, we reset it back to
                                // false prior to backtracking / undoing that iteration.
                                writer.WriteLine($"{sawEmpty} = 0; // false");
                            }
                            Goto(doneLabel);
                        }
                    }

                    // Otherwise, try to match another iteration.
                    Goto(body);
                    writer.WriteLine();

                    doneLabel = backtrack;
                    MarkLabel(skipBacktrack);
                }
            }

            // Emits the code to handle a loop (repeater) with a fixed number of iterations.
            // RegexNode.M is used for the number of iterations (RegexNode.N is ignored), as this
            // might be used to implement the required iterations of other kinds of loops.
            void EmitSingleCharRepeater(RegexNode node, bool emitLengthCheck = true)
            {
                Debug.Assert(node.IsOneFamily || node.IsNotoneFamily || node.IsSetFamily, $"Unexpected type: {node.Kind}");

                int iterations = node.M;
                bool rtl = (node.Options & RegexOptions.RightToLeft) != 0;

                switch (iterations)
                {
                    case 0:
                        // No iterations, nothing to do.
                        return;

                    case 1:
                        // Just match the individual item
                        EmitSingleChar(node, emitLengthCheck);
                        return;

                    case <= RegexNode.MultiVsRepeaterLimit when node.IsOneFamily:
                        // This is a repeated case-sensitive character; emit it as a multi in order to get all the optimizations
                        // afforded to a multi, e.g. unrolling the loop with multi-char reads/comparisons at a time.
                        EmitMultiCharString(new string(node.Ch, iterations), emitLengthCheck, clauseOnly: false, rtl);
                        return;
                }

                if (rtl)
                {
                    TransferSliceStaticPosToPos(); // we don't use static position with rtl
                    using (EmitBlock(writer, $"for (int i = 0; i < {iterations}; i++)"))
                    {
                        EmitSingleChar(node);
                    }
                }
                else if (node.IsSetFamily && node.Str == RegexCharClass.AnyClass)
                {
                    // This is a repeater for anything, which means we only care about length and can jump past that length.
                    if (emitLengthCheck)
                    {
                        EmitSpanLengthCheck(iterations);
                    }
                    sliceStaticPos += iterations;
                }
                else if (iterations <= MaxUnrollSize)
                {
                    // if ((uint)(sliceStaticPos + iterations - 1) >= (uint)slice.Length ||
                    //     slice[sliceStaticPos] != c1 ||
                    //     slice[sliceStaticPos + 1] != c2 ||
                    //     ...)
                    // {
                    //     goto doneLabel;
                    // }
                    writer.Write($"if (");
                    if (emitLengthCheck)
                    {
                        writer.WriteLine($"{SpanLengthCheck(iterations)} ||");
                        writer.Write("    ");
                    }
                    EmitSingleChar(node, emitLengthCheck: false, clauseOnly: true);
                    for (int i = 1; i < iterations; i++)
                    {
                        writer.WriteLine(" ||");
                        writer.Write("    ");
                        EmitSingleChar(node, emitLengthCheck: false, clauseOnly: true);
                    }
                    writer.WriteLine(")");
                    using (EmitBlock(writer, null))
                    {
                        Goto(doneLabel);
                    }
                }
                else
                {
                    // if ((uint)(sliceStaticPos + iterations - 1) >= (uint)slice.Length) goto doneLabel;
                    if (emitLengthCheck)
                    {
                        EmitSpanLengthCheck(iterations);
                        writer.WriteLine();
                    }

                    // If we're able to vectorize the search, do so. Otherwise, fall back to a loop.
                    // For the loop, we're validating that each char matches the target node.
                    // For IndexOf, we're looking for the first thing that _doesn't_ match the target node,
                    // and thus similarly validating that everything does.
                    if (TryEmitIndexOf(requiredHelpers, node, useLast: false, negate: true, out _, out string? indexOfExpr))
                    {
                        using (EmitBlock(writer, $"if ({sliceSpan}.Slice({sliceStaticPos}, {iterations}).{indexOfExpr} >= 0)"))
                        {
                            Goto(doneLabel);
                        }
                    }
                    else
                    {
                        string repeaterSpan = "repeaterSlice"; // As this repeater doesn't wrap arbitrary node emits, this shouldn't conflict with anything
                        writer.WriteLine($"ReadOnlySpan<char> {repeaterSpan} = {sliceSpan}.Slice({sliceStaticPos}, {iterations});");

                        using (EmitBlock(writer, $"for (int i = 0; i < {repeaterSpan}.Length; i++)"))
                        {
                            string tmpTextSpanLocal = sliceSpan; // we want EmitSingleChar to refer to this temporary
                            int tmpSliceStaticPos = sliceStaticPos;
                            sliceSpan = repeaterSpan;
                            sliceStaticPos = 0;
                            EmitSingleChar(node, emitLengthCheck: false, offset: "i");
                            sliceSpan = tmpTextSpanLocal;
                            sliceStaticPos = tmpSliceStaticPos;
                        }
                    }

                    sliceStaticPos += iterations;
                }
            }

            // Emits the code to handle a non-backtracking, variable-length loop around a single character comparison.
            void EmitSingleCharAtomicLoop(RegexNode node, bool emitLengthChecksIfRequired = true)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Oneloop or RegexNodeKind.Oneloopatomic or RegexNodeKind.Notoneloop or RegexNodeKind.Notoneloopatomic or RegexNodeKind.Setloop or RegexNodeKind.Setloopatomic, $"Unexpected type: {node.Kind}");

                // If this is actually a repeater, emit that instead.
                if (node.M == node.N)
                {
                    EmitSingleCharRepeater(node, emitLengthChecksIfRequired);
                    return;
                }

                // If this is actually an optional single char, emit that instead.
                if (node.M == 0 && node.N == 1)
                {
                    EmitAtomicSingleCharZeroOrOne(node);
                    return;
                }

                Debug.Assert(node.N > node.M);
                int minIterations = node.M;
                int maxIterations = node.N;
                bool rtl = (node.Options & RegexOptions.RightToLeft) != 0;
                string iterationLocal = ReserveName("iteration");

                if (rtl)
                {
                    TransferSliceStaticPosToPos(); // we don't use static position for rtl

                    if (node.IsSetFamily && maxIterations == int.MaxValue && node.Str == RegexCharClass.AnyClass)
                    {
                        // If this loop will consume the remainder of the input, just set the iteration variable
                        // to pos directly rather than looping to get there.
                        writer.WriteLine($"int {iterationLocal} = pos;");
                    }
                    else
                    {
                        writer.WriteLine($"int {iterationLocal} = 0;");

                        string expr = $"inputSpan[pos - {iterationLocal} - 1]";
                        if (node.IsSetFamily)
                        {
                            expr = MatchCharacterClass(expr, node.Str!, negate: false, additionalDeclarations, requiredHelpers);
                        }
                        else
                        {
                            expr = $"{expr} {(node.IsOneFamily ? "==" : "!=")} {Literal(node.Ch)}";
                        }

                        string maxClause = maxIterations != int.MaxValue ? $"{CountIsLessThan(iterationLocal, maxIterations)} && " : "";
                        using (EmitBlock(writer, $"while ({maxClause}pos > {iterationLocal} && {expr})"))
                        {
                            writer.WriteLine($"{iterationLocal}++;");
                        }
                        writer.WriteLine();
                    }
                }
                else if (node.IsSetFamily && maxIterations == int.MaxValue && node.Str == RegexCharClass.AnyClass)
                {
                    // .* was used with RegexOptions.Singleline, which means it'll consume everything.  Just jump to the end.
                    // The unbounded constraint is the same as in the Notone case above, done purely for simplicity.

                    TransferSliceStaticPosToPos();
                    writer.WriteLine($"int {iterationLocal} = inputSpan.Length - pos;");
                }
                else if (maxIterations == int.MaxValue && TryEmitIndexOf(requiredHelpers, node, useLast: false, negate: true, out _, out string? indexOfExpr))
                {
                    // We're unbounded and we can use an IndexOf method to perform the search. The unbounded restriction is
                    // purely for simplicity; it could be removed in the future with additional code to handle that case.

                    writer.Write($"int {iterationLocal} = {sliceSpan}");
                    if (sliceStaticPos != 0)
                    {
                        writer.Write($".Slice({sliceStaticPos})");
                    }
                    writer.WriteLine($".{indexOfExpr};");

                    using (EmitBlock(writer, $"if ({iterationLocal} < 0)"))
                    {
                        writer.WriteLine(sliceStaticPos > 0 ?
                            $"{iterationLocal} = {sliceSpan}.Length - {sliceStaticPos};" :
                            $"{iterationLocal} = {sliceSpan}.Length;");
                    }
                    writer.WriteLine();
                }
                else
                {
                    // For everything else, do a normal loop.

                    string expr = $"{sliceSpan}[{iterationLocal}]";
                    expr = node.IsSetFamily ?
                        MatchCharacterClass(expr, node.Str!, negate: false, additionalDeclarations, requiredHelpers) :
                        $"{expr} {(node.IsOneFamily ? "==" : "!=")} {Literal(node.Ch)}";

                    if (minIterations != 0 || maxIterations != int.MaxValue)
                    {
                        // For any loops other than * loops, transfer text pos to pos in
                        // order to zero it out to be able to use the single iteration variable
                        // for both iteration count and indexer.
                        TransferSliceStaticPosToPos();
                    }

                    writer.WriteLine($"int {iterationLocal} = {sliceStaticPos};");
                    sliceStaticPos = 0;

                    string maxClause = maxIterations != int.MaxValue ? $"{CountIsLessThan(iterationLocal, maxIterations)} && " : "";
                    using (EmitBlock(writer, $"while ({maxClause}(uint){iterationLocal} < (uint){sliceSpan}.Length && {expr})"))
                    {
                        writer.WriteLine($"{iterationLocal}++;");
                    }
                    writer.WriteLine();
                }

                // Check to ensure we've found at least min iterations.
                if (minIterations > 0)
                {
                    using (EmitBlock(writer, $"if ({CountIsLessThan(iterationLocal, minIterations)})"))
                    {
                        Goto(doneLabel);
                    }
                    writer.WriteLine();
                }

                // Now that we've completed our optional iterations, advance the text span
                // and pos by the number of iterations completed.

                if (!rtl)
                {
                    writer.WriteLine($"{sliceSpan} = {sliceSpan}.Slice({iterationLocal});");
                    writer.WriteLine($"pos += {iterationLocal};");
                }
                else
                {
                    writer.WriteLine($"pos -= {iterationLocal};");
                }
            }

            // Emits the code to handle a non-backtracking optional zero-or-one loop.
            void EmitAtomicSingleCharZeroOrOne(RegexNode node)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Oneloop or RegexNodeKind.Oneloopatomic or RegexNodeKind.Notoneloop or RegexNodeKind.Notoneloopatomic or RegexNodeKind.Setloop or RegexNodeKind.Setloopatomic, $"Unexpected type: {node.Kind}");
                Debug.Assert(node.M == 0 && node.N == 1);

                bool rtl = (node.Options & RegexOptions.RightToLeft) != 0;
                if (rtl)
                {
                    TransferSliceStaticPosToPos(); // we don't use static pos for rtl
                }

                string expr = !rtl ?
                    $"{sliceSpan}[{sliceStaticPos}]" :
                    "inputSpan[pos - 1]";

                if (node.IsSetFamily)
                {
                    expr = MatchCharacterClass(expr, node.Str!, negate: false, additionalDeclarations, requiredHelpers);
                }
                else
                {
                    expr = $"{expr} {(node.IsOneFamily ? "==" : "!=")} {Literal(node.Ch)}";
                }

                string spaceAvailable =
                    rtl ? "pos > 0" :
                    sliceStaticPos != 0 ? $"(uint){sliceSpan}.Length > (uint){sliceStaticPos}" :
                    $"!{sliceSpan}.IsEmpty";

                using (EmitBlock(writer, $"if ({spaceAvailable} && {expr})"))
                {
                    if (!rtl)
                    {
                        writer.WriteLine($"{sliceSpan} = {sliceSpan}.Slice(1);");
                        writer.WriteLine($"pos++;");
                    }
                    else
                    {
                        writer.WriteLine($"pos--;");
                    }
                }
            }

            void EmitNonBacktrackingRepeater(RegexNode node)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Loop or RegexNodeKind.Lazyloop, $"Unexpected type: {node.Kind}");
                Debug.Assert(node.M < int.MaxValue, $"Unexpected M={node.M}");
                Debug.Assert(node.M == node.N, $"Unexpected M={node.M} == N={node.N}");
                Debug.Assert(node.ChildCount() == 1, $"Expected 1 child, found {node.ChildCount()}");
                Debug.Assert(!rm.Analysis.MayBacktrack(node.Child(0)), $"Expected non-backtracking node {node.Kind}");

                // Ensure every iteration of the loop sees a consistent value.
                TransferSliceStaticPosToPos();

                // Loop M==N times to match the child exactly that numbers of times.
                string i = ReserveName("loop_iteration");
                using (EmitBlock(writer, $"for (int {i} = 0; {i} < {node.M}; {i}++)"))
                {
                    EmitNode(node.Child(0));
                    TransferSliceStaticPosToPos(); // make sure static the static position remains at 0 for subsequent constructs
                }
            }

            void EmitLoop(RegexNode node)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Loop or RegexNodeKind.Lazyloop, $"Unexpected type: {node.Kind}");
                Debug.Assert(node.M < int.MaxValue, $"Unexpected M={node.M}");
                Debug.Assert(node.N >= node.M, $"Unexpected M={node.M}, N={node.N}");
                Debug.Assert(node.ChildCount() == 1, $"Expected 1 child, found {node.ChildCount()}");
                RegexNode child = node.Child(0);

                int minIterations = node.M;
                int maxIterations = node.N;

                // Special-case some repeaters.
                if (minIterations == maxIterations)
                {
                    switch (minIterations)
                    {
                        case 0:
                            // No iteration. Nop.
                            return;

                        case 1:
                            // One iteration.  Just emit the child without any loop ceremony.
                            EmitNode(child);
                            return;

                        case > 1 when !rm.Analysis.MayBacktrack(child):
                            // The child doesn't backtrack.  Emit it as a non-backtracking repeater.
                            // (If the child backtracks, we need to fall through to the more general logic
                            // that supports unwinding iterations.)
                            EmitNonBacktrackingRepeater(node);
                            return;
                    }
                }

                // We might loop any number of times.  In order to ensure this loop and subsequent code sees sliceStaticPos
                // the same regardless, we always need it to contain the same value, and the easiest such value is 0.
                // So, we transfer sliceStaticPos to pos, and ensure that any path out of here has sliceStaticPos as 0.
                TransferSliceStaticPosToPos();

                bool isAtomic = rm.Analysis.IsAtomicByAncestor(node);
                string? startingStackpos = null;
                if (isAtomic || minIterations > 1)
                {
                    // If the loop is atomic, constructs will need to backtrack around it, and as such any backtracking
                    // state pushed by the loop should be removed prior to exiting the loop.  Similarly, if the loop has
                    // a minimum iteration count greater than 1, we might end up with at least one successful iteration
                    // only to find we can't iterate further, and will need to clear any pushed state from the backtracking
                    // stack.  For both cases, we need to store the starting stack index so it can be reset to that position.
                    startingStackpos = ReserveName("startingStackpos");
                    additionalDeclarations.Add($"int {startingStackpos} = 0;");
                    writer.WriteLine($"{startingStackpos} = stackpos;");
                }

                string originalDoneLabel = doneLabel;
                string body = ReserveName("LoopBody");
                string endLoop = ReserveName("LoopEnd");
                string iterationCount = ReserveName("loop_iteration");

                // Loops that match empty iterations need additional checks in place to prevent infinitely matching (since
                // you could end up looping an infinite number of times at the same location).  We can avoid those
                // additional checks if we can prove that the loop can never match empty, which we can do by computing
                // the minimum length of the child; only if it's 0 might iterations be empty.
                bool iterationMayBeEmpty = child.ComputeMinLength() == 0;
                string? startingPos = iterationMayBeEmpty ? ReserveName("loop_starting_pos") : null;

                if (iterationMayBeEmpty)
                {
                    additionalDeclarations.Add($"int {iterationCount} = 0, {startingPos} = 0;");
                    writer.WriteLine($"{startingPos} = pos;");
                }
                else
                {
                    additionalDeclarations.Add($"int {iterationCount} = 0;");
                }
                writer.WriteLine($"{iterationCount} = 0;");
                writer.WriteLine();

                // Iteration body
                MarkLabel(body, emitSemicolon: false);

                // We need to store the starting pos and crawl position so that it may be backtracked through later.
                // This needs to be the starting position from the iteration we're leaving, so it's pushed before updating
                // it to pos. Note that unlike some other constructs that only need to push state on to the stack if
                // they're inside of a loop (because if they're not inside of a loop, nothing would overwrite the locals),
                // here we still need the stack, because each iteration of _this_ loop may have its own state, e.g. we
                // need to know where each iteration began so when backtracking we can jump back to that location.  This is
                // true even if the loop is atomic, as we might need to backtrack within the loop in order to match the
                // minimum iteration count.
                EmitStackPush(
                    expressionHasCaptures && iterationMayBeEmpty ? new[] { "base.Crawlpos()", startingPos!, "pos" } :
                    expressionHasCaptures ? new[] { "base.Crawlpos()", "pos" } :
                    iterationMayBeEmpty ? new[] { startingPos!, "pos" } :
                    new[] { "pos" });
                writer.WriteLine();

                // Save off some state.  We need to store the current pos so we can compare it against
                // pos after the iteration, in order to determine whether the iteration was empty. Empty
                // iterations are allowed as part of min matches, but once we've met the min quote, empty matches
                // are considered match failures.
                if (iterationMayBeEmpty)
                {
                    writer.WriteLine($"{startingPos} = pos;");
                }

                // Proactively increase the number of iterations.  We do this prior to the match rather than once
                // we know it's successful, because we need to decrement it as part of a failed match when
                // backtracking; it's thus simpler to just always decrement it as part of a failed match, even
                // when initially greedily matching the loop, which then requires we increment it before trying.
                writer.WriteLine($"{iterationCount}++;");
                writer.WriteLine();

                // Last but not least, we need to set the doneLabel that a failed match of the body will jump to.
                // Such an iteration match failure may or may not fail the whole operation, depending on whether
                // we've already matched the minimum required iterations, so we need to jump to a location that
                // will make that determination.
                string iterationFailedLabel = ReserveName("LoopIterationNoMatch");
                doneLabel = iterationFailedLabel;

                // Finally, emit the child.
                Debug.Assert(sliceStaticPos == 0);
                EmitNode(child);
                writer.WriteLine();
                TransferSliceStaticPosToPos(); // ensure sliceStaticPos remains 0
                bool childBacktracks = doneLabel != iterationFailedLabel;

                // Loop condition.  Continue iterating greedily if we've not yet reached the maximum.  We also need to stop
                // iterating if the iteration matched empty and we already hit the minimum number of iterations.
                writer.WriteLine();
                if (maxIterations == int.MaxValue && !iterationMayBeEmpty)
                {
                    // The loop has no upper bound and iterations can't be empty; this is a greedy loop, so regardless of whether
                    // there's a min iterations required, we need to loop again.
                    writer.WriteLine("// The loop has no upper bound. Continue iterating greedily.");
                    Goto(body);
                }
                else
                {
                    FinishEmitBlock clause;
                    if (!iterationMayBeEmpty)
                    {
                        // Iterations won't be empty, but there is an upper bound. Whether or not there's a min iterations required, we need to keep
                        // iterating until we're at the maximum, and since the min is never more than the max, we don't need to check the min.
                        writer.WriteLine($"// The loop has an upper bound of {maxIterations}. Continue iterating greedily if it hasn't yet been reached.");
                        clause = EmitBlock(writer, $"if ({CountIsLessThan(iterationCount, maxIterations)})");
                    }
                    else if (minIterations > 0 && maxIterations == int.MaxValue)
                    {
                        // Iterations may be empty, and there's a minimum iteration count required (but no maximum), so loop if either
                        // the iteration isn't empty or we still need more iterations to meet the minimum.
                        writer.WriteLine($"// The loop has a lower bound of {minIterations} but no upper bound. Continue iterating greedily");
                        writer.WriteLine($"// if the last iteration wasn't empty (or if it was, if the lower bound hasn't yet been reached).");
                        clause = EmitBlock(writer, $"if (pos != {startingPos} || {CountIsLessThan(iterationCount, minIterations)})");
                    }
                    else if (minIterations > 0)
                    {
                        // Iterations may be empty and there's both a lower and upper bound on the loop.
                        writer.WriteLine($"// The loop has a lower bound of {minIterations} and an upper bound of {maxIterations}. Continue iterating");
                        writer.WriteLine($"// greedily if the upper bound hasn't yet been reached and either the last iteration was non-empty or the");
                        writer.WriteLine($"// lower bound hasn't yet been reached.");
                        clause = EmitBlock(writer, $"if ((pos != {startingPos} || {CountIsLessThan(iterationCount, minIterations)}) && {CountIsLessThan(iterationCount, maxIterations)})");
                    }
                    else if (maxIterations == int.MaxValue)
                    {
                        // Iterations may be empty and there's no lower or upper bound.
                        writer.WriteLine($"// The loop is unbounded. Continue iterating greedily as long as the last iteration wasn't empty.");
                        clause = EmitBlock(writer, $"if (pos != {startingPos})");
                    }
                    else
                    {
                        // Iterations may be empty, there's no lower bound, but there is an upper bound.
                        writer.WriteLine($"// The loop has an upper bound of {maxIterations}. Continue iterating greedily if the upper bound hasn't");
                        writer.WriteLine($"// yet been reached (as long as the last iteration wasn't empty).");
                        clause = EmitBlock(writer, $"if (pos != {startingPos} && {CountIsLessThan(iterationCount, maxIterations)})");
                    }

                    using (clause)
                    {
                        Goto(body);
                    }
                    Goto(endLoop);
                }

                // We've matched as many iterations as we can with this configuration.  Jump to what comes after the loop.
                writer.WriteLine();

                // Now handle what happens when an iteration fails, which could be an initial failure or it
                // could be while backtracking.  We need to reset state to what it was before just that iteration
                // started.  That includes resetting pos and clearing out any captures from that iteration.
                writer.WriteLine("// The loop iteration failed. Put state back to the way it was before the iteration.");
                MarkLabel(iterationFailedLabel, emitSemicolon: false);
                using (EmitBlock(writer, $"if (--{iterationCount} < 0)"))
                {
                    // If the loop has a lower bound of 0, then we may try to match what comes after the loop
                    // having matched 0 iterations.  If that fails, it'll then backtrack here, and the iteration
                    // count will become negative, indicating the loop has exhausted its choices.
                    writer.WriteLine("// Unable to match the remainder of the expression after exhausting the loop.");
                    Goto(originalDoneLabel);
                }
                EmitStackPop(iterationMayBeEmpty ?
                    new[] { "pos", startingPos! } :
                    new[] { "pos" });
                if (expressionHasCaptures)
                {
                    EmitUncaptureUntil(StackPop());
                }
                SliceInputSpan();

                // If there's a required minimum iteration count, validate now that we've processed enough iterations.
                if (minIterations > 0)
                {
                    if (childBacktracks)
                    {
                        // The child backtracks.  If we don't have any iterations, there's nothing to backtrack into,
                        // and at least one iteration is required, so fail the loop.
                        using (EmitBlock(writer, $"if ({iterationCount} == 0)"))
                        {
                            writer.WriteLine("// No iterations have been matched to backtrack into. Fail the loop.");
                            Goto(originalDoneLabel);
                        }
                        writer.WriteLine();

                        // We have at least one iteration; if that's insufficient to meet the minimum, backtrack
                        // into the previous iteration.  We only need to do this check if the min iteration requirement
                        // is more than one, since the above check already handles the case where the min count is 1,
                        // since the only value that wouldn't meet that is 0.
                        if (minIterations > 1)
                        {
                            using (EmitBlock(writer, $"if ({CountIsLessThan(iterationCount, minIterations)})"))
                            {
                                writer.WriteLine($"// All possible iterations have matched, but it's below the required minimum of {minIterations}.");
                                writer.WriteLine($"// Backtrack into the prior iteration.");
                                Goto(doneLabel);
                            }
                            writer.WriteLine();
                        }
                    }
                    else
                    {
                        // The child doesn't backtrack, which means there's no other way the matched iterations could
                        // match differently, so if we haven't already greedily processed enough iterations, fail the loop.
                        using (EmitBlock(writer, $"if ({CountIsLessThan(iterationCount, minIterations)})"))
                        {
                            writer.WriteLine($"// All possible iterations have matched, but it's below the required minimum of {minIterations}. Fail the loop.");

                            // If the minimum iterations is 1, then since we're only here if there are fewer, there must be 0
                            // iterations, in which case there's nothing to reset.  If, however, the minimum iteration count is
                            // greater than 1, we need to check if there was at least one successful iteration, in which case
                            // any backtracking state still set needs to be reset; otherwise, constructs earlier in the sequence
                            // trying to pop their own state will erroneously pop this state instead.
                            if (minIterations > 1)
                            {
                                Debug.Assert(startingStackpos is not null);
                                using (EmitBlock(writer, $"if ({iterationCount} != 0)"))
                                {
                                    writer.WriteLine($"// Ensure any stale backtracking state is removed.");
                                    writer.WriteLine($"stackpos = {startingStackpos};");
                                }
                            }

                            Goto(originalDoneLabel);
                        }
                        writer.WriteLine();
                    }
                }

                if (isAtomic)
                {
                    doneLabel = originalDoneLabel;
                    MarkLabel(endLoop, emitSemicolon: startingStackpos is null);

                    // The loop is atomic, which means any backtracking will go around this loop.  That also means we can't leave
                    // stack polluted with state from successful iterations, so we need to remove all such state; such state will
                    // only have been pushed if minIterations > 0.
                    if (startingStackpos is not null)
                    {
                        writer.WriteLine($"stackpos = {startingStackpos}; // Ensure any remaining backtracking state is removed.");
                    }
                }
                else
                {
                    if (childBacktracks)
                    {
                        Goto(endLoop);
                        writer.WriteLine();

                        string backtrack = ReserveName("LoopBacktrack");
                        MarkLabel(backtrack, emitSemicolon: false);

                        // We're backtracking.  Check the timeout.
                        EmitTimeoutCheckIfNeeded(writer, rm);

                        using (EmitBlock(writer, $"if ({iterationCount} == 0)"))
                        {
                            writer.WriteLine("// No iterations of the loop remain to backtrack into. Fail the loop.");
                            Goto(originalDoneLabel);
                        }
                        Goto(doneLabel);
                        doneLabel = backtrack;
                    }

                    bool isInLoop = rm.Analysis.IsInLoop(node);
                    MarkLabel(endLoop, emitSemicolon: !isInLoop);

                    // If this loop is itself not in another loop, nothing more needs to be done:
                    // upon backtracking, locals being used by this loop will have retained their
                    // values and be up-to-date.  But if this loop is inside another loop, multiple
                    // iterations of this loop each need their own state, so we need to use the stack
                    // to hold it, and we need a dedicated backtracking section to handle restoring
                    // that state before jumping back into the loop itself.
                    if (isInLoop)
                    {
                        writer.WriteLine();

                        // Store the loop's state
                        EmitStackPush(
                            startingPos is not null && startingStackpos is not null ? new[] { startingPos, startingStackpos, iterationCount } :
                            startingPos is not null ? new[] { startingPos, iterationCount } :
                            startingStackpos is not null ? new[] { startingStackpos, iterationCount } :
                            new[] { iterationCount });

                        // Skip past the backtracking section
                        string end = ReserveName("LoopSkipBacktrack");
                        Goto(end);
                        writer.WriteLine();

                        // Emit a backtracking section that restores the loop's state and then jumps to the previous done label
                        string backtrack = ReserveName("LoopBacktrack");
                        MarkLabel(backtrack, emitSemicolon: false);
                        EmitStackPop(
                            startingPos is not null && startingStackpos is not null ? new[] { iterationCount, startingStackpos, startingPos } :
                            startingPos is not null ? new[] { iterationCount, startingPos } :
                            startingStackpos is not null ? new[] { iterationCount, startingStackpos } :
                            new[] { iterationCount });

                        // We're backtracking.  Check the timeout.
                        EmitTimeoutCheckIfNeeded(writer, rm);

                        Goto(doneLabel);
                        writer.WriteLine();

                        doneLabel = backtrack;
                        MarkLabel(end);
                    }
                }
            }

            // Gets a comparison for whether the iteration count is less than the upper bound.
            static string CountIsLessThan(string count, int exclusiveUpper) =>
                exclusiveUpper == 1 ? $"{count} == 0" : $"{count} < {exclusiveUpper}";

            // Gets a comparison for whether the iteration count is greater than or equal to the upper bound
            static string CountIsGreaterThanOrEqualTo(string count, int exclusiveUpper) =>
                exclusiveUpper == 1 ? $"{count} != 0" : $"{count} >= {exclusiveUpper}";

            // Emits code to unwind the capture stack until the crawl position specified in the provided local.
            void EmitUncaptureUntil(string capturepos)
            {
                const string UncaptureUntil = nameof(UncaptureUntil);

                if (!additionalLocalFunctions.ContainsKey(UncaptureUntil))
                {
                    additionalLocalFunctions.Add(UncaptureUntil, new string[]
                    {
                        $"// <summary>Undo captures until it reaches the specified capture position.</summary>",
                        $"[MethodImpl(MethodImplOptions.AggressiveInlining)]",
                        $"void {UncaptureUntil}(int capturePosition)",
                        $"{{",
                        $"    while (base.Crawlpos() > capturePosition)",
                        $"    {{",
                        $"        base.Uncapture();",
                        $"    }}",
                        $"}}",
                    });
                }

                writer.WriteLine($"{UncaptureUntil}({capturepos});");
            }

            /// <summary>Pushes values on to the backtracking stack.</summary>
            void EmitStackPush(params string[] args)
            {
                Debug.Assert(args.Length is >= 1);

                const string MethodName = "StackPush";
                string key = $"{MethodName}{args.Length}";

                additionalDeclarations.Add("int stackpos = 0;");

                if (!requiredHelpers.ContainsKey(key))
                {
                    var lines = new string[24 + args.Length];
                    lines[0] = $"/// <summary>Pushes {args.Length} value{(args.Length == 1 ? "" : "s")} onto the backtracking stack.</summary>";
                    lines[1] = $"[MethodImpl(MethodImplOptions.AggressiveInlining)]";
                    lines[2] = $"internal static void {MethodName}(ref int[] stack, ref int pos{FormatN(", int arg{0}", args.Length)})";
                    lines[3] = $"{{";
                    lines[4] = $"    // If there's space available for {(args.Length > 1 ? $"all {args.Length} values, store them" : "the value, store it")}.";
                    lines[5] = $"    int[] s = stack;";
                    lines[6] = $"    int p = pos;";
                    lines[7] = $"    if ((uint){(args.Length > 1 ? $"(p + {args.Length - 1})" : "p")} < (uint)s.Length)";
                    lines[8] = $"    {{";
                    for (int i = 0; i < args.Length; i++)
                    {
                        lines[9 + i] = $"        s[p{(i == 0 ? "" : $" + {i}")}] = arg{i};";
                    }
                    lines[9 + args.Length] = args.Length > 1 ? $"        pos += {args.Length};" : "        pos++;";
                    lines[10 + args.Length] = $"        return;";
                    lines[11 + args.Length] = $"    }}";
                    lines[12 + args.Length] = $"";
                    lines[13 + args.Length] = $"    // Otherwise, resize the stack to make room and try again.";
                    lines[14 + args.Length] = $"    WithResize(ref stack, ref pos{FormatN(", arg{0}", args.Length)});";
                    lines[15 + args.Length] = $"";
                    lines[16 + args.Length] = $"    // <summary>Resize the backtracking stack array and push {args.Length} value{(args.Length == 1 ? "" : "s")} onto the stack.</summary>";
                    lines[17 + args.Length] = $"    [MethodImpl(MethodImplOptions.NoInlining)]";
                    lines[18 + args.Length] = $"    static void WithResize(ref int[] stack, ref int pos{FormatN(", int arg{0}", args.Length)})";
                    lines[19 + args.Length] = $"    {{";
                    lines[20 + args.Length] = $"        Array.Resize(ref stack, (pos + {args.Length - 1}) * 2);";
                    lines[21 + args.Length] = $"        {MethodName}(ref stack, ref pos{FormatN(", arg{0}", args.Length)});";
                    lines[22 + args.Length] = $"    }}";
                    lines[23 + args.Length] = $"}}";

                    requiredHelpers.Add(key, lines);
                }

                writer.WriteLine($"{HelpersTypeName}.{MethodName}(ref base.runstack!, ref stackpos, {string.Join(", ", args)});");
            }

            /// <summary>Pops values from the backtracking stack into the specified locations.</summary>
            void EmitStackPop(params string[] args)
            {
                Debug.Assert(args.Length is >= 1);

                if (args.Length == 1)
                {
                    writer.WriteLine($"{args[0]} = {StackPop()};");
                    return;
                }

                const string MethodName = "StackPop";
                string key = $"{MethodName}{args.Length}";

                if (!requiredHelpers.ContainsKey(key))
                {
                    var lines = new string[5 + args.Length];
                    lines[0] = $"/// <summary>Pops {args.Length} value{(args.Length == 1 ? "" : "s")} from the backtracking stack.</summary>";
                    lines[1] = $"[MethodImpl(MethodImplOptions.AggressiveInlining)]";
                    lines[2] = $"internal static void {MethodName}(int[] stack, ref int pos{FormatN(", out int arg{0}", args.Length)})";
                    lines[3] = $"{{";
                    for (int i = 0; i < args.Length; i++)
                    {
                        lines[4 + i] = $"    arg{i} = stack[--pos];";
                    }
                    lines[4 + args.Length] = $"}}";

                    requiredHelpers.Add(key, lines);
                }

                writer.WriteLine($"{HelpersTypeName}.{MethodName}(base.runstack!, ref stackpos, out {string.Join(", out ", args)});");
            }

            /// <summary>Expression for popping the next item from the backtracking stack.</summary>
            string StackPop() => "base.runstack![--stackpos]";

            /// <summary>Concatenates the strings resulting from formatting the format string with the values [0, count).</summary>
            static string FormatN(string format, int count) =>
                string.Concat(from i in Enumerable.Range(0, count)
                              select string.Format(format, i));
        }

        /// <summary>Emits a timeout check if the regex timeout wasn't explicitly set to infinite.</summary>
        /// <remarks>
        /// Regex timeouts exist to avoid catastrophic backtracking.  The goal with timeouts isn't to be accurate to the timeout value,
        /// but to ensure that significant backtracking can be stopped.  As such, we allow for up to O(n) work in the length of the input
        /// between checks, which means we emit checks anywhere backtracking is introduced, such that every check can have O(n) work
        /// associated with it.  This means checks:
        /// - when restarting the whole match evaluation at a new index. Every match could end up doing O(n) work without a timeout
        ///   check, and since this could then result in O(n) matches, we need a timeout check on each new position in order to
        ///   avoid O(n^2) work without a timeout check.
        /// - when backtracking backwards in a loop. Every backtracking step through the loop could evaluate the remainder of the
        ///   pattern, which can lead to O(2^n) work if unchecked.
        /// - when backtracking forwards in a lazy loop. Every backtracking step through the loop could evaluate the remainder of the
        ///   pattern, which can lead to O(2^n) work if unchecked.
        /// - when backtracking to the next branch of an alternation. Every branch of the alternation could evaluate the remainder of the
        ///   pattern, which can lead to O(2^n) work if unchecked.
        /// - when performing a lookaround.  Each lookaround can result in doing O(n) work, which means m lookarounds can result in
        ///   O(m*n) work.  Lookarounds can be in loops, so without timeout checks in a lookaround, a pattern like `((?=(?>a*))a)+`
        ///   could do O(n^2) work without a timeout check.
        /// Note that some other constructs have code that needs to deal with backtracking, e.g. conditionals needing to ensure
        /// that if any of their children have backtracking that code which backtracks back into the conditional is appropriately
        /// routed to the correct child, but such constructs aren't actually introducing backtracking and thus don't need to be
        /// instrumented for timeouts.
        /// </remarks>
        private static void EmitTimeoutCheckIfNeeded(IndentedTextWriter writer, RegexMethod rm, bool appendNewLineIfTimeoutEmitted = true)
        {
            // If the match timeout was explicitly set to infinite, then no timeout code needs to be emitted.
            if (rm.MatchTimeout != Timeout.Infinite)
            {
                // If the timeout was explicitly set to non-infinite, then we always want to do the timeout check count tracking
                // and actual timeout checks now and then.  If, however, the timeout was left null, we only want to do the timeout
                // checks if they've been enabled by an AppContext switch being set before any of the regex code was used.
                // Whether these checks are needed are stored into a static readonly bool on the helpers class, such that
                // tiered-up code can eliminate the whole block in the vast majority case where the AppContext switch isn't set.
                using (rm.MatchTimeout is null ? EmitBlock(writer, $"if ({HelpersTypeName}.{HasDefaultTimeoutFieldName})") : default)
                {
                    writer.WriteLine("base.CheckTimeout();");
                }

                if (appendNewLineIfTimeoutEmitted)
                {
                    writer.WriteLine();
                }
            }
        }

        /// <summary>Tries to create an IndexOf expression for the node.</summary>
        /// <param name="node">The RegexNode. If it's a loop, only the one/notone/set aspect of the node is factored in.</param>
        /// <param name="useLast">true to use LastIndexOf variants; false to use IndexOf variants.</param>
        /// <param name="negate">true to search for the opposite of the node.</param>
        /// <param name="literalLength">0 if returns false. If it returns true, string.Length for a multi, otherwise 1.</param>
        /// <param name="indexOfExpr">The resulting expression if it returns true; otherwise, null.</param>
        /// <returns>true if an expression could be produced; otherwise, false.</returns>
        private static bool TryEmitIndexOf(
            Dictionary<string, string[]> requiredHelpers,
            RegexNode node,
            bool useLast, bool negate,
            out int literalLength, [NotNullWhen(true)] out string? indexOfExpr)
        {
            string last = useLast ? "Last" : "";

            if (node.Kind == RegexNodeKind.Multi)
            {
                Debug.Assert(!negate, "Negation isn't appropriate for a multi");
                indexOfExpr = $"{last}IndexOf({Literal(node.Str!)})";
                literalLength = node.Str!.Length;
                return true;
            }

            if (node.IsOneFamily)
            {
                indexOfExpr = negate ? $"{last}IndexOfAnyExcept({Literal(node.Ch)})" : $"{last}IndexOf({Literal(node.Ch)})";
                literalLength = 1;
                return true;
            }

            if (node.IsNotoneFamily)
            {
                indexOfExpr = negate ? $"{last}IndexOf({Literal(node.Ch)})" : $"{last}IndexOfAnyExcept({Literal(node.Ch)})";
                literalLength = 1;
                return true;
            }

            if (node.IsSetFamily)
            {
                bool negated = RegexCharClass.IsNegated(node.Str) ^ negate;

                Span<char> setChars = stackalloc char[5]; // current max that's vectorized
                int setCharsCount = RegexCharClass.GetSetChars(node.Str, setChars);

                // Prefer IndexOfAnyInRange over IndexOfAny for sets of 3-5 values that fit in a single range.
                if (setCharsCount is not (1 or 2) && RegexCharClass.TryGetSingleRange(node.Str, out char lowInclusive, out char highInclusive))
                {
                    string indexOfAnyInRangeName = !negated ?
                        "IndexOfAnyInRange" :
                        "IndexOfAnyExceptInRange";

                    indexOfExpr = $"{last}{indexOfAnyInRangeName}({Literal(lowInclusive)}, {Literal(highInclusive)})";

                    literalLength = 1;
                    return true;
                }

                if (setCharsCount > 0)
                {
                    (string indexOfName, string indexOfAnyName) = !negated ?
                        ("IndexOf", "IndexOfAny") :
                        ("IndexOfAnyExcept", "IndexOfAnyExcept");

                    setChars = setChars.Slice(0, setCharsCount);
                    indexOfExpr = setChars.Length switch
                    {
                        1 => $"{last}{indexOfName}({Literal(setChars[0])})",
                        2 => $"{last}{indexOfAnyName}({Literal(setChars[0])}, {Literal(setChars[1])})",
                        3 => $"{last}{indexOfAnyName}({Literal(setChars[0])}, {Literal(setChars[1])}, {Literal(setChars[2])})",
                        _ => $"{last}{indexOfAnyName}({EmitIndexOfAnyValuesOrLiteral(setChars, requiredHelpers)})",
                    };

                    literalLength = 1;
                    return true;
                }

                if (RegexCharClass.TryGetAsciiSetChars(node.Str, out char[]? asciiChars))
                {
                    string indexOfAnyName = !negated ?
                        "IndexOfAny" :
                        "IndexOfAnyExcept";

                    indexOfExpr = $"{last}{indexOfAnyName}({EmitIndexOfAnyValues(asciiChars, requiredHelpers)})";

                    literalLength = 1;
                    return true;
                }
            }

            indexOfExpr = null;
            literalLength = 0;
            return false;
        }

        private static string MatchCharacterClass(string chExpr, string charClass, bool negate, HashSet<string> additionalDeclarations, Dictionary<string, string[]> requiredHelpers)
        {
            // We need to perform the equivalent of calling RegexRunner.CharInClass(ch, charClass),
            // but that call is relatively expensive.  Before we fall back to it, we try to optimize
            // some common cases for which we can do much better, such as known character classes
            // for which we can call a dedicated method, or a fast-path for ASCII using a lookup table.
            // In some cases, multiple optimizations are possible for a given character class: the checks
            // in this method are generally ordered from fastest / simplest to slowest / most complex so
            // that we get the best optimization for a given char class.

            // First, see if the char class is a built-in one for which there's a better function
            // we can just call directly.
            switch (charClass)
            {
                case RegexCharClass.AnyClass:
                    return negate ? "false" : "true"; // This assumes chExpr never has side effects.

                case RegexCharClass.DigitClass:
                case RegexCharClass.NotDigitClass:
                    negate ^= charClass == RegexCharClass.NotDigitClass;
                    return $"{(negate ? "!" : "")}char.IsDigit({chExpr})";

                case RegexCharClass.SpaceClass:
                case RegexCharClass.NotSpaceClass:
                    negate ^= charClass == RegexCharClass.NotSpaceClass;
                    return $"{(negate ? "!" : "")}char.IsWhiteSpace({chExpr})";

                case RegexCharClass.WordClass:
                case RegexCharClass.NotWordClass:
                    AddIsWordCharHelper(requiredHelpers);
                    negate ^= charClass == RegexCharClass.NotWordClass;
                    return $"{(negate ? "!" : "")}{HelpersTypeName}.IsWordChar({chExpr})";

                case RegexCharClass.ControlClass:
                case RegexCharClass.NotControlClass:
                    negate ^= charClass == RegexCharClass.NotControlClass;
                    return $"{(negate ? "!" : "")}char.IsControl({chExpr})";

                case RegexCharClass.LetterClass:
                case RegexCharClass.NotLetterClass:
                    negate ^= charClass == RegexCharClass.NotLetterClass;
                    return $"{(negate ? "!" : "")}char.IsLetter({chExpr})";

                case RegexCharClass.LetterOrDigitClass:
                case RegexCharClass.NotLetterOrDigitClass:
                    negate ^= charClass == RegexCharClass.NotLetterOrDigitClass;
                    return $"{(negate ? "!" : "")}char.IsLetterOrDigit({chExpr})";

                case RegexCharClass.LowerClass:
                case RegexCharClass.NotLowerClass:
                    negate ^= charClass == RegexCharClass.NotLowerClass;
                    return $"{(negate ? "!" : "")}char.IsLower({chExpr})";

                case RegexCharClass.UpperClass:
                case RegexCharClass.NotUpperClass:
                    negate ^= charClass == RegexCharClass.NotUpperClass;
                    return $"{(negate ? "!" : "")}char.IsUpper({chExpr})";

                case RegexCharClass.NumberClass:
                case RegexCharClass.NotNumberClass:
                    negate ^= charClass == RegexCharClass.NotNumberClass;
                    return $"{(negate ? "!" : "")}char.IsNumber({chExpr})";

                case RegexCharClass.PunctuationClass:
                case RegexCharClass.NotPunctuationClass:
                    negate ^= charClass == RegexCharClass.NotPunctuationClass;
                    return $"{(negate ? "!" : "")}char.IsPunctuation({chExpr})";

                case RegexCharClass.SeparatorClass:
                case RegexCharClass.NotSeparatorClass:
                    negate ^= charClass == RegexCharClass.NotSeparatorClass;
                    return $"{(negate ? "!" : "")}char.IsSeparator({chExpr})";

                case RegexCharClass.SymbolClass:
                case RegexCharClass.NotSymbolClass:
                    negate ^= charClass == RegexCharClass.NotSymbolClass;
                    return $"{(negate ? "!" : "")}char.IsSymbol({chExpr})";

                case RegexCharClass.AsciiLetterClass:
                case RegexCharClass.NotAsciiLetterClass:
                    negate ^= charClass == RegexCharClass.NotAsciiLetterClass;
                    return $"{(negate ? "!" : "")}char.IsAsciiLetter({chExpr})";

                case RegexCharClass.AsciiLetterOrDigitClass:
                case RegexCharClass.NotAsciiLetterOrDigitClass:
                    negate ^= charClass == RegexCharClass.NotAsciiLetterOrDigitClass;
                    return $"{(negate ? "!" : "")}char.IsAsciiLetterOrDigit({chExpr})";

                case RegexCharClass.HexDigitClass:
                case RegexCharClass.NotHexDigitClass:
                    negate ^= charClass == RegexCharClass.NotHexDigitClass;
                    return $"{(negate ? "!" : "")}char.IsAsciiHexDigit({chExpr})";

                case RegexCharClass.HexDigitLowerClass:
                case RegexCharClass.NotHexDigitLowerClass:
                    negate ^= charClass == RegexCharClass.NotHexDigitLowerClass;
                    return $"{(negate ? "!" : "")}char.IsAsciiHexDigitLower({chExpr})";

                case RegexCharClass.HexDigitUpperClass:
                case RegexCharClass.NotHexDigitUpperClass:
                    negate ^= charClass == RegexCharClass.NotHexDigitUpperClass;
                    return $"{(negate ? "!" : "")}char.IsAsciiHexDigitUpper({chExpr})";
            }

            // Next, handle simple sets of one range, e.g. [A-Z], [0-9], etc.  This includes some built-in classes, like ECMADigitClass.
            if (RegexCharClass.TryGetSingleRange(charClass, out char lowInclusive, out char highInclusive))
            {
                negate ^= RegexCharClass.IsNegated(charClass);
                return (lowInclusive, highInclusive) switch
                {
                    ('\0', '\u007F') => $"{(negate ? "!" : "")}char.IsAscii({chExpr})",
                    ('0', '9') => $"{(negate ? "!" : "")}char.IsAsciiDigit({chExpr})",
                    ('a', 'z') => $"{(negate ? "!" : "")}char.IsAsciiLetterLower({chExpr})",
                    ('A', 'Z') => $"{(negate ? "!" : "")}char.IsAsciiLetterUpper({chExpr})",
                    ('\ud800', '\udfff') => $"{(negate ? "!" : "")}char.IsSurrogate({chExpr})",
                    ('\ud800', '\udbff') => $"{(negate ? "!" : "")}char.IsHighSurrogate({chExpr})",
                    ('\udc00', '\udfff') => $"{(negate ? "!" : "")}char.IsLowSurrogate({chExpr})",
                    _ when lowInclusive == highInclusive => $"({chExpr} {(negate ? "!=" : "==")} {Literal(lowInclusive)})",
                    _ => $"{(negate ? "!" : "")}char.IsBetween({chExpr}, {Literal(lowInclusive)}, {Literal(highInclusive)})",
                };
            }

            // Next, if the character class contains nothing but Unicode categories, we can call char.GetUnicodeCategory and
            // compare against it.  It has a fast-lookup path for ASCII, so is as good or better than any lookup we'd generate (plus
            // we get smaller code), and it's what we'd do for the fallback (which we get to avoid generating) as part of CharInClass,
            // but without the optimizations the C# compiler will provide for switches.
            Span<UnicodeCategory> categories = stackalloc UnicodeCategory[30]; // number of UnicodeCategory values (though it's unheard of to have a set with all of them)
            if (RegexCharClass.TryGetOnlyCategories(charClass, categories, out int numCategories, out bool negated))
            {
                int categoryMask = 0;
                foreach (UnicodeCategory category in categories.Slice(0, numCategories))
                {
                    categoryMask |= 1 << (int)category;
                }

                negate ^= negated;
                return numCategories == 1 ?
                    $"(char.GetUnicodeCategory({chExpr}) {(negate ? "!=" : "==")} UnicodeCategory.{categories[0]})" :
                    $"((0x{categoryMask:X} & (1 << (int)char.GetUnicodeCategory({chExpr}))) {(negate ? "==" : "!=")} 0)";
            }

            // Next, if there's only 2 or 3 chars in the set (fairly common due to the sets we create for prefixes),
            // it may be cheaper and smaller to compare against each than it is to use a lookup table.  We can also special-case
            // the very common case with case insensitivity of two characters next to each other being the upper and lowercase
            // ASCII variants of each other, in which case we can use bit manipulation to avoid a comparison.
            Span<char> setChars = stackalloc char[3];
            int mask;
            switch (RegexCharClass.GetSetChars(charClass, setChars))
            {
                case 2:
                    negate ^= RegexCharClass.IsNegated(charClass);
                    if (RegexCharClass.DifferByOneBit(setChars[0], setChars[1], out mask))
                    {
                        return $"(({chExpr} | 0x{mask:X}) {(negate ? "!=" : "==")} {Literal((char)(setChars[1] | mask))})";
                    }
                    additionalDeclarations.Add("char ch;");
                    return negate ?
                        $"(((ch = {chExpr}) != {Literal(setChars[0])}) & (ch != {Literal(setChars[1])}))" :
                        $"(((ch = {chExpr}) == {Literal(setChars[0])}) | (ch == {Literal(setChars[1])}))";

                case 3:
                    negate ^= RegexCharClass.IsNegated(charClass);
                    additionalDeclarations.Add("char ch;");
                    return (negate, RegexCharClass.DifferByOneBit(setChars[0], setChars[1], out mask)) switch
                    {
                        (false, false) => $"(((ch = {chExpr}) == {Literal(setChars[0])}) | (ch == {Literal(setChars[1])}) | (ch == {Literal(setChars[2])}))",
                        (true,  false) => $"(((ch = {chExpr}) != {Literal(setChars[0])}) & (ch != {Literal(setChars[1])}) & (ch != {Literal(setChars[2])}))",
                        (false, true)  => $"((((ch = {chExpr}) | 0x{mask:X}) == {Literal((char)(setChars[1] | mask))}) | (ch == {Literal(setChars[2])}))",
                        (true,  true)  => $"((((ch = {chExpr}) | 0x{mask:X}) != {Literal((char)(setChars[1] | mask))}) & (ch != {Literal(setChars[2])}))",
                    };
            }

            // Next, handle simple sets of two ASCII letter ranges that are cased versions of each other, e.g. [A-Za-z].
            // This can be implemented as if it were a single range, with an additional bitwise operation.
            if (RegexCharClass.TryGetDoubleRange(charClass, out (char LowInclusive, char HighInclusive) rangeLower, out (char LowInclusive, char HighInclusive) rangeUpper) &&
                CharExtensions.IsAsciiLetter(rangeUpper.LowInclusive) &&
                CharExtensions.IsAsciiLetter(rangeUpper.HighInclusive) &&
                (rangeLower.LowInclusive | 0x20) == rangeUpper.LowInclusive &&
                (rangeLower.HighInclusive | 0x20) == rangeUpper.HighInclusive)
            {
                Debug.Assert(rangeLower.LowInclusive != rangeUpper.LowInclusive);
                negate ^= RegexCharClass.IsNegated(charClass);
                return $"((uint)(({chExpr} | 0x20) - {Literal(rangeUpper.LowInclusive)}) {(negate ? ">" : "<=")} (uint)({Literal(rangeUpper.HighInclusive)} - {Literal(rangeUpper.LowInclusive)}))";
            }

            // Analyze the character set more to determine what code to generate.
            RegexCharClass.CharClassAnalysisResults analysis = RegexCharClass.Analyze(charClass);

            // Next, handle sets where the high - low + 1 range is <= 32.  In that case, we can emit
            // a branchless lookup in a uint that does not rely on loading any objects (e.g. the string-based
            // lookup we use later).  This nicely handles common sets like [\t\r\n ].
            if (analysis.OnlyRanges && (analysis.UpperBoundExclusiveIfOnlyRanges - analysis.LowerBoundInclusiveIfOnlyRanges) <= 32)
            {
                additionalDeclarations.Add("uint charMinusLowUInt32;");

                // Create the 32-bit value with 1s at indices corresponding to every character in the set,
                // where the bit is computed to be the char value minus the lower bound starting from
                // most significant bit downwards.
                bool negatedClass = RegexCharClass.IsNegated(charClass);
                uint bitmap = 0;
                for (int i = analysis.LowerBoundInclusiveIfOnlyRanges; i < analysis.UpperBoundExclusiveIfOnlyRanges; i++)
                {
                    if (RegexCharClass.CharInClass((char)i, charClass) ^ negatedClass)
                    {
                        bitmap |= 1u << (31 - (i - analysis.LowerBoundInclusiveIfOnlyRanges));
                    }
                }

                // To determine whether a character is in the set, we subtract the lowest char; this subtraction happens before the result is
                // zero-extended to uint, meaning that `charMinusLowUInt32` will always have upper 16 bits equal to 0.
                // We then left shift the constant with this offset, and apply a bitmask that has the highest
                // bit set (the sign bit) if and only if `chExpr` is in the [low, low + 32) range.
                // Then we only need to check whether this final result is less than 0: this will only be
                // the case if both `charMinusLowUInt32` was in fact the index of a set bit in the constant, and also
                // `chExpr` was in the allowed range (this ensures that false positive bit shifts are ignored).
                negate ^= negatedClass;
                return $"((int)((0x{bitmap:X}U << (short)(charMinusLowUInt32 = (ushort)({chExpr} - {Literal((char)analysis.LowerBoundInclusiveIfOnlyRanges)}))) & (charMinusLowUInt32 - 32)) {(negate ? ">=" : "<")} 0)";
            }

            // Next, handle sets where the high - low + 1 range is <= 64.  As with the 32-bit case above, we can emit
            // a branchless lookup in a ulong that does not rely on loading any objects (e.g. the string-based lookup
            // we use later). Note that unlike RegexCompiler, the source generator doesn't know whether the code is going
            // to be run in a 32-bit or 64-bit process: in a 64-bit process, this is an optimization, but in a 32-bit process,
            // it's a deoptimization.  In general we optimize for 64-bit perf, so this code remains; it complicates the code
            // too much to try to include both this and a fallback for the check. This, however, is why we do the 32-bit
            // version and check first, as that variant performs equally well on both 32-bit and 64-bit systems.
            if (analysis.OnlyRanges && (analysis.UpperBoundExclusiveIfOnlyRanges - analysis.LowerBoundInclusiveIfOnlyRanges) <= 64)
            {
                additionalDeclarations.Add("ulong charMinusLowUInt64;");

                // Create the 64-bit value with 1s at indices corresponding to every character in the set,
                // where the bit is computed to be the char value minus the lower bound starting from
                // most significant bit downwards.
                bool negatedClass = RegexCharClass.IsNegated(charClass);
                ulong bitmap = 0;
                for (int i = analysis.LowerBoundInclusiveIfOnlyRanges; i < analysis.UpperBoundExclusiveIfOnlyRanges; i++)
                {
                    if (RegexCharClass.CharInClass((char)i, charClass) ^ negatedClass)
                    {
                        bitmap |= 1ul << (63 - (i - analysis.LowerBoundInclusiveIfOnlyRanges));
                    }
                }

                // To determine whether a character is in the set, we subtract the lowest char; this subtraction happens before
                // the result is zero-extended to uint, meaning that `charMinusLowUInt64` will always have upper 32 bits equal to 0.
                // We then left shift the constant with this offset, and apply a bitmask that has the highest bit set (the sign bit)
                // if and only if `chExpr` is in the [low, low + 64) range. Then we only need to check whether this final result is
                // less than 0: this will only be the case if both `charMinusLowUInt64` was in fact the index of a set bit in the constant,
                // and also `chExpr` was in the allowed range (this ensures that false positive bit shifts are ignored).
                negate ^= negatedClass;
                return $"((long)((0x{bitmap:X}UL << (int)(charMinusLowUInt64 = (uint){chExpr} - {Literal((char)analysis.LowerBoundInclusiveIfOnlyRanges)})) & (charMinusLowUInt64 - 64)) {(negate ? ">=" : "<")} 0)";
            }

            // All options after this point require a ch local.
            additionalDeclarations.Add("char ch;");

            // Next, handle simple sets of two ranges, e.g. [\p{IsGreek}\p{IsGreekExtended}].
            if (RegexCharClass.TryGetDoubleRange(charClass, out (char LowInclusive, char HighInclusive) range0, out (char LowInclusive, char HighInclusive) range1))
            {
                negate ^= RegexCharClass.IsNegated(charClass);

                string range0Clause = range0.LowInclusive == range0.HighInclusive ?
                    $"((ch = {chExpr}) {(negate ? "!=" : "==")} {Literal(range0.LowInclusive)})" :
                    $"((uint)((ch = {chExpr}) - {Literal(range0.LowInclusive)}) {(negate ? ">" : "<=")} (uint)({Literal(range0.HighInclusive)} - {Literal(range0.LowInclusive)}))";

                string range1Clause = range1.LowInclusive == range1.HighInclusive ?
                    $"(ch {(negate ? "!=" : "==")} {Literal(range1.LowInclusive)})" :
                    $"((uint)(ch - {Literal(range1.LowInclusive)}) {(negate ? ">" : "<=")} (uint)({Literal(range1.HighInclusive)} - {Literal(range1.LowInclusive)}))";

                return negate ?
                    $"({range0Clause} & {range1Clause})" :
                    $"({range0Clause} | {range1Clause})";
            }

            if (analysis.ContainsNoAscii)
            {
                // We determined that the character class contains only non-ASCII,
                // for example if the class were [\u1000-\u2000\u3000-\u4000\u5000-\u6000].
                // (In the future, we could possibly extend the rm.Analysis to produce a known
                // lower-bound and compare against that rather than always using 128 as the
                // pivot point.)
                return EmitContainsNoAscii();
            }

            if (analysis.AllAsciiContained)
            {
                // We determined that every ASCII character is in the class, for example
                // if the class were the negated example from case 1 above:
                // [^\p{IsGreek}\p{IsGreekExtended}].
                return EmitAllAsciiContained();
            }

            // Now, our big hammer is to generate a lookup table that lets us quickly index by character into a yes/no
            // answer as to whether the character is in the target character class.  However, we don't want to store
            // a lookup table for every possible character for every character class in the regular expression; at one
            // bit for each of 65K characters, that would be an 8K bitmap per character class.  Instead, we handle the
            // common case of ASCII input via such a lookup table, which at one bit for each of 128 characters is only
            // 16 bytes per character class.  We of course still need to be able to handle inputs that aren't ASCII, so
            // we check the input against 128, and have a fallback if the input is >= to it.  Determining the right
            // fallback could itself be expensive.  For example, if it's possible that a value >= 128 could match the
            // character class, we output a call to RegexRunner.CharInClass, but we don't want to have to enumerate the
            // entire character class evaluating every character against it, just to determine whether it's a match.
            // Instead, we employ some quick heuristics that will always ensure we provide a correct answer even if
            // we could have sometimes generated better code to give that answer.

            // Generate the lookup table to store 128 answers as bits. We use a const string instead of a byte[] / static
            // data property because it lets IL emit handle all the details for us.
            string bitVectorString = StringExtensions.Create(8, charClass, static (dest, charClass) => // String length is 8 chars == 16 bytes == 128 bits.
            {
                for (int i = 0; i < 128; i++)
                {
                    char c = (char)i;
                    if (RegexCharClass.CharInClass(c, charClass))
                    {
                        dest[i >> 4] |= (char)(1 << (i & 0xF));
                    }
                }
            });

            // There's a chance that the class contains either no ASCII characters or all of them,
            // and the analysis could not find it (for example if the class has a subtraction).
            // We optimize away the bit vector in these trivial cases.
            switch (bitVectorString)
            {
                case "\0\0\0\0\0\0\0\0": return EmitContainsNoAscii();
                case "\uffff\uffff\uffff\uffff\uffff\uffff\uffff\uffff": return EmitAllAsciiContained();
            }

            // We determined that the character class may contain ASCII, so we
            // output the lookup against the lookup table.

            if (analysis.ContainsOnlyAscii)
            {
                // If all inputs that could match are ASCII, we only need the lookup table, guarded
                // by a check for the upper bound (which serves both to limit for what characters
                // we need to access the lookup table and to bounds check the lookup table access).
                return negate ?
                    $"((ch = {chExpr}) >= {Literal((char)analysis.UpperBoundExclusiveIfOnlyRanges)} || ({Literal(bitVectorString)}[ch >> 4] & (1 << (ch & 0xF))) == 0)" :
                    $"((ch = {chExpr}) < {Literal((char)analysis.UpperBoundExclusiveIfOnlyRanges)} && ({Literal(bitVectorString)}[ch >> 4] & (1 << (ch & 0xF))) != 0)";
            }

            if (analysis.AllNonAsciiContained)
            {
                // If every non-ASCII value is considered a match, we can immediately succeed for any
                // non-ASCII inputs, and access the lookup table for the rest.
                return negate ?
                    $"((ch = {chExpr}) < 128 && ({Literal(bitVectorString)}[ch >> 4] & (1 << (ch & 0xF))) == 0)" :
                    $"((ch = {chExpr}) >= 128 || ({Literal(bitVectorString)}[ch >> 4] & (1 << (ch & 0xF))) != 0)";
            }

            // We know that the whole class wasn't ASCII, and we don't know anything about the non-ASCII
            // characters other than that some might be included, for example if the character class
            // were [\w\d], so if ch >= 128, we need to fall back to calling CharInClass. For ASCII, we
            // can use a lookup table, but if it's a known set of ASCII characters we can also use a helper.
            string asciiExpr = bitVectorString switch
            {
                "\0\0\0\u03ff\ufffe\u07ff\ufffe\u07ff" => $"{(negate ? "!" : "")}char.IsAsciiLetterOrDigit(ch)",

                "\0\0\0\u03FF\0\0\0\0" => $"{(negate ? "!" : "")}char.IsAsciiDigit(ch)",

                "\0\0\0\0\ufffe\u07FF\ufffe\u07ff" => $"{(negate ? "!" : "")}char.IsAsciiLetter(ch)",
                "\0\0\0\0\0\0\ufffe\u07ff" => $"{(negate ? "!" : "")}char.IsAsciiLetterLower(ch)",
                "\0\0\0\0\ufffe\u07FF\0\0" => $"{(negate ? "!" : "")}char.IsAsciiLetterUpper(ch)",

                "\0\0\0\u03FF\u007E\0\u007E\0" => $"{(negate ? "!" : "")}char.IsAsciiHexDigit(ch)",
                "\0\0\0\u03FF\0\0\u007E\0" => $"{(negate ? "!" : "")}char.IsAsciiHexDigitLower(ch)",
                "\0\0\0\u03FF\u007E\0\0\0" => $"{(negate ? "!" : "")}char.IsAsciiHexDigitUpper(ch)",

                _ => $"({Literal(bitVectorString)}[ch >> 4] & (1 << (ch & 0xF))) {(negate ? "=" : "!")}= 0",
            };
            return $"((ch = {chExpr}) < 128 ? {asciiExpr} : {(negate ? "!" : "")}RegexRunner.CharInClass((char)ch, {Literal(charClass)}))";

            string EmitContainsNoAscii()
            {
                return negate ?
                    $"((ch = {chExpr}) < 128 || !RegexRunner.CharInClass((char)ch, {Literal(charClass)}))" :
                    $"((ch = {chExpr}) >= 128 && RegexRunner.CharInClass((char)ch, {Literal(charClass)}))";
            }

            string EmitAllAsciiContained()
            {
                return negate ?
                    $"((ch = {chExpr}) >= 128 && !RegexRunner.CharInClass((char)ch, {Literal(charClass)}))" :
                    $"((ch = {chExpr}) < 128 || RegexRunner.CharInClass((char)ch, {Literal(charClass)}))";
            }
        }

        /// <summary>
        /// Replaces <see cref="AdditionalDeclarationsPlaceholder"/> in <paramref name="writer"/> with
        /// all of the variable declarations in <paramref name="declarations"/>.
        /// </summary>
        /// <param name="writer">The writer around a StringWriter to have additional declarations inserted into.</param>
        /// <param name="declarations">The additional declarations to insert.</param>
        /// <param name="position">The position into the writer at which to insert the additional declarations.</param>
        /// <param name="indent">The indentation to use for the additional declarations.</param>
        private static void ReplaceAdditionalDeclarations(IndentedTextWriter writer, HashSet<string> declarations, int position, int indent)
        {
            if (declarations.Count != 0)
            {
                var tmp = new StringBuilder();
                foreach (string decl in declarations.OrderBy(s => s))
                {
                    for (int i = 0; i < indent; i++)
                    {
                        tmp.Append(IndentedTextWriter.DefaultTabString);
                    }

                    tmp.AppendLine(decl);
                }

                ((StringWriter)writer.InnerWriter).GetStringBuilder().Insert(position, tmp.ToString());
            }
        }

        /// <summary>Formats the character as valid C#.</summary>
        private static string Literal(char c) => SymbolDisplay.FormatLiteral(c, quote: true);

        /// <summary>Formats the string as valid C#.</summary>
        private static string Literal(string s) => SymbolDisplay.FormatLiteral(s, quote: true);

        private static string Literal(RegexOptions options)
        {
            string s = options.ToString();
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            {
                // The options were formatted as an int, which means the runtime couldn't
                // produce a textual representation.  So just output casting the value as an int.
                Debug.Fail("This shouldn't happen, as we should only get to the point of emitting code if RegexOptions was valid.");
                return $"(RegexOptions)({(int)options})";
            }

            // Parse the runtime-generated "Option1, Option2" into each piece and then concat
            // them back together.
            string[] parts = s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = "RegexOptions." + parts[i].Trim();
            }
            return string.Join(" | ", parts);
        }

        /// <summary>Gets a textual description of the node fit for rendering in a comment in source.</summary>
        private static string DescribeNode(RegexNode node, RegexMethod rm)
        {
            bool rtl = (node.Options & RegexOptions.RightToLeft) != 0;
            string direction = rtl ? " right-to-left" : "";
            return node.Kind switch
            {
                RegexNodeKind.Alternate => $"Match with {node.ChildCount()} alternative expressions{(rm.Analysis.IsAtomicByAncestor(node) ? ", atomically" : "")}.",
                RegexNodeKind.Atomic => $"Atomic group.",
                RegexNodeKind.Beginning => "Match if at the beginning of the string.",
                RegexNodeKind.Bol => "Match if at the beginning of a line.",
                RegexNodeKind.Boundary => $"Match if at a word boundary.",
                RegexNodeKind.Capture when node.M == -1 && node.N != -1 => $"Non-capturing balancing group. Uncaptures the {DescribeCapture(node.N, rm)}.",
                RegexNodeKind.Capture when node.N != -1 => $"Balancing group. Captures the {DescribeCapture(node.M, rm)} and uncaptures the {DescribeCapture(node.N, rm)}.",
                RegexNodeKind.Capture when node.N == -1 => $"{DescribeCapture(node.M, rm)}.",
                RegexNodeKind.Concatenate => "Match a sequence of expressions.",
                RegexNodeKind.ECMABoundary => $"Match if at a word boundary (according to ECMAScript rules).",
                RegexNodeKind.Empty => $"Match an empty string.",
                RegexNodeKind.End => "Match if at the end of the string.",
                RegexNodeKind.EndZ => "Match if at the end of the string or if before an ending newline.",
                RegexNodeKind.Eol => "Match if at the end of a line.",
                RegexNodeKind.Loop or RegexNodeKind.Lazyloop => node.M == 0 && node.N == 1 ? $"Optional ({(node.Kind is RegexNodeKind.Loop ? "greedy" : "lazy")})." : $"Loop {DescribeLoop(node, rm)}{direction}.",
                RegexNodeKind.Multi => $"Match the string {Literal(node.Str!)}{direction}.",
                RegexNodeKind.NonBoundary => $"Match if at anything other than a word boundary.",
                RegexNodeKind.NonECMABoundary => $"Match if at anything other than a word boundary (according to ECMAScript rules).",
                RegexNodeKind.Nothing => $"Fail to match.",
                RegexNodeKind.Notone => $"Match any character other than {Literal(node.Ch)}{direction}.",
                RegexNodeKind.Notoneloop or RegexNodeKind.Notoneloopatomic or RegexNodeKind.Notonelazy => $"Match a character other than {Literal(node.Ch)} {DescribeLoop(node, rm)}{direction}.",
                RegexNodeKind.One => $"Match {Literal(node.Ch)}{direction}.",
                RegexNodeKind.Oneloop or RegexNodeKind.Oneloopatomic or RegexNodeKind.Onelazy => $"Match {Literal(node.Ch)} {DescribeLoop(node, rm)}{direction}.",
                RegexNodeKind.NegativeLookaround => $"Zero-width negative {(rtl ? "lookbehind" : "lookahead")}.",
                RegexNodeKind.Backreference => $"Match the same text as matched by the {DescribeCapture(node.M, rm)}{direction}.",
                RegexNodeKind.PositiveLookaround => $"Zero-width positive {(rtl ? "lookbehind" : "lookahead")}.",
                RegexNodeKind.Set => $"Match {DescribeSet(node.Str!)}{direction}.",
                RegexNodeKind.Setloop or RegexNodeKind.Setloopatomic or RegexNodeKind.Setlazy => $"Match {DescribeSet(node.Str!)} {DescribeLoop(node, rm)}{direction}.",
                RegexNodeKind.Start => "Match if at the start position.",
                RegexNodeKind.ExpressionConditional => $"Conditionally match one of two expressions depending on whether an initial expression matches.",
                RegexNodeKind.BackreferenceConditional => $"Conditionally match one of two expressions depending on whether the {DescribeCapture(node.M, rm)} matched.",
                RegexNodeKind.UpdateBumpalong => $"Advance the next matching position.",
                _ => $"Unknown node type {node.Kind}",
            };
        }

        /// <summary>Gets an identifier to describe a capture group.</summary>
        private static string DescribeCapture(int capNum, RegexMethod rm)
        {
            // If we can get a capture name from the captures collection and it's not just a numerical representation of the group, use it.
            string name = RegexParser.GroupNameFromNumber(rm.Analysis.RegexTree.CaptureNumberSparseMapping, rm.Analysis.RegexTree.CaptureNames, rm.Analysis.RegexTree.CaptureCount, capNum);
            if (!string.IsNullOrEmpty(name) &&
                (!int.TryParse(name, out int id) || id != capNum))
            {
                name = Literal(name);
            }
            else
            {
                // Otherwise, create a numerical description of the capture group.
                int tens = capNum % 10;
                name = tens is >= 1 and <= 3 && capNum % 100 is < 10 or > 20 ? // Ends in 1, 2, 3 but not 11, 12, or 13
                    tens switch
                    {
                        1 => $"{capNum}st",
                        2 => $"{capNum}nd",
                        _ => $"{capNum}rd",
                    } :
                    $"{capNum}th";
            }

            return $"{name} capture group";
        }

        /// <summary>Gets a textual description of what characters match a set.</summary>
        private static string DescribeSet(string charClass) =>
            charClass switch
            {
                RegexCharClass.AnyClass => "any character",
                RegexCharClass.DigitClass => "a Unicode digit",
                RegexCharClass.ECMADigitClass => "'0' through '9'",
                RegexCharClass.ECMASpaceClass => "a whitespace character (ECMA)",
                RegexCharClass.ECMAWordClass => "a word character (ECMA)",
                RegexCharClass.NotDigitClass => "any character other than a Unicode digit",
                RegexCharClass.NotECMADigitClass => "any character other than '0' through '9'",
                RegexCharClass.NotECMASpaceClass => "any character other than a whitespace character (ECMA)",
                RegexCharClass.NotECMAWordClass => "any character other than a word character (ECMA)",
                RegexCharClass.NotSpaceClass => "any character other than a whitespace character",
                RegexCharClass.NotWordClass => "any character other than a word character",
                RegexCharClass.SpaceClass => "a whitespace character",
                RegexCharClass.WordClass => "a word character",
                _ => $"a character in the set {RegexCharClass.DescribeSet(charClass)}",
            };

        /// <summary>Writes a textual description of the node tree fit for rending in source.</summary>
        /// <param name="writer">The writer to which the description should be written.</param>
        /// <param name="node">The node being written.</param>
        /// <param name="prefix">The prefix to write at the beginning of every line, including a "//" for a comment.</param>
        /// <param name="analyses">Analysis of the tree</param>
        /// <param name="depth">The depth of the current node.</param>
        private static void DescribeExpressionAsXmlComment(TextWriter writer, RegexNode node, RegexMethod rm, int depth = 0)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                writer.WriteLine("/// ...");
                return;
            }

            do
            {
                bool skip = node.Kind switch
                {
                    // For concatenations, flatten the contents into the parent, but only if the parent isn't a form of alternation,
                    // where each branch is considered to be independent rather than a concatenation.
                    RegexNodeKind.Concatenate when node.Parent is not { Kind: RegexNodeKind.Alternate or RegexNodeKind.BackreferenceConditional or RegexNodeKind.ExpressionConditional } => true,

                    // For atomic, skip the node if we'll instead render the atomic label as part of rendering the child.
                    RegexNodeKind.Atomic when node.Child(0).Kind is RegexNodeKind.Loop or RegexNodeKind.Lazyloop or RegexNodeKind.Alternate => true,

                    // Skip nodes that are implementation details with no visible behavioral impact.
                    RegexNodeKind.UpdateBumpalong => true,

                    // Don't skip anything else.
                    _ => false,
                };

                if (!skip)
                {
                    string tag = node.Parent?.Kind switch
                    {
                        RegexNodeKind.ExpressionConditional when node.Parent.Child(0) == node => "Condition: ",
                        RegexNodeKind.ExpressionConditional when node.Parent.Child(1) == node => "Matched: ",
                        RegexNodeKind.ExpressionConditional when node.Parent.Child(2) == node => "Not Matched: ",

                        RegexNodeKind.BackreferenceConditional when node.Parent.Child(0) == node => "Matched: ",
                        RegexNodeKind.BackreferenceConditional when node.Parent.Child(1) == node => "Not Matched: ",

                        _ => "",
                    };

                    string nodeDescription = DescribeNode(node, rm);

                    // Write out the line for the node.
                    const char BulletPoint = '\u25CB';
                    writer.WriteLine($"/// {new string(' ', depth * 4)}{BulletPoint} {tag}{EscapeXmlComment(nodeDescription)}<br/>");
                }

                // Process each child.
                int childCount = node.ChildCount();
                if (childCount == 0)
                {
                    break;
                }

                if (!skip)
                {
                    depth++;
                }

                // Process all but the last child recursively, then loop around to process the last.
                for (int i = 0; i < childCount - 1; i++)
                {
                    DescribeExpressionAsXmlComment(writer, node.Child(i), rm, depth);
                }
                node = node.Child(childCount - 1);
            }
            while (true);
        }

        /// <summary>Gets a textual description of a loop's style and bounds.</summary>
        private static string DescribeLoop(RegexNode node, RegexMethod rm)
        {
            string style = node.Kind switch
            {
                _ when node.M == node.N => "exactly",
                RegexNodeKind.Oneloopatomic or RegexNodeKind.Notoneloopatomic or RegexNodeKind.Setloopatomic => "atomically",
                RegexNodeKind.Oneloop or RegexNodeKind.Notoneloop or RegexNodeKind.Setloop => "greedily",
                RegexNodeKind.Onelazy or RegexNodeKind.Notonelazy or RegexNodeKind.Setlazy => "lazily",
                RegexNodeKind.Loop => rm.Analysis.IsAtomicByAncestor(node) ? "greedily and atomically" : "greedily",
                _ /* RegexNodeKind.Lazyloop */ => rm.Analysis.IsAtomicByAncestor(node) ? "lazily and atomically" : "lazily",
            };

            string bounds =
                node.M == node.N ? $" {node.M} times" :
                (node.M, node.N) switch
                {
                    (0, int.MaxValue) => " any number of times",
                    (1, int.MaxValue) => " at least once",
                    (2, int.MaxValue) => " at least twice",
                    (_, int.MaxValue) => $" at least {node.M} times",
                    (0, 1) => ", optionally",
                    (0, _) => $" at most {node.N} times",
                    _ => $" at least {node.M} and at most {node.N} times"
                };

            return style + bounds;
        }

        private static FinishEmitBlock EmitBlock(IndentedTextWriter writer, string? clause, bool faux = false)
        {
            if (clause is not null)
            {
                writer.WriteLine(clause);
            }
            writer.WriteLine(faux ? "//{" : "{");
            writer.Indent++;
            return new FinishEmitBlock(writer, faux);
        }

        private static void EmitAdd(IndentedTextWriter writer, string variable, int value)
        {
            if (value == 0)
            {
                return;
            }

            writer.WriteLine(
                value == 1 ? $"{variable}++;" :
                value == -1 ? $"{variable}--;" :
                value > 0 ? $"{variable} += {value};" :
                value < 0 && value > int.MinValue ? $"{variable} -= {-value};" :
                $"{variable} += {value.ToString(CultureInfo.InvariantCulture)};");
        }

        private readonly struct FinishEmitBlock : IDisposable
        {
            private readonly IndentedTextWriter _writer;
            private readonly bool _faux;

            public FinishEmitBlock(IndentedTextWriter writer, bool faux)
            {
                _writer = writer;
                _faux = faux;
            }

            public void Dispose()
            {
                if (_writer is not null)
                {
                    _writer.Indent--;
                    _writer.WriteLine(_faux ? "//}" : "}");
                }
            }
        }
    }
}
