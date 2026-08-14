// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Antlr4.Runtime;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    private readonly Stack<DataDeclarationFrame> _dataDeclarationFrames = new();

    private sealed class DataDeclarationFrame
    {
        public DataDeclarationFrame(CILParser.DataDeclContext owner, bool shouldCommit)
        {
            Owner = owner;
            ShouldCommit = shouldCommit;
        }

        public CILParser.DataDeclContext Owner { get; }

        public bool ShouldCommit { get; }

        public BlobBuilder Data { get; } = new();

        public Dictionary<string, List<Blob>>? ReferenceFixups { get; set; }

        public string? Name { get; set; }

        public byte Section { get; set; }
    }

    internal void BeginDataDeclaration(CILParser.DataDeclContext context)
    {
        BeginSemanticRoot(context);
        _dataDeclarationFrames.Push(new(
            context,
            context.Parent is not CILParser.MethodDeclContext || _currentMethod is not null));
    }

    internal void EndDataDeclaration(CILParser.DataDeclContext context)
    {
        bool hasSyntaxError = EndSemanticRoot(context);
        context.HasSyntaxError = hasSyntaxError;

        DataDeclarationFrame? frame = TryGetDataDeclarationFrame(context);
        if (frame is null)
        {
            return;
        }

        _dataDeclarationFrames.Pop();
        if (hasSyntaxError || !frame.ShouldCommit)
        {
            return;
        }

        int declarationOffset = _mappedFieldData.Count;
        if (frame.Name is not null && !_mappedFieldDataNames.ContainsKey(frame.Name))
        {
            _mappedFieldDataNames.Add(frame.Name, declarationOffset);
        }

        _mappedFieldData.LinkSuffix(frame.Data);
        if (frame.ReferenceFixups is null)
        {
            return;
        }

        foreach ((string target, List<Blob> declarationFixups) in frame.ReferenceFixups)
        {
            if (!_mappedFieldDataReferenceFixups.TryGetValue(target, out List<Blob>? fixups))
            {
                _mappedFieldDataReferenceFixups.Add(target, fixups = new());
            }

            fixups.AddRange(declarationFixups);
        }
    }

    internal void SetDataDeclarationHeader(
        CILParser.DdHeadContext context,
        byte section,
        IToken name)
    {
        if (TryGetDataDeclarationFrame(context) is { } frame)
        {
            frame.Section = section;
            frame.Name = ParseIdentifier(name);
        }
    }

    internal void SetAnonymousDataDeclarationHeader(CILParser.DdHeadContext context, byte section)
    {
        if (TryGetDataDeclarationFrame(context) is { } frame)
        {
            frame.Section = section;
        }
    }

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
    internal byte GetMappedDataSection() => 0;

    internal byte GetTlsDataSection(CILParser.TlsContext context)
    {
        ReportError(
            DiagnosticIds.UnsupportedTlsData,
            DiagnosticMessageTemplates.UnsupportedTlsData,
            context);
        return 1;
    }

    internal byte GetCilDataSection() => 2;
