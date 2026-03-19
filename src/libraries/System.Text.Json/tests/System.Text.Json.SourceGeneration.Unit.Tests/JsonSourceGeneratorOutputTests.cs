// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    /// Any change to source generator output strategy must be accompanied by an update to the
    /// baseline files. To regenerate all baselines automatically, build with:
    /// <code>
    /// dotnet build /p:UpdateBaselines=true
    /// dotnet test --no-build
    /// </code>
    /// The <c>/p:UpdateBaselines=true</c> flag defines the <c>UPDATE_BASELINES</c> compilation
    /// constant, which causes tests to overwrite baseline files with the actual generated output
    /// instead of asserting against them. <b>Requires</b> the <c>RepoRootDir</c> environment
    /// variable to point at the repository root (e.g. <c>D:\repos\runtime</c>).
    /// After updating, rebuild <em>without</em> the flag and re-run tests to confirm they pass.
    /// </para>
    /// </summary>
    [ActiveIssue("https://github.com/dotnet/runtime/issues/58226", TestPlatforms.Browser)]
    [SkipOnCoreClr("https://github.com/dotnet/runtime/issues/71962", ~RuntimeConfiguration.Release)]
    [SkipOnMono("https://github.com/dotnet/runtime/issues/92467")]
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotX86Process))]
    public class SourceGeneratedOutputTests(ITestOutputHelper logger)
    {
        private static readonly string BaselinesRelativePath = IO.Path.Combine(
            "src", "libraries", "System.Text.Json", "tests",
            "System.Text.Json.SourceGeneration.Unit.Tests", "Baselines");

        /// <summary>
        /// Runs the source generator on <paramref name="source"/> and verifies that the generated
        /// type-info file for <paramref name="typeInfoPropertyName"/> matches the baseline stored in
        /// <c>Baselines/{baselineFileName}</c>.
        /// </summary>
        private void VerifyAgainstBaseline(
            string source,
            string typeInfoPropertyName,
            string baselineFileName)
        {
            Compilation compilation = CompilationHelper.CreateCompilation(source);
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation, logger: logger);

            string expectedSuffix = $"MyContext.{typeInfoPropertyName}.g.cs";
            SyntaxTree? tree = result.NewCompilation.SyntaxTrees
                .FirstOrDefault(t => t.FilePath.EndsWith(expectedSuffix, StringComparison.Ordinal));
            Assert.NotNull(tree);

            SourceText generatedSourceText = tree.GetText();

            string baselinePath = IO.Path.Combine("Baselines", baselineFileName);
            string baseline = LineEndingsHelper.Normalize(IO.File.ReadAllText(baselinePath));
            string[] expectedLines = baseline.Replace("%VERSION%", typeof(JsonSourceGenerator).Assembly.GetName().Version?.ToString())
                                             .Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            bool matches = RoslynTestUtils.CompareLines(expectedLines, generatedSourceText, out string errorMessage);

#if UPDATE_BASELINES
            if (!matches)
            {
                const string envVarName = "RepoRootDir";
                string? repoRootDir = Environment.GetEnvironmentVariable(envVarName);
                Assert.True(repoRootDir is not null,
                    $"To update baselines, specify a '{envVarName}' environment variable pointing to the repo root.");

                string lines = string.Join(Environment.NewLine, generatedSourceText.Lines.Select(l => l.ToString()));
                string fullPath = IO.Path.Combine(repoRootDir, BaselinesRelativePath, baselineFileName);
                IO.File.WriteAllText(fullPath, lines + Environment.NewLine);
                matches = true;
            }
#endif

            Assert.True(matches, errorMessage);
        }

        [Fact]
        public void SimplePoco()
        {
            string source = """
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
                """;

            VerifyAgainstBaseline(source, "Person", "SimplePoco.generated.txt");
        }

        [Fact]
        public void ParameterizedConstructor()
        {
            string source = """
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
                """;

            VerifyAgainstBaseline(source, "Point", "ParameterizedConstructor.generated.txt");
        }

        [Fact]
        public void EnumType()
        {
            string source = """
                using System.Text.Json.Serialization;
                namespace TestApp
                {
                    [JsonSerializable(typeof(Color))]
                    internal partial class MyContext : JsonSerializerContext { }
                    public enum Color { Red, Green, Blue }
                }
                """;

            VerifyAgainstBaseline(source, "Color", "EnumType.generated.txt");
        }

        [Fact]
        public void ListProperty()
        {
            string source = """
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
                """;

            VerifyAgainstBaseline(source, "Order", "ListProperty.generated.txt");
        }

        [Fact]
        public void DictionaryProperty()
        {
            string source = """
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
                """;

            VerifyAgainstBaseline(source, "Settings", "DictionaryProperty.generated.txt");
        }

        [Fact]
        public void RecordType()
        {
            string source = """
                using System.Text.Json.Serialization;
                namespace TestApp
                {
                    [JsonSerializable(typeof(Coordinate))]
                    internal partial class MyContext : JsonSerializerContext { }
                    public record Coordinate(double Latitude, double Longitude);
                }
                """;

            VerifyAgainstBaseline(source, "Coordinate", "RecordType.generated.txt");
        }

        [Fact]
        public void JsonPropertyNameAttribute()
        {
            string source = """
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
                """;

            VerifyAgainstBaseline(source, "Product", "JsonPropertyNameAttribute.generated.txt");
        }

        [Fact]
        public void ConstructorWithDefaultValues()
        {
            string source = """
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
                """;

            VerifyAgainstBaseline(source, "Config", "ConstructorWithDefaultValues.generated.txt");
        }

        [Fact]
        public void NullableProperties()
        {
            string source = """
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
                """;

            VerifyAgainstBaseline(source, "Measurement", "NullableProperties.generated.txt");
        }

        [Fact]
        public void ByRefConstructorParameters()
        {
            string source = """
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
                """;

            VerifyAgainstBaseline(source, "ByRefParams", "ByRefConstructorParameters.generated.txt");
        }
    }
}
