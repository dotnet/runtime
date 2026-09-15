// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Antlr4.Runtime;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    internal CILParser.DataDeclarationBuilder CreateDataDeclaration(
        CILParser.DataDeclContext context)
        => new(
            !IsDeclarationSuppressed &&
            (context.Parent is not CILParser.MethodDeclContext ||
             _currentMethod is not null));

    internal void EndDataDeclaration(
        CILParser.DataDeclContext context,
        CILParser.DataDeclarationBuilder builder,
        int initialSyntaxErrorCount)
    {
        bool hasSyntaxError =
            HasSyntaxErrorsSince(initialSyntaxErrorCount) ||
            context.exception is not null;
        context.HasSyntaxError = hasSyntaxError;

        if (hasSyntaxError || !builder.ShouldCommit)
        {
            return;
        }

        int declarationOffset = _mappedFieldData.Count;
        if (builder.Name is not null && !_mappedFieldDataNames.ContainsKey(builder.Name))
        {
            _mappedFieldDataNames.Add(builder.Name, declarationOffset);
        }

        _mappedFieldData.LinkSuffix(builder.Data);
        if (builder.ReferenceFixups is null)
        {
            return;
        }

        foreach (CILParser.DataLabelReferenceValue reference in builder.ReferenceFixups)
        {
            _mappedFieldDataReferenceFixups.Add(reference with
            {
                DataOffset = declarationOffset + reference.DataOffset,
            });
        }
    }

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
    internal void SetDataDeclarationHeader(
        CILParser.DataDeclarationBuilder builder,
        byte section,
        IToken name)
    {
        _ = section;
        builder.Name = ParseIdentifier(name);
    }

    internal void SetAnonymousDataDeclarationHeader(
        CILParser.DataDeclarationBuilder builder,
        byte section)
    {
        _ = builder;
        _ = section;
    }

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
    internal int ParseDataItemCount(IToken token) => ParseInt32(token);

    internal void AddDataString(CILParser.DataDeclarationBuilder builder, string value)
        => builder.Data.WriteUTF16(value);

    internal void AddDataReference(
        CILParser.DataDeclarationBuilder builder,
        IToken targetToken)
    {
        string target = ParseIdentifier(targetToken);
        int pointerSize = VTableFixupSupport.GetPointerSize(
            _options.Machine ?? Machine.I386);
        builder.ReferenceFixups ??= new List<CILParser.DataLabelReferenceValue>();
        builder.ReferenceFixups.Add(new CILParser.DataLabelReferenceValue(
            target,
            builder.Data.Count,
            pointerSize,
            targetToken));
        builder.Data.ReserveBytes(pointerSize);
    }

    internal void AddDataBytes(
        CILParser.DataDeclarationBuilder builder,
        ImmutableArray<byte> value)
        => builder.Data.WriteBytes(value);

    internal void AddFloatingPointData(
        CILParser.DataDeclarationBuilder builder,
        IToken kind,
        double value,
        int count)
    {
        if (count <= 0)
        {
            return;
        }

        if (kind.Text == "float32")
        {
            float single = (float)value;
            for (int i = 0; i < count; i++)
            {
                builder.Data.WriteSingle(single);
            }
        }
        else
        {
            Debug.Assert(kind.Text == "float64");
            for (int i = 0; i < count; i++)
            {
                builder.Data.WriteDouble(value);
            }
        }
    }

    internal void AddInt64Data(
        CILParser.DataDeclarationBuilder builder,
        IToken kind,
        IToken value,
        int count)
    {
        Debug.Assert(kind.Text == "int64");
        if (count <= 0)
        {
            return;
        }

        long parsedValue = ParseInt64(value);
        for (int i = 0; i < count; i++)
        {
            builder.Data.WriteInt64(parsedValue);
        }
    }

    internal void AddIntegerData(
        CILParser.DataDeclarationBuilder builder,
        IToken kind,
        IToken value,
        int count)
    {
        if (count <= 0)
        {
            return;
        }

        int parsedValue = ParseInt32(value);
        switch (kind.Text)
        {
            case "int8":
                builder.Data.WriteBytes((byte)parsedValue, count);
                break;
            case "int16":
                for (int i = 0; i < count; i++)
                {
                    builder.Data.WriteInt16((short)parsedValue);
                }
                break;
            default:
                Debug.Assert(kind.Text == "int32");
                for (int i = 0; i < count; i++)
                {
                    builder.Data.WriteInt32(parsedValue);
                }
                break;
        }
    }

    internal void AddZeroData(
        CILParser.DataDeclarationBuilder builder,
        IToken kind,
        int count)
    {
        if (count <= 0)
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
        builder.Data.WriteBytes(0, checked(elementSize * count));
    }
#pragma warning restore CA1822


}