#pragma warning restore CA1822

    internal int ParseDataItemCount(IToken token) => ParseInt32(token);

    internal void AddDataString(CILParser.DdItemContext context, string value)
    {
        TryGetDataDeclarationFrame(context)?.Data.WriteUTF16(value);
    }

    internal void AddDataReference(CILParser.DdItemContext context, IToken targetToken)
    {
        if (TryGetDataDeclarationFrame(context) is not { } frame)
        {
            return;
        }

        string target = ParseIdentifier(targetToken);
        Dictionary<string, List<Blob>> fixups =
            frame.ReferenceFixups ??= new Dictionary<string, List<Blob>>();
        if (!fixups.TryGetValue(target, out List<Blob>? targetFixups))
        {
            fixups.Add(target, targetFixups = new());
        }

        targetFixups.Add(frame.Data.ReserveBytes(sizeof(int)));
    }

    internal void AddDataBytes(
        CILParser.DdItemContext context,
        ImmutableArray<byte> value)
    {
        TryGetDataDeclarationFrame(context)?.Data.WriteBytes(value);
    }

    internal void AddFloatingPointData(
        CILParser.DdItemContext context,
        IToken kind,
        double value,
        int count)
    {
        if (count <= 0 || TryGetDataDeclarationFrame(context) is not { } frame)
        {
            return;
        }

        if (kind.Text == "float32")
        {
            float single = (float)value;
            for (int i = 0; i < count; i++)
            {
                frame.Data.WriteSingle(single);
            }
        }
        else
        {
            Debug.Assert(kind.Text == "float64");
            for (int i = 0; i < count; i++)
            {
                frame.Data.WriteDouble(value);
            }
        }
    }

    internal void AddInt64Data(
        CILParser.DdItemContext context,
        IToken kind,
        IToken value,
        int count)
    {
        Debug.Assert(kind.Text == "int64");
        if (count <= 0 || TryGetDataDeclarationFrame(context) is not { } frame)
        {
            return;
        }

        long parsedValue = ParseInt64(value);
        for (int i = 0; i < count; i++)
        {
            frame.Data.WriteInt64(parsedValue);
        }
    }

    internal void AddIntegerData(
        CILParser.DdItemContext context,
        IToken kind,
        IToken value,
        int count)
    {
        if (count <= 0 || TryGetDataDeclarationFrame(context) is not { } frame)
        {
            return;
        }

        int parsedValue = ParseInt32(value);
        switch (kind.Text)
        {
            case "int8":
                frame.Data.WriteBytes((byte)parsedValue, count);
                break;
            case "int16":
                for (int i = 0; i < count; i++)
                {
                    frame.Data.WriteInt16((short)parsedValue);
                }
                break;
            default:
                Debug.Assert(kind.Text == "int32");
                for (int i = 0; i < count; i++)
                {
                    frame.Data.WriteInt32(parsedValue);
                }
                break;
        }
    }

    internal void AddZeroData(CILParser.DdItemContext context, IToken kind, int count)
    {
        if (count <= 0 || TryGetDataDeclarationFrame(context) is not { } frame)
        {
            return;
        }

        int elementSize = kind.Text switch
        {
            "int8" => sizeof(byte),
            "int16" => sizeof(short),
            "int32" or "float32" => sizeof(int),
            "int64" or "float64" => sizeof(long),
            _ => throw new UnreachableException(),
        };
        frame.Data.WriteBytes(0, checked(elementSize * count));
    }

    private DataDeclarationFrame? TryGetDataDeclarationFrame(ParserRuleContext context)
    {
        Debug.Assert(_dataDeclarationFrames.Count > 0);
        DataDeclarationFrame? frame =
            _dataDeclarationFrames.Count == 0 ? null : _dataDeclarationFrames.Peek();
        CILParser.DataDeclContext? owner = FindDataDeclarationOwner(context);
        Debug.Assert(frame is null || ReferenceEquals(frame.Owner, owner));
        return frame is not null && ReferenceEquals(frame.Owner, owner) ? frame : null;
    }

    private static CILParser.DataDeclContext? FindDataDeclarationOwner(RuleContext context)
    {
        for (RuleContext? current = context; current is not null; current = current.Parent)
        {
            if (current is CILParser.DataDeclContext declaration)
            {
                return declaration;
            }
        }

        return null;
    }

#pragma warning disable CA1822 // Structural rules are driven by parser actions.
    public GrammarResult VisitDataDecl(CILParser.DataDeclContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

    public GrammarResult VisitDdHead(CILParser.DdHeadContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

    public GrammarResult VisitDdBody(CILParser.DdBodyContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

    public GrammarResult VisitDdItemList(CILParser.DdItemListContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

    public GrammarResult VisitDdItem(CILParser.DdItemContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

    GrammarResult ICILVisitor<GrammarResult>.VisitDdItemCount(CILParser.DdItemCountContext context)
        => VisitDdItemCount(context);

    public static GrammarResult.Literal<int> VisitDdItemCount(CILParser.DdItemCountContext context)
        => new(context.Value);

    GrammarResult ICILVisitor<GrammarResult>.VisitTls(CILParser.TlsContext context)
        => VisitTls(context);

    public static GrammarResult.Literal<byte> VisitTls(CILParser.TlsContext context)
        => new(context.Value);
#pragma warning restore CA1822
}
