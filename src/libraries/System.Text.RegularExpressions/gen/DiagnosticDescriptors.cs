// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace System.Text.RegularExpressions.Generator
{
    internal static class DiagnosticDescriptors
    {
        private const string Category = "GeneratedRegex";

        public static DiagnosticDescriptor InvalidGeneratedRegexAttribute { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1040",
            title: new LocalizableResourceString(nameof(SR.InvalidGeneratedRegexAttributeTitle), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.InvalidGeneratedRegexAttributeMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            category: Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.NotConfigurable);

        public static DiagnosticDescriptor MultipleGeneratedRegexAttributes { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1041",
            title: new LocalizableResourceString(nameof(SR.InvalidGeneratedRegexAttributeTitle), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.MultipleGeneratedRegexAttributesMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            category: Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.NotConfigurable);

        public static DiagnosticDescriptor InvalidRegexArguments { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1042",
            title: new LocalizableResourceString(nameof(SR.InvalidGeneratedRegexAttributeTitle), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.InvalidRegexArgumentsMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            category: Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.NotConfigurable);

        public static DiagnosticDescriptor RegexMethodMustHaveValidSignature { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1043",
            title: new LocalizableResourceString(nameof(SR.InvalidGeneratedRegexAttributeTitle), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.RegexMethodMustHaveValidSignatureMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            category: Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.NotConfigurable);

        public static DiagnosticDescriptor LimitedSourceGeneration { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1044",
            title: new LocalizableResourceString(nameof(SR.LimitedSourceGenerationTitle), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.LimitedSourceGenerationMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            category: Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor UseRegexSourceGeneration { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1045",
            title: new LocalizableResourceString(nameof(SR.UseRegexSourceGeneratorTitle), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.UseRegexSourceGeneratorMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            category: Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);
    }
}
