// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Interop;
using Microsoft.Interop.UnitTests;
using Xunit;

using VerifyCS = Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<Microsoft.Interop.LibraryImportGenerator, Microsoft.Interop.Analyzers.LibraryImportDiagnosticsAnalyzer>;

namespace LibraryImportGenerator.UnitTests
{
    public class UnsafeCodeGeneration
    {
        // The generator must not add an `unsafe` modifier to the containing type; instead any stub that
        // needs an unsafe context opens an explicit `unsafe` block in its body. This keeps the generated
        // output valid regardless of whether an `unsafe` modifier on a type establishes a body context.
        // These are structural assertions because the compile-only tests can't distinguish the two shapes:
        // both a class-level `unsafe` modifier and a body `unsafe` block compile under the test LangVersion.

        [Fact]
        public async Task WrapperStubWrapsBodyInUnsafeBlockAndDoesNotMarkContainingTypeUnsafe()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class C
                {
                    [LibraryImport("DoesNotExist", StringMarshalling = StringMarshalling.Utf16)]
                    public static partial void Method(string s);
                }
                """;
            await new UnsafeShapeTest(compilation =>
            {
                MethodDeclarationSyntax stub = GetGeneratedStubSyntax(compilation, "C", "Method");
                AssertNoUnsafeModifierOnContainingTypes(stub);
                StatementSyntax onlyStatement = Assert.Single(stub.Body!.Statements);
                Assert.IsType<UnsafeStatementSyntax>(onlyStatement);
            })
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            }.RunAsync();
        }

        [Fact]
        public async Task ForwarderStubDoesNotMarkContainingTypeUnsafe()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class C
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial void Method();
                }
                """;
            await new UnsafeShapeTest(compilation =>
            {
                MethodDeclarationSyntax stub = GetGeneratedStubSyntax(compilation, "C", "Method");
                // A forwarder is a bodyless `extern` stub, so it has no `unsafe` block to rely on.
                Assert.Null(stub.Body);
                Assert.True(stub.Modifiers.Any(SyntaxKind.ExternKeyword));
                AssertNoUnsafeModifierOnContainingTypes(stub);
            })
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            }.RunAsync();
        }

        [Fact]
        public async Task UserDeclaredUnsafeOnContainingTypeIsPreserved()
        {
            string source = """
                using System.Runtime.InteropServices;
                unsafe partial class C
                {
                    [LibraryImport("DoesNotExist", StringMarshalling = StringMarshalling.Utf16)]
                    public static partial void Method(string s);
                }
                """;
            await new UnsafeShapeTest(compilation =>
            {
                MethodDeclarationSyntax stub = GetGeneratedStubSyntax(compilation, "C", "Method");
                // The generator copies the user's type modifiers verbatim, so a user-authored `unsafe` is kept.
                TypeDeclarationSyntax containingType = stub.Ancestors().OfType<TypeDeclarationSyntax>().First();
                Assert.True(containingType.Modifiers.Any(SyntaxKind.UnsafeKeyword));
                // The body is still wrapped in an explicit `unsafe` block, independent of the type modifier.
                StatementSyntax onlyStatement = Assert.Single(stub.Body!.Statements);
                Assert.IsType<UnsafeStatementSyntax>(onlyStatement);
            })
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            }.RunAsync();
        }

        [Fact]
        public async Task UserDeclaredUnsafeOnForwarderMethodIsPreserved()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class C
                {
                    [LibraryImport("DoesNotExist")]
                    public static unsafe partial void Method();
                }
                """;
            await new UnsafeShapeTest(compilation =>
            {
                MethodDeclarationSyntax stub = GetGeneratedStubSyntax(compilation, "C", "Method");
                // A forwarder is a bodyless `extern` stub; the user's `unsafe` modifier is copied verbatim onto it.
                Assert.Null(stub.Body);
                Assert.True(stub.Modifiers.Any(SyntaxKind.ExternKeyword));
                Assert.True(stub.Modifiers.Any(SyntaxKind.UnsafeKeyword));
                AssertNoUnsafeModifierOnContainingTypes(stub);
            })
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            }.RunAsync();
        }

        [Fact]
        public async Task UserDeclaredUnsafeOnWrapperMethodIsPreserved()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class C
                {
                    [LibraryImport("DoesNotExist", StringMarshalling = StringMarshalling.Utf16)]
                    public static unsafe partial void Method(string s, int* i);
                }
                """;
            await new UnsafeShapeTest(compilation =>
            {
                MethodDeclarationSyntax stub = GetGeneratedStubSyntax(compilation, "C", "Method");
                // The user's `unsafe` modifier (required for the `int*` parameter) is copied verbatim onto the stub.
                Assert.True(stub.Modifiers.Any(SyntaxKind.UnsafeKeyword));
                AssertNoUnsafeModifierOnContainingTypes(stub);
                // The body is still wrapped in an explicit `unsafe` block, independent of the method modifier.
                StatementSyntax onlyStatement = Assert.Single(stub.Body!.Statements);
                Assert.IsType<UnsafeStatementSyntax>(onlyStatement);
            })
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            }.RunAsync();
        }

        [Fact]
        public async Task GeneratedLocalPInvokeIsAlwaysUnsafe()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class C
                {
                    [LibraryImport("DoesNotExist", StringMarshalling = StringMarshalling.Utf16)]
                    public static partial void Method(string s);
                }
                """;
            await new UnsafeShapeTest(compilation =>
            {
                MethodDeclarationSyntax stub = GetGeneratedStubSyntax(compilation, "C", "Method");
                LocalFunctionStatementSyntax localPInvoke = GetLocalPInvoke(stub);
                // The local P/Invoke is the interop boundary, so it is always caller-unsafe regardless of the
                // contract the user declared on the method the generator implements.
                Assert.True(localPInvoke.Modifiers.Any(SyntaxKind.ExternKeyword));
                Assert.True(localPInvoke.Modifiers.Any(SyntaxKind.UnsafeKeyword));
            })
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            }.RunAsync();
        }

        [Fact]
        public async Task GeneratedLocalPInvokeIsUnsafeWhenUserMethodIsUnsafe()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class C
                {
                    [LibraryImport("DoesNotExist", StringMarshalling = StringMarshalling.Utf16)]
                    public static unsafe partial void Method(string s);
                }
                """;
            await new UnsafeShapeTest(compilation =>
            {
                MethodDeclarationSyntax stub = GetGeneratedStubSyntax(compilation, "C", "Method");
                LocalFunctionStatementSyntax localPInvoke = GetLocalPInvoke(stub);
                Assert.True(localPInvoke.Modifiers.Any(SyntaxKind.ExternKeyword));
                Assert.True(localPInvoke.Modifiers.Any(SyntaxKind.UnsafeKeyword));
            })
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            }.RunAsync();
        }

        // Under the updated memory safety rules ("unsafe evolution"), a call into native code is a safety contract
        // the compiler cannot verify. The compiler itself only requires an explicit `safe`/`unsafe` modifier when
        // the generated implementing part is `extern`, which depends on whether the signature needs marshalling.
        // That is an implementation detail of the generator, so the analyzer requires the modifier for every
        // method with `[LibraryImport]`.

        [Fact]
        public async Task UpdatedRulesWrapperStubWithoutSafetyModifierReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class C
                {
                    [LibraryImport("DoesNotExist", StringMarshalling = StringMarshalling.Utf16)]
                    public static partial void {|SYSLIB1064:Method|}(string s);
                }
                """;
            await new UpdatedMemorySafetyRulesTest
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            }.RunAsync();
        }

        [Fact]
        public async Task UpdatedRulesForwarderStubWithoutSafetyModifierReportsDiagnostic()
        {
            // The generated forwarder is `extern`, so the compiler reports CS9389 for the same declaration. The
            // generator still emits it rather than something that compiles, so that suppressing SYSLIB1064 does
            // not silently turn the missing contract into a runtime failure.
            string source = """
                using System.Runtime.InteropServices;
                partial class C
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial int {|CS9389:{|SYSLIB1064:Method|}|}(int i);
                }
                """;
            await new UpdatedMemorySafetyRulesTest
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            }.RunAsync();
        }

        [Fact]
        public async Task DownlevelUpdatedRulesWithoutSafetyModifierReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class C
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial int {|CS9389:{|SYSLIB1064:Method|}|}(int i);
                }
                """;
            await new DownlevelUpdatedMemorySafetyRulesTest
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            }.RunAsync();
        }

        [Theory]
        [InlineData("unsafe")]
        [InlineData("safe")]
        public async Task DownlevelUpdatedRulesExplicitSafetyModifierSatisfiesRequirement(string safetyModifier)
        {
            string source = $$"""
                using System.Runtime.InteropServices;
                partial class C
                {
                    [LibraryImport("DoesNotExist")]
                    public static {{safetyModifier}} partial int Method(int i);
                }
                """;
            await new DownlevelUpdatedMemorySafetyRulesTest
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            }.RunAsync();
        }

        [Fact]
        public async Task DownlevelLegacyRulesDoNotRequireSafetyModifier()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class C
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial int Method(int i);
                }
                """;
            await new Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<
                DownlevelLibraryImportGenerator,
                Microsoft.Interop.Analyzers.DownlevelLibraryImportDiagnosticsAnalyzer>.Test(TestTargetFramework.Standard2_0)
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            }.RunAsync();
        }

        [Theory]
        [InlineData("[LibraryImport(\"DoesNotExist\", StringMarshalling = StringMarshalling.Utf16)]", "unsafe", "void Method(string s)")]
        // Utf8 marshalling uses a caller-allocated `stackalloc` buffer which, combined with the `[SkipLocalsInit]`
        // the generator emits, requires an unsafe context under the updated rules.
        [InlineData("[LibraryImport(\"DoesNotExist\", StringMarshalling = StringMarshalling.Utf8)]", "unsafe", "void Method(string s)")]
        [InlineData("[LibraryImport(\"DoesNotExist\")]", "unsafe", "int Method(int i)")]
        [InlineData("[LibraryImport(\"DoesNotExist\")]", "safe", "int Method(int i)")]
        public async Task UpdatedRulesExplicitSafetyModifierSatisfiesRequirement(string attribute, string safetyModifier, string signature)
        {
            string source = $$"""
                using System.Runtime.InteropServices;
                partial class C
                {
                    {{attribute}}
                    public static {{safetyModifier}} partial {{signature}};
                }
                """;
            await new UpdatedMemorySafetyRulesTest
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            }.RunAsync();
        }

        [Fact]
        public async Task UpdatedRulesSafeModifierIsForwardedToGeneratedExternStub()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class C
                {
                    [LibraryImport("DoesNotExist")]
                    public static safe partial int Method(int i);
                }
                """;
            await new UpdatedMemorySafetyRulesTest(compilation =>
            {
                MethodDeclarationSyntax stub = GetGeneratedStubSyntax(compilation, "C", "Method");
                // Both parts of a partial member must agree on their safety modifier, so the user's `safe` is
                // copied onto the generated `extern` forwarder.
                Assert.True(stub.Modifiers.Any(SyntaxKind.ExternKeyword));
                Assert.Contains(stub.Modifiers, modifier => modifier.IsKind(SyntaxFacts.GetContextualKeywordKind("safe")));
            })
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            }.RunAsync();
        }

        [Fact]
        public async Task LegacyRulesDoNotRequireSafetyModifier()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class C
                {
                    [LibraryImport("DoesNotExist", StringMarshalling = StringMarshalling.Utf16)]
                    public static partial void Method(string s);

                    [LibraryImport("DoesNotExist")]
                    public static partial int Forwarded(int i);
                }
                """;
            await VerifyCS.VerifySourceGeneratorAsync(source);
        }

        private static MethodDeclarationSyntax GetGeneratedStubSyntax(Compilation compilation, string typeName, string methodName)
        {
            INamedTypeSymbol type = compilation.GetTypeByMetadataName(typeName)!;
            IMethodSymbol method = type.GetMembers(methodName).OfType<IMethodSymbol>().Single();
            // The generated stub is the implementing part of the user's partial method declaration.
            IMethodSymbol implementation = method.PartialImplementationPart ?? method;
            return (MethodDeclarationSyntax)implementation.DeclaringSyntaxReferences.Single().GetSyntax();
        }

        private static void AssertNoUnsafeModifierOnContainingTypes(MethodDeclarationSyntax stub)
        {
            foreach (TypeDeclarationSyntax containingType in stub.Ancestors().OfType<TypeDeclarationSyntax>())
            {
                Assert.DoesNotContain(containingType.Modifiers, modifier => modifier.IsKind(SyntaxKind.UnsafeKeyword));
            }
        }

        private static LocalFunctionStatementSyntax GetLocalPInvoke(MethodDeclarationSyntax stub) =>
            Assert.Single(stub.Body!.DescendantNodes().OfType<LocalFunctionStatementSyntax>());

        private sealed class UnsafeShapeTest : VerifyCS.Test
        {
            private readonly Action<Compilation> _verifyCompilation;

            public UnsafeShapeTest(Action<Compilation> verifyCompilation)
                : base(referenceAncillaryInterop: false)
            {
                _verifyCompilation = verifyCompilation;
            }

            protected override void VerifyFinalCompilation(Compilation compilation) => _verifyCompilation(compilation);
        }

        /// <summary>
        /// Runs the generator with the updated memory safety rules ("unsafe evolution") enabled.
        /// </summary>
        private sealed class UpdatedMemorySafetyRulesTest : VerifyCS.Test
        {
            private readonly Action<Compilation>? _verifyCompilation;

            public UpdatedMemorySafetyRulesTest(Action<Compilation>? verifyCompilation = null)
                : base(referenceAncillaryInterop: false)
            {
                _verifyCompilation = verifyCompilation;
            }

            protected override ParseOptions CreateParseOptions()
            {
                // Roslyn does not expose the memory safety rules version through a public API yet, so opt in
                // through the same feature flag the compiler uses.
                var parseOptions = (CSharpParseOptions)base.CreateParseOptions();
                return parseOptions.WithFeatures(
                    [.. parseOptions.Features, new KeyValuePair<string, string>(MemorySafetyRules.UpdatedMemorySafetyRulesFeature, "")]);
            }

            protected override void VerifyFinalCompilation(Compilation compilation) => _verifyCompilation?.Invoke(compilation);
        }

        /// <summary>
        /// Runs the downlevel generator, which targets frameworks without the runtime marshalling support, with
        /// the updated memory safety rules enabled.
        /// </summary>
        private sealed class DownlevelUpdatedMemorySafetyRulesTest
            : Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<
                DownlevelLibraryImportGenerator,
                Microsoft.Interop.Analyzers.DownlevelLibraryImportDiagnosticsAnalyzer>.Test
        {
            public DownlevelUpdatedMemorySafetyRulesTest()
                : base(TestTargetFramework.Standard2_0)
            {
            }

            protected override ParseOptions CreateParseOptions()
            {
                var parseOptions = (CSharpParseOptions)base.CreateParseOptions();
                return parseOptions.WithFeatures(
                    [.. parseOptions.Features, new KeyValuePair<string, string>(MemorySafetyRules.UpdatedMemorySafetyRulesFeature, "")]);
            }
        }
    }
}
