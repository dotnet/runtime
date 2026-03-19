// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Tests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SourceGenerators.Tests;
using Xunit;
using Xunit.Abstractions;

namespace System.Text.Json.SourceGeneration.UnitTests
{
    /// <summary>
    /// Verifies the exact source output of the System.Text.Json source generator against
    /// checked-in baseline files under the <c>Baselines/</c> directory.
    /// <para>
    /// Baselines are organized as <c>Baselines/{TestId}/{TFM}/{HintName}.cs.txt</c> where:
    /// <list type="bullet">
    ///   <item><c>TestId</c> — the test method name (e.g. <c>SimplePoco</c>)</item>
    ///   <item><c>TFM</c> — <c>netcoreapp</c> or <c>net462</c></item>
    ///   <item><c>HintName</c> — the source generator hint name (e.g. <c>MyContext.Person.g</c>)</item>
    /// </list>
    /// Every generated file is checked — not just the type-specific one.
    /// </para>
    /// <para>
    /// To regenerate all baselines after a source generator output change:
    /// <code>
    /// set RepoRootDir=D:\repos\runtime
    /// dotnet build /p:UpdateBaselines=true
    /// dotnet test --no-build
    /// </code>
    /// Then rebuild <em>without</em> the flag and re-run tests to confirm they pass.
    /// </para>
    /// </summary>
    [ActiveIssue("https://github.com/dotnet/runtime/issues/58226", TestPlatforms.Browser)]
    [SkipOnCoreClr("https://github.com/dotnet/runtime/issues/71962", ~RuntimeConfiguration.Release)]
    [SkipOnMono("https://github.com/dotnet/runtime/issues/92467")]
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotX86Process))]
    public class SourceGeneratedOutputTests(ITestOutputHelper logger)
    {
        [Fact]
        public void SimplePoco()
        {
            VerifyAgainstBaseline("""
                using System.Text.Json.Serialization;
                namespace TestApp
                {
                    [JsonSerializable(typeof(Person))]
                    internal partial class MyContext : JsonSerializerContext { }
                    public class Person
                    {
                        public string Name { get; set; }
                        public int Age { get; set; }
                    }
                }
                """, nameof(SimplePoco));
        }

        [Fact]
        public void ParameterizedConstructor()
        {
            VerifyAgainstBaseline("""
                using System.Text.Json.Serialization;
                namespace TestApp
                {
                    [JsonSerializable(typeof(Point))]
                    internal partial class MyContext : JsonSerializerContext { }
                    public class Point
                    {
                        public Point(int x, int y) { X = x; Y = y; }
                        public int X { get; }
                        public int Y { get; }
                    }
                }
                """, nameof(ParameterizedConstructor));
        }

        [Fact]
        public void EnumType()
        {
            VerifyAgainstBaseline("""
                using System.Text.Json.Serialization;
                namespace TestApp
                {
                    [JsonSerializable(typeof(Color))]
                    internal partial class MyContext : JsonSerializerContext { }
                    public enum Color { Red, Green, Blue }
                }
                """, nameof(EnumType));
        }

        [Fact]
        public void ListProperty()
        {
            VerifyAgainstBaseline("""
                using System.Collections.Generic;
                using System.Text.Json.Serialization;
                namespace TestApp
                {
                    [JsonSerializable(typeof(Order))]
                    internal partial class MyContext : JsonSerializerContext { }
                    public class Order
                    {
                        public List<string> Items { get; set; }
                    }
                }
                """, nameof(ListProperty));
        }

        [Fact]
        public void DictionaryProperty()
        {
            VerifyAgainstBaseline("""
                using System.Collections.Generic;
                using System.Text.Json.Serialization;
                namespace TestApp
                {
                    [JsonSerializable(typeof(Settings))]
                    internal partial class MyContext : JsonSerializerContext { }
                    public class Settings
                    {
                        public Dictionary<string, int> Values { get; set; }
                    }
                }
                """, nameof(DictionaryProperty));
        }

        [Fact]
        public void RecordType()
        {
            VerifyAgainstBaseline("""
                using System.Text.Json.Serialization;
                namespace TestApp
                {
                    [JsonSerializable(typeof(Coordinate))]
                    internal partial class MyContext : JsonSerializerContext { }
                    public record Coordinate(double Latitude, double Longitude);
                }
                """, nameof(RecordType));
        }

        [Fact]
        public void JsonPropertyNameAttribute()
        {
            VerifyAgainstBaseline("""
                using System.Text.Json.Serialization;
                namespace TestApp
                {
                    [JsonSerializable(typeof(Product))]
                    internal partial class MyContext : JsonSerializerContext { }
                    public class Product
                    {
                        [JsonPropertyName("product_name")]
                        public string Name { get; set; }
                        [JsonPropertyName("unit_price")]
                        public decimal Price { get; set; }
                    }
                }
                """, nameof(JsonPropertyNameAttribute));
        }

        [Fact]
        public void ConstructorWithDefaultValues()
        {
            VerifyAgainstBaseline("""
                using System.Text.Json.Serialization;
                namespace TestApp
                {
                    [JsonSerializable(typeof(Config))]
                    internal partial class MyContext : JsonSerializerContext { }
                    public class Config
                    {
                        public Config(string name, int retries = 3, bool enabled = true)
                        {
                            Name = name;
                            Retries = retries;
                            Enabled = enabled;
                        }
                        public string Name { get; }
                        public int Retries { get; }
                        public bool Enabled { get; }
                    }
                }
                """, nameof(ConstructorWithDefaultValues));
        }

        [Fact]
        public void NullableProperties()
        {
            VerifyAgainstBaseline("""
                using System.Text.Json.Serialization;
                namespace TestApp
                {
                    [JsonSerializable(typeof(Measurement))]
                    internal partial class MyContext : JsonSerializerContext { }
                    public class Measurement
                    {
                        public double Value { get; set; }
                        public double? Margin { get; set; }
                        public int? Count { get; set; }
                    }
                }
                """, nameof(NullableProperties));
        }

        [Fact]
        public void ByRefConstructorParameters()
        {
            VerifyAgainstBaseline("""
                using System;
                using System.Text.Json.Serialization;
                namespace TestApp
                {
                    [JsonSerializable(typeof(ByRefParams))]
                    internal partial class MyContext : JsonSerializerContext { }
                    public class ByRefParams
                    {
                        public ByRefParams(in DateTime date, ref string name, out int result)
                        {
                            Date = date;
                            Name = name;
                            result = 0;
                        }
                        public DateTime Date { get; set; }
                        public string Name { get; set; }
                        public int Result { get; set; }
                    }
                }
                """, nameof(ByRefConstructorParameters));
        }

        [Fact]
        public void OutParameterWithUnsupportedType()
        {
            VerifyAgainstBaseline("""
                using System;
                using System.Threading.Tasks;
                using System.Text.Json.Serialization;
                namespace TestApp
                {
                    [JsonSerializable(typeof(OutTask))]
                    internal partial class MyContext : JsonSerializerContext { }
                    public class OutTask
                    {
                        public OutTask(string name, out Task pending)
                        {
                            Name = name;
                            pending = Task.CompletedTask;
                        }
                        public string Name { get; set; }
                    }
                }
                """, nameof(OutParameterWithUnsupportedType));
        }

        #region Baseline comparison infrastructure

        private static readonly string s_baselinesRelativePath = IO.Path.Combine(
            "src", "libraries", "System.Text.Json", "tests",
            "System.Text.Json.SourceGeneration.Unit.Tests", "Baselines");

        private static readonly string s_tfmSubFolder =
#if NET
            "netcoreapp";
#else
            "net462";
#endif

        /// <summary>
        /// Runs the source generator on <paramref name="source"/> and verifies that every
        /// generated file matches the corresponding baseline in
        /// <c>Baselines/{testId}/{tfm}/{hintName}.cs.txt</c>.
        /// </summary>
        private void VerifyAgainstBaseline(string source, string testId)
        {
            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);

            var inputPaths = new HashSet<string>(compilation.SyntaxTrees.Select(t => t.FilePath));
            List<SyntaxTree> generatedTrees = result.NewCompilation.SyntaxTrees
                .Where(t => !inputPaths.Contains(t.FilePath))
                .ToList();

            Assert.True(generatedTrees.Count > 0, "Source generator produced no output.");

            string baselineDir = IO.Path.Combine("Baselines", testId, s_tfmSubFolder);

            string[] actualFiles = generatedTrees
                .Select(t => ToBaselineFileName(t.FilePath))
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToArray();

#if UPDATE_BASELINES
            {
                const string envVarName = "RepoRootDir";
                string? repoRootDir = Environment.GetEnvironmentVariable(envVarName);
                Assert.True(repoRootDir is not null,
                    $"To update baselines, set the '{envVarName}' environment variable to the repo root.");

                string absDir = IO.Path.Combine(repoRootDir, s_baselinesRelativePath, testId, s_tfmSubFolder);
                IO.Directory.CreateDirectory(absDir);

                // Remove stale baselines that are no longer generated.
                if (IO.Directory.Exists(absDir))
                {
                    foreach (string existing in IO.Directory.GetFiles(absDir, "*.cs.txt"))
                    {
                        string name = IO.Path.GetFileName(existing);
                        if (!actualFiles.Contains(name, StringComparer.Ordinal))
                        {
                            IO.File.Delete(existing);
                        }
                    }
                }

                foreach (SyntaxTree tree in generatedTrees)
                {
                    string baselineFileName = ToBaselineFileName(tree.FilePath);
                    SourceText generatedSourceText = tree.GetText();
                    string absPath = IO.Path.Combine(absDir, baselineFileName);
                    IO.File.WriteAllText(absPath, generatedSourceText.ToString());
                }

                return;
            }
#else
            // Collect expected baseline files from disk.
            Assert.True(IO.Directory.Exists(baselineDir),
                $"Baseline directory not found: {baselineDir}. Build with /p:UpdateBaselines=true to generate baselines.");

            string[] expectedFiles = IO.Directory.GetFiles(baselineDir, "*.cs.txt")
                .Select(f => IO.Path.GetFileName(f))
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToArray();

            // Verify that the set of generated files matches the set of baselines.
            Assert.True(
                expectedFiles.SequenceEqual(actualFiles, StringComparer.Ordinal),
                $"Generated file set mismatch.\nExpected: [{string.Join(", ", expectedFiles)}]\nActual:   [{string.Join(", ", actualFiles)}]");

            // Verify content of each generated file.
            foreach (SyntaxTree tree in generatedTrees)
            {
                string baselineFileName = ToBaselineFileName(tree.FilePath);
                string baselinePath = IO.Path.Combine(baselineDir, baselineFileName);
                SourceText generatedSourceText = tree.GetText();

                string baseline = LineEndingsHelper.Normalize(IO.File.ReadAllText(baselinePath));
                string[] expectedLines = baseline
                    .Replace("%VERSION%", typeof(JsonSourceGenerator).Assembly.GetName().Version?.ToString())
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                bool matches = RoslynTestUtils.CompareLines(expectedLines, generatedSourceText, out string errorMessage);
                Assert.True(matches, $"Baseline mismatch for {baselineFileName}.\n{errorMessage}");
            }
#endif
        }

        /// <summary>
        /// Converts a generated tree file path (e.g. ending in <c>MyContext.Person.g.cs</c>)
        /// to a baseline file name (e.g. <c>MyContext.Person.g.cs.txt</c>).
        /// </summary>
        private static string ToBaselineFileName(string generatedFilePath)
            => IO.Path.GetFileName(generatedFilePath) + ".txt";

        #endregion
    }
}
