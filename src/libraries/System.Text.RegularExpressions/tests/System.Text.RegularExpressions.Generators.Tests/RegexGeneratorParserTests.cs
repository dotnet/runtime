// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.RegularExpressions.Generator.Tests
{
    public class RegexGeneratorParserTests
    {
        [Theory]
        [InlineData("ab[]")]
        public async Task Diagnostic_InvalidRegexPattern(string pattern)
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator($@"
                using System.Text.RegularExpressions;
                partial class C
                {{
                    [RegexGenerator(""{pattern}"")]
                    private static partial Regex InvalidPattern();
                }}
            ");

            Assert.Equal(DiagnosticDescriptors.InvalidRegexArguments.Id, Assert.Single(diagnostics).Id);
        }

        [Theory]
        [InlineData(128)]
        public async Task Diagnostic_InvalidRegexOptions(int options)
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@$"
                using System.Text.RegularExpressions;
                partial class C
                {{
                    [RegexGenerator(""ab"", (RegexOptions){options})]
                    private static partial Regex InvalidPattern();
                }}
            ");

            Assert.Equal(DiagnosticDescriptors.InvalidRegexArguments.Id, Assert.Single(diagnostics).Id);
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(0)]
        public async Task Diagnostic_InvalidRegexTimeout(int matchTimeout)
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@$"
                using System.Text.RegularExpressions;
                partial class C
                {{
                    [RegexGenerator(""ab"", RegexOptions.None, {matchTimeout.ToString(CultureInfo.InvariantCulture)})]
                    private static partial Regex InvalidPattern();
                }}
            ");

            Assert.Equal(DiagnosticDescriptors.InvalidRegexArguments.Id, Assert.Single(diagnostics).Id);
        }

        [Fact]
        public async Task Diagnostic_MethodMustReturnRegex()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [RegexGenerator(""ab"")]
                    private static partial int MethodMustReturnRegex();
                }
            ");

            Assert.Equal(DiagnosticDescriptors.RegexMethodMustReturnRegex.Id, Assert.Single(diagnostics).Id);
        }

        [Fact]
        public async Task Diagnostic_MethodMustBeStatic()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [RegexGenerator(""ab"")]
                    private partial Regex MethodMustBeStatic();
                }
            ");

            Assert.Equal(DiagnosticDescriptors.RegexMethodMustBeStatic.Id, Assert.Single(diagnostics).Id);
        }

        [Fact]
        public async Task Diagnostic_MethodMustNotBeGeneric()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [RegexGenerator(""ab"")]
                    private static partial Regex MethodMustNotBeGeneric<T>();
                }
            ");

            Assert.Equal(DiagnosticDescriptors.RegexMethodMustNotBeGeneric.Id, Assert.Single(diagnostics).Id);
        }

        [Fact]
        public async Task Diagnostic_MethodMustBeParameterless()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [RegexGenerator(""ab"")]
                    private static partial Regex MethodMustBeParameterless(int i);
                }
            ");

            Assert.Equal(DiagnosticDescriptors.RegexMethodMustBeParameterless.Id, Assert.Single(diagnostics).Id);
        }

        [Fact]
        public async Task Diagnostic_MethodMustBePartial()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [RegexGenerator(""ab"")]
                    private static Regex MethodMustBePartial() => null;
                }
            ");

            Assert.Equal(DiagnosticDescriptors.RegexMethodMustBePartial.Id, Assert.Single(diagnostics).Id);
        }

        [ActiveIssue("https://github.com/dotnet/roslyn/pull/55866")]
        [Fact]
        public async Task Diagnostic_InvalidLangVersion()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [RegexGenerator(""ab"")]
                    private static partial Regex InvalidLangVersion();
                }
            ", langVersion: LanguageVersion.CSharp9);

            Assert.Equal(DiagnosticDescriptors.InvalidLangVersion.Id, Assert.Single(diagnostics).Id);
        }

        [Fact]
        public async Task Valid_ClassWithoutNamespace()
        {
            Assert.Empty(await RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [RegexGenerator(""ab"")]
                    private static partial Regex Valid();
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_ClassWithNamespace()
        {
            Assert.Empty(await RunGenerator(@"
                using System.Text.RegularExpressions;
                namespace A
                {
                    partial class C
                    {
                        [RegexGenerator(""ab"")]
                        private static partial Regex Valid();
                    }
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_ClassWithFileScopedNamespace()
        {
            Assert.Empty(await RunGenerator(@"
                using System.Text.RegularExpressions;
                namespace A;
                partial class C
                {
                    [RegexGenerator(""ab"")]
                    private static partial Regex Valid();
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_NestedClassWithoutNamespace()
        {
            Assert.Empty(await RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class B
                {
                    partial class C
                    {
                        [RegexGenerator(""ab"")]
                        private static partial Regex Valid();
                    }
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_NestedClassWithNamespace()
        {
            Assert.Empty(await RunGenerator(@"
                using System.Text.RegularExpressions;
                namespace A
                {
                    partial class B
                    {
                        partial class C
                        {
                            [RegexGenerator(""ab"")]
                            private static partial Regex Valid();
                        }
                    }
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_NestedClassWithFileScopedNamespace()
        {
            Assert.Empty(await RunGenerator(@"
                using System.Text.RegularExpressions;
                namespace A;
                partial class B
                {
                    partial class C
                    {
                        [RegexGenerator(""ab"")]
                        private static partial Regex Valid();
                    }
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_NestedClassesWithNamespace()
        {
            Assert.Empty(await RunGenerator(@"
                using System.Text.RegularExpressions;
                namespace A
                {
                    public partial class B
                    {
                        internal partial class C
                        {
                            protected internal partial class D
                            {
                                private protected partial class E
                                {
                                    private partial class F
                                    {
                                        [RegexGenerator(""ab"")]
                                        private static partial Regex Valid();
                                    }
                                }
                            }
                        }
                    }
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_NullableRegex()
        {
            Assert.Empty(await RunGenerator(@"
                #nullable enable
                using System.Text.RegularExpressions;
                partial class C
                {
                    [RegexGenerator(""ab"")]
                    private static partial Regex? Valid();
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_InternalRegex()
        {
            Assert.Empty(await RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [RegexGenerator(""ab"")]
                    internal static partial Regex Valid();
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_PublicRegex()
        {
            Assert.Empty(await RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [RegexGenerator(""ab"")]
                    public static partial Regex Valid();
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_PrivateProtectedRegex()
        {
            Assert.Empty(await RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [RegexGenerator(""ab"")]
                    private protected static partial Regex Valid();
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_PublicSealedClass()
        {
            Assert.Empty(await RunGenerator(@"
                using System.Text.RegularExpressions;
                public sealed partial class C
                {
                    [RegexGenerator(""ab"")]
                    private static partial Regex Valid();
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_InternalAbstractClass()
        {
            Assert.Empty(await RunGenerator(@"
                using System.Text.RegularExpressions;
                internal abstract partial class C
                {
                    [RegexGenerator(""ab"")]
                    private static partial Regex Valid();
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_MultiplRegexMethodsPerClass()
        {
            Assert.Empty(await RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C1
                {
                    [RegexGenerator(""a"")]
                    private static partial Regex A();

                    [RegexGenerator(""b"")]
                    public static partial Regex B();

                    [RegexGenerator(""b"")]
                    public static partial Regex C();
                }
                partial class C2
                {
                    [RegexGenerator(""d"")]
                    public static partial Regex D();

                    [RegexGenerator(""d"")]
                    public static partial Regex E();
                }
            ", compile: true));
        }

        private async Task<IReadOnlyList<Diagnostic>> RunGenerator(
            string code, bool compile = false, LanguageVersion langVersion = LanguageVersion.Preview, CancellationToken cancellationToken = default)
        {
            string corelib = Assembly.GetAssembly(typeof(object))!.Location;
            string runtimeDir = Path.GetDirectoryName(corelib)!;
            var refs = new List<MetadataReference>()
            {
                MetadataReference.CreateFromFile(corelib),
                MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")),
                MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Text.RegularExpressions.dll"))
            };

            var proj = new AdhocWorkspace()
                .AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create()))
                .AddProject("RegexGeneratorTest", "RegexGeneratorTest.dll", "C#")
                .WithMetadataReferences(refs)
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable))
                .WithParseOptions(new CSharpParseOptions(langVersion))
                .AddDocument("RegexGenerator.g.cs", SourceText.From(code, Encoding.UTF8)).Project;

            Assert.True(proj.Solution.Workspace.TryApplyChanges(proj.Solution));

            Compilation? comp = await proj!.GetCompilationAsync(CancellationToken.None).ConfigureAwait(false);
            Debug.Assert(comp is not null);

            var generator = new RegexGenerator();
            CSharpGeneratorDriver cgd = CSharpGeneratorDriver.Create(new[] { generator.AsSourceGenerator() }, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(langVersion));
            GeneratorDriver gd = cgd.RunGenerators(comp!, cancellationToken);
            GeneratorDriverRunResult generatorResults = gd.GetRunResult();
            if (!compile)
            {
                return generatorResults.Diagnostics;
            }

            comp = comp.AddSyntaxTrees(generatorResults.GeneratedTrees.ToArray());
            EmitResult results = comp.Emit(Stream.Null, cancellationToken: cancellationToken);

            return generatorResults.Diagnostics.Concat(results.Diagnostics).Where(d => d.Severity != DiagnosticSeverity.Hidden).ToArray();
        }
    }
}
