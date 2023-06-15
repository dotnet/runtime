// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    /// <summary>
    /// Simple User-application of System.Runtime.InteropServices.MarshalAsAttribute
    /// </summary>
    public sealed record MarshalAsInfo(
        UnmanagedType UnmanagedType,
        CharEncoding CharEncoding) : MarshallingInfoStringSupport(CharEncoding)
    {
        // UnmanagedType.LPUTF8Str is not in netstandard2.0, so we define a constant for the value here.
        // See https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.unmanagedtype
        internal const UnmanagedType UnmanagedType_LPUTF8Str = (UnmanagedType)0x30;
    }

    /// <summary>
    /// This class suppports parsing a System.Runtime.InteropServices.MarshalAsAttribute.
    /// </summary>
    public sealed class MarshalAsAttributeParser : IMarshallingInfoAttributeParser, IUseSiteAttributeParser
    {
        private readonly Compilation _compilation;
        private readonly IGeneratorDiagnostics _diagnostics;
        private readonly DefaultMarshallingInfo _defaultInfo;

        public MarshalAsAttributeParser(Compilation compilation, IGeneratorDiagnostics diagnostics, DefaultMarshallingInfo defaultInfo)
        {
            _compilation = compilation;
            _diagnostics = diagnostics;
            _defaultInfo = defaultInfo;
        }

        public bool CanParseAttributeType(INamedTypeSymbol attributeType) => attributeType.ToDisplayString() == TypeNames.System_Runtime_InteropServices_MarshalAsAttribute;

        UseSiteAttributeData IUseSiteAttributeParser.ParseAttribute(AttributeData attributeData, IElementInfoProvider elementInfoProvider, GetMarshallingInfoCallback marshallingInfoCallback)
        {
            ImmutableDictionary<string, TypedConstant> namedArguments = ImmutableDictionary.CreateRange(attributeData.NamedArguments);
            SizeAndParamIndexInfo arraySizeInfo = SizeAndParamIndexInfo.Unspecified;
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
            return new UseSiteAttributeData(0, arraySizeInfo, attributeData);
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

            // All other data on attribute is defined as NamedArguments.
            foreach (KeyValuePair<string, TypedConstant> namedArg in attributeData.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case nameof(MarshalAsAttribute.SafeArraySubType):
                    case nameof(MarshalAsAttribute.SafeArrayUserDefinedSubType):
                    case nameof(MarshalAsAttribute.IidParameterIndex):
                    case nameof(MarshalAsAttribute.MarshalTypeRef):
                    case nameof(MarshalAsAttribute.MarshalType):
                    case nameof(MarshalAsAttribute.MarshalCookie):
                        _diagnostics.ReportConfigurationNotSupported(attributeData, $"{attributeData.AttributeClass!.Name}{Type.Delimiter}{namedArg.Key}");
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

            // We'll support the UnmanagedType.Interface option, but we'll explicitly
            // exclude ComImport types as they will not work as expected
            // unless they are migrated to [GeneratedComInterface].
            if (unmanagedType == UnmanagedType.Interface)
            {
                if (type is INamedTypeSymbol { IsComImport: true })
                {
                    return new MarshalAsInfo(unmanagedType, _defaultInfo.CharEncoding);
                }
                return ComInterfaceMarshallingInfoProvider.CreateComInterfaceMarshallingInfo(_compilation, type);
            }

            if (isArrayType)
            {
                if (type is not IArrayTypeSymbol { ElementType: ITypeSymbol elementType })
                {
                    _diagnostics.ReportConfigurationNotSupported(attributeData, nameof(UnmanagedType), unmanagedType.ToString());
                    return NoMarshallingInfo.Instance;
                }

                MarshallingInfo elementMarshallingInfo = NoMarshallingInfo.Instance;
                if (elementUnmanagedType != (UnmanagedType)SizeAndParamIndexInfo.UnspecifiedConstSize)
                {
                    elementMarshallingInfo = elementType.SpecialType == SpecialType.System_String
                        ? CreateStringMarshallingInfo(elementType, elementUnmanagedType)
                        : new MarshalAsInfo(elementUnmanagedType, _defaultInfo.CharEncoding);
                }
                else
                {
                    elementMarshallingInfo = marshallingInfoCallback(elementType, useSiteAttributes, indirectionDepth + 1);
                }

                CountInfo countInfo = NoCountInfo.Instance;

                if (useSiteAttributes.TryGetUseSiteAttributeInfo(indirectionDepth, out UseSiteAttributeData useSiteAttributeData))
                {
                    countInfo = useSiteAttributeData.CountInfo;
                }

                return ArrayMarshallingInfoProvider.CreateArrayMarshallingInfo(_compilation, type, elementType, countInfo, elementMarshallingInfo);
            }

            if (type.SpecialType == SpecialType.System_String)
            {
                return CreateStringMarshallingInfo(type, unmanagedType);
            }

            return new MarshalAsInfo(unmanagedType, _defaultInfo.CharEncoding);
        }

        private MarshallingInfo CreateStringMarshallingInfo(
            ITypeSymbol type,
            UnmanagedType unmanagedType)
        {
            string? marshallerName = unmanagedType switch
            {
                UnmanagedType.BStr => TypeNames.BStrStringMarshaller,
                UnmanagedType.LPStr => TypeNames.AnsiStringMarshaller,
                UnmanagedType.LPTStr or UnmanagedType.LPWStr => TypeNames.Utf16StringMarshaller,
                MarshalAsInfo.UnmanagedType_LPUTF8Str => TypeNames.Utf8StringMarshaller,
                _ => null
            };

            if (marshallerName is null)
                return new MarshalAsInfo(unmanagedType, _defaultInfo.CharEncoding);

            return StringMarshallingInfoProvider.CreateStringMarshallingInfo(_compilation, type, marshallerName);
        }
    }
}
