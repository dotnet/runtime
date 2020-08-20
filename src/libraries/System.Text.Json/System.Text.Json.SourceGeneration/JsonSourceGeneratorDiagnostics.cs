// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;

namespace System.Text.Json.SourceGeneration
{
    internal sealed partial class JsonSourceGeneratorHelper
    {
        // Diagnostic descriptors for user.
        private DiagnosticDescriptor _generatedTypeClass;
        private DiagnosticDescriptor _failedToGenerateTypeClass;
        private DiagnosticDescriptor _failedToAddNewTypesFromMembers;
        private DiagnosticDescriptor _notSupported;

        private void InitializeDiagnosticDescriptors()
        {
            _generatedTypeClass =
                new DiagnosticDescriptor(
                    "JsonSourceGeneration",
                    "Generated type class generation",
                    "Generated type class {1} for root type {0}",
                    "category",
                    DiagnosticSeverity.Info,
                    isEnabledByDefault: true);
            _failedToGenerateTypeClass =
                new DiagnosticDescriptor(
                    "JsonSourceGeneration",
                    "Failed to generate typeclass",
                    "Failed in sourcegenerating nested type {1} for root type {0}.",
                    "category",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true,
                    description: "Error message: {2}");
            _failedToAddNewTypesFromMembers =
                new DiagnosticDescriptor(
                    "JsonSourceGeneration",
                    "Failed to add new types from current type",
                    "Failed to iterate fields and properties for current type {1} for root type {0}",
                    "category",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true);
            _notSupported =
                new DiagnosticDescriptor(
                    "JsonSourceGeneration",
                    "Current type is not supported",
                    "Failed in sourcegenerating nested type {1} for root type {0}",
                    "category",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true);
        }
    }
}
