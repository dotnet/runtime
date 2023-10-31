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
    /// This class suppports parsing a System.Runtime.InteropServices.MarshalAsAttribute into either a <see cref="MarshalAsInfo"/> or a <see cref="NativeMarshallingAttributeInfo"/>
    /// if the marshalling is implemented with a custom marshaller in the framework.
    /// </summary>
    public sealed class MarshalAsWithCustomMarshallersParser : IMarshallingInfoAttributeParser
    {
        private readonly Compilation _compilation;
        private readonly GeneratorDiagnosticsBag _diagnostics;
        private readonly IMarshallingInfoAttributeParser _marshalAsAttributeParser;

        /// <summary>
        /// Create a new instance of <see cref="MarshalAsWithCustomMarshallersParser"/>.
        /// </summary>
        /// <param name="compilation">The compilation that the attributes are defined within.</param>
        /// <param name="diagnostics">The diagnostics bag to which to report diagnostics.</param>
        /// <param name="marshalAsAttributeParser">The parser that will do basic parsing of a MarshalAsAttribute into a <see cref="MarshalAsInfo"/> element.</param>
        public MarshalAsWithCustomMarshallersParser(Compilation compilation, GeneratorDiagnosticsBag diagnostics, IMarshallingInfoAttributeParser marshalAsAttributeParser)
        {
            _compilation = compilation;
            _diagnostics = diagnostics;
            _marshalAsAttributeParser = marshalAsAttributeParser;
        }

        public bool CanParseAttributeType(INamedTypeSymbol attributeType) => attributeType.ToDisplayString() == TypeNames.System_Runtime_InteropServices_MarshalAsAttribute;

        MarshallingInfo? IMarshallingInfoAttributeParser.ParseAttribute(AttributeData attributeData, ITypeSymbol type, int indirectionDepth, UseSiteAttributeProvider useSiteAttributes, GetMarshallingInfoCallback marshallingInfoCallback)
        {
            var marshalAsInfo = (MarshalAsInfo)_marshalAsAttributeParser.ParseAttribute(attributeData, type, indirectionDepth, useSiteAttributes, marshallingInfoCallback);

            // We'll support the UnmanagedType.Interface option, but we'll explicitly
            // leave ComImport types with the MarshalAs info instead of the custom marshaller
            // as they will not work as expected unless they are migrated to [GeneratedComInterface].
            if (marshalAsInfo.UnmanagedType == UnmanagedType.Interface)
            {
                return type is INamedTypeSymbol { IsComImport: true }
                    ? marshalAsInfo
                    : ComInterfaceMarshallingInfoProvider.CreateComInterfaceMarshallingInfo(_compilation, type);
            }

            if (marshalAsInfo is MarshalAsArrayInfo arrayInfo)
            {
                if (type is not IArrayTypeSymbol { ElementType: ITypeSymbol elementType })
                {
                    _diagnostics.ReportConfigurationNotSupported(attributeData, nameof(UnmanagedType), arrayInfo.UnmanagedType.ToString());
                    return NoMarshallingInfo.Instance;
                }

                MarshallingInfo elementMarshallingInfo = NoMarshallingInfo.Instance;
                if (arrayInfo.ArraySubType != (UnmanagedType)SizeAndParamIndexInfo.UnspecifiedConstSize)
                {
                    if (elementType.SpecialType == SpecialType.System_String)
                    {
                        elementMarshallingInfo = CreateStringMarshallingInfo(elementType, new MarshalAsScalarInfo(arrayInfo.ArraySubType, arrayInfo.CharEncoding));
                    }
                    else if (arrayInfo.ArraySubType == UnmanagedType.Interface && elementType is not INamedTypeSymbol { IsComImport: true })
                    {
                        elementMarshallingInfo = ComInterfaceMarshallingInfoProvider.CreateComInterfaceMarshallingInfo(_compilation, elementType);
                    }
                    else
                    {
                        elementMarshallingInfo = new MarshalAsScalarInfo(arrayInfo.ArraySubType, arrayInfo.CharEncoding);
                    }
                }
                else
                {
                    elementMarshallingInfo = marshallingInfoCallback(elementType, useSiteAttributes, indirectionDepth + 1);
                }

                return ArrayMarshallingInfoProvider.CreateArrayMarshallingInfo(_compilation, type, elementType, arrayInfo.CountInfo, elementMarshallingInfo);
            }

            if (type.SpecialType == SpecialType.System_String)
            {
                return CreateStringMarshallingInfo(type, marshalAsInfo);
            }

            if (type.SpecialType == SpecialType.System_Object && marshalAsInfo is MarshalAsScalarInfo(UnmanagedType.Struct, _))
            {
                return CustomMarshallingInfoHelper.CreateMarshallingInfoByMarshallerTypeName(_compilation, type, TypeNames.ComVariantMarshaller);
            }

            return marshalAsInfo;
        }

        private MarshallingInfo CreateStringMarshallingInfo(
            ITypeSymbol type,
            MarshalAsInfo marshalAsInfo)
        {
            string? marshallerName = marshalAsInfo.UnmanagedType switch
            {
                UnmanagedType.BStr => TypeNames.BStrStringMarshaller,
                UnmanagedType.LPStr => TypeNames.AnsiStringMarshaller,
                UnmanagedType.LPTStr or UnmanagedType.LPWStr => TypeNames.Utf16StringMarshaller,
                MarshalAsInfo.UnmanagedType_LPUTF8Str => TypeNames.Utf8StringMarshaller,
                _ => null
            };

            if (marshallerName is null)
            {
                return marshalAsInfo;
            }

            return CustomMarshallingInfoHelper.CreateMarshallingInfoByMarshallerTypeName(_compilation, type, marshallerName);
        }
    }
}
