// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace ILAssembler;

#pragma warning disable CA1822 // Visitor wrappers intentionally preserve the existing instance API.
internal sealed partial class GrammarActions
{
    GrammarResult ICILVisitor<GrammarResult>.VisitBound(CILParser.BoundContext context) => VisitBound(context);

    public GrammarResult.Literal<(int? Lower, int? Upper)> VisitBound(CILParser.BoundContext context)
        => new((
            context.HasLower ? context.Lower : null,
            context.HasUpper ? context.Upper : null));

    GrammarResult ICILVisitor<GrammarResult>.VisitBounds(CILParser.BoundsContext context) => VisitBounds(context);

    public GrammarResult.Sequence<(int? Lower, int? Upper)> VisitBounds(CILParser.BoundsContext context)
        => new(GetBoundsValue(context.Value)
            .Select(bound => (bound.Lower, bound.Upper))
            .ToImmutableArray());

    GrammarResult ICILVisitor<GrammarResult>.VisitCallConv(CILParser.CallConvContext context) => VisitCallConv(context);

    public static GrammarResult.Literal<byte> VisitCallConv(CILParser.CallConvContext context)
        => new(context.Value);

    GrammarResult ICILVisitor<GrammarResult>.VisitCallKind(CILParser.CallKindContext context) => VisitCallKind(context);

    public static GrammarResult.Literal<SignatureCallingConvention> VisitCallKind(CILParser.CallKindContext context)
        => new((SignatureCallingConvention)context.Value);

    private BlobBuilder BuildMethodReferenceSignature(
        CILParser.CallConvContext callConvention,
        CILParser.TypeContext returnType,
        CILParser.SigArgsContext signatureArguments,
        int genericArity)
    {
        BlobBuilder signature = new();
        byte header = VisitCallConv(callConvention).Value;
        if (genericArity > 0)
        {
            header |= (byte)SignatureAttributes.Generic;
        }
        signature.WriteByte(header);
        if (genericArity > 0)
        {
            signature.WriteCompressedInteger(genericArity);
        }

        ImmutableArray<SignatureArg> arguments = VisitSigArgs(signatureArguments).Value;
        signature.WriteCompressedInteger(arguments.Count(argument => !argument.IsSentinel));
        VisitType(returnType).Value.WriteContentTo(signature);
        foreach (SignatureArg argument in arguments)
        {
            argument.SignatureBlob.WriteContentTo(signature);
        }

        return signature;
    }

    GrammarResult ICILVisitor<GrammarResult>.VisitElementType(CILParser.ElementTypeContext context) => VisitElementType(context);

    public GrammarResult.FormattedBlob VisitElementType(CILParser.ElementTypeContext context)
        => new(MaterializeElementType(GetElementTypeValue(context.Value)));

    GrammarResult ICILVisitor<GrammarResult>.VisitGenArity(CILParser.GenArityContext context) => VisitGenArity(context);

    public static GrammarResult.Literal<int> VisitGenArity(CILParser.GenArityContext context)
        => new(context.Value);

    GrammarResult ICILVisitor<GrammarResult>.VisitGenArityNotEmpty(CILParser.GenArityNotEmptyContext context)
        => VisitGenArityNotEmpty(context);

    public static GrammarResult.Literal<int> VisitGenArityNotEmpty(CILParser.GenArityNotEmptyContext context)
        => new(context.Value);

    GrammarResult ICILVisitor<GrammarResult>.VisitNativeInt(CILParser.NativeIntContext context)
        => new GrammarResult.Literal<SignatureTypeCode>((SignatureTypeCode)context.Value);

    GrammarResult ICILVisitor<GrammarResult>.VisitNativeUint(CILParser.NativeUintContext context)
        => new GrammarResult.Literal<SignatureTypeCode>((SignatureTypeCode)context.Value);

    GrammarResult ICILVisitor<GrammarResult>.VisitMethodRef(CILParser.MethodRefContext context) => VisitMethodRef(context);

    public GrammarResult.Literal<EntityRegistry.EntityBase> VisitMethodRef(CILParser.MethodRefContext context)
        => new(MaterializeMethodReference(GetMethodReferenceValue(context.Value)));

    private EntityRegistry.MemberReferenceEntity CreateExplicitMethodReference(
        CILParser.CallConvContext callConv,
        CILParser.TypeContext returnType,
        CILParser.TypeSpecContext owner,
        CILParser.MethodNameContext methodName,
        CILParser.GenArityContext? genericArity,
        CILParser.SigArgsContext parameterList)
        => _entityRegistry.CreateLazilyRecordedMemberReference(
            VisitTypeSpec(owner).Value,
            VisitMethodName(methodName).Value,
            CreateExplicitMethodSignature(callConv, returnType, genericArity, parameterList));

