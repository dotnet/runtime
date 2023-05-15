// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingSourceGenerator
    {
        internal sealed class Helpers
        {
            public static DiagnosticDescriptor TypeNotSupported { get; } = CreateTypeNotSupportedDescriptor(nameof(SR.TypeNotSupported));
            public static DiagnosticDescriptor AbstractOrInterfaceNotSupported { get; } = CreateTypeNotSupportedDescriptor(nameof(SR.AbstractOrInterfaceNotSupported));
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
                category: GeneratorProjectName,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public static DiagnosticDescriptor LanguageVersionNotSupported { get; } = new DiagnosticDescriptor(
                id: "SYSLIB1102",
                title: new LocalizableResourceString(nameof(SR.LanguageVersionIsNotSupportedTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.Language_VersionIsNotSupportedMessageFormat), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
                category: GeneratorProjectName,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            public static DiagnosticDescriptor CreateTypeNotSupportedDescriptor(string nameofLocalizableMessageFormat) =>
                new DiagnosticDescriptor(
                id: "SYSLIB1100",
                title: new LocalizableResourceString(nameof(SR.TypeNotSupportedTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameofLocalizableMessageFormat, SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
                category: GeneratorProjectName,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public static class TypeFullName
            {
                public const string ConfigurationKeyNameAttribute = "Microsoft.Extensions.Configuration.ConfigurationKeyNameAttribute";
                public const string CultureInfo = "System.Globalization.CultureInfo";
                public const string DateOnly = "System.DateOnly";
                public const string DateTimeOffset = "System.DateTimeOffset";
                public const string Dictionary = "System.Collections.Generic.Dictionary`2";
                public const string GenericIDictionary = "System.Collections.Generic.IDictionary`2";
                public const string Guid = "System.Guid";
                public const string Half = "System.Half";
                public const string HashSet = "System.Collections.Generic.HashSet`1";
                public const string IConfiguration = "Microsoft.Extensions.Configuration.IConfiguration";
                public const string IConfigurationSection = "Microsoft.Extensions.Configuration.IConfigurationSection";
                public const string IDictionary = "System.Collections.Generic.IDictionary";
                public const string Int128 = "System.Int128";
                public const string ISet = "System.Collections.Generic.ISet`1";
                public const string IServiceCollection = "Microsoft.Extensions.DependencyInjection.IServiceCollection";
                public const string List = "System.Collections.Generic.List`1";
                public const string TimeOnly = "System.TimeOnly";
                public const string TimeSpan = "System.TimeSpan";
                public const string UInt128 = "System.UInt128";
                public const string Uri = "System.Uri";
                public const string Version = "System.Version";
            }

            public static bool TypesAreEqual(ITypeSymbol first, ITypeSymbol? second)
                    => first.Equals(second, SymbolEqualityComparer.Default);
        }
    }
}
