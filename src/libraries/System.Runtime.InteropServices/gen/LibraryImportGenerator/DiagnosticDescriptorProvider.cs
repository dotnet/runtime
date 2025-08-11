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
                // Use the new type-not-supported messages for MarshalAs scenarios when appropriate
                GeneratorDiagnostic.NotSupported { NotSupportedDetails: null, TypePositionInfo: { IsManagedReturnPosition: true, MarshallingAttributeInfo: MarshalAsInfo } } notSupported
                    => ShouldUseMarshalAsSpecificMessage(notSupported.TypePositionInfo)
                        ? GeneratorDiagnostics.MarshalAsReturnConfigurationNotSupported
                        : GeneratorDiagnostics.TypeNotSupportedWithMarshallingInfoReturn,
                GeneratorDiagnostic.NotSupported { NotSupportedDetails: null, TypePositionInfo: { IsManagedReturnPosition: false, MarshallingAttributeInfo: MarshalAsInfo } } notSupported
                    => ShouldUseMarshalAsSpecificMessage(notSupported.TypePositionInfo)
                        ? GeneratorDiagnostics.MarshalAsParameterConfigurationNotSupported
                        : GeneratorDiagnostics.TypeNotSupportedWithMarshallingInfoParameter,
                GeneratorDiagnostic.NotSupported { NotSupportedDetails: not null, TypePositionInfo.IsManagedReturnPosition: true } => GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails,
                GeneratorDiagnostic.NotSupported { NotSupportedDetails: not null, TypePositionInfo.IsManagedReturnPosition: false } => GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails,
                GeneratorDiagnostic.UnnecessaryData { TypePositionInfo.IsManagedReturnPosition: false } => GeneratorDiagnostics.UnnecessaryParameterMarshallingInfo,
                GeneratorDiagnostic.UnnecessaryData { TypePositionInfo.IsManagedReturnPosition: true } => GeneratorDiagnostics.UnnecessaryReturnMarshallingInfo,
                GeneratorDiagnostic.NotRecommended => GeneratorDiagnostics.LibraryImportUsageDoesNotFollowBestPractices,
                { IsFatal: false } => null,
                { TypePositionInfo.IsManagedReturnPosition: true } => GeneratorDiagnostics.ReturnTypeNotSupported,
                { TypePositionInfo.IsManagedReturnPosition: false } => GeneratorDiagnostics.ParameterTypeNotSupported,
            };
        }

        /// <summary>
        /// Determines whether to use the MarshalAs-specific error message or the generic type-not-supported message.
        /// Returns true if there's likely an explicit MarshalAs attribute, false if the marshalling behavior is inferred.
        /// </summary>
        private static bool ShouldUseMarshalAsSpecificMessage(TypePositionInfo typePositionInfo)
        {
            // For now, use a conservative approach: prefer the type-not-supported message in most cases
            // since it's generally clearer for users. Only use the MarshalAs-specific message for
            // very specific scenarios where it adds value.

            // In the future, we could enhance this by:
            // 1. Checking if there are actual MarshalAs attributes in the source location
            // 2. Analyzing the compilation context to determine if this is from external assembly
            // 3. Providing different messages based on the specific UnmanagedType values

            // For now, always prefer the clearer type-focused message
            _ = typePositionInfo; // Parameter kept for future enhancement
            return false;
        }
    }
}
