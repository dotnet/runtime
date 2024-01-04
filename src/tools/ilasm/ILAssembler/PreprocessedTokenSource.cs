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
        private readonly Stack<(ITokenSource Source, int ActiveIfDefBlocks)> _includeSourceStack = new();
        private readonly Func<string, ITokenSource> _loadIncludedDocument;

        private readonly Dictionary<string, string?> _definedVars = new();
        private readonly Stack<(string Var, bool Defined, bool IsElse)> _activeIfDefBlocks = new();

        public PreprocessedTokenSource(ITokenSource underlyingSource, Func<string, ITokenSource> loadIncludedDocument)
        {
            _includeSourceStack.Push((underlyingSource, 0));
            _loadIncludedDocument = loadIncludedDocument;
        }

        private ITokenSource CurrentTokenSource => _includeSourceStack.Peek().Source;
        private int ActiveIfDefBlocksInCurrentSource => _includeSourceStack.Peek().ActiveIfDefBlocks;

        public int Line => CurrentTokenSource.Line;

        public int Column => CurrentTokenSource.Column;

        public ICharStream InputStream => CurrentTokenSource.InputStream;

        // TODO: Update SourceName to output a string describing the whole "include" stack.
        public string SourceName => CurrentTokenSource.SourceName;

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

        public IToken NextToken()
        {
            IToken nextToken = NextTokenWithoutNestedEof(errorOnEof: ActiveIfDefBlocksInCurrentSource != 0);

            if (nextToken.Type == CILLexer.PP_INCLUDE)
            {
                var pathToken = NextTokenWithoutNestedEof(errorOnEof: true);
                if (pathToken.Type != CILLexer.QSTRING)
                {
                    ReportPreprocessorSyntaxError(nextToken);
                    return pathToken;
                }
                var path = pathToken.Text;
                // TODO: Parse out escape sequences and quotes in path.
                _includeSourceStack.Push((_loadIncludedDocument(path.Substring(1, path.Length - 2)), 0));
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
                    // TODO: Parse out escape sequences and quotes in path.
                    _definedVars.Add(identifier.Text, valueMaybe.Text.Substring(1, valueMaybe.Text.Length - 2));
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
                var (source, activeIfDef) = _includeSourceStack.Pop();
                _includeSourceStack.Push((source, --activeIfDef));
                return NextTokenWithoutNestedEof(activeIfDef != 0);
            }
            else if (nextToken.Type == CILLexer.ID && _definedVars.TryGetValue(nextToken.Text, out string? newValue) && newValue is not null)
            {
                // If token is an ID, we need to check for defined macro values and substitute.
                IWritableToken writableToken = (IWritableToken)nextToken;
                writableToken.Type = newValue.Contains(".") ? CILLexer.DOTTEDNAME : CILLexer.ID;
                writableToken.Text = newValue;
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
                var (source, activeIfDef) = _includeSourceStack.Pop();
                _includeSourceStack.Push((source, ++activeIfDef));
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
                    var (source, activeIfDef) = _includeSourceStack.Pop();
                    _includeSourceStack.Push((source, ++activeIfDef));

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
                var (source, activeIfDef) = _includeSourceStack.Pop();
                _includeSourceStack.Push((source, --activeIfDef));
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

        private static string GetErrorDisplay(int c)
        {
            string result = ((char)c).ToString();
            switch (c)
            {
                case -1:
                    result = "<EOF>";
                    break;
                case 10:
                    result = "\\n";
                    break;
                case 9:
                    result = "\\t";
                    break;
                case 13:
                    result = "\\r";
                    break;
            }

            return result;
        }
    }
}
