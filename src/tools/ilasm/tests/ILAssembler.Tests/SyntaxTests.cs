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
using Antlr4.Runtime;
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
        public void StringCharStream_SeekPastEnd_ClampsToEnd()
        {
            Type streamType = typeof(DocumentCompiler).Assembly.GetType(
                "ILAssembler.StringCharStream",
                throwOnError: true)!;
            var stream = (ICharStream)Activator.CreateInstance(
                streamType,
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: ["abc", "test.il"],
                culture: null)!;

            stream.Seek(10);

            Assert.Equal(stream.Size, stream.Index);
            Assert.Equal(TokenConstants.EOF, stream.LA(1));
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

        [Theory]
        [InlineData("not-a-number", double.MaxValue)]
        [InlineData("-not-a-number", double.MinValue)]
        public void InvalidFloatingLiteral_SaturatesWithOriginalSign(string text, double expected)
        {
            object actions = CreateGrammarActions();
            Type grammarActionsType = actions.GetType();
            MethodInfo parseFloatingLiteral = grammarActionsType.GetMethod(
                "ParseFloatingLiteral",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            var token = new CommonToken(CILLexer.FLOAT64, text);

            Assert.Equal(expected, (double)parseFloatingLiteral.Invoke(actions, [token])!);
        }

        [Theory]
        [InlineData("ParseBoolean", 0)]
        [InlineData("ParseFileAttribute", 1)]
        [InlineData("ParseSecurityAction", 1)]
        [InlineData("ParseVTableFixupAttribute", 0)]
        [InlineData("ParseManifestResourceAttribute", 0)]
        public void InvalidSingleTokenConversion_ReturnsSafeFallback(
            string methodName,
            int expected)
        {
            object actions = CreateGrammarActions();
            MethodInfo conversion = actions.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            var token = new CommonToken(CILLexer.ID, "invalid");

            object result = conversion.Invoke(actions, [token])!;

            Assert.Equal(expected, Convert.ToInt32(result));
        }

        [Fact]
        public void InvalidContextSingleTokenConversions_ReturnSafeFallback()
        {
            object actions = CreateGrammarActions();
            Type actionsType = actions.GetType();
            var token = new CommonToken(CILLexer.ID, "invalid");
            var parent = new ParserRuleContext();
            var assemblyContext = new CILParser.AsmAttrAnyContext(parent, invokingState: 0)
            {
                Start = token,
                Stop = token,
            };
            var exportedTypeContext = new CILParser.ExptAttrContext(parent, invokingState: 0)
            {
                Start = token,
                Stop = token,
            };

            actionsType.GetMethod(
                "SetAssemblyAttribute",
                BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(actions, [assemblyContext]);
            actionsType.GetMethod(
                "SetExportedTypeAttribute",
                BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(actions, [exportedTypeContext]);

            Assert.Equal(0, (int)assemblyContext.Value);
            Assert.Equal(0, (int)assemblyContext.Mask);
            Assert.Equal(0, (int)exportedTypeContext.Value);
            Assert.Equal(0, (int)exportedTypeContext.Mask);
        }

        [Fact]
        public void SyntheticToken_SourceSpanIsClamped()
        {
            var token = new CommonToken(CILLexer.Eof)
            {
                StartIndex = -1,
                StopIndex = -1,
            };
            MethodInfo getSourceSpan = typeof(Location).GetMethod(
                "GetSourceSpan",
                BindingFlags.Static | BindingFlags.NonPublic)!;

            SourceSpan span = (SourceSpan)getSourceSpan.Invoke(obj: null, [token])!;

            Assert.Equal(0, span.Start);
            Assert.Equal(0, span.Length);
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
            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(
                source,
                new Options { ErrorTolerant = true });

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
        [InlineData(".class public auto ansi Test { .field public static literal bool F = bool(invalid true) }")]
        [InlineData(".class extern { }")]
        [InlineData(".class public auto ansi Test { .export public { } }")]
        [InlineData(".assembly extern Name { .ver : }")]
        public void MalformedTypedGrammarValues_ReportParserDiagnosticsInsteadOfThrowing(string source)
        {
            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(
                source,
                new Options { ErrorTolerant = true });

            Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "Parser");
        }

        private static object CreateGrammarActions()
        {
            Type grammarActionsType = typeof(DocumentCompiler).Assembly.GetType(
                "ILAssembler.GrammarActions",
                throwOnError: true)!;
            return Activator.CreateInstance(
                grammarActionsType,
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args:
                [
                    new Dictionary<string, SourceText>(),
                    new Options(),
                    (Func<string, byte[]?>)(_ => throw new InvalidOperationException("Unexpected resource")),
                ],
                culture: null)!;
        }

        [Theory]
        [InlineData("[mscorlib]System.Object")]
        [InlineData("[.module Other.netmodule]System.Object")]
        [InlineData("System.Object")]
        [InlineData("class [mscorlib]System.Object")]
        [InlineData("class [.module Other.netmodule]System.Object")]
        [InlineData("class [mscorlib]Generic`1<int32>")]
        public void EmptyClasses_DoNotBufferFollowingDeclarations(string baseType)
        {
            var source = new StringBuilder(".assembly extern mscorlib { } .module extern Other.netmodule ");
            for (int i = 0; i < 100; i++)
            {
                source.Append($".class public C{i} extends {baseType} {{ }} ");
            }

            var tokens = new MeasuringTokenStream(new CILLexer(new AntlrInputStream(source.ToString())));
            CILParser parser = CreateParser(tokens);

            parser.decls();

            Assert.Equal(0, parser.NumberOfSyntaxErrors);
            Assert.Equal(TokenConstants.EOF, tokens.LA(1));
            Assert.InRange(tokens.MaximumBufferedTokens, 1, 64);
        }

        [Theory]
        [InlineData("[Scope]", false)]
        [InlineData("method void [Scope]::Target()", false)]
        [InlineData("field int32 [Scope]::Value", false)]
        [InlineData("[.module Scope]", true)]
        [InlineData("method void [.module Scope]::Target()", true)]
        [InlineData("field int32 [.module Scope]::Value", true)]
        public void BareScopes_ArePreservedForOwners(string source, bool isModule)
        {
            var tokens = new UnbufferedTokenStream(new CILLexer(new AntlrInputStream(source)));
            CILParser parser = CreateParser(tokens);

            CILParser.OwnerTypeContext owner = parser.ownerType();

            Assert.Equal(0, parser.NumberOfSyntaxErrors);
            Assert.False(owner.HasSyntaxError);
            Assert.Equal(TokenConstants.EOF, tokens.LA(1));
            CILParser.TypeSpecificationValue? scope = owner.Value switch
            {
                CILParser.TypeOwnerValue type => type.Type,
                CILParser.MemberOwnerValue
                {
                    Member: CILParser.MethodMemberReferenceValue
                    {
                        Method: CILParser.ParsedMethodReferenceValue method
                    }
                } => method.Owner,
                CILParser.MemberOwnerValue
                {
                    Member: CILParser.FieldMemberReferenceValue
                    {
                        Field: CILParser.ParsedFieldReferenceValue field
                    }
                } => field.Owner,
                _ => throw new InvalidOperationException($"Unexpected owner: {owner.Value}")
            };
            if (isModule)
            {
                Assert.Equal("Scope", Assert.IsType<CILParser.ModuleTypeSpecificationValue>(scope).ModuleName);
            }
            else
            {
                Assert.Equal("Scope", Assert.IsType<CILParser.AssemblyTypeSpecificationValue>(scope).AssemblyName);
            }
        }

        private static CILParser CreateParser(ITokenStream tokens)
        {
            var parser = new CILParser(tokens) { BuildParseTree = false };
            typeof(CILParser).GetProperty("Actions", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(parser, CreateGrammarActions());

            return parser;
        }

        private sealed class MeasuringTokenStream(ITokenSource source) : UnbufferedTokenStream(source)
        {
            public int MaximumBufferedTokens { get; private set; }

            public override void Release(int marker)
            {
                MaximumBufferedTokens = Math.Max(MaximumBufferedTokens, n);
                base.Release(marker);
            }
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
                        .method public static void M(int32 'value') cil managed
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

        [Theory]
        [InlineData("""
            .assembly extern Dependency
            {
                .publicKey = (01 02 03 04)
            }
            .assembly test { }
            """)]
        [InlineData("""
            .assembly test { }
            .language "3f5162f8-07c6-11d3-9053-00c04fa302a1"
            """)]
        [InlineData("""
            .assembly test { }
            .class Test
            {
                .method static void M() cil managed
                {
                    .line 1, 1 : 1, 2 "test.cs"
                    ret
                }
            }
            """)]
        [InlineData("""
            .assembly test { }
            .class Test
            {
                .method static void M(int32 value) cil managed
                {
                    ret
                }
            }
            """)]
        public void NativeIlasmUnsupportedSyntax_ReportsError(string source)
        {
            ImmutableArray<Diagnostic> diagnostics =
                DocumentCompilerTestHelpers.CompileAndGetDiagnostics(
                    source,
                    new Options { ErrorTolerant = true });

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
