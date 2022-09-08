// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    /// <summary>
    /// This class suppports parsing a System.Runtime.InteropServices.Marshalling.MarshalUsingAttribute.
    /// </summary>
    public sealed class MarshalUsingAttributeParser : IMarshallingInfoAttributeParser, IUseSiteAttributeParser
    {
        private readonly Compilation _compilation;
        private readonly IGeneratorDiagnostics _diagnostics;

        public MarshalUsingAttributeParser(Compilation compilation, IGeneratorDiagnostics diagnostics)
        {
            _compilation = compilation;
            _diagnostics = diagnostics;
        }

        public bool CanParseAttributeType(INamedTypeSymbol attributeType) => attributeType.ToDisplayString() == TypeNames.MarshalUsingAttribute;

        MarshallingInfo? IMarshallingInfoAttributeParser.ParseAttribute(AttributeData attributeData, ITypeSymbol type, int indirectionDepth, UseSiteAttributeProvider useSiteAttributes, GetMarshallingInfoCallback marshallingInfoCallback)
        {
            Debug.Assert(attributeData.AttributeClass!.ToDisplayString() == TypeNames.MarshalUsingAttribute);
            CountInfo countInfo = NoCountInfo.Instance;
            if (useSiteAttributes.TryGetUseSiteAttributeInfo(indirectionDepth, out UseSiteAttributeData useSiteInfo))
            {
                countInfo = useSiteInfo.CountInfo;
            }

            if (attributeData.ConstructorArguments.Length == 0)
            {
                // This attribute only has count information.
                // It does not provide any marshalling info.
                // Return null here to respresent the lack of any marshalling info,
                // instead of the presence of invalid marshalling info.
                return null;
            }

            if (attributeData.ConstructorArguments[0].Value is not INamedTypeSymbol namedType)
            {
                return NoMarshallingInfo.Instance;
            }

            return CustomMarshallingInfoHelper.CreateNativeMarshallingInfo(
                type,
                namedType,
                attributeData,
                useSiteAttributes,
                marshallingInfoCallback,
                indirectionDepth,
                countInfo,
                _diagnostics,
                _compilation
                );
        }

        UseSiteAttributeData IUseSiteAttributeParser.ParseAttribute(AttributeData attributeData, IElementInfoProvider elementInfoProvider, GetMarshallingInfoCallback marshallingInfoCallback)
        {
            ImmutableDictionary<string, TypedConstant> namedArgs = ImmutableDictionary.CreateRange(attributeData.NamedArguments);
            CountInfo countInfo = ParseCountInfo(attributeData, elementInfoProvider, marshallingInfoCallback);
            int elementIndirectionDepth = namedArgs.TryGetValue(ManualTypeMarshallingHelper.MarshalUsingProperties.ElementIndirectionDepth, out TypedConstant value) ? (int)value.Value! : 0;
            return new UseSiteAttributeData(elementIndirectionDepth, countInfo, attributeData);
        }

        private CountInfo ParseCountInfo(AttributeData attributeData, IElementInfoProvider elementInfoProvider, GetMarshallingInfoCallback marshallingInfoCallback)
        {
            int? constSize = null;
            string? elementName = null;
            foreach (KeyValuePair<string, TypedConstant> arg in attributeData.NamedArguments)
            {
                if (arg.Key == ManualTypeMarshallingHelper.MarshalUsingProperties.ConstantElementCount)
                {
                    constSize = (int)arg.Value.Value!;
                }
                else if (arg.Key == ManualTypeMarshallingHelper.MarshalUsingProperties.CountElementName)
                {
                    if (arg.Value.Value is null)
                    {
                        _diagnostics.ReportConfigurationNotSupported(attributeData, ManualTypeMarshallingHelper.MarshalUsingProperties.CountElementName, "null");
                        return NoCountInfo.Instance;
                    }
                    elementName = (string)arg.Value.Value!;
                }
            }

            if (constSize is not null && elementName is not null)
            {
                _diagnostics.ReportInvalidMarshallingAttributeInfo(attributeData, nameof(SR.ConstantAndElementCountInfoDisallowed));
            }
            else if (constSize is not null)
            {
                return new ConstSizeCountInfo(constSize.Value);
            }
            else if (elementName is not null)
            {
                if (!elementInfoProvider.TryGetInfoForElementName(attributeData, elementName, marshallingInfoCallback, out TypePositionInfo elementInfo))
                {
                    _diagnostics.ReportConfigurationNotSupported(attributeData, ManualTypeMarshallingHelper.MarshalUsingProperties.CountElementName, elementName);
                    return NoCountInfo.Instance;
                }
                return new CountElementCountInfo(elementInfo);
            }

            return NoCountInfo.Instance;
        }
    }
}
