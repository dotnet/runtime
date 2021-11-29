// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace System.Text.RegularExpressions.Generator
{
    internal static class DiagnosticDescriptors
    {
        public static DiagnosticDescriptor InvalidRegexGeneratorAttribute { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1040",
            title: new LocalizableResourceString(nameof(SR.InvalidRegexGeneratorAttributeTitle), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.InvalidRegexGeneratorAttributeMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            category: "RegexGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.NotConfigurable);

        public static DiagnosticDescriptor MultipleRegexGeneratorAttributes { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1041",
            title: new LocalizableResourceString(nameof(SR.InvalidRegexGeneratorAttributeTitle), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.MultipleRegexGeneratorAttributesMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            category: "RegexGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.NotConfigurable);

        public static DiagnosticDescriptor InvalidRegexArguments { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1042",
            title: new LocalizableResourceString(nameof(SR.InvalidRegexGeneratorAttributeTitle), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.InvalidRegexArgumentsMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            category: "RegexGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.NotConfigurable);

        public static DiagnosticDescriptor RegexMethodMustHaveValidSignature { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1043",
            title: new LocalizableResourceString(nameof(SR.InvalidRegexGeneratorAttributeTitle), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.RegexMethodMustHaveValidSignatureMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            category: "RegexGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.NotConfigurable);

        public static DiagnosticDescriptor InvalidLangVersion { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1044",
            title: new LocalizableResourceString(nameof(SR.InvalidRegexGeneratorAttributeTitle), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.InvalidLangVersionMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            category: "RegexGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.NotConfigurable);
    }
}
