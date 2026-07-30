// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Testing;
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
    }
}
