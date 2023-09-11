// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Interop.JavaScript
{
    internal sealed class DescriptorProvider : IDiagnosticDescriptorProvider
    {
        public DiagnosticDescriptor InvalidMarshallingAttributeInfo => GeneratorDiagnostics.MarshallingAttributeConfigurationNotSupported;

        public DiagnosticDescriptor ConfigurationNotSupported => GeneratorDiagnostics.ConfigurationNotSupported;

        public DiagnosticDescriptor ConfigurationValueNotSupported => GeneratorDiagnostics.ConfigurationValueNotSupported;

        public DiagnosticDescriptor? GetDescriptor(GeneratorDiagnostic diagnostic)
        {
            return diagnostic switch
            {
                GeneratorDiagnostic.NotSupported { NotSupportedDetails: not null, TypePositionInfo.IsManagedReturnPosition: true } => GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails,
                GeneratorDiagnostic.NotSupported { NotSupportedDetails: not null, TypePositionInfo.IsManagedReturnPosition: false } => GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails,
                { IsFatal: false } => null, // The JSImport and JSExport generators don't report any non-fatal diagnostics.
                { TypePositionInfo.IsManagedReturnPosition: true } => GeneratorDiagnostics.ReturnTypeNotSupported,
                { TypePositionInfo.IsManagedReturnPosition: false } => GeneratorDiagnostics.ParameterTypeNotSupported,
            };
        }
    }
}
