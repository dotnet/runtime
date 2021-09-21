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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.RegularExpressions.Generator.Tests
{
    // Tests don't actually use reflection emit, but they do generate assembly via Roslyn in-memory at run time and expect it to be JIT'd.
    // The tests also use typeof(object).Assembly.Location, which returns an empty string on wasm.
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
    [PlatformSpecific(~TestPlatforms.Browser)]
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

            Assert.Equal("SYSLIB1042", Assert.Single(diagnostics).Id);
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

            Assert.Equal("SYSLIB1042", Assert.Single(diagnostics).Id);
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

            Assert.Equal("SYSLIB1042", Assert.Single(diagnostics).Id);
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

            Assert.Equal("SYSLIB1043", Assert.Single(diagnostics).Id);
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

            Assert.Equal("SYSLIB1043", Assert.Single(diagnostics).Id);
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

            Assert.Equal("SYSLIB1043", Assert.Single(diagnostics).Id);
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

            Assert.Equal("SYSLIB1043", Assert.Single(diagnostics).Id);
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

            Assert.Equal("SYSLIB1043", Assert.Single(diagnostics).Id);
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

            Assert.Equal("SYSLIB1044", Assert.Single(diagnostics).Id);
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
        public async Task Valid_ClassWithNestedNamespaces()
        {
            Assert.Empty(await RunGenerator(@"
                using System.Text.RegularExpressions;
                namespace A
                {
                    namespace B
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

        [Fact]
        public async Task Valid_OnStruct()
        {
            Assert.Empty(await RunGenerator(@"
                using System.Text.RegularExpressions;
                internal partial struct C
                {
                    [RegexGenerator(""ab"")]
                    private static partial Regex Valid();
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_OnRecord()
        {
            Assert.Empty(await RunGenerator(@"
                using System.Text.RegularExpressions;
                internal partial record C
                {
                    [RegexGenerator(""ab"")]
                    private static partial Regex Valid();
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_OnRecordStruct()
        {
            Assert.Empty(await RunGenerator(@"
                using System.Text.RegularExpressions;
                internal partial record struct C
                {
                    [RegexGenerator(""ab"")]
                    private static partial Regex Valid();
                }
            ", compile: true));
        }

        [Fact]
        public async Task Valid_NestedVaryingTypes()
        {
            Assert.Empty(await RunGenerator(@"
                using System.Text.RegularExpressions;
                public partial class A
                {
                    public partial record class B
                    {
                        public partial record struct C
                        {
                            public partial record D
                            {
                                public partial struct E
                                {
                                    [RegexGenerator(""ab"")]
                                    public static partial Regex Valid();
                                }
                            }
                        }
                    }
                }
            ", compile: true));
        }

        private async Task<IReadOnlyList<Diagnostic>> RunGenerator(
            string code, bool compile = false, LanguageVersion langVersion = LanguageVersion.Preview, CancellationToken cancellationToken = default)
        {
            var proj = new AdhocWorkspace()
                .AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create()))
                .AddProject("RegexGeneratorTest", "RegexGeneratorTest.dll", "C#")
                .WithMetadataReferences(s_refs)
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
            if (!results.Success)
            {
                throw new ArgumentException(
                    string.Join(Environment.NewLine, results.Diagnostics.Concat(generatorResults.Diagnostics)) + Environment.NewLine +
                    string.Join(Environment.NewLine, generatorResults.GeneratedTrees.Select(t => t.ToString())));
            }

            return generatorResults.Diagnostics.Concat(results.Diagnostics).Where(d => d.Severity != DiagnosticSeverity.Hidden).ToArray();
        }

        private static readonly MetadataReference[] s_refs = CreateReferences();

        private static MetadataReference[] CreateReferences()
        {
            string corelibPath = typeof(object).Assembly.Location;
            return new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(corelibPath)!, "System.Runtime.dll")),
                MetadataReference.CreateFromFile(typeof(Unsafe).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Regex).Assembly.Location),
            };
        }
    }
}
