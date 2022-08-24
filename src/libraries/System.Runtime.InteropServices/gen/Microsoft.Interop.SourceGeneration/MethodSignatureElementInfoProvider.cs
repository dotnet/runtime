// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public sealed class MethodSignatureElementInfoProvider : IElementInfoProvider
    {
        private readonly Compilation _compilation;
        private readonly IGeneratorDiagnostics _generatorDiagnostics;
        private readonly IMethodSymbol _method;
        private readonly ImmutableArray<IUseSiteAttributeParser> _useSiteAttributeParsers;

        public MethodSignatureElementInfoProvider(Compilation compilation, IGeneratorDiagnostics generatorDiagnostics, IMethodSymbol method, ImmutableArray<IUseSiteAttributeParser> useSiteAttributeParsers)
        {
            _compilation = compilation;
            _generatorDiagnostics = generatorDiagnostics;
            _method = method;
            _useSiteAttributeParsers = useSiteAttributeParsers;
        }

        public string FindNameForParamIndex(int paramIndex) => paramIndex >= _method.Parameters.Length ? string.Empty : _method.Parameters[paramIndex].Name;

        public bool TryGetInfoForElementName(AttributeData attrData, string elementName, GetMarshallingInfoCallback marshallingInfoCallback, IElementInfoProvider rootProvider, out TypePositionInfo info)
        {
            if (elementName == CountElementCountInfo.ReturnValueElementName)
            {
                info = new TypePositionInfo(
                    ManagedTypeInfo.CreateTypeInfoForTypeSymbol(_method.ReturnType),
                    marshallingInfoCallback(_method.ReturnType, new UseSiteAttributeProvider(_useSiteAttributeParsers, _method.GetReturnTypeAttributes(), rootProvider, _generatorDiagnostics, marshallingInfoCallback), 0)) with
                {
                    ManagedIndex = TypePositionInfo.ReturnIndex
                };
                return true;
            }

            for (int i = 0; i < _method.Parameters.Length; i++)
            {
                IParameterSymbol param = _method.Parameters[i];
                if (param.Name == elementName)
                {
                    info = TypePositionInfo.CreateForParameter(
                        param,
                        marshallingInfoCallback(param.Type, new UseSiteAttributeProvider(_useSiteAttributeParsers, param.GetAttributes(), rootProvider, _generatorDiagnostics, marshallingInfoCallback), 0), _compilation) with
                    {
                        ManagedIndex = i
                    };
                    return true;
                }
            }
            info = null;
            return false;
        }

        public bool TryGetInfoForParamIndex(AttributeData attrData, int paramIndex, GetMarshallingInfoCallback marshallingInfoCallback, IElementInfoProvider rootProvider, out TypePositionInfo info)
        {
            if (paramIndex >= _method.Parameters.Length)
            {
                info = null;
                return false;
            }
            IParameterSymbol param = _method.Parameters[paramIndex];

            info = TypePositionInfo.CreateForParameter(
                param,
                marshallingInfoCallback(param.Type, new UseSiteAttributeProvider(_useSiteAttributeParsers, param.GetAttributes(), rootProvider, _generatorDiagnostics, marshallingInfoCallback), 0), _compilation) with
            {
                ManagedIndex = paramIndex
            };
            return true;
        }
    }
}
