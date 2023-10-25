// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace System.Text.Json.SourceGeneration
{
    public sealed partial class JsonSourceGenerator
    {
        internal static class DiagnosticDescriptors
        {
            // Must be kept in sync with https://github.com/dotnet/runtime/blob/main/docs/project/list-of-diagnostics.md

            public static DiagnosticDescriptor TypeNotSupported { get; } = DiagnosticDescriptorHelper.Create(
                id: "SYSLIB1030",
                title: new LocalizableResourceString(nameof(SR.TypeNotSupportedTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.TypeNotSupportedMessageFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public static DiagnosticDescriptor DuplicateTypeName { get; } = DiagnosticDescriptorHelper.Create(
                id: "SYSLIB1031",
                title: new LocalizableResourceString(nameof(SR.DuplicateTypeNameTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.DuplicateTypeNameMessageFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public static DiagnosticDescriptor ContextClassesMustBePartial { get; } = DiagnosticDescriptorHelper.Create(
                id: "SYSLIB1032",
                title: new LocalizableResourceString(nameof(SR.ContextClassesMustBePartialTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.ContextClassesMustBePartialMessageFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public static DiagnosticDescriptor MultipleJsonConstructorAttribute { get; } = DiagnosticDescriptorHelper.Create(
                id: "SYSLIB1033",
                title: new LocalizableResourceString(nameof(SR.MultipleJsonConstructorAttributeTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.MultipleJsonConstructorAttributeFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            public static DiagnosticDescriptor JsonStringEnumConverterNotSupportedInAot { get; } = DiagnosticDescriptorHelper.Create(
                id: "SYSLIB1034",
                title: new LocalizableResourceString(nameof(SR.JsonStringEnumConverterNotSupportedTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.JsonStringEnumConverterNotSupportedMessageFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public static DiagnosticDescriptor MultipleJsonExtensionDataAttribute { get; } = DiagnosticDescriptorHelper.Create(
                id: "SYSLIB1035",
                title: new LocalizableResourceString(nameof(SR.MultipleJsonExtensionDataAttributeTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.MultipleJsonExtensionDataAttributeFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            public static DiagnosticDescriptor DataExtensionPropertyInvalid { get; } = DiagnosticDescriptorHelper.Create(
                id: "SYSLIB1036",
                title: new LocalizableResourceString(nameof(SR.DataExtensionPropertyInvalidTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.DataExtensionPropertyInvalidFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            public static DiagnosticDescriptor InaccessibleJsonIncludePropertiesNotSupported { get; } = DiagnosticDescriptorHelper.Create(
                id: "SYSLIB1038",
                title: new LocalizableResourceString(nameof(SR.InaccessibleJsonIncludePropertiesNotSupportedTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.InaccessibleJsonIncludePropertiesNotSupportedFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public static DiagnosticDescriptor PolymorphismNotSupported { get; } = DiagnosticDescriptorHelper.Create(
                id: "SYSLIB1039",
                title: new LocalizableResourceString(nameof(SR.FastPathPolymorphismNotSupportedTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.FastPathPolymorphismNotSupportedMessageFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public static DiagnosticDescriptor JsonConverterAttributeInvalidType { get; } = DiagnosticDescriptorHelper.Create(
                id: "SYSLIB1220",
                title: new LocalizableResourceString(nameof(SR.JsonConverterAttributeInvalidTypeTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.JsonConverterAttributeInvalidTypeMessageFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public static DiagnosticDescriptor JsonUnsupportedLanguageVersion { get; } = DiagnosticDescriptorHelper.Create(
                id: "SYSLIB1221",
                title: new LocalizableResourceString(nameof(SR.JsonUnsupportedLanguageVersionTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.JsonUnsupportedLanguageVersionMessageFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            public static DiagnosticDescriptor JsonConstructorInaccessible { get; } = DiagnosticDescriptorHelper.Create(
                id: "SYSLIB1222",
                title: new LocalizableResourceString(nameof(SR.JsonConstructorInaccessibleTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.JsonConstructorInaccessibleMessageFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public static DiagnosticDescriptor DerivedJsonConverterAttributesNotSupported { get; } = DiagnosticDescriptorHelper.Create(
                id: "SYSLIB1223",
                title: new LocalizableResourceString(nameof(SR.DerivedJsonConverterAttributesNotSupportedTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.DerivedJsonConverterAttributesNotSupportedMessageFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public static DiagnosticDescriptor JsonSerializableAttributeOnNonContextType { get; } = DiagnosticDescriptorHelper.Create(
                id: "SYSLIB1224",
                title: new LocalizableResourceString(nameof(SR.JsonSerializableAttributeOnNonContextTypeTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.JsonSerializableAttributeOnNonContextTypeMessageFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);
        }
    }
}
