// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace ILAssembler;

#pragma warning disable CA1822 // Visitor wrappers intentionally preserve the existing instance API.
internal sealed partial class GrammarActions
{
    GrammarResult ICILVisitor<GrammarResult>.VisitIidParamIndex(CILParser.IidParamIndexContext context)
        => VisitIidParamIndex(context);

    public GrammarResult.Literal<int?> VisitIidParamIndex(CILParser.IidParamIndexContext context)
        => new(MaterializeIidParamIndex(GetIidParamIndexValue(context.Value)));

    GrammarResult ICILVisitor<GrammarResult>.VisitMarshalBlob(CILParser.MarshalBlobContext context)
        => VisitMarshalBlob(context);

    public GrammarResult.FormattedBlob VisitMarshalBlob(CILParser.MarshalBlobContext context)
        => new(MaterializeMarshallingDescriptor(GetMarshallingDescriptorValue(context.Value)));

    GrammarResult ICILVisitor<GrammarResult>.VisitMarshalClause(CILParser.MarshalClauseContext context)
        => VisitMarshalClause(context);

    public GrammarResult.FormattedBlob VisitMarshalClause(CILParser.MarshalClauseContext context)
        => new(MaterializeMarshallingDescriptor(GetMarshallingDescriptorValue(context.Value)));

    GrammarResult ICILVisitor<GrammarResult>.VisitNativeType(CILParser.NativeTypeContext context)
        => VisitNativeType(context);

    public GrammarResult.FormattedBlob VisitNativeType(CILParser.NativeTypeContext context)
        => new(MaterializeNativeType(GetNativeTypeValue(context.Value)));

    GrammarResult ICILVisitor<GrammarResult>.VisitNativeTypeElement(CILParser.NativeTypeElementContext context)
        => VisitNativeTypeElement(context);

    public GrammarResult.FormattedBlob VisitNativeTypeElement(CILParser.NativeTypeElementContext context)
        => new(MaterializeNativeTypeElement(GetNativeTypeElementValue(context.Value)));

    GrammarResult ICILVisitor<GrammarResult>.VisitVariantType(CILParser.VariantTypeContext context)
        => VisitVariantType(context);

    public GrammarResult.Literal<VarEnum> VisitVariantType(CILParser.VariantTypeContext context)
        => new(MaterializeVariantType(GetVariantTypeValue(context.Value)));

    GrammarResult ICILVisitor<GrammarResult>.VisitVariantTypeElement(CILParser.VariantTypeElementContext context)
        => VisitVariantTypeElement(context);

    public GrammarResult.Literal<VarEnum> VisitVariantTypeElement(CILParser.VariantTypeElementContext context)
        => new(MaterializeVariantTypeElement(
            context.Value as VariantTypeElementValue ?? new VariantTypeElementValue(0)));

    public GrammarResult VisitNativeTypeArrayPointerInfo(CILParser.NativeTypeArrayPointerInfoContext context)
        => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);

    public GrammarResult VisitPointerArrayTypeSize(CILParser.PointerArrayTypeSizeContext context)
        => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);

    public GrammarResult VisitPointerArrayTypeParamIndex(CILParser.PointerArrayTypeParamIndexContext context)
        => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);

    public GrammarResult VisitPointerNativeType(CILParser.PointerNativeTypeContext context)
        => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);

    public GrammarResult VisitPointerArrayTypeSizeParamIndex(CILParser.PointerArrayTypeSizeParamIndexContext context)
        => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);

    public GrammarResult VisitPointerArrayTypeNoSizeData(CILParser.PointerArrayTypeNoSizeDataContext context)
        => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);
}
