// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.Shared;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
    public static class DiagnosticDescriptors
    {
        public static DiagnosticDescriptor GetDiagnosticDescriptor(DiagnosticId diagnosticId)
        {
            var diagnosticString = new DiagnosticString(diagnosticId);
            return new DiagnosticDescriptor(diagnosticId.AsString(),
                diagnosticString.GetTitleFormat(),
                diagnosticString.GetMessageFormat(),
                diagnosticId.GetDiagnosticCategory(),
                DiagnosticSeverity.Warning,
                true);
        }

        public static DiagnosticDescriptor GetDiagnosticDescriptor(DiagnosticId diagnosticId, DiagnosticString diagnosticString)
            => new DiagnosticDescriptor(diagnosticId.AsString(),
                diagnosticString.GetTitle(),
                diagnosticString.GetMessage(),
                diagnosticId.GetDiagnosticCategory(),
                DiagnosticSeverity.Warning,
                true);

        public static DiagnosticDescriptor GetDiagnosticDescriptor(DiagnosticId diagnosticId,
            LocalizableResourceString? lrsTitle = null,
            LocalizableResourceString? lrsMessage = null,
            string? diagnosticCategory = null,
            DiagnosticSeverity diagnosticSeverity = DiagnosticSeverity.Warning,
            bool isEnabledByDefault = true,
            string? helpLinkUri = null)
        {
            if (lrsTitle == null || lrsMessage == null)
            {
                var diagnosticString = new DiagnosticString(diagnosticId);
                return new DiagnosticDescriptor(diagnosticId.AsString(),
                    diagnosticString.GetTitleFormat(),
                    diagnosticString.GetMessageFormat(),
                    diagnosticCategory ?? diagnosticId.GetDiagnosticCategory(),
                    diagnosticSeverity,
                    isEnabledByDefault,
                    helpLinkUri);
            }

            return new DiagnosticDescriptor(diagnosticId.AsString(),
                lrsTitle!,
                lrsMessage!,
                diagnosticCategory ?? diagnosticId.GetDiagnosticCategory(),
                diagnosticSeverity,
                isEnabledByDefault,
                helpLinkUri);
        }
    }
}
