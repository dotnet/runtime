// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Xunit;

namespace ILAssembler.Tests
{
    public class PreprocessedTokenSourceTests
    {
        [Fact]
        public void Define_Token_ExcludedFromStream()
        {
            string source = """
                #define X
                A
                """;

            ITokenSource lexer = CreateLexerForSource(source);
            PreprocessedTokenSource preprocessor = new PreprocessedTokenSource(lexer, NoIncludeDirectivesCallback);
            preprocessor.OnPreprocessorSyntaxError += NoLexerDiagnosticsCallback;
            BufferedTokenStream stream = new(preprocessor);
            stream.Fill();
            Assert.Collection(stream.GetTokens(),
                token =>
                {
                    Assert.Equal(CILLexer.ID, token.Type);
                    Assert.Equal("A", token.Text);
                },
                token => Assert.Equal(CILLexer.Eof, token.Type));
        }

        [Fact]
        public void IfDef_False_Tokens_RemovedFromStream()
        {
            string source = """
                #ifdef X
                A
                #endif
                B
                """;

            ITokenSource lexer = CreateLexerForSource(source);
            PreprocessedTokenSource preprocessor = new PreprocessedTokenSource(lexer, NoIncludeDirectivesCallback);
            preprocessor.OnPreprocessorSyntaxError += NoLexerDiagnosticsCallback;
            BufferedTokenStream stream = new(preprocessor);
            stream.Fill();
            Assert.Collection(stream.GetTokens(),
                token =>
                {
                    Assert.Equal(CILLexer.ID, token.Type);
                    Assert.Equal("B", token.Text);
                },
                token => Assert.Equal(CILLexer.Eof, token.Type));
        }

        [Fact]
        public void IfDef_True_Tokens_LeftInStream()
        {
            string source = """
                #define X
                #ifdef X
                A
                #endif
                B
                """;

            ITokenSource lexer = CreateLexerForSource(source);
            PreprocessedTokenSource preprocessor = new PreprocessedTokenSource(lexer, NoIncludeDirectivesCallback);
            preprocessor.OnPreprocessorSyntaxError += NoLexerDiagnosticsCallback;
            BufferedTokenStream stream = new(preprocessor);
            stream.Fill();
            Assert.Collection(stream.GetTokens(),
                token =>
                {
                    Assert.Equal(CILLexer.ID, token.Type);
                    Assert.Equal("A", token.Text);
                },
                token =>
                {
                    Assert.Equal(CILLexer.ID, token.Type);
                    Assert.Equal("B", token.Text);
                },
                token => Assert.Equal(CILLexer.Eof, token.Type));
        }

        [Fact]
        public void IfNDef_False_Tokens_RemovedFromStream()
        {
            string source = """
                #define X
                #ifndef X
                A
                #endif
                B
                """;

            ITokenSource lexer = CreateLexerForSource(source);
            PreprocessedTokenSource preprocessor = new PreprocessedTokenSource(lexer, NoIncludeDirectivesCallback);
            preprocessor.OnPreprocessorSyntaxError += NoLexerDiagnosticsCallback;
            BufferedTokenStream stream = new(preprocessor);
            stream.Fill();
            Assert.Collection(stream.GetTokens(),
                token =>
                {
                    Assert.Equal(CILLexer.ID, token.Type);
                    Assert.Equal("B", token.Text);
                },
                token => Assert.Equal(CILLexer.Eof, token.Type));
        }

        [Fact]
        public void IfNDef_True_Tokens_LeftInStream()
        {
            string source = """
                #ifndef X
                A
                #endif
                B
                """;

            ITokenSource lexer = CreateLexerForSource(source);
            PreprocessedTokenSource preprocessor = new PreprocessedTokenSource(lexer, NoIncludeDirectivesCallback);
            preprocessor.OnPreprocessorSyntaxError += NoLexerDiagnosticsCallback;
            BufferedTokenStream stream = new(preprocessor);
            stream.Fill();
            Assert.Collection(stream.GetTokens(),
                token =>
                {
                    Assert.Equal(CILLexer.ID, token.Type);
                    Assert.Equal("A", token.Text);
                },
                token =>
                {
                    Assert.Equal(CILLexer.ID, token.Type);
                    Assert.Equal("B", token.Text);
                },
                token => Assert.Equal(CILLexer.Eof, token.Type));
        }


        [Fact]
        public void IfDef_False_Else_Tokens_RemovedFromStream()
        {
            string source = """
                #ifdef X
                A
                #else
                B
                #endif
                """;

            ITokenSource lexer = CreateLexerForSource(source);
            PreprocessedTokenSource preprocessor = new PreprocessedTokenSource(lexer, NoIncludeDirectivesCallback);
            preprocessor.OnPreprocessorSyntaxError += NoLexerDiagnosticsCallback;
            BufferedTokenStream stream = new(preprocessor);
            stream.Fill();
            Assert.Collection(stream.GetTokens(),
                token =>
                {
                    Assert.Equal(CILLexer.ID, token.Type);
                    Assert.Equal("B", token.Text);
                },
                token => Assert.Equal(CILLexer.Eof, token.Type));
        }

        [Fact]
        public void IfDef_Else_True_Tokens_LeftInStream()
        {
            string source = """
                #define X
                #ifdef X
                A
                #else
                C
                #endif
                B
                """;

            ITokenSource lexer = CreateLexerForSource(source);
            PreprocessedTokenSource preprocessor = new PreprocessedTokenSource(lexer, NoIncludeDirectivesCallback);
            preprocessor.OnPreprocessorSyntaxError += NoLexerDiagnosticsCallback;
            BufferedTokenStream stream = new(preprocessor);
            stream.Fill();
            Assert.Collection(stream.GetTokens(),
                token =>
                {
                    Assert.Equal(CILLexer.ID, token.Type);
                    Assert.Equal("A", token.Text);
                },
                token =>
                {
                    Assert.Equal(CILLexer.ID, token.Type);
                    Assert.Equal("B", token.Text);
                },
                token => Assert.Equal(CILLexer.Eof, token.Type));
        }

