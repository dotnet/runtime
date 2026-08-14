// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    internal ushort ParseVTableFixupAttribute(IToken token)
        => token.Text switch
        {
            "int32" => VTableFixupSupport.COR_VTABLE_32BIT,
            "int64" => VTableFixupSupport.COR_VTABLE_64BIT,
            "fromunmanaged" => VTableFixupSupport.COR_VTABLE_FROM_UNMANAGED,
            "callmostderived" => VTableFixupSupport.COR_VTABLE_CALL_MOST_DERIVED,
            "retainappdomain" =>
                VTableFixupSupport.COR_VTABLE_FROM_UNMANAGED_RETAIN_APPDOMAIN,
            _ => throw new UnreachableException()
        };

    internal ushort AddVTableFixupAttribute(ushort attributes, ushort value)
        => (ushort)(attributes | value);

    internal ushort CompleteVTableFixupAttributes(ushort attributes) => attributes;

    internal object CreateVTableFixup(IToken slotCount, ushort flags, IToken dataLabel)
    {
        int diagnosticCount = _diagnostics.Count;
        int count = ParseInt32(slotCount);
        bool hasValidSlotCount = _diagnostics.Count == diagnosticCount;
        if (hasValidSlotCount && (uint)count > ushort.MaxValue)
        {
            ReportError(
                DiagnosticIds.InvalidVTableSlotCount,
                string.Format(
                    DiagnosticMessageTemplates.InvalidVTableSlotCount,
                    count,
                    ushort.MaxValue),
                slotCount);
            hasValidSlotCount = false;
        }

        return new VTableFixupValue(
            count,
            flags,
            ParseIdentifier(dataLabel),
            hasValidSlotCount);
    }

    internal object CreateRawVTable(ImmutableArray<byte> value) => new RawVTableValue(value);

    public GrammarResult VisitVtableDecl(CILParser.VtableDeclContext context)
        => throw new NotImplementedException(
            "raw vtable fixups blob (.vtable) not supported - use .vtfixup instead");

    GrammarResult ICILVisitor<GrammarResult>.VisitVtfixupAttr(
        CILParser.VtfixupAttrContext context)
        => VisitVtfixupAttr(context);

    public static GrammarResult.Literal<ushort> VisitVtfixupAttr(
        CILParser.VtfixupAttrContext context)
        => new(context.Value);

    GrammarResult ICILVisitor<GrammarResult>.VisitVtfixupAttrElement(
        CILParser.VtfixupAttrElementContext context)
        => VisitVtfixupAttrElement(context);

    public static GrammarResult.Literal<ushort> VisitVtfixupAttrElement(
        CILParser.VtfixupAttrElementContext context)
        => new(context.Value);

    GrammarResult ICILVisitor<GrammarResult>.VisitVtfixupDecl(
        CILParser.VtfixupDeclContext context)
        => VisitVtfixupDecl(context);

    public GrammarResult VisitVtfixupDecl(CILParser.VtfixupDeclContext context)
    {
        if (context.Value is VTableFixupValue value)
        {
            _vtableFixups.Add(new VTableFixupDeclaration(
                new VTableFixupSupport.VTableFixupEntry(
                    value.SlotCount,
                    value.Flags,
                    value.DataLabel),
                context,
                value.HasValidSlotCount));
        }

        return GrammarResult.SentinelValue.Result;
    }
}
