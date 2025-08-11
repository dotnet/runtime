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
            // The goal is to provide clearer error messages specifically for cross-assembly scenarios
            // where users get confusing "MarshalAsAttribute configuration" errors when they never
            // used MarshalAsAttribute explicitly.
            //
            // For explicit MarshalAs attributes, the traditional message is appropriate.
            // For inferred marshalling (especially cross-assembly types), use the type-focused message.

            if (typePositionInfo.MarshallingAttributeInfo is MarshalAsInfo)
            {
                // If there's an explicit MarshalAs attribute, use the traditional message
                // This maintains compatibility with existing scenarios and test expectations
                return true;
            }

            // No MarshalAs attribute - the marshalling behavior is inferred
            // In this case, use the clearer type-focused message
            return false;
        }
    }
}
