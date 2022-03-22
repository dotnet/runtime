// Licensed to the .NET Foundation under one or more agreements.if #DEBUG
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.RegularExpressions.Tests
{
    /// <summary>
    /// This class is to be ignored wrt unit tests in Release mode.
    /// It contains temporary experimental code, such as lightweight profiling and debuggging locally.
    /// Set <see cref="Enabled"/> to true to run all the tests.
    /// </summary>
    public class RegexExperiment
    {
        private readonly ITestOutputHelper _output;

        public RegexExperiment(ITestOutputHelper output) => _output = output;

        public static bool Enabled => false;

        /// <summary>Temporary local output directory for experiment results.</summary>
        private static readonly string s_tmpWorkingDir = Path.GetTempPath();

        /// <summary>Works as a console.</summary>
        private static string OutputFilePath => Path.Combine(s_tmpWorkingDir, "vsoutput.txt");

        /// <summary>Output directory for generated dgml files.</summary>
        private static string DgmlOutputDirectoryPath => Path.Combine(s_tmpWorkingDir, "dgml");

        [Fact]
        public void RegenerateUnicodeTables()
        {
            if (!Enabled)
            {
                return;
            }

            MethodInfo? genUnicode = typeof(Regex).GetMethod("GenerateUnicodeTables", BindingFlags.NonPublic | BindingFlags.Static);
            // GenerateUnicodeTables is not available in Release build
            if (genUnicode is not null)
            {
                genUnicode.Invoke(null, new object[] { s_tmpWorkingDir });
            }
        }

        private static long MeasureMatchTime(Regex re, string input, out Match match)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                match = re.Match(input);
                return sw.ElapsedMilliseconds;
            }
            catch (RegexMatchTimeoutException)
            {
                match = Match.Empty;
                return -1;
            }
            catch (Exception)
            {
                match = Match.Empty;
                return -2;
            }
        }

        /// <summary>
        /// Creates a regex that in the NonBacktracking engine in DEBUG mode represents intersection of regexes
        /// </summary>
        private static string And(params string[] regexes)
        {
            string conj = $"(?:{regexes[regexes.Length - 1]})";
            for (int i = regexes.Length - 2; i >= 0; i--)
            {
                conj = $"(?({regexes[i]}){conj}|[0-[0]])";
            }

            return conj;
        }

        /// <summary>
        /// Creates a regex that in the NonBacktracking engine in DEBUG mode represents complement of regex
        /// </summary>
        private static string Not(string regex) => $"(?({regex})[0-[0]]|.*)";

        /// <summary>
        /// When <see cref="Enabled"/> is set to return true, outputs DGML diagrams for the specified pattern.
        /// This is useful for understanding what graphs the NonBacktracking engine creates for the specified pattern.
        /// </summary>
        [Fact]
        public void ViewSampleRegexInDGML()
        {
            if (!Enabled)
            {
                return;
            }

            if (!Directory.Exists(DgmlOutputDirectoryPath))
            {
                Directory.CreateDirectory(DgmlOutputDirectoryPath);
            }

            try
            {
                /*lang=regex*/
                string pattern = @"abc|cd";

                ViewDGML(pattern, "DFA");
                ViewDGML(pattern, "DFA_DotStar", addDotStar: true);

                ViewDGML(pattern, "NFA", nfa: true, maxStates: 12);
                ViewDGML(pattern, "NFA_DotStar", nfa: true, addDotStar: true, maxStates: 12);

                static void ViewDGML(string pattern, string name, bool nfa = false, bool addDotStar = false, bool reverse = false, int maxStates = -1, int maxLabelLength = 20)
                {
                    var regex = new Regex(pattern, RegexHelpers.RegexOptionNonBacktracking | RegexOptions.Singleline);
                    if (regex.GetType().GetMethod("SaveDGML", BindingFlags.NonPublic | BindingFlags.Instance) is MethodInfo saveDgml)
                    {
                        var sw = new StringWriter();
                        saveDgml.Invoke(regex, new object[] { sw, nfa, addDotStar, reverse, maxStates, maxLabelLength });
                        string path = Path.Combine(DgmlOutputDirectoryPath, $"{name}.dgml");
                        File.WriteAllText(path, sw.ToString());
                        Console.WriteLine(path);
                    }
                }
            }
            catch (NotSupportedException e) when (e.Message.Contains("conditional"))
            {
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        [InlineData(".*a+", -1, new string[] { ".*a+" }, false, false)]
        [InlineData("ann", -1, new string[] { "nna" }, true, false)]
        [InlineData("(something|otherstuff)+", 10, new string[] { "Unexplored", "some" }, false, true)]
        [InlineData("(something|otherstuff)+", 10, new string[] { "Unexplored", "ffut" }, true, true)]
        public void TestDGMLGeneration(string pattern, int explorationbound, string[] expectedDgmlFragments, bool exploreInReverse, bool exploreAsNFA)
        {
            StringWriter sw = new StringWriter();
            var re = new Regex(pattern, RegexHelpers.RegexOptionNonBacktracking | RegexOptions.Singleline);
            if (TrySaveDGML(re, sw, exploreAsNFA, addDotStar: false, exploreInReverse, explorationbound, maxLabelLength: -1))
            {
                string str = sw.ToString();
                Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", str);
                Assert.Contains("DirectedGraph", str);
                foreach (string fragment in expectedDgmlFragments)
                {
                    Assert.Contains(fragment, str);
                }
            }

            static bool TrySaveDGML(Regex regex, TextWriter writer, bool nfa, bool addDotStar, bool reverse, int maxStates, int maxLabelLength)
            {
                MethodInfo saveDgml = regex.GetType().GetMethod("SaveDGML", BindingFlags.NonPublic | BindingFlags.Instance);
                if (saveDgml is not null)
                {
                    saveDgml.Invoke(regex, new object[] { writer, nfa, addDotStar, reverse, maxStates, maxLabelLength });
                    return true;
                }

                return false;
            }
        }

        #region Tests involving Intersection and Complement
        // Currently only run in DEBUG mode in the NonBacktracking engine
        //[ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        private void SRMTest_ConjuctionIsMatch()
        {
            try
            {
                var re = new Regex(And(".*a.*", ".*b.*"), RegexHelpers.RegexOptionNonBacktracking | RegexOptions.Singleline | RegexOptions.IgnoreCase);
                bool ok = re.IsMatch("xxaaxxBxaa");
                Assert.True(ok);
                bool fail = re.IsMatch("xxaaxxcxaa");
                Assert.False(fail);
            }
            catch (NotSupportedException e)
            {
                // In Release build (?( test-pattern ) yes-pattern | no-pattern ) is not supported
                Assert.Contains("conditional", e.Message);
            }
        }

        //[ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        private void SRMTest_ConjuctionFindMatch()
        {
            try
            {
                // contains lower, upper, and a digit, and is between 2 and 4 characters long
                var re = new Regex(And(".*[a-z].*", ".*[A-Z].*", ".*[0-9].*", ".{2,4}"), RegexHelpers.RegexOptionNonBacktracking | RegexOptions.Singleline);
                var match = re.Match("xxaac\n5Bxaa");
                Assert.True(match.Success);
                Assert.Equal(4, match.Index);
                Assert.Equal(4, match.Length);
            }
            catch (NotSupportedException e)
            {
                // In Release build (?( test-pattern ) yes-pattern | no-pattern ) is not supported
                Assert.Contains("conditional", e.Message);
            }
        }

        //[ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        private void SRMTest_ComplementFindMatch()
        {
            try
            {
                // contains lower, upper, and a digit, and is between 4 and 8 characters long, does not contain 2 consequtive digits
                var re = new Regex(And(".*[a-z].*", ".*[A-Z].*", ".*[0-9].*", ".{4,8}",
                    Not(".*(?:01|12|23|34|45|56|67|78|89).*")), RegexHelpers.RegexOptionNonBacktracking | RegexOptions.Singleline);
                var match = re.Match("xxaac12Bxaas3455");
                Assert.True(match.Success);
                Assert.Equal(6, match.Index);
                Assert.Equal(7, match.Length);
            }
            catch (NotSupportedException e)
            {
                // In Release build (?( test-pattern ) yes-pattern | no-pattern ) is not supported
                Assert.Contains("conditional", e.Message);
            }
        }

        //[ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        private void PasswordSearch()
        {
            try
            {
                string twoLower = ".*[a-z].*[a-z].*";
                string twoUpper = ".*[A-Z].*[A-Z].*";
                string threeDigits = ".*[0-9].*[0-9].*[0-9].*";
                string oneSpecial = @".*[\x21-\x2F\x3A-\x40\x5B-x60\x7B-\x7E].*";
                string Not_countUp = Not(".*(?:012|123|234|345|456|567|678|789).*");
                string Not_countDown = Not(".*(?:987|876|765|654|543|432|321|210).*");
                // Observe that the space character (immediately before '!' in ASCII) is excluded
                string length = "[!-~]{8,12}";

                // Just to make the chance that the randomly generated part actually has a match
                // be astronomically unlikely require 'X' and 'r' to be present also,
                // although this constraint is really bogus from password constraints point of view
                string contains_first_P_and_then_r = ".*X.*r.*";

                // Conjunction of all the above constraints
                string all = And(twoLower, twoUpper, threeDigits, oneSpecial, Not_countUp, Not_countDown, length, contains_first_P_and_then_r);

                // search for the password in a context surrounded by word boundaries
                Regex re = new Regex($@"\b{all}\b", RegexHelpers.RegexOptionNonBacktracking | RegexOptions.Singleline);

                // Does not qualify because of 123 and connot end between 2 and 3 because of \b
                string almost1 = "X@ssW0rd123";
                // Does not have at least two uppercase
                string almost2 = "X@55w0rd";

                // These two qualify
                string matching1 = "X@55W0rd";
                string matching2 = "Xa5$w00rD";

                foreach (int k in new int[] { 500, 1000, 5000, 10000, 50000, 100000 })
                {
                    Random random = new(k);
                    byte[] buffer1 = new byte[k];
                    byte[] buffer2 = new byte[k];
                    byte[] buffer3 = new byte[k];
                    random.NextBytes(buffer1);
                    random.NextBytes(buffer2);
                    random.NextBytes(buffer3);
                    string part1 = new string(Array.ConvertAll(buffer1, b => (char)b));
                    string part2 = new string(Array.ConvertAll(buffer2, b => (char)b));
                    string part3 = new string(Array.ConvertAll(buffer3, b => (char)b));

                    string input = $"{part1} {almost1} {part2} {matching1} {part3} {matching2}, finally this {almost2} does not qualify either";

                    int expextedMatch1Index = (2 * k) + almost1.Length + 3;
                    int expextedMatch1Length = matching1.Length;

                    int expextedMatch2Index = (3 * k) + almost1.Length + matching1.Length + 5;
                    int expextedMatch2Length = matching2.Length;

                    // Random text hiding almostPassw and password
                    int t = System.Environment.TickCount;
                    Match match1 = re.Match(input);
                    Match match2 = match1.NextMatch();
                    Match match3 = match2.NextMatch();
                    t = System.Environment.TickCount - t;

                    _output.WriteLine($@"k={k}, t={t}ms");

                    Assert.True(match1.Success);
                    Assert.Equal(expextedMatch1Index, match1.Index);
                    Assert.Equal(expextedMatch1Length, match1.Length);
                    Assert.Equal(matching1, match1.Value);

                    Assert.True(match2.Success);
                    Assert.Equal(expextedMatch2Index, match2.Index);
                    Assert.Equal(expextedMatch2Length, match2.Length);
                    Assert.Equal(matching2, match2.Value);

                    Assert.False(match3.Success);
                }
            }
            catch (NotSupportedException e)
            {
                // In Release build (?( test-pattern ) yes-pattern | no-pattern ) is not supported
                Assert.Contains("conditional", e.Message);
            }
        }

        //[ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        private void PasswordSearchDual()
        {
            try
            {
                string Not_twoLower = Not(".*[a-z].*[a-z].*");
                string Not_twoUpper = Not(".*[A-Z].*[A-Z].*");
                string Not_threeDigits = Not(".*[0-9].*[0-9].*[0-9].*");
                string Not_oneSpecial = Not(@".*[\x21-\x2F\x3A-\x40\x5B-x60\x7B-\x7E].*");
                string countUp = ".*(?:012|123|234|345|456|567|678|789).*";
                string countDown = ".*(?:987|876|765|654|543|432|321|210).*";
                // Observe that the space character (immediately before '!' in ASCII) is excluded
                string Not_length = Not("[!-~]{8,12}");

                // Just to make the chance that the randomly generated part actually has a match
                // be astronomically unlikely require 'P' and 'r' to be present also,
                // although this constraint is really bogus from password constraints point of view
                string Not_contains_first_P_and_then_r = Not(".*X.*r.*");

                // Negated disjunction of all the above constraints
                // By deMorgan's laws we know that ~(A|B|...|C) = ~A&~B&...&~C and ~~A = A
                // So Not(Not_twoLower|...) is equivalent to twoLower&~(...)
                string all = Not($"{Not_twoLower}|{Not_twoUpper}|{Not_threeDigits}|{Not_oneSpecial}|{countUp}|{countDown}|{Not_length}|{Not_contains_first_P_and_then_r}");

                // search for the password in a context surrounded by word boundaries
                Regex re = new Regex($@"\b{all}\b", RegexHelpers.RegexOptionNonBacktracking | RegexOptions.Singleline);

                // Does not qualify because of 123 and connot end between 2 and 3 because of \b
                string almost1 = "X@ssW0rd123";
                // Does not have at least two uppercase
                string almost2 = "X@55w0rd";

                // These two qualify
                string matching1 = "X@55W0rd";
                string matching2 = "Xa5$w00rD";

                foreach (int k in new int[] { 500, 1000, 5000, 10000, 50000, 100000 })
                {
                    Random random = new(k);
                    byte[] buffer1 = new byte[k];
                    byte[] buffer2 = new byte[k];
                    byte[] buffer3 = new byte[k];
                    random.NextBytes(buffer1);
                    random.NextBytes(buffer2);
                    random.NextBytes(buffer3);
                    string part1 = new string(Array.ConvertAll(buffer1, b => (char)b));
                    string part2 = new string(Array.ConvertAll(buffer2, b => (char)b));
                    string part3 = new string(Array.ConvertAll(buffer3, b => (char)b));

                    string input = $"{part1} {almost1} {part2} {matching1} {part3} {matching2}, finally this {almost2} does not qualify either";

                    int expectedMatch1Index = (2 * k) + almost1.Length + 3;
                    int expectedMatch1Length = matching1.Length;

                    int expectedMatch2Index = (3 * k) + almost1.Length + matching1.Length + 5;
                    int expectedMatch2Length = matching2.Length;

                    // Random text hiding almost and matching strings
                    int t = System.Environment.TickCount;
                    Match match1 = re.Match(input);
                    Match match2 = match1.NextMatch();
                    Match match3 = match2.NextMatch();
                    t = System.Environment.TickCount - t;

                    _output.WriteLine($@"k={k}, t={t}ms");

                    Assert.True(match1.Success);
                    Assert.Equal(expectedMatch1Index, match1.Index);
                    Assert.Equal(expectedMatch1Length, match1.Length);
                    Assert.Equal(matching1, match1.Value);

                    Assert.True(match2.Success);
                    Assert.Equal(expectedMatch2Index, match2.Index);
                    Assert.Equal(expectedMatch2Length, match2.Length);
                    Assert.Equal(matching2, match2.Value);

                    Assert.False(match3.Success);
                }
            }
            catch (NotSupportedException e)
            {
                // In Release build (?( test-pattern ) yes-pattern | no-pattern ) is not supported
                Assert.Contains("conditional", e.Message);
            }
        }

        //[ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        //[InlineData("[abc]{0,10}", "a[abc]{0,3}", "xxxabbbbbbbyyy", true, "abbb")]
        //[InlineData("[abc]{0,10}?", "a[abc]{0,3}?", "xxxabbbbbbbyyy", true, "a")]
        private void TestConjunctionOverCounting(string conjunct1, string conjunct2, string input, bool success, string match)
        {
            try
            {
                string pattern = And(conjunct1, conjunct2);
                Regex re = new Regex(pattern, RegexHelpers.RegexOptionNonBacktracking);
                Match m = re.Match(input);
                Assert.Equal(success, m.Success);
                Assert.Equal(match, m.Value);
            }
            catch (NotSupportedException e)
            {
                // In Release build (?( test-pattern ) yes-pattern | no-pattern ) is not supported
                Assert.Contains("conditional", e.Message);
            }
        }
        #endregion

        #region Random input generation tests
        public static IEnumerable<object[]> GenerateRandomMembers_TestData()
        {
            string[] patterns = new string[] { @"pa[5\$s]{2}w[o0]rd$", @"\w\d+", @"\d{10}" };
            foreach (string pattern in patterns)
            {
                Regex re = new Regex(pattern, RegexHelpers.RegexOptionNonBacktracking);
                foreach (bool negative in new bool[] { false, true })
                {
                    // Generate 3 positive and 3 negative inputs
                    List<string> inputs = new(GenerateRandomMembersViaReflection(re, 3, 123, negative));
                    foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
                    {
                        foreach (string input in inputs)
                        {
                            yield return new object[] { engine, pattern, input, !negative };
                        }
                    }
                }
            }
        }

        /// <summary>Test random input generation correctness</summary>
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        [MemberData(nameof(GenerateRandomMembers_TestData))]
        public async Task GenerateRandomMembers(RegexEngine engine, string pattern, string input, bool isMatch)
        {
            Regex regex = await RegexHelpers.GetRegexAsync(engine, pattern);
            Assert.Equal(isMatch, regex.IsMatch(input));
        }

        private static IEnumerable<string> GenerateRandomMembersViaReflection(Regex regex, int how_many_inputs, int randomseed, bool negative)
        {
            MethodInfo? gen = regex.GetType().GetMethod("GenerateRandomMembers", BindingFlags.NonPublic | BindingFlags.Instance);
            if (gen is not null)
            {
                return (IEnumerable<string>)gen.Invoke(regex, new object[] { how_many_inputs, randomseed, negative });
            }
            else
            {
                return new string[] { };
            }
        }
        #endregion
    }
}
