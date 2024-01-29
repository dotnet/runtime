// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace System.Text.RegularExpressions.Generator
{
    internal static class DiagnosticDescriptors
    {
        private const string Category = "Performance";

        public static DiagnosticDescriptor InvalidGeneratedRegexAttribute { get; } = DiagnosticDescriptorHelper.Create(
            id: "SYSLIB1040",
            title: new LocalizableResourceString(nameof(SR.InvalidGeneratedRegexAttributeTitle), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.InvalidGeneratedRegexAttributeMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            category: Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.NotConfigurable);

        public static DiagnosticDescriptor MultipleGeneratedRegexAttributes { get; } = DiagnosticDescriptorHelper.Create(
            id: "SYSLIB1041",
            title: new LocalizableResourceString(nameof(SR.InvalidGeneratedRegexAttributeTitle), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.MultipleGeneratedRegexAttributesMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            category: Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.NotConfigurable);

        public static DiagnosticDescriptor InvalidRegexArguments { get; } = DiagnosticDescriptorHelper.Create(
            id: "SYSLIB1042",
            title: new LocalizableResourceString(nameof(SR.InvalidGeneratedRegexAttributeTitle), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.InvalidRegexArgumentsMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            category: Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.NotConfigurable);

        public static DiagnosticDescriptor RegexMethodMustHaveValidSignature { get; } = DiagnosticDescriptorHelper.Create(
            id: "SYSLIB1043",
            title: new LocalizableResourceString(nameof(SR.InvalidGeneratedRegexAttributeTitle), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.RegexMethodMustHaveValidSignatureMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            category: Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.NotConfigurable);

        public static DiagnosticDescriptor LimitedSourceGeneration { get; } = DiagnosticDescriptorHelper.Create(
            id: "SYSLIB1044",
            title: new LocalizableResourceString(nameof(SR.LimitedSourceGenerationTitle), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.LimitedSourceGenerationMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            category: Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor UseRegexSourceGeneration { get; } = DiagnosticDescriptorHelper.Create(
            id: "SYSLIB1045",
            title: new LocalizableResourceString(nameof(SR.UseRegexSourceGeneratorTitle), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.UseRegexSourceGeneratorMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            category: Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);
    }
}
