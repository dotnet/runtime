// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Antlr4.Runtime;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    private sealed record SourceDirectiveValue(
        bool AutoIncrement,
        int StartLine,
        int StartColumn,
        int EndLine,
        int EndColumn,
        string? DocumentPath);

    private sealed record LanguageDirectiveValue(
        string Language,
        string? Vendor,
        string? DocumentType);

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
    internal bool IsAutoIncrementSourceDirective(IToken token) => token.Text == "#line";
#pragma warning restore CA1822

    internal object CreateSourceLine(bool autoIncrement, IToken line, IToken? path)
    {
        int lineNumber = ParseInt32(line);
        return CreateSourceDirective(autoIncrement, lineNumber, 0, lineNumber, 0, path);
    }

    internal object CreateSourceColumn(
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

    internal object CreateSourceColumnRange(
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

    internal object CreateSourceLineRange(
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

    internal object CreateSourceRange(
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

    internal void EndSourceDirective(CILParser.ExtSourceSpecContext context)
    {
        context.HasSyntaxError = EndSemanticRoot(context);
        if (context.HasSyntaxError ||
            context.Value is not SourceDirectiveValue value ||
            !CanApplySharedDirective(context))
        {
            context.Value = null;
            return;
        }

        ApplySourceDirective(value);
    }

    private void ApplySourceDirective(SourceDirectiveValue value)
    {
        if (value.DocumentPath is not null)
        {
            _currentDocumentPath = value.DocumentPath;
        }

        if (_currentMethod is null || _currentDocumentPath is null)
        {
            return;
        }

        int ilOffset = _currentMethod.Definition.MethodBody.Offset;
        _currentMethod.Definition.DebugInfo.DocumentPath ??= _currentDocumentPath;

        EntityRegistry.SequencePoint sequencePoint;
        if (value.StartLine == 0xFEEFEE)
        {
            sequencePoint = EntityRegistry.SequencePoint.Hidden(ilOffset);
        }
        else
        {
            int endColumn = value.EndColumn;
            if (value.EndLine == value.StartLine && endColumn == value.StartColumn)
            {
                endColumn++;
            }

            sequencePoint = new EntityRegistry.SequencePoint(
                ilOffset,
                value.StartLine,
                value.StartColumn,
                value.EndLine,
                endColumn);
        }

        List<EntityRegistry.SequencePoint> sequencePoints =
            _currentMethod.Definition.DebugInfo.SequencePoints;
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

    internal object CreateLanguageDirective(string language)
        => new LanguageDirectiveValue(language, null, null);

    internal object CreateLanguageDirective(string language, string vendor)
        => new LanguageDirectiveValue(language, vendor, null);

    internal object CreateLanguageDirective(
        string language,
        string vendor,
        string documentType)
        => new LanguageDirectiveValue(language, vendor, documentType);
#pragma warning restore CA1822

    internal void EndLanguageDirective(CILParser.LanguageDeclContext context)
    {
        context.HasSyntaxError = EndSemanticRoot(context);
        if (context.HasSyntaxError ||
            context.Value is not LanguageDirectiveValue value ||
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
        => context.Parent is not CILParser.MethodDeclContext || _currentMethod is not null;

}
