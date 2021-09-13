// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace System.Text.RegularExpressions.Generator
{
    public static class DiagnosticDescriptors
    {
        // TODO: Assign valid IDs

        public static DiagnosticDescriptor InvalidRegexGeneratorAttribute { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1100",
            title: new LocalizableResourceString(nameof(SR.InvalidRegexGeneratorAttributeMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.InvalidRegexGeneratorAttributeMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            category: "RegexGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MultipleRegexGeneratorAttributes { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1101",
            title: new LocalizableResourceString(nameof(SR.MultipleRegexGeneratorAttributesMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.MultipleRegexGeneratorAttributesMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            category: "RegexGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor InvalidRegexArguments { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1102",
            title: new LocalizableResourceString(nameof(SR.InvalidRegexArgumentsMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.InvalidRegexArgumentsMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            category: "RegexGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor RegexMethodMustReturnRegex { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1103",
            title: new LocalizableResourceString(nameof(SR.RegexMethodMustReturnRegexMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.RegexMethodMustReturnRegexMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            category: "RegexGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor RegexMethodMustBeParameterless { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1104",
            title: new LocalizableResourceString(nameof(SR.RegexMethodMustBeParameterlessMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.RegexMethodMustBeParameterlessMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            category: "RegexGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor RegexMethodMustNotBeGeneric { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1105",
            title: new LocalizableResourceString(nameof(SR.RegexMethodMustNotBeGenericMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.RegexMethodMustNotBeGenericMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            category: "RegexGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor RegexMethodMustBePartial { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1106",
            title: new LocalizableResourceString(nameof(SR.RegexMethodMustBePartialMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.RegexMethodMustBePartialMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            category: "RegexGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor RegexMethodMustBeStatic { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1107",
            title: new LocalizableResourceString(nameof(SR.RegexMethodMustBeStaticMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.RegexMethodMustBeStaticMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            category: "RegexGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor InvalidLangVersion { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1108",
            title: new LocalizableResourceString(nameof(SR.InvalidLangVersionMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.InvalidLangVersionMessage), SR.ResourceManager, typeof(FxResources.System.Text.RegularExpressions.Generator.SR)),
            category: "RegexGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}