    private BlobBuilder CreateExplicitMethodSignature(
        CILParser.CallConvContext callConv,
        CILParser.TypeContext returnType,
        CILParser.GenArityContext? genericArity,
        CILParser.SigArgsContext parameterList)
    {
        BlobBuilder signature = new();
        byte signatureHeader = VisitCallConv(callConv).Value;
        int arity = genericArity is null ? 0 : VisitGenArity(genericArity).Value;
        if (arity != 0)
        {
            signatureHeader |= (byte)SignatureAttributes.Generic;
        }

        signature.WriteByte(signatureHeader);
        if (arity != 0)
        {
            signature.WriteCompressedInteger(arity);
        }

        ImmutableArray<SignatureArg> parameters = VisitSigArgs(parameterList).Value;
        signature.WriteCompressedInteger(parameters.Count(parameter => !parameter.IsSentinel));
        VisitType(returnType).Value.WriteContentTo(signature);
        foreach (SignatureArg parameter in parameters)
        {
            parameter.SignatureBlob.WriteContentTo(signature);
        }

        return signature;
    }

    GrammarResult ICILVisitor<GrammarResult>.VisitParamAttr(CILParser.ParamAttrContext context) => VisitParamAttr(context);

    public static GrammarResult.Literal<ParameterAttributes> VisitParamAttr(CILParser.ParamAttrContext context)
        => new((ParameterAttributes)context.Value);

    GrammarResult ICILVisitor<GrammarResult>.VisitParamAttrElement(CILParser.ParamAttrElementContext context)
        => VisitParamAttrElement(context);

    public static GrammarResult.Flag<ParameterAttributes> VisitParamAttrElement(CILParser.ParamAttrElementContext context)
        => new((ParameterAttributes)context.Value, context.ShouldAppend);

    GrammarResult ICILVisitor<GrammarResult>.VisitSigArg(CILParser.SigArgContext context) => VisitSigArg(context);

    public GrammarResult.Literal<SignatureArg> VisitSigArg(CILParser.SigArgContext context)
        => new(MaterializeSignatureArgument(GetSignatureArgumentValue(context.Value)));

    GrammarResult ICILVisitor<GrammarResult>.VisitSigArgs(CILParser.SigArgsContext context) => VisitSigArgs(context);

    public GrammarResult.Sequence<SignatureArg> VisitSigArgs(CILParser.SigArgsContext context)
        => new(MaterializeSignatureArguments(GetSignatureArgumentsValue(context.Value)));

    GrammarResult ICILVisitor<GrammarResult>.VisitSimpleType(CILParser.SimpleTypeContext context) => VisitSimpleType(context);

    public static GrammarResult.Literal<SignatureTypeCode> VisitSimpleType(CILParser.SimpleTypeContext context)
        => new((SignatureTypeCode)context.Value);

    GrammarResult ICILVisitor<GrammarResult>.VisitType(CILParser.TypeContext context) => VisitType(context);

    public GrammarResult.FormattedBlob VisitType(CILParser.TypeContext context)
        => new(MaterializeType(GetTypeValue(context.Value)));

    GrammarResult ICILVisitor<GrammarResult>.VisitTypeArgs(CILParser.TypeArgsContext context) => VisitTypeArgs(context);

    public GrammarResult.FormattedBlob VisitTypeArgs(CILParser.TypeArgsContext context)
        => new(MaterializeTypeArguments(GetTypeArgumentsValue(context.Value)));

    GrammarResult ICILVisitor<GrammarResult>.VisitTypeList(CILParser.TypeListContext context) => VisitTypeList(context);

    public GrammarResult.Sequence<EntityRegistry.TypeEntity> VisitTypeList(CILParser.TypeListContext context)
    {
        CILParser.TypeSpecContext[] bounds = context.typeSpec();
        ImmutableArray<EntityRegistry.TypeEntity>.Builder builder =
            ImmutableArray.CreateBuilder<EntityRegistry.TypeEntity>(bounds.Length);
        foreach (CILParser.TypeSpecContext typeSpec in bounds)
        {
            builder.Add(VisitTypeSpec(typeSpec).Value);
        }
        return new(builder.MoveToImmutable());
    }

    GrammarResult ICILVisitor<GrammarResult>.VisitTypeSpec(CILParser.TypeSpecContext context) => VisitTypeSpec(context);

    public GrammarResult.Literal<EntityRegistry.TypeEntity> VisitTypeSpec(CILParser.TypeSpecContext context)
        => new(ResolveTypeSpecification(GetTypeSpecificationValue(context.Value)));

    GrammarResult ICILVisitor<GrammarResult>.VisitOptionalModifier(CILParser.OptionalModifierContext context)
        => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);

    GrammarResult ICILVisitor<GrammarResult>.VisitSZArrayModifier(CILParser.SZArrayModifierContext context)
        => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);

    GrammarResult ICILVisitor<GrammarResult>.VisitRequiredModifier(CILParser.RequiredModifierContext context)
        => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);

    GrammarResult ICILVisitor<GrammarResult>.VisitPtrModifier(CILParser.PtrModifierContext context)
        => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);

    GrammarResult ICILVisitor<GrammarResult>.VisitPinnedModifier(CILParser.PinnedModifierContext context)
        => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);

    GrammarResult ICILVisitor<GrammarResult>.VisitGenericArgumentsModifier(CILParser.GenericArgumentsModifierContext context)
        => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);

    GrammarResult ICILVisitor<GrammarResult>.VisitByRefModifier(CILParser.ByRefModifierContext context)
        => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);

    GrammarResult ICILVisitor<GrammarResult>.VisitArrayModifier(CILParser.ArrayModifierContext context)
        => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);
}
