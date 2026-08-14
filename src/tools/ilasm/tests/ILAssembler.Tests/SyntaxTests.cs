// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Internal.IL;
using Xunit;
using DocumentCompilerTestHelpers = ILAssembler.Tests.DocumentCompilerTestHelpers;

namespace ILAssembler.Tests
{
    public class SyntaxTests
    {

        [Theory]
        [InlineData("\"hello\"", "hello")]
        [InlineData("\"hello\\tworld\"", "hello\tworld")]
        [InlineData("\"hello\\nworld\"", "hello\nworld")]
        [InlineData("\"hello\\rworld\"", "hello\rworld")]
        [InlineData("\"\\\"quoted\\\"\"", "\"quoted\"")]
        [InlineData("\"back\\\\slash\"", "back\\slash")]
        [InlineData("\"null\\0char\"", "null\0char")]
        [InlineData("\"octal\\101\"", "octalA")]  // \101 = 65 = 'A'
        public void StringHelpers_ParsesEscapeSequences(string input, string expected)
        {
            var result = StringHelpers.ParseQuotedString(input);
            Assert.Equal(expected, result);
        }


        [Fact]
        public void Diagnostic_LiteralOutOfRange()
        {
            // An integer literal that overflows
            string source = """
                .class public auto ansi beforefieldinit Test
                {
                    .pack 99999999999999999999999999999999
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.LiteralOutOfRange, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }


        [Fact]
        public void Diagnostic_FileNotFound()
        {
            // Reference a file that doesn't exist in an exported type declaration
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class extern public MyExportedType
                {
                    .file NonExistentFile.dll
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            // Expect FileNotFound error + MissingExportedTypeImplementation warning
            Assert.Equal(2, diagnostics.Length);
            var error = diagnostics.First(d => d.Severity == DiagnosticSeverity.Error);
            Assert.Equal(DiagnosticIds.FileNotFound, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }


        [Fact]
        public void Diagnostic_ByteArrayTooShort()
        {
            // A bytearray that's too short for the data type being loaded
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }

                .class public auto ansi beforefieldinit Test extends [mscorlib]System.Object
                {
                    .method public static float64 TestMethod() cil managed
                    {
                        ldc.r8 bytearray (AA BB)
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.ByteArrayTooShort, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }


        [Fact]
        public void FloatLiteral_TrailingDot()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void M() cil managed
                    {
                        ldc.r4 0.
                        pop
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }


        [Fact]
        public void FloatLiteral_SignedExponent()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void M() cil managed
                    {
                        ldc.r8 5.1234567890000001e+054
                        pop
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }


        [Fact]
        public void TrailingDotFloat()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly TestFloat { }

                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static float64 M() cil managed
                    {
                        ldc.r8 1.
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }


        [Fact]
        public void Int64MinValue_Accepted()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly TestInt64Min { }

                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static int64 M() cil managed
                    {
                        ldc.i8 -9223372036854775808
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }


        [Theory]
        [InlineData("""
            .assembly extern mscorlib { }
            .assembly test { }
            .class public auto ansi Test
            {
            """)]
        [InlineData("""
            .assembly extern mscorlib { }
            .assembly test { }
            .namespace NS
            {
                .class public auto ansi Test
                {
                    .method public static void M() cil managed
                    {
            """)]
        [InlineData(".class public auto ansi")]
        [InlineData(".method public static void")]
        [InlineData("""
            .assembly extern mscorlib { }
            .assembly test { }
            .class public auto ansi Test
            {
                .method public static void M() cil managed
                {
                    .try
            """)]
        [InlineData("""
            .assembly extern mscorlib { }
            .assembly test { }
            .class public auto ansi Test
            {
                .method public static void M(int32 .method cil managed
                {
                    .maxstack 2
                    ret
                }
            }
            """)]
        [InlineData("""
            .assembly extern mscorlib { }
            .assembly test { }
            .class public auto ansi
            {
                .method public instance void M() cil managed
                {
                    .override [mscorlib]System.Object::ToString
                    ret
                }
            }
            """)]
        public void TruncatedDocument_ReportsDiagnosticsInsteadOfThrowing(string source)
        {
            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());

            Assert.Contains(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        }

        [Theory]
        [InlineData(".assembly extern { }")]
        [InlineData(".mresource public { }")]
        [InlineData(".class public auto ansi Test { .event { } }")]
        [InlineData(".class public auto ansi Test { .property { } }")]
        [InlineData("""
            .class public auto ansi Test
            {
                .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string) = { string( }
            }
            """)]
        [InlineData("""
            .class public auto ansi Test
            {
                .method public static void M(int32,) cil managed
                {
                    ret
                }
            }
            """)]
        [InlineData(".class public auto ansi Test { .field public }")]
        [InlineData(".typedef")]
        [InlineData(".custom")]
        [InlineData(".class flags( public Test { }")]
        [InlineData(".class public auto ansi Test<+> { }")]
        [InlineData(".class public auto ansi Test { .field marshal( int32 F }")]
        [InlineData(".class public auto ansi Test { .field public int32 F = bytearray( }")]
        [InlineData("""
            .class public auto ansi Test
            {
                .method pinvokeimpl( public static void M() cil managed
                {
                    ret
                }
            }
            """)]
        [InlineData("""
            .class public auto ansi Test
            {
                .method public static void M(,) cil managed
                {
                    ret
                }
            }
            """)]
        [InlineData("""
            .class public auto ansi Test
            {
                .method public static void M() cil managed
                {
                    .custom
                    ret
                }
            }
            """)]
        [InlineData(".permission demand class X (Name = )")]
        [InlineData(".class extern { }")]
        [InlineData(".class public auto ansi Test { .export public { } }")]
        [InlineData(".assembly extern Name { .ver : }")]
        public void MalformedTypedGrammarValues_ReportParserDiagnosticsInsteadOfThrowing(string source)
        {
            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());

            Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "Parser");
        }

        public static TheoryData<string, bool> TruncatedDirectiveMutations
        {
            get
            {
                string[] sources =
                [
                    ".assembly extern Dependency { .publickeytoken = (01 02 03 04) .ver 1:2:3:4 }",
                    ".mresource public Resource { .assembly extern Dependency }",
                    ".class extern public Exported { .assembly extern Dependency }",
                    ".typedef method instance void [mscorlib]System.Object::.ctor() as Constructor",
                    ".permission demand [mscorlib]System.Security.Permissions.SecurityPermissionAttribute = { }",
                    """
                    .class public auto ansi Test<T> extends [mscorlib]System.Object implements [mscorlib]System.IDisposable
                    {
                        .field public marshal(int32) int32 F = int32(1)
                        .event specialname [mscorlib]System.EventHandler E { }
                        .property specialname int32 P() { }
                        .method public static void M(int32 value) cil managed
                        {
                            .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = (01 00 00 00)
                            ret
                        }
                    }
                    """
                ];

                HashSet<string> uniqueMutations = new(StringComparer.Ordinal);
                TheoryData<string, bool> mutations = new();
                foreach (string source in sources)
                {
                    for (int i = 1; i < source.Length; i++)
                    {
                        if (!char.IsWhiteSpace(source[i - 1]) &&
                            char.IsWhiteSpace(source[i]))
                        {
                            string mutation = source.Substring(0, i);
                            if (uniqueMutations.Add(mutation))
                            {
                                mutations.Add(mutation, false);
                                mutations.Add(mutation, true);
                            }
                        }
                    }
                }

                return mutations;
            }
        }

        [Theory]
        [MemberData(nameof(TruncatedDirectiveMutations))]
        public void TruncatedDirectiveMutationCorpus_ReportsDiagnosticsInsteadOfThrowing(
            string source,
            bool errorTolerant)
        {
            ImmutableArray<Diagnostic> diagnostics =
                DocumentCompilerTestHelpers.CompileAndGetDiagnostics(
                    source,
                    new Options { ErrorTolerant = errorTolerant });

            Assert.Contains(
                diagnostics,
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        }

        [Fact]
        public void ParserErrorListener_ReportsSyntaxErrors()
        {
            // A method with a misplaced token should generate a parser error
            string source = """
                .assembly test { }
                .class public auto ansi MyClass
                {
                    .method public static void Test(int32 int32 int32) cil managed
                    {
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            // Parser should report a syntax error for the repeated int32 tokens
            Assert.Contains(diagnostics, d => d.Id == "Parser");
        }

    }
}
