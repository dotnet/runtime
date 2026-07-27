// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    /// <summary>
    /// Simple User-application of System.Runtime.InteropServices.MarshalAsAttribute
    /// </summary>
    public abstract record MarshalAsInfo(
        UnmanagedType UnmanagedType,
        CharEncoding CharEncoding) : MarshallingInfoStringSupport(CharEncoding), IForwardedMarshallingInfo
    {
        // UnmanagedType.LPUTF8Str is not in netstandard2.0, so we define a constant for the value here.
        // See https://learn.microsoft.com/dotnet/api/system.runtime.interopservices.unmanagedtype
        internal const UnmanagedType UnmanagedType_LPUTF8Str = (UnmanagedType)0x30;

        private protected abstract bool TryCreateAttributeSyntax([NotNullWhen(true)] out AttributeSyntax? attribute);

        bool IForwardedMarshallingInfo.TryCreateAttributeSyntax([NotNullWhen(true)] out AttributeSyntax? attribute) => TryCreateAttributeSyntax(out attribute);
    }

    public sealed record MarshalAsScalarInfo(
        UnmanagedType UnmanagedType,
        CharEncoding CharEncoding) : MarshalAsInfo(UnmanagedType, CharEncoding)
    {
        private protected override bool TryCreateAttributeSyntax([NotNullWhen(true)] out AttributeSyntax? attribute)
        {
            if (UnmanagedType == UnmanagedType.CustomMarshaler)
            {
                attribute = null;
                return false;
            }

            attribute = Attribute(
                ParseName(TypeNames.System_Runtime_InteropServices_MarshalAsAttribute),
                AttributeArgumentList(
                    SingletonSeparatedList(
                        AttributeArgument(
                            CastExpression(TypeSyntaxes.System_Runtime_InteropServices_UnmanagedType,
                            LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                Literal((int)UnmanagedType))))
                    )
                )
            );
            return true;
        }
    }

    public sealed record MarshalAsInterfaceInfo(
        UnmanagedType UnmanagedType,
        CharEncoding CharEncoding,
        TypePositionInfo? IidParameterIndexInfo) : MarshalAsInfo(UnmanagedType, CharEncoding)
    {
        public override IEnumerable<TypePositionInfo> ElementDependencies
            => IidParameterIndexInfo is null ? [] : [IidParameterIndexInfo];

        private protected override bool TryCreateAttributeSyntax([NotNullWhen(true)] out AttributeSyntax? attribute)
        {
            attribute = Attribute(
                ParseName(TypeNames.System_Runtime_InteropServices_MarshalAsAttribute),
                AttributeArgumentList(
                    SingletonSeparatedList(
                        AttributeArgument(
                            CastExpression(TypeSyntaxes.System_Runtime_InteropServices_UnmanagedType,
                            LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                Literal((int)UnmanagedType))))
                    )
                )
            );

            if (IidParameterIndexInfo is { ManagedIndex: int paramIndex } && !TypePositionInfo.IsSpecialIndex(paramIndex))
            {
                attribute = attribute.AddArgumentListArguments(
                    AttributeArgument(NameEquals(nameof(MarshalAsAttribute.IidParameterIndex)), null,
                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(paramIndex))));
            }

            return true;
        }
    }

    public sealed record MarshalAsArrayInfo(
        UnmanagedType UnmanagedType,
        CharEncoding CharEncoding,
        UnmanagedType ArraySubType,
        CountInfo CountInfo) : MarshalAsInfo(UnmanagedType, CharEncoding)
    {
        private protected override bool TryCreateAttributeSyntax([NotNullWhen(true)] out AttributeSyntax? attribute)
        {
            if (ArraySubType == UnmanagedType.CustomMarshaler)
            {
                attribute = null;
                return false;
            }

            attribute = Attribute(
                ParseName(TypeNames.System_Runtime_InteropServices_MarshalAsAttribute),
                AttributeArgumentList(
                    SingletonSeparatedList(
                        AttributeArgument(
                            CastExpression(TypeSyntaxes.System_Runtime_InteropServices_UnmanagedType,
                            LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                Literal((int)UnmanagedType))))
                    )
                )
            );

            if (ArraySubType != (UnmanagedType)SizeAndParamIndexInfo.UnspecifiedConstSize)
            {
                attribute = attribute.AddArgumentListArguments(
                    AttributeArgument(CastExpression(TypeSyntaxes.System_Runtime_InteropServices_UnmanagedType,
                        LiteralExpression(SyntaxKind.NumericLiteralExpression,
                            Literal((int)ArraySubType))))
                        .WithNameEquals(NameEquals(IdentifierName(nameof(ArraySubType)))));
            }

            if (CountInfo is SizeAndParamIndexInfo sizeParamIndex)
            {
                if (sizeParamIndex.ConstSize != SizeAndParamIndexInfo.UnspecifiedConstSize)
                {
                    attribute = attribute.AddArgumentListArguments(
                        AttributeArgument(NameEquals("SizeConst"), null,
                            LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                Literal(sizeParamIndex.ConstSize)))
                    );
                }
                if (sizeParamIndex.ParamAtIndex is { ManagedIndex: int paramIndex })
                {
                    attribute = attribute.AddArgumentListArguments(
                        AttributeArgument(NameEquals("SizeParamIndex"), null,
                            LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                Literal(paramIndex)))
                    );
                }
            }
            return true;
        }
    }

    /// <summary>
    /// This class suppports parsing a System.Runtime.InteropServices.MarshalAsAttribute into a <see cref="MarshalAsInfo"/>.
    /// </summary>
    public sealed class MarshalAsAttributeParser : IMarshallingInfoAttributeParser, IUseSiteAttributeParser
    {
        private static readonly string IidParameterIndexConfigurationName
            = $"{nameof(MarshalAsAttribute)}{Type.Delimiter}{nameof(MarshalAsAttribute.IidParameterIndex)}";
        private static string IidParameterIndexConfigurationNameWithSupportedShape
            => SR.Format(SR.IidParameterIndexUnsupportedConfigurationName, IidParameterIndexConfigurationName);

        private readonly GeneratorDiagnosticsBag _diagnostics;
        private readonly DefaultMarshallingInfo _defaultInfo;

        public MarshalAsAttributeParser(GeneratorDiagnosticsBag diagnostics, DefaultMarshallingInfo defaultInfo)
        {
            _diagnostics = diagnostics;
            _defaultInfo = defaultInfo;
        }

        public bool CanParseAttributeType(INamedTypeSymbol attributeType) => attributeType.ToDisplayString() == TypeNames.System_Runtime_InteropServices_MarshalAsAttribute;

        UseSiteAttributeData IUseSiteAttributeParser.ParseAttribute(AttributeData attributeData, IElementInfoProvider elementInfoProvider, GetMarshallingInfoCallback marshallingInfoCallback)
        {
            ImmutableDictionary<string, TypedConstant> namedArguments = ImmutableDictionary.CreateRange(attributeData.NamedArguments);
            SizeAndParamIndexInfo arraySizeInfo = SizeAndParamIndexInfo.Unspecified;
            TypePositionInfo? iidParameterIndexInfo = null;
            if (namedArguments.TryGetValue(nameof(MarshalAsAttribute.SizeConst), out TypedConstant sizeConstArg))
            {
                arraySizeInfo = arraySizeInfo with { ConstSize = (int)sizeConstArg.Value! };
            }
            if (namedArguments.TryGetValue(nameof(MarshalAsAttribute.SizeParamIndex), out TypedConstant sizeParamIndexArg))
            {
                if (!elementInfoProvider.TryGetInfoForParamIndex(attributeData, (short)sizeParamIndexArg.Value!, marshallingInfoCallback, out TypePositionInfo paramIndexInfo))
                {
                    _diagnostics.ReportConfigurationNotSupported(attributeData, nameof(MarshalAsAttribute.SizeParamIndex), sizeParamIndexArg.Value.ToString());
                }
                arraySizeInfo = arraySizeInfo with { ParamAtIndex = paramIndexInfo };
            }
            if (namedArguments.TryGetValue(nameof(MarshalAsAttribute.IidParameterIndex), out TypedConstant iidParameterIndexArg))
            {
                if (!elementInfoProvider.TryGetInfoForParamIndex(attributeData, (int)iidParameterIndexArg.Value!, marshallingInfoCallback, out iidParameterIndexInfo)
                    || !IsValidIidParameter(iidParameterIndexInfo))
                {
                    _diagnostics.ReportConfigurationNotSupported(attributeData, IidParameterIndexConfigurationNameWithSupportedShape);
                    iidParameterIndexInfo = null;
                }
            }
            return new UseSiteAttributeData(0, arraySizeInfo, attributeData, iidParameterIndexInfo);
        }

        MarshallingInfo? IMarshallingInfoAttributeParser.ParseAttribute(AttributeData attributeData, ITypeSymbol type, int indirectionDepth, UseSiteAttributeProvider useSiteAttributes, GetMarshallingInfoCallback marshallingInfoCallback)
        {
            object unmanagedTypeObj = attributeData.ConstructorArguments[0].Value!;
            UnmanagedType unmanagedType = unmanagedTypeObj is short unmanagedTypeAsShort
                ? (UnmanagedType)unmanagedTypeAsShort
                : (UnmanagedType)unmanagedTypeObj;
            if (!Enum.IsDefined(typeof(UnmanagedType), unmanagedType)
                || unmanagedType == UnmanagedType.CustomMarshaler
                || unmanagedType == UnmanagedType.SafeArray)
            {
                _diagnostics.ReportConfigurationNotSupported(attributeData, nameof(UnmanagedType), unmanagedType.ToString());
            }

            bool isArrayType = unmanagedType == UnmanagedType.LPArray || unmanagedType == UnmanagedType.ByValArray;
            UnmanagedType elementUnmanagedType = (UnmanagedType)SizeAndParamIndexInfo.UnspecifiedConstSize;

            bool hasIidParameterIndex = false;
            // All other data on attribute is defined as NamedArguments.
            foreach (KeyValuePair<string, TypedConstant> namedArg in attributeData.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case nameof(MarshalAsAttribute.SafeArraySubType):
                    case nameof(MarshalAsAttribute.SafeArrayUserDefinedSubType):
                    case nameof(MarshalAsAttribute.MarshalTypeRef):
                    case nameof(MarshalAsAttribute.MarshalType):
                    case nameof(MarshalAsAttribute.MarshalCookie):
                        _diagnostics.ReportConfigurationNotSupported(attributeData, $"{attributeData.AttributeClass!.Name}{Type.Delimiter}{namedArg.Key}");
                        break;
                    case nameof(MarshalAsAttribute.IidParameterIndex):
                        if (isArrayType)
                        {
                            _diagnostics.ReportConfigurationNotSupported(attributeData, IidParameterIndexConfigurationNameWithSupportedShape);
                        }
                        else
                        {
                            hasIidParameterIndex = true;
                        }
                        break;
                    case nameof(MarshalAsAttribute.ArraySubType):
                        if (!isArrayType)
                        {
                            _diagnostics.ReportConfigurationNotSupported(attributeData, $"{attributeData.AttributeClass!.Name}{Type.Delimiter}{namedArg.Key}");
                        }
                        elementUnmanagedType = (UnmanagedType)namedArg.Value.Value!;
                        break;
                }
            }

            if (!isArrayType)
            {
                TypePositionInfo? iidParameterIndexInfo = null;
                if (hasIidParameterIndex)
                {
                    bool hasUseSiteData = useSiteAttributes.TryGetUseSiteAttributeInfo(indirectionDepth, out UseSiteAttributeData iidUseSiteAttributeData);
                    bool hasIidParameterInfo = hasUseSiteData && iidUseSiteAttributeData.IidParameterIndexInfo is not null;
                    bool supportedShape = unmanagedType == UnmanagedType.Interface
                        && type.SpecialType == SpecialType.System_Object
                        && IsOutParameter(attributeData)
                        && hasIidParameterInfo;

                    if (supportedShape)
                    {
                        iidParameterIndexInfo = iidUseSiteAttributeData.IidParameterIndexInfo;
                    }
                    else if (hasIidParameterInfo)
                    {
                        _diagnostics.ReportConfigurationNotSupported(attributeData, IidParameterIndexConfigurationNameWithSupportedShape);
                    }
                }

                if (unmanagedType == UnmanagedType.Interface)
                {
                    return new MarshalAsInterfaceInfo(unmanagedType, _defaultInfo.CharEncoding, iidParameterIndexInfo);
                }

                return new MarshalAsScalarInfo(unmanagedType, _defaultInfo.CharEncoding);
            }

            CountInfo countInfo = NoCountInfo.Instance;

            if (useSiteAttributes.TryGetUseSiteAttributeInfo(indirectionDepth, out UseSiteAttributeData useSiteAttributeData))
            {
                countInfo = useSiteAttributeData.CountInfo;
            }

            return new MarshalAsArrayInfo(unmanagedType, _defaultInfo.CharEncoding, elementUnmanagedType, countInfo);
        }

        private static bool IsGuidType(TypePositionInfo info)
            => info.ManagedType.FullTypeName is TypeNames.System_Guid
                or $"{TypeNames.GlobalAlias}{TypeNames.System_Guid}";

        // The IID parameter referenced by 'IidParameterIndex' must be a 'Guid' passed either by value
        // or as 'in' (REFIID semantics) or 'ref'.
        private static bool IsValidIidParameter(TypePositionInfo info)
            => IsGuidType(info)
                && info.RefKind is RefKind.None or RefKind.In or RefKind.Ref;

        private static bool IsOutParameter(AttributeData attributeData)
            => attributeData.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax
            {
                Parent.Parent: ParameterSyntax parameterSyntax
            }
            && parameterSyntax.Modifiers.IndexOf(SyntaxKind.OutKeyword) >= 0;
    }
}
