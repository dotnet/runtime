// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    internal sealed class DiagnosticDescriptorProvider : IDiagnosticDescriptorProvider
    {
        public DiagnosticDescriptor InvalidMarshallingAttributeInfo => GeneratorDiagnostics.MarshallingAttributeConfigurationNotSupported;

        public DiagnosticDescriptor ConfigurationNotSupported => GeneratorDiagnostics.ConfigurationNotSupported;

        public DiagnosticDescriptor ConfigurationValueNotSupported => GeneratorDiagnostics.ConfigurationValueNotSupported;

        public DiagnosticDescriptor? GetDescriptor(GeneratorDiagnostic diagnostic)
        {
            return diagnostic switch
            {
                GeneratorDiagnostic.NotSupported { NotSupportedDetails: null, TypePositionInfo: { IsManagedReturnPosition: true, MarshallingAttributeInfo: MarshalAsInfo } } => GeneratorDiagnostics.MarshalAsReturnConfigurationNotSupported,
                GeneratorDiagnostic.NotSupported { NotSupportedDetails: null, TypePositionInfo: { IsManagedReturnPosition: false, MarshallingAttributeInfo: MarshalAsInfo } } => GeneratorDiagnostics.MarshalAsParameterConfigurationNotSupported,
                GeneratorDiagnostic.NotSupported { NotSupportedDetails: not null, TypePositionInfo.IsManagedReturnPosition: true } => GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails,
                GeneratorDiagnostic.NotSupported { NotSupportedDetails: not null, TypePositionInfo.IsManagedReturnPosition: false } => GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails,
                GeneratorDiagnostic.UnnecessaryData { TypePositionInfo.IsManagedReturnPosition: false } => GeneratorDiagnostics.UnnecessaryParameterMarshallingInfo,
                GeneratorDiagnostic.UnnecessaryData { TypePositionInfo.IsManagedReturnPosition: true } => GeneratorDiagnostics.UnnecessaryReturnMarshallingInfo,
                { IsFatal: false } => null,
                { TypePositionInfo.IsManagedReturnPosition: true } => GeneratorDiagnostics.ReturnTypeNotSupported,
                { TypePositionInfo.IsManagedReturnPosition: false } => GeneratorDiagnostics.ParameterTypeNotSupported,
            };
        }
    }
}
