// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Many of these optimizations don't exist in .NET Framework.")]
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBuiltWithAggressiveTrimming))]
    public class RegexReductionTests
    {
        // These tests depend on using reflection to access internals of Regex in order to validate
        // if, when, and how various optimizations are being employed.  As implementation details
        // change, these tests will need to be updated as well.  Note, too, that Compiled Regexes
        // null out the _code field being accessed here, so this mechanism won't work to validate
        // Compiled, which also means it won't work to validate optimizations only enabled
        // when using Compiled, such as auto-atomicity for the last node in a regex.

        private static readonly FieldInfo s_regexCode;
        private static readonly FieldInfo s_regexCodeCodes;
        private static readonly FieldInfo s_regexCodeTree;
        private static readonly FieldInfo s_regexCodeFindOptimizations;
        private static readonly PropertyInfo s_regexCodeFindOptimizationsMaxPossibleLength;
        private static readonly FieldInfo s_regexCodeTreeMinRequiredLength;

        static RegexReductionTests()
        {
            if (PlatformDetection.IsNetFramework || PlatformDetection.IsBuiltWithAggressiveTrimming)
            {
                // These members may not exist or may have been trimmed away, and the tests won't run.
                return;
            }

            s_regexCode = typeof(Regex).GetField("_code", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(s_regexCode);

            s_regexCodeFindOptimizations = s_regexCode.FieldType.GetField("FindOptimizations", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(s_regexCodeFindOptimizations);

            s_regexCodeFindOptimizationsMaxPossibleLength = s_regexCodeFindOptimizations.FieldType.GetProperty("MaxPossibleLength", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(s_regexCodeFindOptimizationsMaxPossibleLength);

            s_regexCodeCodes = s_regexCode.FieldType.GetField("Codes", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(s_regexCodeCodes);

            s_regexCodeTree = s_regexCode.FieldType.GetField("Tree", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(s_regexCodeTree);

            s_regexCodeTreeMinRequiredLength = s_regexCodeTree.FieldType.GetField("MinRequiredLength", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(s_regexCodeTreeMinRequiredLength);
        }

        private static string GetRegexCodes(Regex r)
        {
            object code = s_regexCode.GetValue(r);
            Assert.NotNull(code);
            string result = code.ToString();

            // In release builds, the above ToString won't be informative.
            // Also include the numerical codes, which are not as comprehensive
            // but which exist in release builds as well.
            int[] codes = s_regexCodeCodes.GetValue(code) as int[];
            Assert.NotNull(codes);
            result += Environment.NewLine + string.Join(", ", codes);

            return result;
        }

        private static int GetMinRequiredLength(Regex r)
        {
            object code = s_regexCode.GetValue(r);
            Assert.NotNull(code);

            object tree = s_regexCodeTree.GetValue(code);
            Assert.NotNull(tree);

            object minRequiredLength = s_regexCodeTreeMinRequiredLength.GetValue(tree);
            Assert.IsType<int>(minRequiredLength);

            return (int)minRequiredLength;
        }

        private static int? GetMaxPossibleLength(Regex r)
        {
            object code = s_regexCode.GetValue(r);
            Assert.NotNull(code);

            object findOpts = s_regexCodeFindOptimizations.GetValue(code);
            Assert.NotNull(findOpts);

            object maxPossibleLength = s_regexCodeFindOptimizationsMaxPossibleLength.GetValue(findOpts);
            Assert.True(maxPossibleLength is null || maxPossibleLength is int);

            return (int?)maxPossibleLength;
        }

        [Theory]
        // Two greedy one loops
        [InlineData("a*a*", "a*")]
        [InlineData("(a*a*)", "(a*)")]
        [InlineData("a*(?:a*)", "a*")]
        [InlineData("a*a+", "a+")]
        [InlineData("a*a?", "a*")]
        [InlineData("a*a{1,3}", "a+")]
        [InlineData("a+a*", "a+")]
        [InlineData("a+a+", "a{2,}")]
        [InlineData("a+a?", "a+")]
        [InlineData("a+a{1,3}", "a{2,}")]
        [InlineData("a?a*", "a*")]
        [InlineData("a?a+", "a+")]
        [InlineData("a?a?", "a{0,2}")]
        [InlineData("a?a{1,3}", "a{1,4}")]
        [InlineData("a{1,3}a*", "a+")]
        [InlineData("a{1,3}a+", "a{2,}")]
        [InlineData("a{1,3}a?", "a{1,4}")]
        [InlineData("a{1,3}a{1,3}", "a{2,6}")]
        // Greedy one loop and one
        [InlineData("a*a", "a+")]
        [InlineData("a+a", "a{2,}")]
        [InlineData("a?a", "a{1,2}")]
        [InlineData("a{1,3}a", "a{2,4}")]
        [InlineData("aa*", "a+")]
        [InlineData("aa+", "a{2,}")]
        [InlineData("aa?", "a{1,2}")]
        [InlineData("aa{1,3}", "a{2,4}")]
        // Two atomic one loops
        [InlineData("(?>a*)(?>a*)", "(?>a*)")]
        [InlineData("(?>a*)(?>(?:a*))", "(?>a*)")]
        [InlineData("(?>a*)(?>a+)", "(?>a+)")]
        [InlineData("(?>a*)(?>a?)", "(?>a*)")]
        [InlineData("(?>a*)(?>a{1,3})", "(?>a+)")]
        [InlineData("(?>a+)(?>a*)", "(?>a+)")]
        [InlineData("(?>a+)(?>a+)", "(?>a{2,})")]
        [InlineData("(?>a+)(?>a?)", "(?>a+)")]
        [InlineData("(?>a+)(?>a{1,3})", "(?>a{2,})")]
        [InlineData("(?>a?)(?>a*)", "(?>a*)")]
        [InlineData("(?>a?)(?>a+)", "(?>a+)")]
        [InlineData("(?>a?)(?>a?)", "(?>a{0,2})")]
        [InlineData("(?>a?)(?>a{1,3})", "(?>a{1,4})")]
        [InlineData("(?>a{1,3})(?>a*)", "(?>a+)")]
        [InlineData("(?>a{1,3})(?>a+)", "(?>a{2,})")]
        [InlineData("(?>a{1,3})(?>a?)", "(?>a{1,4})")]
        [InlineData("(?>a{1,3})(?>a{1,3})", "(?>a{2,6})")]
        // Atomic one loop and one
        [InlineData("(?>a*)a", "(?>a+)")]
        [InlineData("(?>a+)a", "(?>a{2,})")]
        [InlineData("(?>a?)a", "(?>a{1,2})")]
        [InlineData("(?>a{1,3})a", "(?>a{2,4})")]
        [InlineData("a(?>a*)", "(?>a+)")]
        [InlineData("a(?>a+)", "(?>a{2,})")]
        [InlineData("a(?>a?)", "(?>a{1,2})")]
        [InlineData("a(?>a{1,3})", "(?>a{2,4})")]
        // Two lazy one loops
        [InlineData("a*?a*?", "a*?")]
        [InlineData("a*?a+?", "a+?")]
        [InlineData("a*?a??", "a*?")]
        [InlineData("a*?a{1,3}?", "a+?")]
        [InlineData("a+?a*?", "a+?")]
        [InlineData("a+?a+?", "a{2,}?")]
        [InlineData("a+?a??", "a+?")]
        [InlineData("a+?a{1,3}?", "a{2,}?")]
        [InlineData("a??a*?", "a*?")]
        [InlineData("a??a+?", "a+?")]
        [InlineData("a??a??", "a{0,2}?")]
        [InlineData("a??a{1,3}?", "a{1,4}?")]
        [InlineData("a{1,3}?a*?", "a+?")]
        [InlineData("a{1,3}?a+?", "a{2,}?")]
        [InlineData("a{1,3}?a??", "a{1,4}?")]
        [InlineData("a{1,3}?a{1,3}?", "a{2,6}?")]
        // Lazy one loop and one
        [InlineData("a*?a", "a+?")]
        [InlineData("a+?a", "a{2,}?")]
        [InlineData("a??a", "a{1,2}?")]
        [InlineData("a{1,3}?a", "a{2,4}?")]
        [InlineData("aa*?", "a+?")]
        [InlineData("aa+?", "a{2,}?")]
        [InlineData("aa??", "a{1,2}?")]
        [InlineData("aa{1,3}?", "a{2,4}?")]
        // Two greedy notone loops
        [InlineData("[^a]*[^a]*", "[^a]*")]
        [InlineData("[^a]*[^a]+", "[^a]+")]
        [InlineData("[^a]*[^a]?", "[^a]*")]
        [InlineData("[^a]*[^a]{1,3}", "[^a]+")]
        [InlineData("[^a]+[^a]*", "[^a]+")]
        [InlineData("[^a]+[^a]+", "[^a]{2,}")]
        [InlineData("[^a]+[^a]?", "[^a]+")]
        [InlineData("[^a]+[^a]{1,3}", "[^a]{2,}")]
        [InlineData("[^a]?[^a]*", "[^a]*")]
        [InlineData("[^a]?[^a]+", "[^a]+")]
        [InlineData("[^a]?[^a]?", "[^a]{0,2}")]
        [InlineData("[^a]?[^a]{1,3}", "[^a]{1,4}")]
        [InlineData("[^a]{1,3}[^a]*", "[^a]+")]
        [InlineData("[^a]{1,3}[^a]+", "[^a]{2,}")]
        [InlineData("[^a]{1,3}[^a]?", "[^a]{1,4}")]
        [InlineData("[^a]{1,3}[^a]{1,3}", "[^a]{2,6}")]
        // Two lazy notone loops
        [InlineData("[^a]*?[^a]*?", "[^a]*?")]
        [InlineData("[^a]*?[^a]+?", "[^a]+?")]
        [InlineData("[^a]*?[^a]??", "[^a]*?")]
        [InlineData("[^a]*?[^a]{1,3}?", "[^a]+?")]
        [InlineData("[^a]+?[^a]*?", "[^a]+?")]
        [InlineData("[^a]+?[^a]+?", "[^a]{2,}?")]
        [InlineData("[^a]+?[^a]??", "[^a]+?")]
        [InlineData("[^a]+?[^a]{1,3}?", "[^a]{2,}?")]
        [InlineData("[^a]??[^a]*?", "[^a]*?")]
        [InlineData("[^a]??[^a]+?", "[^a]+?")]
        [InlineData("[^a]??[^a]??", "[^a]{0,2}?")]
        [InlineData("[^a]??[^a]{1,3}?", "[^a]{1,4}?")]
        [InlineData("[^a]{1,3}?[^a]*?", "[^a]+?")]
        [InlineData("[^a]{1,3}?[^a]+?", "[^a]{2,}?")]
        [InlineData("[^a]{1,3}?[^a]??", "[^a]{1,4}?")]
        [InlineData("[^a]{1,3}?[^a]{1,3}?", "[^a]{2,6}?")]
        // Two atomic notone loops
        [InlineData("(?>[^a]*)(?>[^a]*)", "(?>[^a]*)")]
        [InlineData("(?>[^a]*)(?>[^a]+)", "(?>[^a]+)")]
        [InlineData("(?>[^a]*)(?>[^a]?)", "(?>[^a]*)")]
        [InlineData("(?>[^a]*)(?>[^a]{1,3})", "(?>[^a]+)")]
        [InlineData("(?>[^a]+)(?>[^a]*)", "(?>[^a]+)")]
        [InlineData("(?>[^a]+)(?>[^a]+)", "(?>[^a]{2,})")]
        [InlineData("(?>[^a]+)(?>[^a]?)", "(?>[^a]+)")]
        [InlineData("(?>[^a]+)(?>[^a]{1,3})", "(?>[^a]{2,})")]
        [InlineData("(?>[^a]?)(?>[^a]*)", "(?>[^a]*)")]
        [InlineData("(?>[^a]?)(?>[^a]+)", "(?>[^a]+)")]
        [InlineData("(?>[^a]?)(?>[^a]?)", "(?>[^a]{0,2})")]
        [InlineData("(?>[^a]?)(?>[^a]{1,3})", "(?>[^a]{1,4})")]
        [InlineData("(?>[^a]{1,3})(?>[^a]*)", "(?>[^a]+)")]
        [InlineData("(?>[^a]{1,3})(?>[^a]+)", "(?>[^a]{2,})")]
        [InlineData("(?>[^a]{1,3})(?>[^a]?)", "(?>[^a]{1,4})")]
        [InlineData("(?>[^a]{1,3})(?>[^a]{1,3})", "(?>[^a]{2,6})")]
        // Greedy notone loop and notone
        [InlineData("[^a]*[^a]", "[^a]+")]
        [InlineData("[^a]+[^a]", "[^a]{2,}")]
        [InlineData("[^a]?[^a]", "[^a]{1,2}")]
        [InlineData("[^a]{1,3}[^a]", "[^a]{2,4}")]
        [InlineData("[^a][^a]*", "[^a]+")]
        [InlineData("[^a][^a]+", "[^a]{2,}")]
        [InlineData("[^a][^a]?", "[^a]{1,2}")]
        [InlineData("[^a][^a]{1,3}", "[^a]{2,4}")]
        // Lazy notone loop and notone
        [InlineData("[^a]*?[^a]", "[^a]+?")]
        [InlineData("[^a]+?[^a]", "[^a]{2,}?")]
        [InlineData("[^a]??[^a]", "[^a]{1,2}?")]
        [InlineData("[^a]{1,3}?[^a]", "[^a]{2,4}?")]
        [InlineData("[^a][^a]*?", "[^a]+?")]
        [InlineData("[^a][^a]+?", "[^a]{2,}?")]
        [InlineData("[^a][^a]??", "[^a]{1,2}?")]
        [InlineData("[^a][^a]{1,3}?", "[^a]{2,4}?")]
        // Atomic notone loop and notone
        [InlineData("(?>[^a]*)[^a]", "(?>[^a]+)")]
        [InlineData("(?>[^a]+)[^a]", "(?>[^a]{2,})")]
        [InlineData("(?>[^a]?)[^a]", "(?>[^a]{1,2})")]
        [InlineData("(?>[^a]{1,3})[^a]", "(?>[^a]{2,4})")]
        [InlineData("[^a](?>[^a]*)", "(?>[^a]+)")]
        [InlineData("[^a](?>[^a]+)", "(?>[^a]{2,})")]
        [InlineData("[^a](?>[^a]?)", "(?>[^a]{1,2})")]
        [InlineData("[^a](?>[^a]{1,3})", "(?>[^a]{2,4})")]
        // Notone and notone
        [InlineData("[^a][^a]", "[^a]{2}")]
        // Two greedy set loops
        [InlineData("[0-9]*[0-9]*", "[0-9]*")]
        [InlineData("[0-9]*[0-9]+", "[0-9]+")]
        [InlineData("[0-9]*[0-9]?", "[0-9]*")]
        [InlineData("[0-9]*[0-9]{1,3}", "[0-9]+")]
        [InlineData("[0-9]+[0-9]*", "[0-9]+")]
        [InlineData("[0-9]+[0-9]+", "[0-9]{2,}")]
        [InlineData("[0-9]+[0-9]?", "[0-9]+")]
        [InlineData("[0-9]+[0-9]{1,3}", "[0-9]{2,}")]
        [InlineData("[0-9]?[0-9]*", "[0-9]*")]
        [InlineData("[0-9]?[0-9]+", "[0-9]+")]
        [InlineData("[0-9]?[0-9]?", "[0-9]{0,2}")]
        [InlineData("[0-9]?[0-9]{1,3}", "[0-9]{1,4}")]
        [InlineData("[0-9]{1,3}[0-9]*", "[0-9]+")]
        [InlineData("[0-9]{1,3}[0-9]+", "[0-9]{2,}")]
        [InlineData("[0-9]{1,3}[0-9]?", "[0-9]{1,4}")]
        [InlineData("[0-9]{1,3}[0-9]{1,3}", "[0-9]{2,6}")]
        // Greedy set loop and set
        [InlineData("[0-9]*[0-9]", "[0-9]+")]
        [InlineData("[0-9]+[0-9]", "[0-9]{2,}")]
        [InlineData("[0-9]?[0-9]", "[0-9]{1,2}")]
        [InlineData("[0-9]{1,3}[0-9]", "[0-9]{2,4}")]
        [InlineData("[0-9][0-9]*", "[0-9]+")]
        [InlineData("[0-9][0-9]+", "[0-9]{2,}")]
        [InlineData("[0-9][0-9]?", "[0-9]{1,2}")]
        [InlineData("[0-9][0-9]{1,3}", "[0-9]{2,4}")]
        // Atomic set loop and set
        [InlineData("(?>[0-9]*)[0-9]", "(?>[0-9]+)")]
        [InlineData("(?>[0-9]+)[0-9]", "(?>[0-9]{2,})")]
        [InlineData("(?>[0-9]?)[0-9]", "(?>[0-9]{1,2})")]
        [InlineData("(?>[0-9]{1,3})[0-9]", "(?>[0-9]{2,4})")]
        [InlineData("[0-9](?>[0-9]*)", "(?>[0-9]+)")]
        [InlineData("[0-9](?>[0-9]+)", "(?>[0-9]{2,})")]
        [InlineData("[0-9](?>[0-9]?)", "(?>[0-9]{1,2})")]
        [InlineData("[0-9](?>[0-9]{1,3})", "(?>[0-9]{2,4})")]
        // Two lazy set loops
        [InlineData("[0-9]*?[0-9]*?", "[0-9]*?")]
        [InlineData("[0-9]*?[0-9]+?", "[0-9]+?")]
        [InlineData("[0-9]*?[0-9]??", "[0-9]*?")]
        [InlineData("[0-9]*?[0-9]{1,3}?", "[0-9]+?")]
        [InlineData("[0-9]+?[0-9]*?", "[0-9]+?")]
        [InlineData("[0-9]+?[0-9]+?", "[0-9]{2,}?")]
        [InlineData("[0-9]+?[0-9]??", "[0-9]+?")]
        [InlineData("[0-9]+?[0-9]{1,3}?", "[0-9]{2,}?")]
        [InlineData("[0-9]??[0-9]*?", "[0-9]*?")]
        [InlineData("[0-9]??[0-9]+?", "[0-9]+?")]
        [InlineData("[0-9]??[0-9]??", "[0-9]{0,2}?")]
        [InlineData("[0-9]??[0-9]{1,3}?", "[0-9]{1,4}?")]
        [InlineData("[0-9]{1,3}?[0-9]*?", "[0-9]+?")]
        [InlineData("[0-9]{1,3}?[0-9]+?", "[0-9]{2,}?")]
        [InlineData("[0-9]{1,3}?[0-9]??", "[0-9]{1,4}?")]
        [InlineData("[0-9]{1,3}?[0-9]{1,3}?", "[0-9]{2,6}?")]
        // Two atomic set loops
        [InlineData("(?>[0-9]*)(?>[0-9]*)", "(?>[0-9]*)")]
        [InlineData("(?>[0-9]*)(?>[0-9]+)", "(?>[0-9]+)")]
        [InlineData("(?>[0-9]*)(?>[0-9]?)", "(?>[0-9]*)")]
        [InlineData("(?>[0-9]*)(?>[0-9]{1,3})", "(?>[0-9]+)")]
        [InlineData("(?>[0-9]+)(?>[0-9]*)", "(?>[0-9]+)")]
        [InlineData("(?>[0-9]+)(?>[0-9]+)", "(?>[0-9]{2,})")]
        [InlineData("(?>[0-9]+)(?>[0-9]?)", "(?>[0-9]+)")]
        [InlineData("(?>[0-9]+)(?>[0-9]{1,3})", "(?>[0-9]{2,})")]
        [InlineData("(?>[0-9]?)(?>[0-9]*)", "(?>[0-9]*)")]
        [InlineData("(?>[0-9]?)(?>[0-9]+)", "(?>[0-9]+)")]
        [InlineData("(?>[0-9]?)(?>[0-9]?)", "(?>[0-9]{0,2})")]
        [InlineData("(?>[0-9]?)(?>[0-9]{1,3})", "(?>[0-9]{1,4})")]
        [InlineData("(?>[0-9]{1,3})(?>[0-9]*)", "(?>[0-9]+)")]
        [InlineData("(?>[0-9]{1,3})(?>[0-9]+)", "(?>[0-9]{2,})")]
        [InlineData("(?>[0-9]{1,3})(?>[0-9]?)", "(?>[0-9]{1,4})")]
        [InlineData("(?>[0-9]{1,3})(?>[0-9]{1,3})", "(?>[0-9]{2,6})")]
        // Lazy set loop and set
        [InlineData("[0-9]*?[0-9]", "[0-9]+?")]
        [InlineData("[0-9]+?[0-9]", "[0-9]{2,}?")]
        [InlineData("[0-9]??[0-9]", "[0-9]{1,2}?")]
        [InlineData("[0-9]{1,3}?[0-9]", "[0-9]{2,4}?")]
        [InlineData("[0-9][0-9]*?", "[0-9]+?")]
        [InlineData("[0-9][0-9]+?", "[0-9]{2,}?")]
        [InlineData("[0-9][0-9]??", "[0-9]{1,2}?")]
        [InlineData("[0-9][0-9]{1,3}?", "[0-9]{2,4}?")]
        // Set and set
        [InlineData("[ace][ace]", "[ace]{2}")]
        // Set and one
        [InlineData("[a]", "a")]
        [InlineData("[a]*", "a*")]
        [InlineData("(?>[a]*)", "(?>a*)")]
        [InlineData("[a]*?", "a*?")]
        // Set and notone
        [InlineData("[^\n]", ".")]
        [InlineData("[^\n]*", ".*")]
        [InlineData("(?>[^\n]*)", "(?>.*)")]
        [InlineData("[^\n]*?", ".*?")]
        // Set reduction
        [InlineData("[\u0001-\uFFFF]", "[^\u0000]")]
        [InlineData("[\u0000-\uFFFE]", "[^\uFFFF]")]
        [InlineData("[\u0000-AB-\uFFFF]", "[\u0000-\uFFFF]")]
        [InlineData("[ABC-EG-J]", "[A-EG-J]")]
        [InlineData("[\u0000-AC-\uFFFF]", "[^B]")]
        [InlineData("[\u0000-AF-\uFFFF]", "[^B-E]")]
        // Large loop patterns
        [InlineData("a*a*a*a*a*a*a*b*b*?a+a*", "a*b*b*?a+")]
        [InlineData("a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?a?aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "a{0,30}aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        // Group elimination
        [InlineData("(?:(?:(?:(?:(?:(?:a*))))))", "a*")]
        // Nested loops
        [InlineData("(?:a*)*", "a*")]
        [InlineData("(?:a*)+", "a*")]
        [InlineData("(?:a+){4}", "a{4,}")]
        [InlineData("(?:a{1,2}){4}", "a{4,8}")]
        // Nested atomic
        [InlineData("(?>(?>(?>(?>abc*))))", "(?>ab(?>c*))")]
        [InlineData("(?>(?>(?>(?>))))", "")]
        [InlineData("(?>(?>(?>(?>(?!)))))", "(?!)")]
        [InlineData("(?=(?>))", "")]
        // Alternation reduction
        [InlineData("a|b", "[ab]")]
        [InlineData("a|b|c|d|e|g|h|z", "[a-eghz]")]
        [InlineData("a|b|c|def|g|h", "(?>[a-c]|def|[gh])")]
        [InlineData("this|that|there|then|those", "th(?>is|at|ere|en|ose)")]
        [InlineData("it's (?>this|that|there|then|those)", "it's (?>th(?>is|at|e(?>re|n)|ose))")]
        [InlineData("it's (?>this|that|there|then|those)!", "it's (?>th(?>is|at|e(?>re|n)|ose))!")]
        [InlineData("abcd|abce", "abc[de]")]
        [InlineData("abcd|abef", "ab(?>cd|ef)")]
        [InlineData("abcd|aefg", "a(?>bcd|efg)")]
        [InlineData("abcd|abc|ab|a", "a(?>bcd|bc|b|)")]
        [InlineData("abcde|abcdef", "abcde(?>|f)")]
        [InlineData("abcdef|abcde", "abcde(?>f|)")]
        [InlineData("abcdef|abcdeg|abcdeh|abcdei|abcdej|abcdek|abcdel", "abcde[f-l]")]
        [InlineData("(ab|ab*)bc", "(a(?:b|b*))bc")]
        [InlineData("abc(?:defgh|defij)klmn", "abcdef(?:gh|ij)klmn")]
        [InlineData("abc(defgh|defij)klmn", "abc(def(?:gh|ij))klmn")]
        [InlineData("a[b-f]|a[g-k]", "a[b-k]")]
        [InlineData("this|this", "this")]
        [InlineData("this|this|this", "this")]
        [InlineData("hello there|hello again|hello|hello|hello|hello", "hello(?> there| again|)")]
        [InlineData("hello there|hello again|hello|hello|hello|hello|hello world", "hello(?> there| again|)")]
        [InlineData("hello there|hello again|hello|hello|hello|hello|hello world|hello", "hello(?> there| again|)")]
        [InlineData("ab|cd|||ef", "ab|cd|")]
        [InlineData("|ab|cd|e||f", "")]
        [InlineData("ab|cd|||||||||||ef", "ab|cd|")]
        [InlineData("ab|cd|||||||||||e||f|||", "ab|cd|")]
        [InlineData("ab|cd|(?!)|ef", "ab|cd|ef")]
        [InlineData("abcd(?:(?i:e)|(?i:f))", "abcd(?i:[ef])")]
        [InlineData("(?i:abcde)|(?i:abcdf)", "(?i:abcd[ef])")]
        [InlineData("xyz(?:(?i:abcde)|(?i:abcdf))", "xyz(?i:abcd[ef])")]
        [InlineData("bonjour|hej|ciao|shalom|zdravo|pozdrav|hallo|hola|hello|hey|witam|tere|bonjou|salam|helo|sawubona", "(?>bonjou(?>r|)|h(?>e(?>j|(?>l(?>lo|o)|y))|allo|ola)|ciao|s(?>halom|a(?>lam|wubona))|zdravo|pozdrav|witam|tere)")]
        [InlineData("\\w\\d123|\\w\\dabc", "\\w\\d(?:123|abc)")]
        [InlineData("(a)(?(1)b)", "(a)(?(1)b|)")]
        [InlineData("(abc)(?(1)def)", "(abc)(?(1)def|)")]
        [InlineData("(?(a)a)", "(?(a)a|)")]
        [InlineData("(?(abc)def)", "(?(abc)def|)")]
        [InlineData("(?(\\w)\\d)", "(?(\\w)\\d|)")]
        // Loops inside alternation constructs
        [InlineData("(abc*|def)ghi", "(ab(?>c*)|def)ghi")]
        [InlineData("(abc|def*)ghi", "(abc|de(?>f*))ghi")]
        [InlineData("(abc*|def*)ghi", "(ab(?>c*)|de(?>f*))ghi")]
        [InlineData("(abc*|def*)", "(ab(?>c*)|de(?>f*))")]
        [InlineData("(?(\\w)abc*|def*)ghi", "(?(\\w)ab(?>c*)|de(?>f*))ghi")]
        [InlineData("(?(\\w)abc*|def*)", "(?(\\w)ab(?>c*)|de(?>f*))")]
        [InlineData("(?(xyz*)abc|def)", "(?(xy(?>z*))abc|def)")]
        [InlineData("(?(xyz*)abc|def)\\w", "(?(xy(?>z*))abc|def)\\w")]
        // Loops followed by alternation constructs
        [InlineData("a*(bcd|efg)", "(?>a*)(bcd|efg)")]
        [InlineData("a*(?(xyz)bcd|efg)", "(?>a*)(?(xyz)bcd|efg)")]
        // Auto-atomicity
        [InlineData("a*b", "(?>a*)b")]
        [InlineData("a*b+", "(?>a*)(?>b+)")]
        [InlineData("a*b*", "(?>a*)(?>b*)")]
        [InlineData("a*b+c*", "(?>a*)(?>b+)(?>c*)")]
        [InlineData("a*b*c*", "(?>a*)(?>b*)(?>c*)")]
        [InlineData("a*b*c*|d*[ef]*", "(?>a*)(?>b*)(?>c*)|(?>d*)(?>[ef]*)")]
        [InlineData("(a*)(b*)(c*)", "((?>a*))((?>b*))((?>c*))")]
        [InlineData("a*b{3,4}", "(?>a*)(?>b{3,4})")]
        [InlineData("[ab]*[^a]*", "[ab]*(?>[^a]*)")]
        [InlineData("[aa]*[^a]*", "(?>a*)(?>[^a]*)")]
        [InlineData("a??", "")]
        [InlineData("(abc*?)", "(ab)")]
        [InlineData("a{1,3}?", "a{1,4}?")]
        [InlineData("a{2,3}?", "a{2}")]
        [InlineData("bc(a){1,3}?", "bc(a){1,2}?")]
        [InlineData("c{3,}?|f{2,}?", "c{3}|f{2}")]
        [InlineData("[a-z]*[\x0000-\xFFFF]+", "[a-z]*(?>[\x0000-\xFFFF]+)")]
        [InlineData("a+b", "(?>a+)b")]
        [InlineData("a?b", "(?>a?)b")]
        [InlineData("[^\n]*\n", "(?>[^\n]*)\n")]
        [InlineData("[^\n]*\n+", "(?>[^\n]*)(?>\n+)")]
        [InlineData("(a+)b", "((?>a+))b")]
        [InlineData("a*(?:bcd|efg)", "(?>a*)(?:bcd|efg)")]
        [InlineData("\\w*\\b", "(?>\\w*)\\b")]
        [InlineData("\\d*\\b", "(?>\\d*)\\b")]
        [InlineData("(?:abc*|def*)g", "(?:ab(?>c*)|de(?>f*))g")]
        [InlineData("(?:a[ce]*|b*)g", "(?:a(?>[ce]*)|(?>b*))g")]
        [InlineData("(?:a[ce]*|b*)c", "(?:a[ce]*|(?>b*))c")]
        [InlineData("apple|(?:orange|pear)|grape", "apple|orange|pear|grape")]
        [InlineData("(?:abc)*", "(?>(?>(?>(?:abc)*)))")]
        [InlineData("(?:w*)+", "(?>(?>w*)+)")]
        [InlineData("(?:w*)+\\.", "(?>w*)+\\.")]
        [InlineData("(a[bcd]e*)*fg", "(a[bcd](?>e*))*fg")]
        [InlineData("(\\w[bcd]\\s*)*fg", "(\\w[bcd](?>\\s*))*fg")]
        // IgnoreCase set creation
        [InlineData("(?i)abcd", "[Aa][Bb][Cc][Dd]")]
        [InlineData("(?i)abcd|efgh", "[Aa][Bb][Cc][Dd]|[Ee][Ff][Gg][Hh]")]
        [InlineData("(?i)a|b", "[AaBb]")]
        [InlineData("(?i)[abcd]", "[AaBbCcDd]")]
        [InlineData("(?i)[acexyz]", "[AaCcEeXxYyZz]")]
        [InlineData("(?i)\\w", "\\w")]
        [InlineData("(?i)\\d", "\\d")]
        [InlineData("(?i).", ".")]
        [InlineData("(?i)\\$", "\\$")]
        public void PatternsReduceIdentically(string pattern1, string pattern2)
        {
            string result1 = GetRegexCodes(new Regex(pattern1));
            string result2 = GetRegexCodes(new Regex(pattern2));
            if (result1 != result2)
            {
                throw new Xunit.Sdk.EqualException(result2, result1);
            }
        }

        [Theory]
        // Not coalescing loops
        [InlineData("aa", "a{2}")]
        [InlineData("a[^a]", "a{2}")]
        [InlineData("[^a]a", "[^a]{2}")]
        [InlineData("a*b*", "a*")]
        [InlineData("a*b*", "b*")]
        [InlineData("[^a]*[^b]", "[^a]*")]
        [InlineData("[ace]*[acd]", "[ace]*")]
        [InlineData("a+b+", "a+")]
        [InlineData("a+b+", "b+")]
        [InlineData("a*(a*)", "a*")]
        [InlineData("(a*)a*", "a*")]
        [InlineData("a*(?>a*)", "a*")]
        [InlineData("a*a*?", "a*")]
        [InlineData("a*?a*", "a*")]
        [InlineData("a*[^a]*", "a*")]
        [InlineData("[^a]*a*", "a*")]
        [InlineData("a{2147483646}a", "a{2147483647}")]
        [InlineData("a{2147483647}a", "a{2147483647}")]
        [InlineData("a{0,2147483646}a", "a{0,2147483647}")]
        [InlineData("aa{2147483646}", "a{2147483647}")]
        [InlineData("aa{0,2147483646}", "a{0,2147483647}")]
        [InlineData("a{2147482647}a{1000}", "a{2147483647}")]
        [InlineData("a{0,2147482647}a{0,1000}", "a{0,2147483647}")]
        [InlineData("[^a]{2147483646}[^a]", "[^a]{2147483647}")]
        [InlineData("[^a]{2147483647}[^a]", "[^a]{2147483647}")]
        [InlineData("[^a]{0,2147483646}[^a]", "[^a]{0,2147483647}")]
        [InlineData("[^a][^a]{2147483646}", "[^a]{2147483647}")]
        [InlineData("[^a][^a]{0,2147483646}", "[^a]{0,2147483647}")]
        [InlineData("[^a]{2147482647}[^a]{1000}", "[^a]{2147483647}")]
        [InlineData("[^a]{0,2147482647}[^a]{0,1000}", "[^a]{0,2147483647}")]
        [InlineData("[ace]{2147483646}[ace]", "[ace]{2147483647}")]
        [InlineData("[ace]{2147483647}[ace]", "[ace]{2147483647}")]
        [InlineData("[ace]{0,2147483646}[ace]", "[ace]{0,2147483647}")]
        [InlineData("[ace][ace]{2147483646}", "[ace]{2147483647}")]
        [InlineData("[ace][ace]{0,2147483646}", "[ace]{0,2147483647}")]
        [InlineData("[ace]{2147482647}[ace]{1000}", "[ace]{2147483647}")]
        [InlineData("[ace]{0,2147482647}[ace]{0,1000}", "[ace]{0,2147483647}")]
        // Not reducing branches of alternations with different casing
        [InlineData("(?i:abcd)|abcd", "abcd|abcd")]
        [InlineData("abcd|(?i:abcd)", "abcd|abcd")]
        // Not applying auto-atomicity
        [InlineData(@"(a*|b*)\w*", @"((?>a*)|(?>b*))\w*")]
        [InlineData("[ab]*[^a]", "(?>[ab]*)[^a]")]
        [InlineData("[ab]*[^a]*", "(?>[ab]*)[^a]*")]
        [InlineData("[ab]*[^a]*?", "(?>[ab]*)[^a]*?")]
        [InlineData("[ab]*(?>[^a]*)", "(?>[ab]*)(?>[^a]*)")]
        [InlineData("[^\n]*\n*", "(?>[^\n]*)\n")]
        [InlineData("(a[bcd]a*)*fg", "(a[bcd](?>a*))*fg")]
        [InlineData("(\\w[bcd]\\d*)*fg", "(\\w[bcd](?>\\d*))*fg")]
        [InlineData("a*(?<=[^a])b", "(?>a*)(?<=[^a])b")]
        [InlineData("[\x0000-\xFFFF]*[a-z]", "(?>[\x0000-\xFFFF]*)[a-z]")]
        [InlineData("[a-z]*[\x0000-\xFFFF]+", "(?>[a-z]*)[\x0000-\xFFFF]+")]
        [InlineData("[^a-c]*[e-g]", "(?>[^a-c]*)[e-g]")]
        [InlineData("[^a-c]*[^e-g]", "(?>[^a-c]*)[^e-g]")]
        [InlineData("(w+)+", "((?>w+))+")]
        [InlineData("(w{1,2})+", "((?>w{1,2}))+")]
        [InlineData("(?:ab|cd|ae)f", "(?>ab|cd|ae)f")]
        // Loops inside alternation constructs
        [InlineData("(abc*|def)chi", "(ab(?>c*)|def)chi")]
        [InlineData("(abc|def*)fhi", "(abc|de(?>f*))fhi")]
        [InlineData("(abc*|def*)\\whi", "(ab(?>c*)|de(?>f*))\\whi")]
        [InlineData("(?(\\w)abc*|def*)\\whi", "(?(\\w)ab(?>c*)|de(?>f*))\\whi")]
        // Loops followed by alternation constructs
        [InlineData("a*(bcd|afg)", "(?>a*)(bcd|afg)")]
        [InlineData("(a*)(?(1)bcd|efg)", "((?>a*))(?(1)bcd|efg)")]
        [InlineData("a*(?(abc)bcd|efg)", "(?>a*)(?(abc)bcd|efg)")]
        [InlineData("a*(?(xyz)acd|efg)", "(?>a*)(?(xyz)acd|efg)")]
        [InlineData("a*(?(xyz)bcd|afg)", "(?>a*)(?(xyz)bcd|afg)")]
        [InlineData("a*(?(xyz)bcd)", "(?>a*)(?(xyz)bcd)")]
        public void PatternsReduceDifferently(string pattern1, string pattern2)
        {
            string result1 = GetRegexCodes(new Regex(pattern1));
            string result2 = GetRegexCodes(new Regex(pattern2));
            if (result1 == result2)
            {
                throw new Xunit.Sdk.EqualException(result2, result1);
            }
        }

        [Theory]
        [InlineData(@"a", RegexOptions.None, 1, 1)]
        [InlineData(@"[^a]", RegexOptions.None, 1, 1)]
        [InlineData(@"[abcdefg]", RegexOptions.None, 1, 1)]
        [InlineData(@"abcd", RegexOptions.None, 4, 4)]
        [InlineData(@"a*", RegexOptions.None, 0, null)]
        [InlineData(@"a*?", RegexOptions.None, 0, null)]
        [InlineData(@"a?", RegexOptions.None, 0, 1)]
        [InlineData(@"a??", RegexOptions.None, 0, 1)]
        [InlineData(@"a+", RegexOptions.None, 1, null)]
        [InlineData(@"a+?", RegexOptions.None, 1, null)]
        [InlineData(@"a{2}", RegexOptions.None, 2, 2)]
        [InlineData(@"a{2}?", RegexOptions.None, 2, 2)]
        [InlineData(@"a{3,17}", RegexOptions.None, 3, 17)]
        [InlineData(@"a{3,17}?", RegexOptions.None, 3, 17)]
        [InlineData(@"[^a]{3,17}", RegexOptions.None, 3, 17)]
        [InlineData(@"[^a]{3,17}?", RegexOptions.None, 3, 17)]
        [InlineData(@"(abcd){5}", RegexOptions.None, 20, 20)]
        [InlineData(@"(abcd|ef){2,6}", RegexOptions.None, 4, 24)]
        [InlineData(@"abcef|de", RegexOptions.None, 2, 5)]
        [InlineData(@"abc(def|ghij)k", RegexOptions.None, 7, 8)]
        [InlineData(@"abc(def|ghij|k||lmnopqrs|t)u", RegexOptions.None, 4, 12)]
        [InlineData(@"(ab)c(def|ghij|k|l|\1|m)n", RegexOptions.None, 4, null)]
        [InlineData(@"abc|de*f|ghi", RegexOptions.None, 2, null)]
        [InlineData(@"abc|de+f|ghi", RegexOptions.None, 3, null)]
        [InlineData(@"abc|(def)+|ghi", RegexOptions.None, 3, null)]
        [InlineData(@"(abc)+|def", RegexOptions.None, 3, null)]
        [InlineData(@"\d{1,2}-\d{1,2}-\d{2,4}", RegexOptions.None, 6, 10)]
        [InlineData(@"\d{1,2}-(?>\d{1,2})-\d{2,4}", RegexOptions.None, 6, 10)]
        [InlineData(@"1(?=9)\d", RegexOptions.None, 2, 2)]
        [InlineData(@"1(?!\d)\w", RegexOptions.None, 2, 2)]
        [InlineData(@"a*a*a*a*a*a*a*b*", RegexOptions.None, 0, null)]
        [InlineData(@"((a{1,2}){4}){3,7}", RegexOptions.None, 12, 56)]
        [InlineData(@"((a{1,2}){4}?){3,7}", RegexOptions.None, 12, 56)]
        [InlineData(@"\b\w{4}\b", RegexOptions.None, 4, 4)]
        [InlineData(@"\b\w{4}\b", RegexOptions.ECMAScript,  4, 4)]
        [InlineData(@"abcd(?=efgh)efgh", RegexOptions.None, 8, 8)]
        [InlineData(@"abcd(?<=cd)efgh", RegexOptions.None, 8, 8)]
        [InlineData(@"abcd(?!ab)efgh", RegexOptions.None, 8, 8)]
        [InlineData(@"abcd(?<!ef)efgh", RegexOptions.None, 8, 8)]
        [InlineData(@"(a{1073741824}){2}", RegexOptions.None, 2147483646, null)] // min length max is bound to int.MaxValue - 1 for convenience in other places where we need to be able to add 1 without risk of overflow
        [InlineData(@"a{1073741824}b{1073741824}", RegexOptions.None, 2147483646, null)]
        [InlineData(@"((((((((((((((((((((((((((((((ab|cd+)|ef+)|gh+)|ij+)|kl+)|mn+)|op+)|qr+)|st+)|uv+)|wx+)|yz+)|01+)|23+)|45+)|67+)|89+)|AB+)|CD+)|EF+)|GH+)|IJ+)|KL+)|MN+)|OP+)|QR+)|ST+)|UV+)|WX+)|YZ)", RegexOptions.None, 2, null)]
        [InlineData(@"(YZ+|(WX+|(UV+|(ST+|(QR+|(OP+|(MN+|(KL+|(IJ+|(GH+|(EF+|(CD+|(AB+|(89+|(67+|(45+|(23+|(01+|(yz+|(wx+|(uv+|(st+|(qr+|(op+|(mn+|(kl+|(ij+|(gh+|(ef+|(de+|(a|bc+)))))))))))))))))))))))))))))))", RegexOptions.None, 1, null)]
        [InlineData(@"a(a(a(a(a(a(a(a(a(a(a(a(a(a(a(a(a(a(a(a(a(a(a(a(a(a(a(a(a(a(ab|cd+)|ef+)|gh+)|ij+)|kl+)|mn+)|op+)|qr+)|st+)|uv+)|wx+)|yz+)|01+)|23+)|45+)|67+)|89+)|AB+)|CD+)|EF+)|GH+)|IJ+)|KL+)|MN+)|OP+)|QR+)|ST+)|UV+)|WX+)|YZ+)", RegexOptions.None, 3, null)]
        [InlineData(@"(((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((a)))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))", RegexOptions.None, 1, 1)]
        [InlineData(@"(?(\d)\d{3}|\d)", RegexOptions.None, 1, 3)]
        [InlineData(@"(?(\d{7})\d{3}|\d{2})", RegexOptions.None, 2, 3)]
        [InlineData(@"(?(\d{7})\d{2}|\d{3})", RegexOptions.None, 2, 3)]
        [InlineData(@"(?(\d)\d{3}|\d{2})", RegexOptions.None, 2, 3)]
        [InlineData(@"(?(\d)|\d{2})", RegexOptions.None, 0, 2)]
        [InlineData(@"(?(\d)\d{3})", RegexOptions.None, 0, 3)]
        [InlineData(@"(abc)(?(1)\d{3}|\d{2})", RegexOptions.None, 5, 6)]
        [InlineData(@"(abc)(?(1)\d{2}|\d{3})", RegexOptions.None, 5, 6)]
        [InlineData(@"(abc)(?(1)|\d{2})", RegexOptions.None, 3, 5)]
        [InlineData(@"(abc)(?(1)\d{3})", RegexOptions.None, 3, 6)]
        [InlineData(@"(abc|)", RegexOptions.None, 0, 3)]
        [InlineData(@"(|abc)", RegexOptions.None, 0, 3)]
        [InlineData(@"(?(x)abc|)", RegexOptions.None, 0, 3)]
        [InlineData(@"(?(x)|abc)", RegexOptions.None, 0, 3)]
        [InlineData(@"(?(x)|abc)^\A\G\z\Z$", RegexOptions.None, 0, 3)]
        [InlineData(@"(?(x)|abc)^\A\G\z$\Z", RegexOptions.Multiline, 0, 3)]
        [InlineData(@"^\A\Gabc", RegexOptions.None, 3, null)] // leading anchor currently prevents ComputeMaxLength from being invoked, as it's not needed
        [InlineData(@"^\A\Gabc", RegexOptions.Multiline, 3, null)]
        [InlineData(@"abc            def", RegexOptions.IgnorePatternWhitespace, 6, 6)]
        [InlineData(@"abcdef", RegexOptions.RightToLeft, 6, null)]
        public void MinMaxLengthIsCorrect(string pattern, RegexOptions options, int expectedMin, int? expectedMax)
        {
            var r = new Regex(pattern, options);
            Assert.Equal(expectedMin, GetMinRequiredLength(r));
            if (!pattern.EndsWith("$", StringComparison.Ordinal) &&
                !pattern.EndsWith(@"\Z", StringComparison.OrdinalIgnoreCase))
            {
                // MaxPossibleLength is currently only computed/stored if there's a trailing End{Z} anchor as the max length is otherwise unused
                r = new Regex($"(?:{pattern})$", options);
            }
            Assert.Equal(expectedMax, GetMaxPossibleLength(r));
        }

        [Fact]
        public void MinMaxLengthIsCorrect_HugeDepth()
        {
            const int Depth = 10_000;
            var r = new Regex($"{new string('(', Depth)}a{new string(')', Depth)}$"); // too deep for analysis on some platform default stack sizes

            int minRequiredLength = GetMinRequiredLength(r);
            Assert.True(
                minRequiredLength == 1 /* successfully analyzed */ || minRequiredLength == 0 /* ran out of stack space to complete analysis */,
                $"Expected 1 or 0, got {minRequiredLength}");

            int? maxPossibleLength = GetMaxPossibleLength(r);
            Assert.True(
                maxPossibleLength == 1 /* successfully analyzed */ || maxPossibleLength is null /* ran out of stack space to complete analysis */,
                $"Expected 1 or null, got {maxPossibleLength}");
        }
    }
}
