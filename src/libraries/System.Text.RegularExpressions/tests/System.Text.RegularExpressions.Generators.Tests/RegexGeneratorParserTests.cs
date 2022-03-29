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
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported), nameof(PlatformDetection.IsNotMobile), nameof(PlatformDetection.IsNotBrowser))]
    public class RegexGeneratorParserTests
    {
        [Fact]
        public async Task Diagnostic_MultipleAttributes()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [RegexGenerator(""ab"")]
                    [RegexGenerator(""abc"")]
                    private static partial Regex MultipleAttributes();
                }
            ");

            Assert.Equal("SYSLIB1041", Assert.Single(diagnostics).Id);
        }

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
        [InlineData(0x800)]
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
        public async Task Diagnostic_RightToLeft_LimitedSupport()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [RegexGenerator(""ab"", RegexOptions.RightToLeft)]
                    private static partial Regex RightToLeftNotSupported();
                }
            ");

            Assert.Equal("SYSLIB1045", Assert.Single(diagnostics).Id);
        }

        [Fact]
        public async Task Diagnostic_NonBacktracking_LimitedSupport()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [RegexGenerator(""ab"", RegexOptions.NonBacktracking)]
                    private static partial Regex RightToLeftNotSupported();
                }
            ");

            Assert.Equal("SYSLIB1045", Assert.Single(diagnostics).Id);
        }

        [Fact]
        public async Task Diagnostic_PositiveLookbehind_LimitedSupport()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [RegexGenerator(""(?<=\b20)\d{2}\b"")]
                    private static partial Regex PositiveLookbehindNotSupported();
                }
            ");

            Assert.Equal("SYSLIB1045", Assert.Single(diagnostics).Id);
        }

        [Fact]
        public async Task Diagnostic_NegativeLookbehind_LimitedSupport()
        {
            IReadOnlyList<Diagnostic> diagnostics = await RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [RegexGenerator(""(?<!(Saturday|Sunday) )\b\w+ \d{1,2}, \d{4}\b"")]
                    private static partial Regex NegativeLookbehindNotSupported();
                }
            ");

            Assert.Equal("SYSLIB1045", Assert.Single(diagnostics).Id);
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

        [Theory]
        [InlineData("RegexOptions.None")]
        [InlineData("RegexOptions.Compiled")]
        [InlineData("RegexOptions.IgnoreCase | RegexOptions.CultureInvariant")]
        public async Task Valid_PatternOptions(string options)
        {
            Assert.Empty(await RunGenerator($@"
                using System.Text.RegularExpressions;
                partial class C
                {{
                    [RegexGenerator(""ab"", {options})]
                    private static partial Regex Valid();
                }}
            ", compile: true));
        }

        [Theory]
        [InlineData("-1")]
        [InlineData("1")]
        [InlineData("1_000")]
        public async Task Valid_PatternOptionsTimeout(string timeout)
        {
            Assert.Empty(await RunGenerator($@"
                using System.Text.RegularExpressions;
                partial class C
                {{
                    [RegexGenerator(""ab"", RegexOptions.None, {timeout})]
                    private static partial Regex Valid();
                }}
            ", compile: true));
        }

        [Fact]
        public async Task Valid_NamedArguments()
        {
            Assert.Empty(await RunGenerator($@"
                using System.Text.RegularExpressions;
                partial class C
                {{
                    [RegexGenerator(pattern: ""ab"", options: RegexOptions.None, matchTimeoutMilliseconds: -1)]
                    private static partial Regex Valid();
                }}
            ", compile: true));
        }

        [Fact]
        public async Task Valid_ReorderedNamedArguments()
        {
            Assert.Empty(await RunGenerator($@"
                using System.Text.RegularExpressions;
                partial class C
                {{
                    [RegexGenerator(options: RegexOptions.None, matchTimeoutMilliseconds: -1, pattern: ""ab"")]
                    private static partial Regex Valid1();

                    [RegexGenerator(matchTimeoutMilliseconds: -1, pattern: ""ab"", options: RegexOptions.None)]
                    private static partial Regex Valid2();
                }}
            ", compile: true));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Valid_ClassWithNamespace(bool allowUnsafe)
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
            ", compile: true, allowUnsafe: allowUnsafe));
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

        public static IEnumerable<object[]> Valid_Modifiers_MemberData()
        {
            foreach (string type in new[] { "class", "struct", "record", "record struct", "record class", "interface" })
            {
                string[] typeModifiers = type switch
                {
                    "class" => new[] { "", "public", "public sealed", "internal abstract", "internal static" },
                    _ => new[] { "", "public", "internal" }
                };

                foreach (string typeModifier in typeModifiers)
                {
                    foreach (bool instance in typeModifier.Contains("static") ? new[] { false } : new[] { false, true })
                    {
                        string[] methodVisibilities = type switch
                        {
                            "class" when !typeModifier.Contains("sealed") && !typeModifier.Contains("static") => new[] { "public", "internal", "private protected", "protected internal", "private" },
                            _ => new[] { "public", "internal", "private" }
                        };

                        foreach (string methodVisibility in methodVisibilities)
                        {
                            yield return new object[] { type, typeModifier, instance, methodVisibility };
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(Valid_Modifiers_MemberData))]
        public async Task Valid_Modifiers(string type, string typeModifier, bool instance, string methodVisibility)
        {
            Assert.Empty(await RunGenerator(@$"
                using System.Text.RegularExpressions;
                {typeModifier} partial {type} C
                {{
                    [RegexGenerator(""ab"")]
                    {methodVisibility} {(instance ? "" : "static")} partial Regex Valid();
                }}
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

        [Fact]
        public async Task MultipleTypeDefinitions_DoesntBreakGeneration()
        {
            byte[] referencedAssembly = CreateAssemblyImage(@"
                namespace System.Text.RegularExpressions;

                [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
                internal sealed class RegexGeneratorAttribute : Attribute
                {
                    public RegexGeneratorAttribute(string pattern){}
                }", "TestAssembly");

            Assert.Empty(await RunGenerator(@"
                using System.Text.RegularExpressions;
                partial class C
                {
                    [RegexGenerator(""abc"")]
                    private static partial Regex Valid();
                }", compile: true, additionalRefs: new[] { MetadataReference.CreateFromImage(referencedAssembly) }));
        }

        private async Task<IReadOnlyList<Diagnostic>> RunGenerator(
            string code, bool compile = false, LanguageVersion langVersion = LanguageVersion.Preview, MetadataReference[]? additionalRefs = null, bool allowUnsafe = false, CancellationToken cancellationToken = default)
        {
            var proj = new AdhocWorkspace()
                .AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create()))
                .AddProject("RegexGeneratorTest", "RegexGeneratorTest.dll", "C#")
                .WithMetadataReferences(additionalRefs is not null ? s_refs.Concat(additionalRefs) : s_refs)
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: allowUnsafe)
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
            if (!results.Success || results.Diagnostics.Length != 0 || generatorResults.Diagnostics.Length != 0)
            {
                throw new ArgumentException(
                    string.Join(Environment.NewLine, results.Diagnostics.Concat(generatorResults.Diagnostics)) + Environment.NewLine +
                    string.Join(Environment.NewLine, generatorResults.GeneratedTrees.Select(t => t.ToString())));
            }

            return generatorResults.Diagnostics.Concat(results.Diagnostics).Where(d => d.Severity != DiagnosticSeverity.Hidden).ToArray();
        }

        private static byte[] CreateAssemblyImage(string source, string assemblyName)
        {
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                new[] { CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview)) },
                s_refs.ToArray(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var ms = new MemoryStream();
            if (compilation.Emit(ms).Success)
            {
               return ms.ToArray();
            }

            throw new InvalidOperationException();
        }

        private static readonly MetadataReference[] s_refs = CreateReferences();

        private static MetadataReference[] CreateReferences()
        {
            if (PlatformDetection.IsBrowser)
            {
                // These tests that use Roslyn don't work well on browser wasm today
                return new MetadataReference[0];
            }

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
