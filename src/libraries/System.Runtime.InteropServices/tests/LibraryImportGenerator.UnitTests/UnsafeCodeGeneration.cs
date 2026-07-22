// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
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

        [Theory]
        [InlineData(false, false, "safe")]
        [InlineData(false, false, "unsafe")]
        [InlineData(false, true, "safe")]
        [InlineData(false, true, "unsafe")]
        [InlineData(true, false, "safe")]
        [InlineData(true, false, "unsafe")]
        [InlineData(true, true, "safe")]
        [InlineData(true, true, "unsafe")]
        public Task UserDeclaredSafetyModifierIsMirroredOnGeneratedExtern(bool downlevel, bool wrapper, string safetyModifier)
        {
            string returnType = wrapper ? "byte" : "void";
            string parameters = wrapper ? "byte p, in byte pIn, ref byte pRef, out byte pOut" : "";
            string source = $$"""
                using System.Runtime.InteropServices;
                partial class C
                {
                    [LibraryImport("DoesNotExist")]
                    public static {{safetyModifier}} partial {{returnType}} Method({{parameters}});
                }
                """;

            return RunSafetyModifierTestAsync(downlevel, source, compilation =>
            {
                MethodDeclarationSyntax stub = GetGeneratedStubSyntax(compilation, "C", "Method");
                AssertSafetyModifier(stub.Modifiers, safetyModifier);
                AssertNoUnsafeModifierOnContainingTypes(stub);

                if (!wrapper)
                {
                    Assert.Null(stub.Body);
                    Assert.True(stub.Modifiers.Any(SyntaxKind.ExternKeyword));
                    return;
                }

                UnsafeStatementSyntax unsafeStatement = Assert.IsType<UnsafeStatementSyntax>(Assert.Single(stub.Body!.Statements));
                LocalFunctionStatementSyntax localExtern = Assert.Single(unsafeStatement.Block.Statements.OfType<LocalFunctionStatementSyntax>());
                Assert.True(localExtern.Modifiers.Any(SyntaxKind.ExternKeyword));
                AssertSafetyModifier(localExtern.Modifiers, safetyModifier);
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public Task UpdatedMemorySafetyRulesRequireExplicitSafetyModifier(bool downlevel)
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class C
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial void {|#0:Method|}();
                }
                """;

            return RunMissingSafetyModifierTestAsync(downlevel, source);
        }

        private static Task RunSafetyModifierTestAsync(bool downlevel, string source, Action<Compilation> verifyCompilation)
        {
            return downlevel
                ? RunSafetyModifierTestAsync<Microsoft.Interop.DownlevelLibraryImportGenerator, Microsoft.Interop.Analyzers.DownlevelLibraryImportDiagnosticsAnalyzer>(
                    source,
                    verifyCompilation,
                    TestTargetFramework.Standard2_0)
                : RunSafetyModifierTestAsync<Microsoft.Interop.LibraryImportGenerator, Microsoft.Interop.Analyzers.LibraryImportDiagnosticsAnalyzer>(
                    source,
                    verifyCompilation,
                    targetFramework: null);
        }

        private static Task RunSafetyModifierTestAsync<TSourceGenerator, TAnalyzer>(
            string source,
            Action<Compilation> verifyCompilation,
            TestTargetFramework? targetFramework)
            where TSourceGenerator : new()
            where TAnalyzer : DiagnosticAnalyzer, new()
        {
            SafetyModifierShapeTest<TSourceGenerator, TAnalyzer> test = targetFramework is null
                ? new SafetyModifierShapeTest<TSourceGenerator, TAnalyzer>(verifyCompilation)
                : new SafetyModifierShapeTest<TSourceGenerator, TAnalyzer>(targetFramework.Value, verifyCompilation);
            test.TestCode = source;
            test.TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck;

            return test.RunAsync();
        }

        private static Task RunMissingSafetyModifierTestAsync(bool downlevel, string source)
        {
            return downlevel
                ? RunMissingSafetyModifierTestAsync<Microsoft.Interop.DownlevelLibraryImportGenerator, Microsoft.Interop.Analyzers.DownlevelLibraryImportDiagnosticsAnalyzer>(
                    source,
                    TestTargetFramework.Standard2_0)
                : RunMissingSafetyModifierTestAsync<Microsoft.Interop.LibraryImportGenerator, Microsoft.Interop.Analyzers.LibraryImportDiagnosticsAnalyzer>(
                    source,
                    targetFramework: null);
        }

        private static Task RunMissingSafetyModifierTestAsync<TSourceGenerator, TAnalyzer>(
            string source,
            TestTargetFramework? targetFramework)
            where TSourceGenerator : new()
            where TAnalyzer : DiagnosticAnalyzer, new()
        {
            static void VerifyCompilation(Compilation compilation)
            {
                INamedTypeSymbol type = compilation.GetTypeByMetadataName("C")!;
                IMethodSymbol method = Assert.Single(type.GetMembers("Method").OfType<IMethodSymbol>());
                Assert.Null(method.PartialImplementationPart);
            }

            SafetyModifierShapeTest<TSourceGenerator, TAnalyzer> test = targetFramework is null
                ? new SafetyModifierShapeTest<TSourceGenerator, TAnalyzer>(VerifyCompilation)
                : new SafetyModifierShapeTest<TSourceGenerator, TAnalyzer>(targetFramework.Value, VerifyCompilation);
            test.TestCode = source;
            test.TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck;
            test.ExpectedDiagnostics.Add(
                VerifyCS.Diagnostic(Microsoft.Interop.GeneratorDiagnostics.InvalidAttributedMethodMissingSafetyModifier)
                    .WithLocation(0)
                    .WithArguments("Method"));

            return test.RunAsync();
        }

        private static void AssertSafetyModifier(SyntaxTokenList modifiers, string expected)
        {
            SyntaxToken modifier = Assert.Single(modifiers, IsSafetyModifier);
            Assert.Equal(expected, modifier.ValueText);

            static bool IsSafetyModifier(SyntaxToken modifier)
                => modifier.IsKind(SyntaxKind.UnsafeKeyword) || modifier.ValueText == "safe";
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

        private sealed class SafetyModifierShapeTest<TSourceGenerator, TAnalyzer> :
            Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<TSourceGenerator, TAnalyzer>.Test
            where TSourceGenerator : new()
            where TAnalyzer : DiagnosticAnalyzer, new()
        {
            private readonly Action<Compilation> _verifyCompilation;

            public SafetyModifierShapeTest(Action<Compilation> verifyCompilation)
                : base(referenceAncillaryInterop: false)
            {
                _verifyCompilation = verifyCompilation;
            }

            public SafetyModifierShapeTest(TestTargetFramework targetFramework, Action<Compilation> verifyCompilation)
                : base(targetFramework)
            {
                _verifyCompilation = verifyCompilation;
            }

            protected override ParseOptions CreateParseOptions()
            {
                CSharpParseOptions options = (CSharpParseOptions)base.CreateParseOptions();
                return options.WithFeatures([new KeyValuePair<string, string>("updated-memory-safety-rules", "true")]);
            }

            protected override bool IsCompilerDiagnosticIncluded(Diagnostic diagnostic, CompilerDiagnostics compilerDiagnostics)
                // The compiler package still enforces the pre-LDM restriction that 'safe' is only valid on extern members.
                => base.IsCompilerDiagnosticIncluded(diagnostic, compilerDiagnostics)
                    && diagnostic.Id is not "CS0751" and not "CS8795" and not "CS9388";

            protected override void VerifyFinalCompilation(Compilation compilation) => _verifyCompilation(compilation);
        }
    }
}
