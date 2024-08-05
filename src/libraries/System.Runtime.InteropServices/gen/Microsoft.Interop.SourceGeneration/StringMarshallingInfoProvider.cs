// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    /// <summary>
    /// This class supports generating marshalling info for the <see cref="string"/> type.
    /// This includes support for the <c>System.Runtime.InteropServices.StringMarshalling</c> enum.
    /// </summary>
    public sealed class StringMarshallingInfoProvider : ITypeBasedMarshallingInfoProvider
    {
        private readonly Compilation _compilation;
        private readonly GeneratorDiagnosticsBag _diagnostics;
        private readonly AttributeData _stringMarshallingCustomAttribute;
        private readonly DefaultMarshallingInfo _defaultMarshallingInfo;

        public StringMarshallingInfoProvider(Compilation compilation, GeneratorDiagnosticsBag diagnostics, AttributeData stringMarshallingCustomAttribute, DefaultMarshallingInfo defaultMarshallingInfo)
        {
            _compilation = compilation;
            _diagnostics = diagnostics;
            _stringMarshallingCustomAttribute = stringMarshallingCustomAttribute;
            _defaultMarshallingInfo = defaultMarshallingInfo;
        }

        public bool CanProvideMarshallingInfoForType(ITypeSymbol type) => type.SpecialType == SpecialType.System_String;

        public MarshallingInfo GetMarshallingInfo(ITypeSymbol type, int indirectionDepth, UseSiteAttributeProvider useSiteAttributes, GetMarshallingInfoCallback marshallingInfoCallback)
        {
            if (_defaultMarshallingInfo.CharEncoding == CharEncoding.Undefined)
            {
                return NoMarshallingInfo.Instance;
            }
            else if (_defaultMarshallingInfo.CharEncoding == CharEncoding.Custom)
            {
                if (_defaultMarshallingInfo.StringMarshallingCustomType is not null)
                {
                    CountInfo countInfo = NoCountInfo.Instance;
                    if (useSiteAttributes.TryGetUseSiteAttributeInfo(indirectionDepth, out var useSiteInfo))
                    {
                        countInfo = useSiteInfo.CountInfo;
                    }
                    return CustomMarshallingInfoHelper.CreateNativeMarshallingInfo(
                        type,
                        _defaultMarshallingInfo.StringMarshallingCustomType,
                        _stringMarshallingCustomAttribute,
                        useSiteAttributes,
                        marshallingInfoCallback,
                        indirectionDepth,
                        countInfo,
                        _diagnostics,
                        _compilation);
                }
            }
            else
            {
                // No marshalling info was computed, but a character encoding was provided.
                return _defaultMarshallingInfo.CharEncoding switch
                {
                    CharEncoding.Utf16 => CustomMarshallingInfoHelper.CreateMarshallingInfoByMarshallerTypeName(_compilation, type, TypeNames.Utf16StringMarshaller),
                    CharEncoding.Utf8 => CustomMarshallingInfoHelper.CreateMarshallingInfoByMarshallerTypeName(_compilation, type, TypeNames.Utf8StringMarshaller),
                    _ => throw new InvalidOperationException()
                };
            }

            return new MarshallingInfoStringSupport(_defaultMarshallingInfo.CharEncoding);
        }
    }
}
