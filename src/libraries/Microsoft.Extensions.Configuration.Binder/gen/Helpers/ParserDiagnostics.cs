// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal static class ParserDiagnostics
    {
        public static DiagnosticDescriptor TypeNotSupported { get; } = CreateTypeNotSupportedDescriptor(nameof(SR.TypeNotSupported));
        public static DiagnosticDescriptor NeedPublicParameterlessConstructor { get; } = CreateTypeNotSupportedDescriptor(nameof(SR.NeedPublicParameterlessConstructor));
        public static DiagnosticDescriptor CollectionNotSupported { get; } = CreateTypeNotSupportedDescriptor(nameof(SR.CollectionNotSupported));
        public static DiagnosticDescriptor DictionaryKeyNotSupported { get; } = CreateTypeNotSupportedDescriptor(nameof(SR.DictionaryKeyNotSupported));
        public static DiagnosticDescriptor ElementTypeNotSupported { get; } = CreateTypeNotSupportedDescriptor(nameof(SR.ElementTypeNotSupported));
        public static DiagnosticDescriptor MultiDimArraysNotSupported { get; } = CreateTypeNotSupportedDescriptor(nameof(SR.MultiDimArraysNotSupported));
        public static DiagnosticDescriptor NullableUnderlyingTypeNotSupported { get; } = CreateTypeNotSupportedDescriptor(nameof(SR.NullableUnderlyingTypeNotSupported));

        public static DiagnosticDescriptor PropertyNotSupported { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1101",
            title: new LocalizableResourceString(nameof(SR.PropertyNotSupportedTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.PropertyNotSupportedMessageFormat), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
            category: ConfigurationBindingGenerator.ProjectName,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LanguageVersionNotSupported { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1102",
            title: new LocalizableResourceString(nameof(SR.LanguageVersionIsNotSupportedTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.Language_VersionIsNotSupportedMessageFormat), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
            category: ConfigurationBindingGenerator.ProjectName,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static DiagnosticDescriptor CreateTypeNotSupportedDescriptor(string nameofLocalizableMessageFormat) =>
            new DiagnosticDescriptor(
            id: "SYSLIB1100",
            title: new LocalizableResourceString(nameof(SR.TypeNotSupportedTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
            messageFormat: new LocalizableResourceString(nameofLocalizableMessageFormat, SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
            category: ConfigurationBindingGenerator.ProjectName,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
    }
}