        [Fact]
        public void IfNDef_Else_False_Tokens_RemovedFromStream()
        {
            string source = """
                #define X
                #ifndef X
                A
                #else
                B
                #endif
                """;

            ITokenSource lexer = CreateLexerForSource(source);
            PreprocessedTokenSource preprocessor = new PreprocessedTokenSource(lexer, NoIncludeDirectivesCallback);
            preprocessor.OnPreprocessorSyntaxError += NoLexerDiagnosticsCallback;
            BufferedTokenStream stream = new(preprocessor);
            stream.Fill();
            Assert.Collection(stream.GetTokens(),
                token =>
                {
                    Assert.Equal(CILLexer.ID, token.Type);
                    Assert.Equal("B", token.Text);
                },
                token => Assert.Equal(CILLexer.Eof, token.Type));
        }

        [Fact]
        public void IfNDef_Else_True_Tokens_LeftInStream()
        {
            string source = """
                #ifndef X
                A
                #else
                C
                #endif
                B
                """;

            ITokenSource lexer = CreateLexerForSource(source);
            PreprocessedTokenSource preprocessor = new PreprocessedTokenSource(lexer, NoIncludeDirectivesCallback);
            preprocessor.OnPreprocessorSyntaxError += NoLexerDiagnosticsCallback;
            BufferedTokenStream stream = new(preprocessor);
            stream.Fill();
            Assert.Collection(stream.GetTokens(),
                token =>
                {
                    Assert.Equal(CILLexer.ID, token.Type);
                    Assert.Equal("A", token.Text);
                },
                token =>
                {
                    Assert.Equal(CILLexer.ID, token.Type);
                    Assert.Equal("B", token.Text);
                },
                token => Assert.Equal(CILLexer.Eof, token.Type));
        }

        [Fact]
        public void IfDef_False_Empty_Body_Leaves_Else_Block_TokensInStream()
        {
            string source = """
                #ifdef X
                #else
                B
                #endif
                """;

            ITokenSource lexer = CreateLexerForSource(source);
            PreprocessedTokenSource preprocessor = new PreprocessedTokenSource(lexer, NoIncludeDirectivesCallback);
            preprocessor.OnPreprocessorSyntaxError += NoLexerDiagnosticsCallback;
            BufferedTokenStream stream = new(preprocessor);
            stream.Fill();
            Assert.Collection(stream.GetTokens(),
                token =>
                {
                    Assert.Equal(CILLexer.ID, token.Type);
                    Assert.Equal("B", token.Text);
                },
                token => Assert.Equal(CILLexer.Eof, token.Type));
        }

        [Fact]
        public void IfNDef_True_Empty_Body_Removes_Else_Block_Tokens()
        {
            string source = """
                #ifndef X
                #else
                A
                #endif
                B
                """;

            ITokenSource lexer = CreateLexerForSource(source);
            PreprocessedTokenSource preprocessor = new PreprocessedTokenSource(lexer, NoIncludeDirectivesCallback);
            preprocessor.OnPreprocessorSyntaxError += NoLexerDiagnosticsCallback;
            BufferedTokenStream stream = new(preprocessor);
            stream.Fill();
            Assert.Collection(stream.GetTokens(),
                token =>
                {
                    Assert.Equal(CILLexer.ID, token.Type);
                    Assert.Equal("B", token.Text);
                },
                token => Assert.Equal(CILLexer.Eof, token.Type));
        }

        [Fact]
        public void Include_Includes_Tokens_With_Original_Source()
        {
            string source1 = """
                A
                #include "source2.il"
                B
                """;

            string source2 = """
                X
                """;

            ITokenSource lexer = CreateLexerForSource(source1, nameof(source1));
            PreprocessedTokenSource preprocessor = new PreprocessedTokenSource(lexer, path =>
            {
                Assert.Equal($"{nameof(source2)}.il", path);
                return CreateLexerForSource(source2, nameof(source2));
            });
            preprocessor.OnPreprocessorSyntaxError += NoLexerDiagnosticsCallback;
            BufferedTokenStream stream = new(preprocessor);
            stream.Fill();
            Assert.Collection(stream.GetTokens(),
                token =>
                {
                    Assert.Equal(CILLexer.ID, token.Type);
                    Assert.Equal("A", token.Text);
                    Assert.Equal(nameof(source1), token.TokenSource.SourceName);
                },
                token =>
                {
                    Assert.Equal(CILLexer.ID, token.Type);
                    Assert.Equal("X", token.Text);
                    Assert.Equal(nameof(source2), token.TokenSource.SourceName);
                },
                token =>
                {
                    Assert.Equal(CILLexer.ID, token.Type);
                    Assert.Equal("B", token.Text);
                    Assert.Equal(nameof(source1), token.TokenSource.SourceName);
                },
                token => Assert.Equal(CILLexer.Eof, token.Type));
        }

        private void NoLexerDiagnosticsCallback(string arg1, int arg2, int arg3, string arg4)
        {
            Assert.Fail($"A lexer diagnostic was encountered at {arg2}:{arg3}. '{arg4}'");
        }

        private static ITokenSource CreateLexerForSource(string source, string? sourceName = null)
        {
            return new CILLexer(
                new AntlrInputStream(source)
                {
                    name = sourceName
                });
        }

        private static ITokenSource NoIncludeDirectivesCallback(string path)
        {
            Assert.Fail("The included-file callback was called when no #include was provided in source.");
            return null!;
        }
    }
}
