// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
    internal bool IsAutoIncrementSourceDirective(IToken token) => token.Text == "#line";
#pragma warning restore CA1822

    internal SourceDirectiveValue CreateSourceLine(
        bool autoIncrement,
        IToken line,
        IToken? path)
    {
        int lineNumber = ParseInt32(line);
        return CreateSourceDirective(autoIncrement, lineNumber, 0, lineNumber, 0, path);
    }

    internal SourceDirectiveValue CreateSourceColumn(
        bool autoIncrement,
        IToken line,
        IToken column,
        IToken? path)
    {
        int lineNumber = ParseInt32(line);
        int columnNumber = ParseInt32(column);
        return CreateSourceDirective(
            autoIncrement,
            lineNumber,
            columnNumber,
            lineNumber,
            columnNumber,
            path);
    }

    internal SourceDirectiveValue CreateSourceColumnRange(
        bool autoIncrement,
        IToken line,
        IToken startColumn,
        IToken endColumn,
        IToken? path)
    {
        int lineNumber = ParseInt32(line);
        return CreateSourceDirective(
            autoIncrement,
            lineNumber,
            ParseInt32(startColumn),
            lineNumber,
            ParseInt32(endColumn),
            path);
    }

    internal SourceDirectiveValue CreateSourceLineRange(
        bool autoIncrement,
        IToken startLine,
        IToken endLine,
        IToken column,
        IToken? path)
    {
        int columnNumber = ParseInt32(column);
        return CreateSourceDirective(
            autoIncrement,
            ParseInt32(startLine),
            columnNumber,
            ParseInt32(endLine),
            columnNumber,
            path);
    }

    internal SourceDirectiveValue CreateSourceRange(
        bool autoIncrement,
        IToken startLine,
        IToken endLine,
        IToken startColumn,
        IToken endColumn,
        IToken? path)
        => CreateSourceDirective(
            autoIncrement,
            ParseInt32(startLine),
            ParseInt32(startColumn),
            ParseInt32(endLine),
            ParseInt32(endColumn),
            path);

    private static SourceDirectiveValue CreateSourceDirective(
        bool autoIncrement,
        int startLine,
        int startColumn,
        int endLine,
        int endColumn,
        IToken? path)
        => new(
            autoIncrement,
            startLine,
            startColumn,
            endLine,
            endColumn,
            path is null ? null : StringHelpers.ParseQuotedString(path.Text));

    internal void EndSourceDirective(
        CILParser.ExtSourceSpecContext context,
        int initialSyntaxErrorCount)
    {
        context.HasSyntaxError =
            HasSyntaxErrorsSince(initialSyntaxErrorCount) ||
            context.exception is not null;
        if (context.HasSyntaxError ||
            context.Value is not { } value ||
            !CanApplySharedDirective(context))
        {
            context.Value = null;
            return;
        }

        ApplySourceDirective(value);
    }

    /// <summary>
    /// Applies a <c>.line</c> or <c>#line</c> directive: a non-empty file name defines that file as a PDB
    /// document and makes it the current document, and inside a method body the directive adds a sequence point
    /// at the current IL offset in the current document.
    /// </summary>
    /// <remarks>
    /// An empty file name (<c>''</c> or <c>""</c>) leaves the current document unchanged, as in native ilasm:
    /// ildasm writes it for "the same file as the previous directive", including on the first directive of a
    /// method. A directive without a file name uses the current document, which is the input file until a
    /// directive names another. A directive at the same offset as the previous point replaces that point,
    /// coordinates and document. There is always a current document here: <see cref="DocumentCompiler"/> calls
    /// <see cref="BeginDocument"/>, which defines the input file, before it parses anything.
    /// </remarks>
    private void ApplySourceDirective(SourceDirectiveValue value)
    {
        Debug.Assert(_currentDocument >= 0, "BeginDocument defines the input file before any directive is applied.");
        if (!string.IsNullOrEmpty(value.DocumentPath))
        {
            _currentDocument = _pdbDocuments.GetOrAdd(value.DocumentPath, _currentLanguageGuid);
        }

        if (_currentMethod is null)
        {
            return;
        }

        int document = _currentDocument;
        int ilOffset = _currentMethod.Definition.MethodBody.Offset;
        EntityRegistry.MethodDebugInfo debugInfo = _currentMethod.Definition.DebugInfo;

        EntityRegistry.SequencePoint sequencePoint;
        if (value.StartLine == 0xFEEFEE)
        {
            sequencePoint = EntityRegistry.SequencePoint.Hidden(document, ilOffset);
        }
        else
        {
            int endColumn = value.EndColumn;
            if (value.EndLine == value.StartLine && endColumn == value.StartColumn)
            {
                endColumn++;
            }

            sequencePoint = new EntityRegistry.SequencePoint(
                document,
                ilOffset,
                value.StartLine,
                value.StartColumn,
                value.EndLine,
                endColumn);
        }

        List<EntityRegistry.SequencePoint> sequencePoints = debugInfo.SequencePoints;
        if (sequencePoints.Count > 0 && sequencePoints[^1].ILOffset == ilOffset)
        {
            sequencePoints[^1] = sequencePoint;
        }
        else
        {
            sequencePoints.Add(sequencePoint);
        }
    }

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
    internal string ParseLanguageString(IToken token)
        => StringHelpers.ParseQuotedString(token.Text);

    internal LanguageDirectiveValue CreateLanguageDirective(string language)
        => new LanguageDirectiveValue(language, null, null);

    internal LanguageDirectiveValue CreateLanguageDirective(string language, string vendor)
        => new LanguageDirectiveValue(language, vendor, null);

    internal LanguageDirectiveValue CreateLanguageDirective(
        string language,
        string vendor,
        string documentType)
        => new LanguageDirectiveValue(language, vendor, documentType);
#pragma warning restore CA1822

    internal void EndLanguageDirective(
        CILParser.LanguageDeclContext context,
        int initialSyntaxErrorCount)
    {
        context.HasSyntaxError =
            HasSyntaxErrorsSince(initialSyntaxErrorCount) ||
            context.exception is not null;
        if (context.HasSyntaxError ||
            context.Value is not { } value ||
            !CanApplySharedDirective(context))
        {
            context.Value = null;
            return;
        }

        if (Guid.TryParse(value.Language, out Guid language))
        {
            _currentLanguageGuid = language;
        }
        if (value.Vendor is not null && Guid.TryParse(value.Vendor, out Guid vendor))
        {
            _currentLanguageVendorGuid = vendor;
        }
        if (value.DocumentType is not null &&
            Guid.TryParse(value.DocumentType, out Guid documentType))
        {
            _currentDocumentTypeGuid = documentType;
        }
    }

    private bool CanApplySharedDirective(ParserRuleContext context)
        => !IsDeclarationSuppressed &&
            (context.Parent is not CILParser.MethodDeclContext || _currentMethod is not null);

}
