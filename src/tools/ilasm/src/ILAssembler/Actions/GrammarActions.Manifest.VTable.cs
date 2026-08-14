// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    private readonly List<VTableFixupSupport.VTableFixupEntry> _vtableFixups = new();

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

    internal ushort CompleteVTableFixupAttributes(ushort attributes)
    {
        const ushort SlotSizeMask =
            VTableFixupSupport.COR_VTABLE_32BIT | VTableFixupSupport.COR_VTABLE_64BIT;
        return (attributes & SlotSizeMask) == 0
            ? (ushort)(attributes | VTableFixupSupport.COR_VTABLE_32BIT)
            : attributes;
    }

    internal object CreateVTableFixup(IToken slotCount, ushort flags, IToken dataLabel)
        => new VTableFixupValue(ParseInt32(slotCount), flags, ParseIdentifier(dataLabel));

    internal object CreateRawVTable(ImmutableArray<byte> value) => new RawVTableValue(value);

    internal void MaterializeVTable(CILParser.VtableDeclContext context)
        => throw new NotImplementedException(
            "raw vtable fixups blob (.vtable) not supported - use .vtfixup instead");

    internal void MaterializeVTableFixup(CILParser.VtfixupDeclContext context)
    {
        if (context.Value is VTableFixupValue value)
        {
            _vtableFixups.Add(new(value.SlotCount, value.Flags, value.DataLabel));
        }
    }
}
