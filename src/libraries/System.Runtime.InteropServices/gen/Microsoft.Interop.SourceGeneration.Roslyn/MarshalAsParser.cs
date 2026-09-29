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

namespace Microsoft.Interop
{
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
                        && type.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Object
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
