// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public sealed class NativeMarshallingAttributeParser : IMarshallingInfoAttributeParser
    {
        private readonly Compilation _compilation;
        private readonly GeneratorDiagnosticsBag _diagnostics;

        public NativeMarshallingAttributeParser(Compilation compilation, GeneratorDiagnosticsBag diagnostics)
        {
            _compilation = compilation;
            _diagnostics = diagnostics;
        }

        public bool CanParseAttributeType(INamedTypeSymbol attributeType) => attributeType.ToDisplayString() == TypeNames.NativeMarshallingAttribute;

        public MarshallingInfo? ParseAttribute(AttributeData attributeData, ITypeSymbol type, int indirectionDepth, UseSiteAttributeProvider useSiteAttributes, GetMarshallingInfoCallback marshallingInfoCallback)
        {
            Debug.Assert(attributeData.AttributeClass!.ToDisplayString() == TypeNames.NativeMarshallingAttribute);
            CountInfo countInfo = NoCountInfo.Instance;
            if (useSiteAttributes.TryGetUseSiteAttributeInfo(indirectionDepth, out var useSiteInfo))
            {
                countInfo = useSiteInfo.CountInfo;
            }

            if (attributeData.ConstructorArguments[0].Value is not INamedTypeSymbol entryPointType)
            {
                return NoMarshallingInfo.Instance;
            }

            return CustomMarshallingInfoHelper.CreateNativeMarshallingInfo(
                type,
                entryPointType,
                attributeData,
                useSiteAttributes,
                marshallingInfoCallback,
                indirectionDepth,
                countInfo,
                _diagnostics,
                _compilation
                );
        }
    }
}
