// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace ILAssembler
{
    public sealed class PreprocessedTokenSource : ITokenSource
    {
        private readonly Stack<(ITokenSource Source, int ActiveIfDefBlocks, string? IncludedFromFile, int IncludedFromLine)> _includeSourceStack = new();
        private readonly Func<string, ITokenSource> _loadIncludedDocument;
        private readonly Func<string, ITokenSource> _createLexer;

        private readonly Dictionary<string, string?> _definedVars = new();
        private readonly Stack<(string Var, bool Defined, bool IsElse)> _activeIfDefBlocks = new();

        public PreprocessedTokenSource(ITokenSource underlyingSource, Func<string, ITokenSource> loadIncludedDocument, Func<string, ITokenSource> createLexer)
        {
            _includeSourceStack.Push((underlyingSource, 0, null, 0));
            _loadIncludedDocument = loadIncludedDocument;
            _createLexer = createLexer;
        }

        private ITokenSource CurrentTokenSource => _includeSourceStack.Peek().Source;
        private int ActiveIfDefBlocksInCurrentSource => _includeSourceStack.Peek().ActiveIfDefBlocks;

        public int Line => CurrentTokenSource.Line;

        public int Column => CurrentTokenSource.Column;

        public ICharStream InputStream => CurrentTokenSource.InputStream;

        /// <summary>
        /// Returns the source name with include stack information for better error reporting.
        /// For nested includes, shows the full include chain.
        /// </summary>
        public string SourceName
        {
            get
            {
                var current = _includeSourceStack.Peek();
                string name = current.Source.SourceName;

                // If this is the root file (no include info), just return the name
                if (current.IncludedFromFile is null)
                {
                    return name;
                }

                // Build include stack description
                var sb = new StringBuilder(name);
                foreach (var frame in _includeSourceStack.Skip(1)) // Skip current, iterate parent frames
                {
                    sb.Append($" (included from '{frame.Source.SourceName}':{current.IncludedFromLine})");
                    current = frame;
                    if (frame.IncludedFromFile is null)
                    {
                        break;
                    }
                }

                return sb.ToString();
            }
        }

        public ITokenFactory TokenFactory { get => CurrentTokenSource.TokenFactory; set => CurrentTokenSource.TokenFactory = value; }

        private IToken NextTokenWithoutNestedEof(bool errorOnEof = false)
        {
            IToken nextToken = CurrentTokenSource.NextToken();

            if (nextToken.Type == CILLexer.Eof)
            {
                // Skip the nested file EOF token.
                // Native ILASM only failed to parse across include file boundaries for the following cases:
                // - A comment tries to cross the file boundary.
                // - The included file does not have at least one fully parsable rule.
                // - A preprocessor directive cannot be parsed across file boundaries.
                // As the second case is quite difficult to replicate and is due to YACC limitations, we'll only maintain the rest of the rules.
                if (errorOnEof)
                {
                    ReportPreprocessorSyntaxError(nextToken);
                }
                _includeSourceStack.Pop();
                if (_includeSourceStack.Count == 0)
                {
                    // If we hit EOF of our entry file, return the EOF token.
                    return nextToken;
                }
                nextToken = CurrentTokenSource.NextToken();
            }
            return nextToken;
        }

        // Queue of tokens produced by macro expansion re-lexing
        private readonly Queue<IToken> _macroExpansionQueue = new();

        public IToken NextToken()
        {
            // If we have queued tokens from a previous macro expansion, return them first
            if (_macroExpansionQueue.Count > 0)
            {
                return _macroExpansionQueue.Dequeue();
            }

            IToken nextToken = NextTokenWithoutNestedEof(errorOnEof: ActiveIfDefBlocksInCurrentSource != 0);

            if (nextToken.Type == CILLexer.PP_INCLUDE)
            {
                var pathToken = NextTokenWithoutNestedEof(errorOnEof: true);
                if (pathToken.Type != CILLexer.QSTRING)
                {
                    ReportPreprocessorSyntaxError(nextToken);
                    return pathToken;
                }
                var path = StringHelpers.ParseQuotedString(pathToken.Text);
                string currentFile = CurrentTokenSource.SourceName;
                int currentLine = nextToken.Line;
                _includeSourceStack.Push((_loadIncludedDocument(path), 0, currentFile, currentLine));
                return NextToken();
            }
            else if (nextToken.Type == CILLexer.PP_DEFINE)
            {
                IToken identifier = NextTokenWithoutNestedEof(errorOnEof: ActiveIfDefBlocksInCurrentSource != 0);
                if (identifier.Type != CILLexer.ID)
                {
                    ReportPreprocessorSyntaxError(identifier);
                    return identifier;
                }
                IToken valueMaybe = NextTokenWithoutNestedEof(errorOnEof: ActiveIfDefBlocksInCurrentSource != 0);
                if (valueMaybe.Type == CILLexer.QSTRING)
                {
                    _definedVars.Add(identifier.Text, StringHelpers.ParseQuotedString(valueMaybe.Text));
                    return NextToken();
                }
                else
                {
                    _definedVars.Add(identifier.Text, null);
                }
                nextToken = valueMaybe;
            }

            if (nextToken.Type == CILLexer.PP_IFDEF)
            {
                return ProcessIfDef(requireDefined: true);
            }
            else if (nextToken.Type == CILLexer.PP_IFNDEF)
            {
                return ProcessIfDef(requireDefined: false);
            }
            else if (nextToken.Type == CILLexer.PP_ELSE)
            {
                if (ActiveIfDefBlocksInCurrentSource == 0)
                {
                    ReportPreprocessorSyntaxError(nextToken);
                    return NextTokenWithoutNestedEof(false);
                }
                var (identifier, expectedDefined, isElse) = _activeIfDefBlocks.Pop();
                if (isElse)
                {
                    // Skip this #else token and set everything up such that we're still in the previous state.
                    _activeIfDefBlocks.Push((identifier, expectedDefined, isElse));
                    ReportPreprocessorSyntaxError(nextToken);
                    return NextTokenWithoutNestedEof(ActiveIfDefBlocksInCurrentSource != 0);
                }
                return ConsumeDisabledPreprocessorBlock(identifier, expectedDefined: !expectedDefined, elseCase: true);
            }
            else if (nextToken.Type == CILLexer.PP_ENDIF)
            {
                if (ActiveIfDefBlocksInCurrentSource == 0)
                {
                    ReportPreprocessorSyntaxError(nextToken);
                    return NextTokenWithoutNestedEof(false);
                }
                var (source, activeIfDef, includedFromFile, includedFromLine) = _includeSourceStack.Pop();
                _includeSourceStack.Push((source, --activeIfDef, includedFromFile, includedFromLine));
                return NextTokenWithoutNestedEof(activeIfDef != 0);
            }
            else if (nextToken.Type == CILLexer.ID && _definedVars.TryGetValue(nextToken.Text, out string? newValue) && newValue is not null)
            {
                // Re-lex the macro value to produce correct tokens.
                // This handles cases like #define NEG_INF "float32(0xFF800000)" where the
                // substituted value contains multiple tokens that must be individually lexed.
                var macroLexer = _createLexer(newValue);
                var tokens = new List<IToken>();
                for (var t = macroLexer.NextToken(); t.Type != Antlr4.Runtime.TokenConstants.EOF; t = macroLexer.NextToken())
                {
                    tokens.Add(t);
                }

                if (tokens.Count == 1)
                {
                    // Single token: modify in place (preserves source location info)
                    IWritableToken writableToken = (IWritableToken)nextToken;
                    writableToken.Type = tokens[0].Type;
                    writableToken.Text = tokens[0].Text;
                }
                else if (tokens.Count > 1)
                {
                    // Multiple tokens: return the first, queue the rest
                    IWritableToken writableToken = (IWritableToken)nextToken;
                    writableToken.Type = tokens[0].Type;
                    writableToken.Text = tokens[0].Text;
                    for (int i = 1; i < tokens.Count; i++)
                    {
                        _macroExpansionQueue.Enqueue(tokens[i]);
                    }
                }
                // If tokens.Count == 0 (empty macro value), just return the original token as-is
            }
            return nextToken;
        }

        private IToken ProcessIfDef(bool requireDefined)
        {
            IToken identifier = NextTokenWithoutNestedEof(errorOnEof: true);
            if (identifier.Type != CILLexer.ID)
            {
                ReportPreprocessorSyntaxError(identifier);
                return identifier;
            }
            if (_definedVars.ContainsKey(identifier.Text) != requireDefined)
            {
                return ConsumeDisabledPreprocessorBlock(identifier.Text, expectedDefined: requireDefined, elseCase: false);
            }
            else
            {
                _activeIfDefBlocks.Push((identifier.Text, Defined: requireDefined, IsElse: false));
                var (source, activeIfDef, includedFromFile, includedFromLine) = _includeSourceStack.Pop();
                _includeSourceStack.Push((source, ++activeIfDef, includedFromFile, includedFromLine));
                return NextToken();
            }
        }
        private IToken ConsumeDisabledPreprocessorBlock(string var, bool expectedDefined, bool elseCase)
        {
            int numNestedPreprocessorBlocks = 0;
            for (IToken nextToken = NextTokenWithoutNestedEof(errorOnEof: true); nextToken.Type != CILLexer.PP_ENDIF || numNestedPreprocessorBlocks != 0; nextToken = NextTokenWithoutNestedEof(errorOnEof: true))
            {
                if (nextToken.Type == CILLexer.Eof)
                {
                    return nextToken;
                }

                // If we've seen any #ifdef or #ifndef tokens,
                // then we'll only check syntax of preprocessor tokens and ignore all other tokens until we've seen a matching #endif
                if (numNestedPreprocessorBlocks > 0)
                {
                    if (nextToken.Type == CILLexer.PP_IFDEF || nextToken.Type == CILLexer.PP_ENDIF)
                    {
                        IToken identifier = NextTokenWithoutNestedEof(errorOnEof: true);
                        if (identifier.Type != CILLexer.ID)
                        {
                            ReportPreprocessorSyntaxError(identifier);
                        }
                        numNestedPreprocessorBlocks++;
                    }
                    if (nextToken.Type == CILLexer.PP_ENDIF)
                    {
                        numNestedPreprocessorBlocks--;
                    }
                    continue;
                }

                if (nextToken.Type == CILLexer.PP_ELSE)
                {
                    if (elseCase)
                    {
                        ReportPreprocessorSyntaxError(nextToken);
                        return NextTokenWithoutNestedEof(errorOnEof: true);
                    }

                    _activeIfDefBlocks.Push((var, Defined: !expectedDefined, IsElse: true));
                    var (source, activeIfDef, includedFromFile, includedFromLine) = _includeSourceStack.Pop();
                    _includeSourceStack.Push((source, ++activeIfDef, includedFromFile, includedFromLine));

                    return NextTokenWithoutNestedEof(errorOnEof: true);
                }
                if (nextToken.Type == CILLexer.PP_IFDEF || nextToken.Type == CILLexer.PP_ENDIF)
                {
                    numNestedPreprocessorBlocks++;
                }
            }

            // If we're skipping an inactive else case, then we're still tracking the active ifdef block.
            if (elseCase)
            {
                var (source, activeIfDef, includedFromFile, includedFromLine) = _includeSourceStack.Pop();
                _includeSourceStack.Push((source, --activeIfDef, includedFromFile, includedFromLine));
            }
            return NextTokenWithoutNestedEof(ActiveIfDefBlocksInCurrentSource != 0);
        }

        public event Action<string, int, int, string>? OnPreprocessorSyntaxError;

        private void ReportPreprocessorSyntaxError(IToken token)
        {
            string text = token.TokenSource.InputStream.GetText(Interval.Of(token.StartIndex, token.TokenSource.InputStream.Index));
            string msg = "preprocessor syntax error at: '" + GetErrorDisplay(text) + "'";
            OnPreprocessorSyntaxError?.Invoke(token.TokenSource.SourceName, token.StartIndex, token.StopIndex - token.StartIndex, msg);
        }

        private static string GetErrorDisplay(string s)
        {
            StringBuilder stringBuilder = new StringBuilder();
            var array = s.AsSpan();
            foreach (char c in array)
            {
                stringBuilder.Append(GetErrorDisplay(c));
            }

            return stringBuilder.ToString();
        }

        private static string GetErrorDisplay(int c) => c switch
        {
            -1 => "<EOF>",
            10 => "\\n",
            9  => "\\t",
            13 => "\\r",
            _  => ((char)c).ToString()
        };
    }
}
