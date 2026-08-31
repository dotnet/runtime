// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingGenerator
    {
        internal sealed partial class Parser
        {
            private static class DiagnosticDescriptors
            {
                public static DiagnosticDescriptor TypeNotSupported { get; } = CreateTypeNotSupportedDescriptor(nameof(SR.TypeNotSupported));
                public static DiagnosticDescriptor MissingPublicInstanceConstructor { get; } = CreateTypeNotSupportedDescriptor(nameof(SR.MissingPublicInstanceConstructor));
                public static DiagnosticDescriptor CollectionNotSupported { get; } = CreateTypeNotSupportedDescriptor(nameof(SR.CollectionNotSupported));
                public static DiagnosticDescriptor DictionaryKeyNotSupported { get; } = CreateTypeNotSupportedDescriptor(nameof(SR.DictionaryKeyNotSupported));
                public static DiagnosticDescriptor ElementTypeNotSupported { get; } = CreateTypeNotSupportedDescriptor(nameof(SR.ElementTypeNotSupported));
                public static DiagnosticDescriptor MultipleParameterizedConstructors { get; } = CreateTypeNotSupportedDescriptor(nameof(SR.MultipleParameterizedConstructors));
                public static DiagnosticDescriptor MultiDimArraysNotSupported { get; } = CreateTypeNotSupportedDescriptor(nameof(SR.MultiDimArraysNotSupported));
                public static DiagnosticDescriptor NullableUnderlyingTypeNotSupported { get; } = CreateTypeNotSupportedDescriptor(nameof(SR.NullableUnderlyingTypeNotSupported));

                public static DiagnosticDescriptor PropertyNotSupported { get; } = DiagnosticDescriptorHelper.Create(
                    id: "SYSLIB1101",
                    title: new LocalizableResourceString(nameof(SR.PropertyNotSupportedTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
                    messageFormat: new LocalizableResourceString(nameof(SR.PropertyNotSupportedMessageFormat), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
                    category: ProjectName,
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true);

                public static DiagnosticDescriptor LanguageVersionNotSupported { get; } = DiagnosticDescriptorHelper.Create(
                    id: "SYSLIB1102",
                    title: new LocalizableResourceString(nameof(SR.LanguageVersionIsNotSupportedTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
                    messageFormat: new LocalizableResourceString(nameof(SR.LanguageVersionIsNotSupportedMessageFormat), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
                    category: ProjectName,
                    defaultSeverity: DiagnosticSeverity.Error,
                    isEnabledByDefault: true);

                public static DiagnosticDescriptor ValueTypesInvalidForBind { get; } = DiagnosticDescriptorHelper.Create(
                    id: "SYSLIB1103",
                    title: new LocalizableResourceString(nameof(SR.ValueTypesInvalidForBindTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
                    messageFormat: new LocalizableResourceString(nameof(SR.ValueTypesInvalidForBindMessageFormat), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
                    category: ProjectName,
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true);

                public static DiagnosticDescriptor CouldNotDetermineTypeInfo { get; } = DiagnosticDescriptorHelper.Create(
                    id: "SYSLIB1104",
                    title: new LocalizableResourceString(nameof(SR.CouldNotDetermineTypeInfoTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
                    messageFormat: new LocalizableResourceString(nameof(SR.CouldNotDetermineTypeInfoMessageFormat), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
                    category: ProjectName,
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true);

                private static DiagnosticDescriptor CreateTypeNotSupportedDescriptor(string nameofLocalizableMessageFormat) =>
                    DiagnosticDescriptorHelper.Create(
                    id: "SYSLIB1100",
                    title: new LocalizableResourceString(nameof(SR.TypeNotSupportedTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
                    messageFormat: new LocalizableResourceString(nameofLocalizableMessageFormat, SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
                    category: ProjectName,
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true);

                public static DiagnosticDescriptor GetNotSupportedDescriptor(NotSupportedReason reason) =>
                    reason switch
                    {
                        NotSupportedReason.UnknownType => TypeNotSupported,
                        NotSupportedReason.MissingPublicInstanceConstructor => MissingPublicInstanceConstructor,
                        NotSupportedReason.CollectionNotSupported => CollectionNotSupported,
                        NotSupportedReason.DictionaryKeyNotSupported => DictionaryKeyNotSupported,
                        NotSupportedReason.ElementTypeNotSupported => ElementTypeNotSupported,
                        NotSupportedReason.MultipleParameterizedConstructors => MultipleParameterizedConstructors,
                        NotSupportedReason.MultiDimArraysNotSupported => MultiDimArraysNotSupported,
                        NotSupportedReason.NullableUnderlyingTypeNotSupported => NullableUnderlyingTypeNotSupported,
                        _ => throw new InvalidOperationException()
                    };
            }
        }
    }
}
