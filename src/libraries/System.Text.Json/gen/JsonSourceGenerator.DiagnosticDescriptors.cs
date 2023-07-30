// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace System.Text.Json.SourceGeneration
{
    public sealed partial class JsonSourceGenerator
    {
        internal static class DiagnosticDescriptors
        {
            public static DiagnosticDescriptor TypeNotSupported { get; } = new DiagnosticDescriptor(
                id: "SYSLIB1030",
                title: new LocalizableResourceString(nameof(SR.TypeNotSupportedTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.TypeNotSupportedMessageFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public static DiagnosticDescriptor DuplicateTypeName { get; } = new DiagnosticDescriptor(
                id: "SYSLIB1031",
                title: new LocalizableResourceString(nameof(SR.DuplicateTypeNameTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.DuplicateTypeNameMessageFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public static DiagnosticDescriptor ContextClassesMustBePartial { get; } = new DiagnosticDescriptor(
                id: "SYSLIB1032",
                title: new LocalizableResourceString(nameof(SR.ContextClassesMustBePartialTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.ContextClassesMustBePartialMessageFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public static DiagnosticDescriptor MultipleJsonConstructorAttribute { get; } = new DiagnosticDescriptor(
                id: "SYSLIB1033",
                title: new LocalizableResourceString(nameof(SR.MultipleJsonConstructorAttributeTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.MultipleJsonConstructorAttributeFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            public static DiagnosticDescriptor MultipleJsonExtensionDataAttribute { get; } = new DiagnosticDescriptor(
                id: "SYSLIB1035",
                title: new LocalizableResourceString(nameof(SR.MultipleJsonExtensionDataAttributeTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.MultipleJsonExtensionDataAttributeFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            public static DiagnosticDescriptor DataExtensionPropertyInvalid { get; } = new DiagnosticDescriptor(
                id: "SYSLIB1036",
                title: new LocalizableResourceString(nameof(SR.DataExtensionPropertyInvalidTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.DataExtensionPropertyInvalidFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            public static DiagnosticDescriptor InaccessibleJsonIncludePropertiesNotSupported { get; } = new DiagnosticDescriptor(
                id: "SYSLIB1038",
                title: new LocalizableResourceString(nameof(SR.InaccessibleJsonIncludePropertiesNotSupportedTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.InaccessibleJsonIncludePropertiesNotSupportedFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public static DiagnosticDescriptor PolymorphismNotSupported { get; } = new DiagnosticDescriptor(
                id: "SYSLIB1039",
                title: new LocalizableResourceString(nameof(SR.FastPathPolymorphismNotSupportedTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.FastPathPolymorphismNotSupportedMessageFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);
        }
    }
}
