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

        ApplySourceDirective(value, context.Start);
    }

    /// <summary>
    /// Applies a <c>.line</c> or <c>#line</c> directive to the source it appears in: a non-empty file name defines
    /// that file as a PDB document and makes it the source's current document, and the directive's coordinates
    /// become the coordinates of the instructions that follow in that source. The directive records no sequence
    /// point itself; the next instruction does (<see cref="RecordSequencePoint"/>), wherever it is.
    /// </summary>
    /// <param name="value">The directive.</param>
    /// <param name="directiveToken">The directive's <c>.line</c> or <c>#line</c> token, which identifies its source.</param>
    /// <remarks>
    /// As in native ilasm, the directive stays in effect until the next directive or the end of its source (the input
    /// file, or one inclusion of an <c>#include</c>d file), wherever it is and across methods, and the next
    /// instruction always gets a point. An empty file name (<c>''</c> or <c>""</c>), which ildasm writes for "the
    /// same file", and a missing one keep the source's current document: the source itself until a directive in it
    /// names another file. A start line of <c>0xFEEFEE</c> makes the points hidden.
    /// </remarks>
    private void ApplySourceDirective(SourceDirectiveValue value, IToken directiveToken)
    {
        SourceLineState state = GetSourceLineState(directiveToken);
        if (!string.IsNullOrEmpty(value.DocumentPath))
        {
            state.Document = _pdbDocuments.GetOrAdd(value.DocumentPath, _currentLanguageGuid);
        }

        int endColumn = value.EndColumn;
        if (value.EndLine == value.StartLine && endColumn == value.StartColumn)
        {
            endColumn++;
        }

        // A start line of 0xFEEFEE makes the points hidden (EntityRegistry.SequencePoint.IsHidden); their
        // columns are not written.
        state.DirectiveCoordinates = (value.StartLine, value.StartColumn, value.EndLine, endColumn);
        _lastSequencePointSpan = null;
    }

    /// <summary>
    /// Records the sequence point of an instruction that is about to be emitted at the method body's current IL
    /// offset, when a PDB is requested (<see cref="GeneratesPdb"/>).
    /// </summary>
    /// <param name="method">The method the instruction is emitted into.</param>
    /// <param name="opcodeToken">The instruction's opcode token, which identifies its source and its line.</param>
    /// <remarks>
    /// <para>
    /// With no <c>.line</c> or <c>#line</c> directive in effect in the instruction's source, the point is on the
    /// instruction's own line of that source (the input <c>.il</c> file or the <c>#include</c>d file), columns 1 to
    /// 2, as native ilasm records it; otherwise it has the coordinates of the directive in effect, in the source's
    /// current document (<see cref="ApplySourceDirective"/>).
    /// </para>
    /// <para>
    /// As in native ilasm, the instruction gets a point only when its span differs from the last point's, in any
    /// method, or a directive has been applied since. So a later method without a <c>.line</c> of its own gets no
    /// point while an earlier directive's span is still current, which is the shape ildasm writes for a method that
    /// had no sequence points, when it writes <c>.line</c> directives at all. Unlike native ilasm, which compares
    /// only lines and columns, the span includes the document.
    /// </para>
    /// </remarks>
    private void RecordSequencePoint(CurrentMethodContext method, IToken opcodeToken)
    {
        if (!GeneratesPdb)
        {
            return;
        }

        SourceLineState state = GetSourceLineState(opcodeToken);
        int document = state.Document ??= _pdbDocuments.GetOrAdd(state.SourceName, _currentLanguageGuid);
        (int startLine, int startColumn, int endLine, int endColumn) =
            state.DirectiveCoordinates ?? (opcodeToken.Line, 1, opcodeToken.Line, 2);
        var span = new SequencePointSpan(document, startLine, startColumn, endLine, endColumn);
        if (_lastSequencePointSpan == span)
        {
            return;
        }

        _lastSequencePointSpan = span;
        int ilOffset = method.Definition.MethodBody.Offset;
        var point = new EntityRegistry.SequencePoint(document, ilOffset, startLine, startColumn, endLine, endColumn);
        List<EntityRegistry.SequencePoint> sequencePoints = method.Definition.DebugInfo.SequencePoints;
        Debug.Assert(sequencePoints.Count == 0 || sequencePoints[^1].ILOffset <= ilOffset, "IL offsets never decrease.");
        if (sequencePoints.Count > 0 && sequencePoints[^1].ILOffset == ilOffset)
        {
            // The last point's instruction wrote no bytes, so this instruction is the one at that offset. The blob
            // cannot hold two points at one offset: a zero offset delta is read as a document-record.
            sequencePoints[^1] = point;
        }
        else
        {
            sequencePoints.Add(point);
        }
    }

    /// <summary>Gets the <c>.line</c> state of the source a token was read from, creating it on first use.</summary>
    private SourceLineState GetSourceLineState(IToken token)
    {
        object source = (object?)token.TokenSource ?? Location.GetSourceName(token);
        if (!_sourceLineStates.TryGetValue(source, out SourceLineState? state))
        {
            state = new SourceLineState(Location.GetSourceName(token));
            _sourceLineStates.Add(source, state);
        }

        return state;
    }

    /// <summary>
    /// The <c>.line</c> state of one source of the input file being parsed: the input file itself, or one inclusion
    /// of an <c>#include</c>d file. As in native ilasm, which parses each input file and each inclusion in its own
    /// environment, a directive applies only to the rest of its source: an included file starts without one, and
    /// does not change the state of the source that includes it.
    /// </summary>
    /// <param name="sourceName">The source's <see cref="SourceText.Path"/>.</param>
    private sealed class SourceLineState(string sourceName)
    {
        /// <summary>Gets the source's name, which is the name of its own PDB document.</summary>
        public string SourceName { get; } = sourceName;

        /// <summary>
        /// Gets or sets the index in the compilation's <see cref="PdbDocumentTable"/> of the source's current document:
        /// the file named by the last directive of the source that named one, otherwise the source itself. It is
        /// <see langword="null"/> while it is the source itself and no point has needed it, so that an included
        /// file becomes a document only when one of its instructions gets a point.
        /// </summary>
        public int? Document { get; set; }

        /// <summary>
        /// Gets or sets the start line, start column, end line and end column of the last directive applied in the
        /// source, or <see langword="null"/> when none has been.
        /// </summary>
        public (int StartLine, int StartColumn, int EndLine, int EndColumn)? DirectiveCoordinates { get; set; }
    }

    /// <summary>The document and source span of a recorded sequence point.</summary>
    private readonly record struct SequencePointSpan(int Document, int StartLine, int StartColumn, int EndLine, int EndColumn);

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
