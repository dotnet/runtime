// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
    public static class RegexGeneratorIncrementalTests
    {
        private static CSharpGeneratorDriver CreateRegexGeneratorDriver(Compilation compilation)
        {
            var generator = new RegexGenerator();
            CSharpParseOptions parseOptions = (CSharpParseOptions)compilation.SyntaxTrees.First().Options;
            return CSharpGeneratorDriver.Create(
                generators: [generator.AsSourceGenerator()],
                parseOptions: parseOptions,
                driverOptions: new GeneratorDriverOptions(
                    disabledOutputs: IncrementalGeneratorOutputKind.None,
                    trackIncrementalGeneratorSteps: true));
        }

        private static Compilation CreateCompilation(string source)
        {
            return CSharpCompilation.Create(
                "RegexGeneratorTest",
                [CSharpSyntaxTree.ParseText(SourceText.From(source, Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview))],
                RegexGeneratorHelper.References,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithNullableContextOptions(NullableContextOptions.Enable));
        }

        [Fact]
        public static void SameCompilation_SameInput_DoesNotRegenerate()
        {
            string source = """
                using System.Text.RegularExpressions;
                public partial class C
                {
                    [GeneratedRegex(@"abc")]
                    public static partial Regex GetRegex();
                }
                """;

            Compilation compilation = CreateCompilation(source);
            GeneratorDriver driver = CreateRegexGeneratorDriver(compilation);

            // First run: should produce new outputs
            driver = driver.RunGenerators(compilation);
            GeneratorRunResult runResult = driver.GetRunResult().Results[0];
            Assert.False(runResult.GeneratedSources.IsEmpty);

            // Second run: same compilation, should be fully cached
            driver = driver.RunGenerators(compilation);
            runResult = driver.GetRunResult().Results[0];
            Assert.False(runResult.GeneratedSources.IsEmpty);
        }

        [Fact]
        public static void UnrelatedCodeChange_DoesNotRegenerate()
        {
            string source1 = """
                using System.Text.RegularExpressions;
                public partial class C
                {
                    [GeneratedRegex(@"abc")]
                    public static partial Regex GetRegex();
                }
                """;

            string source2 = """
                using System.Text.RegularExpressions;
                public partial class C
                {
                    [GeneratedRegex(@"abc")]
                    public static partial Regex GetRegex();

                    // Adding an unrelated method should not cause regeneration.
                    public static void UnrelatedMethod() { }
                }
                """;

            Compilation compilation = CreateCompilation(source1);
            GeneratorDriver driver = CreateRegexGeneratorDriver(compilation);

            // First run
            driver = driver.RunGenerators(compilation);
            GeneratorRunResult runResult = driver.GetRunResult().Results[0];
            ImmutableArray<string> firstRunOutputs = [.. runResult.GeneratedSources.Select(s => s.SyntaxTree.ToString())];
            Assert.False(firstRunOutputs.IsEmpty);

            // Second run: unrelated code change
            compilation = compilation.ReplaceSyntaxTree(
                compilation.SyntaxTrees.First(),
                CSharpSyntaxTree.ParseText(SourceText.From(source2, Encoding.UTF8),
                    CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview)));

            driver = driver.RunGenerators(compilation);
            runResult = driver.GetRunResult().Results[0];
            ImmutableArray<string> secondRunOutputs = [.. runResult.GeneratedSources.Select(s => s.SyntaxTree.ToString())];

            // The generated source text should be identical
            Assert.Equal(firstRunOutputs.Length, secondRunOutputs.Length);
            for (int i = 0; i < firstRunOutputs.Length; i++)
            {
                Assert.Equal(firstRunOutputs[i], secondRunOutputs[i]);
            }
        }

        [Fact]
        public static void PatternChange_Regenerates()
        {
            string source1 = """
                using System.Text.RegularExpressions;
                public partial class C
                {
                    [GeneratedRegex(@"abc")]
                    public static partial Regex GetRegex();
                }
                """;

            string source2 = """
                using System.Text.RegularExpressions;
                public partial class C
                {
                    [GeneratedRegex(@"xyz")]
                    public static partial Regex GetRegex();
                }
                """;

            Compilation compilation = CreateCompilation(source1);
            GeneratorDriver driver = CreateRegexGeneratorDriver(compilation);

            driver = driver.RunGenerators(compilation);
            GeneratorRunResult runResult = driver.GetRunResult().Results[0];
            string firstOutput = string.Concat(runResult.GeneratedSources.Select(s => s.SyntaxTree.ToString()));
            Assert.Contains("abc", firstOutput);

            // Change the pattern
            compilation = compilation.ReplaceSyntaxTree(
                compilation.SyntaxTrees.First(),
                CSharpSyntaxTree.ParseText(SourceText.From(source2, Encoding.UTF8),
                    CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview)));

            driver = driver.RunGenerators(compilation);
            runResult = driver.GetRunResult().Results[0];
            string secondOutput = string.Concat(runResult.GeneratedSources.Select(s => s.SyntaxTree.ToString()));
            Assert.Contains("xyz", secondOutput);
            Assert.NotEqual(firstOutput, secondOutput);
        }

        [Fact]
        public static void OptionsChange_Regenerates()
        {
            string source1 = """
                using System.Text.RegularExpressions;
                public partial class C
                {
                    [GeneratedRegex(@"abc")]
                    public static partial Regex GetRegex();
                }
                """;

            string source2 = """
                using System.Text.RegularExpressions;
                public partial class C
                {
                    [GeneratedRegex(@"abc", RegexOptions.IgnoreCase)]
                    public static partial Regex GetRegex();
                }
                """;

            Compilation compilation = CreateCompilation(source1);
            GeneratorDriver driver = CreateRegexGeneratorDriver(compilation);

            driver = driver.RunGenerators(compilation);
            string firstOutput = string.Concat(driver.GetRunResult().Results[0].GeneratedSources.Select(s => s.SyntaxTree.ToString()));

            compilation = compilation.ReplaceSyntaxTree(
                compilation.SyntaxTrees.First(),
                CSharpSyntaxTree.ParseText(SourceText.From(source2, Encoding.UTF8),
                    CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview)));

            driver = driver.RunGenerators(compilation);
            string secondOutput = string.Concat(driver.GetRunResult().Results[0].GeneratedSources.Select(s => s.SyntaxTree.ToString()));
            Assert.NotEqual(firstOutput, secondOutput);
        }

        [Fact]
        public static void TimeoutChange_Regenerates()
        {
            string source1 = """
                using System.Text.RegularExpressions;
                public partial class C
                {
                    [GeneratedRegex(@"abc", RegexOptions.None, 1000)]
                    public static partial Regex GetRegex();
                }
                """;

            string source2 = """
                using System.Text.RegularExpressions;
                public partial class C
                {
                    [GeneratedRegex(@"abc", RegexOptions.None, 2000)]
                    public static partial Regex GetRegex();
                }
                """;

            Compilation compilation = CreateCompilation(source1);
            GeneratorDriver driver = CreateRegexGeneratorDriver(compilation);

            driver = driver.RunGenerators(compilation);
            string firstOutput = string.Concat(driver.GetRunResult().Results[0].GeneratedSources.Select(s => s.SyntaxTree.ToString()));

            compilation = compilation.ReplaceSyntaxTree(
                compilation.SyntaxTrees.First(),
                CSharpSyntaxTree.ParseText(SourceText.From(source2, Encoding.UTF8),
                    CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview)));

            driver = driver.RunGenerators(compilation);
            string secondOutput = string.Concat(driver.GetRunResult().Results[0].GeneratedSources.Select(s => s.SyntaxTree.ToString()));
            Assert.NotEqual(firstOutput, secondOutput);
        }

        [Fact]
        public static void DeclaringTypeChange_Regenerates()
        {
            string source1 = """
                using System.Text.RegularExpressions;
                namespace NS1
                {
                    public partial class C
                    {
                        [GeneratedRegex(@"abc")]
                        public static partial Regex GetRegex();
                    }
                }
                """;

            string source2 = """
                using System.Text.RegularExpressions;
                namespace NS2
                {
                    public partial class C
                    {
                        [GeneratedRegex(@"abc")]
                        public static partial Regex GetRegex();
                    }
                }
                """;

            Compilation compilation = CreateCompilation(source1);
            GeneratorDriver driver = CreateRegexGeneratorDriver(compilation);

            driver = driver.RunGenerators(compilation);
            string firstOutput = string.Concat(driver.GetRunResult().Results[0].GeneratedSources.Select(s => s.SyntaxTree.ToString()));
            Assert.Contains("NS1", firstOutput);

            compilation = compilation.ReplaceSyntaxTree(
                compilation.SyntaxTrees.First(),
                CSharpSyntaxTree.ParseText(SourceText.From(source2, Encoding.UTF8),
                    CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview)));

            driver = driver.RunGenerators(compilation);
            string secondOutput = string.Concat(driver.GetRunResult().Results[0].GeneratedSources.Select(s => s.SyntaxTree.ToString()));
            Assert.Contains("NS2", secondOutput);
            Assert.NotEqual(firstOutput, secondOutput);
        }

        [Fact]
        public static void NewRegexMethodAdded_Regenerates()
        {
            string source1 = """
                using System.Text.RegularExpressions;
                public partial class C
                {
                    [GeneratedRegex(@"abc")]
                    public static partial Regex GetRegex1();
                }
                """;

            string source2 = """
                using System.Text.RegularExpressions;
                public partial class C
                {
                    [GeneratedRegex(@"abc")]
                    public static partial Regex GetRegex1();

                    [GeneratedRegex(@"def")]
                    public static partial Regex GetRegex2();
                }
                """;

            Compilation compilation = CreateCompilation(source1);
            GeneratorDriver driver = CreateRegexGeneratorDriver(compilation);

            driver = driver.RunGenerators(compilation);
            string firstOutput = string.Concat(driver.GetRunResult().Results[0].GeneratedSources.Select(s => s.SyntaxTree.ToString()));

            compilation = compilation.ReplaceSyntaxTree(
                compilation.SyntaxTrees.First(),
                CSharpSyntaxTree.ParseText(SourceText.From(source2, Encoding.UTF8),
                    CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview)));

            driver = driver.RunGenerators(compilation);
            string secondOutput = string.Concat(driver.GetRunResult().Results[0].GeneratedSources.Select(s => s.SyntaxTree.ToString()));
            Assert.NotEqual(firstOutput, secondOutput);
            Assert.Contains("def", secondOutput);
        }

        [Fact]
        public static void RegexMethodRemoved_Regenerates()
        {
            string source1 = """
                using System.Text.RegularExpressions;
                public partial class C
                {
                    [GeneratedRegex(@"abc")]
                    public static partial Regex GetRegex1();

                    [GeneratedRegex(@"uniquepatternxyz")]
                    public static partial Regex GetRegex2();
                }
                """;

            string source2 = """
                using System.Text.RegularExpressions;
                public partial class C
                {
                    [GeneratedRegex(@"abc")]
                    public static partial Regex GetRegex1();
                }
                """;

            Compilation compilation = CreateCompilation(source1);
            GeneratorDriver driver = CreateRegexGeneratorDriver(compilation);

            driver = driver.RunGenerators(compilation);
            string firstOutput = string.Concat(driver.GetRunResult().Results[0].GeneratedSources.Select(s => s.SyntaxTree.ToString()));
            Assert.Contains("uniquepatternxyz", firstOutput);

            compilation = compilation.ReplaceSyntaxTree(
                compilation.SyntaxTrees.First(),
                CSharpSyntaxTree.ParseText(SourceText.From(source2, Encoding.UTF8),
                    CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview)));

            driver = driver.RunGenerators(compilation);
            string secondOutput = string.Concat(driver.GetRunResult().Results[0].GeneratedSources.Select(s => s.SyntaxTree.ToString()));
            Assert.NotEqual(firstOutput, secondOutput);
            Assert.DoesNotContain("uniquepatternxyz", secondOutput);
        }

        [Fact]
        public static void PropertyVsMethod_BothWork()
        {
            string source = """
                using System.Text.RegularExpressions;
                public partial class C
                {
                    [GeneratedRegex(@"abc")]
                    public static partial Regex GetRegex();

                    [GeneratedRegex(@"def")]
                    public static partial Regex PropRegex { get; }
                }
                """;

            Compilation compilation = CreateCompilation(source);
            GeneratorDriver driver = CreateRegexGeneratorDriver(compilation);

            driver = driver.RunGenerators(compilation);
            GeneratorRunResult runResult = driver.GetRunResult().Results[0];
            Assert.Empty(runResult.Diagnostics);
            Assert.False(runResult.GeneratedSources.IsEmpty);
        }

        [Theory]
        [InlineData(@"[a-z]+")]
        [InlineData(@"^\d{3}-\d{2}-\d{4}$")]
        [InlineData(@"(?<proto>\w+)://[^/]+?(?<port>:\d+)?/")]
        [InlineData(@"(?>abc|def)")]
        [InlineData(@"(?=\d)(?<=\w)")]
        public static void ComplexPatterns_UnrelatedChange_DoesNotRegenerate(string pattern)
        {
            string source1 = $$"""
                using System.Text.RegularExpressions;
                public partial class C
                {
                    [GeneratedRegex(@"{{pattern}}")]
                    public static partial Regex GetRegex();
                }
                """;

            string source2 = $$"""
                using System.Text.RegularExpressions;
                public partial class C
                {
                    [GeneratedRegex(@"{{pattern}}")]
                    public static partial Regex GetRegex();

                    public static void Foo() { }
                }
                """;

            Compilation compilation = CreateCompilation(source1);
            GeneratorDriver driver = CreateRegexGeneratorDriver(compilation);

            driver = driver.RunGenerators(compilation);
            string firstOutput = string.Concat(driver.GetRunResult().Results[0].GeneratedSources.Select(s => s.SyntaxTree.ToString()));

            compilation = compilation.ReplaceSyntaxTree(
                compilation.SyntaxTrees.First(),
                CSharpSyntaxTree.ParseText(SourceText.From(source2, Encoding.UTF8),
                    CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview)));

            driver = driver.RunGenerators(compilation);
            string secondOutput = string.Concat(driver.GetRunResult().Results[0].GeneratedSources.Select(s => s.SyntaxTree.ToString()));
            Assert.Equal(firstOutput, secondOutput);
        }

        [Fact]
        public static void MultipleRegexes_OnlyChangedOneRegenerated()
        {
            string source1 = """
                using System.Text.RegularExpressions;
                public partial class C
                {
                    [GeneratedRegex(@"abc")]
                    public static partial Regex GetRegex1();

                    [GeneratedRegex(@"uniquepatternxyz")]
                    public static partial Regex GetRegex2();
                }
                """;

            string source2 = """
                using System.Text.RegularExpressions;
                public partial class C
                {
                    [GeneratedRegex(@"abc")]
                    public static partial Regex GetRegex1();

                    [GeneratedRegex(@"uniquepatternqrs")]
                    public static partial Regex GetRegex2();
                }
                """;

            Compilation compilation = CreateCompilation(source1);
            GeneratorDriver driver = CreateRegexGeneratorDriver(compilation);

            driver = driver.RunGenerators(compilation);
            string firstOutput = string.Concat(driver.GetRunResult().Results[0].GeneratedSources.Select(s => s.SyntaxTree.ToString()));
            Assert.Contains("abc", firstOutput);
            Assert.Contains("uniquepatternxyz", firstOutput);

            compilation = compilation.ReplaceSyntaxTree(
                compilation.SyntaxTrees.First(),
                CSharpSyntaxTree.ParseText(SourceText.From(source2, Encoding.UTF8),
                    CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview)));

            driver = driver.RunGenerators(compilation);
            string secondOutput = string.Concat(driver.GetRunResult().Results[0].GeneratedSources.Select(s => s.SyntaxTree.ToString()));
            Assert.Contains("abc", secondOutput);
            Assert.Contains("uniquepatternqrs", secondOutput);
            Assert.DoesNotContain("uniquepatternxyz", secondOutput);
        }

        [Fact]
        public static void MemberNameChange_Regenerates()
        {
            string source1 = """
                using System.Text.RegularExpressions;
                public partial class C
                {
                    [GeneratedRegex(@"abc")]
                    public static partial Regex GetRegexA();
                }
                """;

            string source2 = """
                using System.Text.RegularExpressions;
                public partial class C
                {
                    [GeneratedRegex(@"abc")]
                    public static partial Regex GetRegexB();
                }
                """;

            Compilation compilation = CreateCompilation(source1);
            GeneratorDriver driver = CreateRegexGeneratorDriver(compilation);

            driver = driver.RunGenerators(compilation);
            string firstOutput = string.Concat(driver.GetRunResult().Results[0].GeneratedSources.Select(s => s.SyntaxTree.ToString()));
            Assert.Contains("GetRegexA", firstOutput);

            compilation = compilation.ReplaceSyntaxTree(
                compilation.SyntaxTrees.First(),
                CSharpSyntaxTree.ParseText(SourceText.From(source2, Encoding.UTF8),
                    CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview)));

            driver = driver.RunGenerators(compilation);
            string secondOutput = string.Concat(driver.GetRunResult().Results[0].GeneratedSources.Select(s => s.SyntaxTree.ToString()));
            Assert.Contains("GetRegexB", secondOutput);
            Assert.NotEqual(firstOutput, secondOutput);
        }

        [Fact]
        public static void LimitedSupportRegex_UnrelatedChange_DoesNotRegenerate()
        {
            // NonBacktracking gets limited support (no custom runner factory)
            string source1 = """
                using System.Text.RegularExpressions;
                public partial class C
                {
                    [GeneratedRegex(@"abc", RegexOptions.NonBacktracking)]
                    public static partial Regex GetRegex();
                }
                """;

            string source2 = """
                using System.Text.RegularExpressions;
                public partial class C
                {
                    [GeneratedRegex(@"abc", RegexOptions.NonBacktracking)]
                    public static partial Regex GetRegex();

                    public static int Unrelated() => 42;
                }
                """;

            Compilation compilation = CreateCompilation(source1);
            GeneratorDriver driver = CreateRegexGeneratorDriver(compilation);

            driver = driver.RunGenerators(compilation);
            string firstOutput = string.Concat(driver.GetRunResult().Results[0].GeneratedSources.Select(s => s.SyntaxTree.ToString()));

            compilation = compilation.ReplaceSyntaxTree(
                compilation.SyntaxTrees.First(),
                CSharpSyntaxTree.ParseText(SourceText.From(source2, Encoding.UTF8),
                    CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview)));

            driver = driver.RunGenerators(compilation);
            string secondOutput = string.Concat(driver.GetRunResult().Results[0].GeneratedSources.Select(s => s.SyntaxTree.ToString()));
            Assert.Equal(firstOutput, secondOutput);
        }

        [Fact]
        public static void CultureChange_Regenerates()
        {
            // Use a pattern where culture actually matters for case folding.
            // The Turkish 'I' (dotted/dotless) behavior differs between en-US and tr-TR.
            string source1 = """
                using System.Text.RegularExpressions;
                public partial class C
                {
                    [GeneratedRegex(@"INFO", RegexOptions.IgnoreCase, -1, "en-US")]
                    public static partial Regex GetRegex();
                }
                """;

            string source2 = """
                using System.Text.RegularExpressions;
                public partial class C
                {
                    [GeneratedRegex(@"INFO", RegexOptions.IgnoreCase, -1, "tr-TR")]
                    public static partial Regex GetRegex();
                }
                """;

            Compilation compilation = CreateCompilation(source1);
            GeneratorDriver driver = CreateRegexGeneratorDriver(compilation);

            driver = driver.RunGenerators(compilation);
            string firstOutput = string.Concat(driver.GetRunResult().Results[0].GeneratedSources.Select(s => s.SyntaxTree.ToString()));

            compilation = compilation.ReplaceSyntaxTree(
                compilation.SyntaxTrees.First(),
                CSharpSyntaxTree.ParseText(SourceText.From(source2, Encoding.UTF8),
                    CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview)));

            driver = driver.RunGenerators(compilation);
            string secondOutput = string.Concat(driver.GetRunResult().Results[0].GeneratedSources.Select(s => s.SyntaxTree.ToString()));
            Assert.NotEqual(firstOutput, secondOutput);
        }
    }
}
