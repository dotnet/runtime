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
        [InlineData(".*a+", new string[] { ".*a+" }, false)]
        [InlineData("ann", new string[] { "nna" }, true)]
        public void TestDGMLGeneration(string pattern, string[] expectedDgmlFragments, bool exploreAsNFA)
        {
            StringWriter sw = new StringWriter();
            var re = new Regex(pattern, RegexHelpers.RegexOptionNonBacktracking | RegexOptions.Singleline);
            if (TryExplore(re, exploreAsNFA))
            {
                if (TrySaveDGML(re, sw, maxLabelLength: -1))
            {
                string str = sw.ToString();
                Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", str);
                Assert.Contains("DirectedGraph", str);
                foreach (string fragment in expectedDgmlFragments)
                {
                    Assert.Contains(fragment, str);
                }
            }
            }

            static bool TryExplore(Regex regex, bool exploreAsNFA)
            {
                MethodInfo saveDgml = regex.GetType().GetMethod("Explore", BindingFlags.NonPublic | BindingFlags.Instance);
                if (saveDgml is not null)
                {
                    saveDgml.Invoke(regex, new object[] { true, true, true, !exploreAsNFA, exploreAsNFA});
                    return true;
                }

                return false;
            }

            static bool TrySaveDGML(Regex regex, TextWriter writer, int maxLabelLength)
            {
                MethodInfo saveDgml = regex.GetType().GetMethod("SaveDGML", BindingFlags.NonPublic | BindingFlags.Instance);
                if (saveDgml is not null)
                {
                    saveDgml.Invoke(regex, new object[] { writer, maxLabelLength });
                    return true;
                }

                return false;
            }
        }

        #region Random input generation tests
        public static IEnumerable<object[]> SampledMatchesMatchAsExpected_TestData()
        {
            string[] patterns = new string[] { @"pa[5\$s]{2}w[o0]rd$", @"\w\d+", @"\d{10}" };
            foreach (string pattern in patterns)
            {
                Regex re = new Regex(pattern, RegexHelpers.RegexOptionNonBacktracking);
                // Generate 3 inputs
                List<string> inputs = new(SampleMatchesViaReflection(re, 3, pattern.GetHashCode()));
                foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
                {
                    foreach (string input in inputs)
                    {
                        yield return new object[] { engine, pattern, input };
                    }
                }
            }
        }

        /// <summary>Test random input generation correctness</summary>
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        [MemberData(nameof(SampledMatchesMatchAsExpected_TestData))]
        public async Task SampledMatchesMatchAsExpected(RegexEngine engine, string pattern, string input)
        {
            Regex regex = await RegexHelpers.GetRegexAsync(engine, pattern);
            Assert.True(regex.IsMatch(input));
        }

        private static IEnumerable<string> SampleMatchesViaReflection(Regex regex, int how_many_inputs, int randomseed)
        {
            MethodInfo? gen = regex.GetType().GetMethod("SampleMatches", BindingFlags.NonPublic | BindingFlags.Instance);
            if (gen is not null)
            {
                return (IEnumerable<string>)gen.Invoke(regex, new object[] { how_many_inputs, randomseed });
            }
            else
            {
                return new string[] { };
            }
        }
        #endregion
    }
}
